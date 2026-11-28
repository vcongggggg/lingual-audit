using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using Lingual.Modules.AITutor.Contracts;
using Lingual.Modules.AITutor.Contracts.DTOs;
using Lingual.Modules.AITutor.Controllers;
using Lingual.Modules.AITutor.Infrastructure.Data;
using Lingual.Modules.AITutor.Options;
using Lingual.Modules.AITutor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Lingual.Modules.MezonBot.Tests;

public class AiQuotaServiceTests
{
    private sealed class AdjustableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public AdjustableTimeProvider(DateTimeOffset initialUtc)
        {
            _utcNow = initialUtc;
        }

        public void SetUtcNow(DateTimeOffset newUtc) => _utcNow = newUtc;

        public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }

    private static (AiQuotaService Service, AITutorDbContext Db, AdjustableTimeProvider Clock, IMemoryCache Cache) CreateService(
        DateTimeOffset? startUtc = null,
        int dailyFree = 5,
        int dailyBonusCap = 5,
        int guestDaily = 2)
    {
        var dbName = $"ai_quota_test_{Guid.NewGuid():N}";
        var dbOptions = new DbContextOptionsBuilder<AITutorDbContext>()
            .UseInMemoryDatabase(dbName)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;
        var db = new AITutorDbContext(dbOptions);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var clock = new AdjustableTimeProvider(startUtc ?? new DateTimeOffset(2026, 11, 23, 5, 0, 0, TimeSpan.Zero)); // 12:00 GMT+7
        var options = Options.Create(new GeminiOptions
        {
            DailyFreeTurns = dailyFree,
            DailyBonusCap = dailyBonusCap,
            GuestDailyTurns = guestDaily
        });
        var logger = new Mock<ILogger<AiQuotaService>>();
        var svc = new AiQuotaService(db, cache, options, clock, logger.Object);
        return (svc, db, clock, cache);
    }

    [Fact]
    public async Task TryConsumeTurnAsync_SucceedsFor5FreeTurns_ThenRejectsWithQuotaExceededAndNeverNegative()
    {
        var (svc, db, _, _) = CreateService();
        using var _db = db;
        var userId = Guid.NewGuid();

        var initial = await svc.GetQuotaAsync(userId);
        initial.DailyFreeLimit.Should().Be(5);
        initial.DailyRemaining.Should().Be(5);
        initial.BonusCredits.Should().Be(0);
        initial.TotalAvailable.Should().Be(5);

        for (int i = 1; i <= 5; i++)
        {
            var res = await svc.TryConsumeTurnAsync(userId, "consume_roleplay", $"turn_{i}");
            res.Allowed.Should().BeTrue();
            res.ConsumedFromDailyFree.Should().BeTrue();
            res.Quota.DailyRemaining.Should().Be(5 - i);
            res.Quota.TotalAvailable.Should().Be(5 - i);
        }

        // 6th call must be rejected and balance must never go negative
        var sixth = await svc.TryConsumeTurnAsync(userId, "consume_roleplay", "turn_6");
        sixth.Allowed.Should().BeFalse();
        sixth.ErrorCode.Should().Be(AiQuotaService.QuotaExceededCode);
        sixth.Quota.DailyRemaining.Should().Be(0);
        sixth.Quota.BonusCredits.Should().Be(0);
        sixth.Quota.TotalAvailable.Should().Be(0);

        var wallet = await db.AiCreditWallets.AsNoTracking().FirstAsync(w => w.UserId == userId);
        wallet.DailyFreeRemaining.Should().Be(0);
        wallet.Balance.Should().Be(0);
    }

    [Fact]
    public async Task TryConsumeTurnAsync_IdempotencyReplayTwice_DeductsOnlyOnce()
    {
        var (svc, db, _, _) = CreateService();
        using var _db = db;
        var userId = Guid.NewGuid();
        const string replayKey = "replay_key_001";

        var first = await svc.TryConsumeTurnAsync(userId, "consume_grammar", replayKey);
        var second = await svc.TryConsumeTurnAsync(userId, "consume_grammar", replayKey);

        first.Allowed.Should().BeTrue();
        first.AlreadyProcessed.Should().BeFalse();
        first.Quota.DailyRemaining.Should().Be(4);

        second.Allowed.Should().BeTrue();
        second.AlreadyProcessed.Should().BeTrue();
        second.Quota.DailyRemaining.Should().Be(4);

        var txCount = await db.AiCreditTransactions.CountAsync(t => t.IdempotencyKey == replayKey);
        txCount.Should().Be(1);
    }

    [Fact]
    public async Task TryConsumeTurnAsync_ConcurrentRace_20ThreadsOn5Turns_Exactly5SucceedAndBalanceNeverNegative()
    {
        var dbName = $"ai_quota_race_{Guid.NewGuid():N}";
        var dbOptions = new DbContextOptionsBuilder<AITutorDbContext>()
            .UseInMemoryDatabase(dbName)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;
        var cache = new MemoryCache(new MemoryCacheOptions());
        var clock = new AdjustableTimeProvider(new DateTimeOffset(2026, 11, 23, 5, 0, 0, TimeSpan.Zero));
        var options = Options.Create(new GeminiOptions { DailyFreeTurns = 5, DailyBonusCap = 5, GuestDailyTurns = 2 });
        var logger = new Mock<ILogger<AiQuotaService>>();
        var userId = Guid.NewGuid();

        // Each concurrent request gets its own scoped AITutorDbContext targeting the same database (mirrors ASP.NET Core DI)
        var tasks = Enumerable.Range(1, 20)
            .Select(i => Task.Run(async () =>
            {
                await using var scopedDb = new AITutorDbContext(dbOptions);
                var scopedSvc = new AiQuotaService(scopedDb, cache, options, clock, logger.Object);
                return await scopedSvc.TryConsumeTurnAsync(userId, "consume_roleplay", $"race_{userId:N}_{i}");
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        results.Count(r => r.Allowed).Should().Be(5);
        results.Count(r => !r.Allowed && r.ErrorCode == AiQuotaService.QuotaExceededCode).Should().Be(15);

        await using var verifyDb = new AITutorDbContext(dbOptions);
        var verifySvc = new AiQuotaService(verifyDb, cache, options, clock, logger.Object);
        var finalStatus = await verifySvc.GetQuotaAsync(userId);
        finalStatus.DailyRemaining.Should().Be(0);
        finalStatus.BonusCredits.Should().Be(0);
        finalStatus.TotalAvailable.Should().Be(0);

        var wallet = await verifyDb.AiCreditWallets.AsNoTracking().FirstAsync(w => w.UserId == userId);
        wallet.DailyFreeRemaining.Should().BeGreaterThanOrEqualTo(0);
        wallet.Balance.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ResetBoundary_CrossingMidnightGmtPlus7_ResetsDailyFreeAndPreservesBonusCredits()
    {
        // 2026-11-23 16:59:50 UTC == 2026-11-23 23:59:50 GMT+7
        var beforeMidnightUtc = new DateTimeOffset(2026, 11, 23, 16, 59, 50, TimeSpan.Zero);
        var (svc, db, clock, _) = CreateService(startUtc: beforeMidnightUtc);
        using var _db = db;
        var userId = Guid.NewGuid();

        // Earn +2 bonus credits and exhaust all 5 free turns
        await svc.GrantBonusCreditsAsync(userId, 1, "reward_game", "g_before_1");
        await svc.GrantBonusCreditsAsync(userId, 1, "reward_quiz", "q_before_2");
        for (int i = 0; i < 5; i++)
        {
            var r = await svc.TryConsumeTurnAsync(userId, "consume_roleplay", $"before_{i}");
            r.Allowed.Should().BeTrue();
            r.ConsumedFromDailyFree.Should().BeTrue();
        }

        var beforeReset = await svc.GetQuotaAsync(userId);
        beforeReset.DailyRemaining.Should().Be(0);
        beforeReset.BonusCredits.Should().Be(2);
        beforeReset.DailyBonusEarned.Should().Be(2);
        beforeReset.TotalAvailable.Should().Be(2);

        // Advance 20 seconds -> 2026-11-23 17:00:10 UTC == 2026-11-24 00:00:10 GMT+7
        clock.Advance(TimeSpan.FromSeconds(20));

        var afterReset = await svc.GetQuotaAsync(userId);
        afterReset.DailyRemaining.Should().Be(5);
        afterReset.BonusCredits.Should().Be(2);
        afterReset.DailyBonusEarned.Should().Be(0);
        afterReset.TotalAvailable.Should().Be(7);

        // Next consume uses fresh daily free turns first, leaving bonus untouched
        var nextTurn = await svc.TryConsumeTurnAsync(userId, "consume_roleplay", "after_midnight_1");
        nextTurn.Allowed.Should().BeTrue();
        nextTurn.ConsumedFromDailyFree.Should().BeTrue();
        nextTurn.Quota.DailyRemaining.Should().Be(4);
        nextTurn.Quota.BonusCredits.Should().Be(2);
    }

    [Fact]
    public async Task GrantBonusCreditsAsync_EnforcesDailyCapOf5AndIdempotency()
    {
        var (svc, db, clock, _) = CreateService();
        using var _db = db;
        var userId = Guid.NewGuid();

        // Grant +1 five times -> succeeds
        for (int i = 1; i <= 5; i++)
        {
            var g = await svc.GrantBonusCreditsAsync(userId, 1, "reward_game", $"cap_key_{i}");
            g.Granted.Should().BeTrue();
            g.CreditsAdded.Should().Be(1);
            g.Quota.BonusCredits.Should().Be(i);
            g.Quota.DailyBonusEarned.Should().Be(i);
        }

        // Duplicate replay of an existing key -> AlreadyGranted, no extra credit
        var duplicate = await svc.GrantBonusCreditsAsync(userId, 1, "reward_game", "cap_key_1");
        duplicate.Granted.Should().BeFalse();
        duplicate.AlreadyGranted.Should().BeTrue();
        duplicate.CreditsAdded.Should().Be(0);

        // 6th distinct grant on same GMT+7 day -> blocked by +5/day cap
        var sixth = await svc.GrantBonusCreditsAsync(userId, 1, "reward_quiz", "cap_key_6");
        sixth.Granted.Should().BeFalse();
        sixth.CreditsAdded.Should().Be(0);
        sixth.DailyCapReached.Should().BeTrue();
        sixth.Quota.BonusCredits.Should().Be(5);

        // Next day GMT+7 -> daily cap resets so new reward works
        clock.Advance(TimeSpan.FromHours(14));
        var nextDayGrant = await svc.GrantBonusCreditsAsync(userId, 1, "reward_flashcard", "cap_key_next_day");
        nextDayGrant.Granted.Should().BeTrue();
        nextDayGrant.CreditsAdded.Should().Be(1);
        nextDayGrant.Quota.BonusCredits.Should().Be(6);
        nextDayGrant.Quota.DailyBonusEarned.Should().Be(1);
    }

    [Fact]
    public async Task GuestQuota_Allows2TurnsPerIpPerDay_Blocks3rd_AndResetsAtMidnightGmtPlus7()
    {
        var beforeMidnightUtc = new DateTimeOffset(2026, 11, 23, 16, 59, 50, TimeSpan.Zero);
        var (svc, db, clock, _) = CreateService(startUtc: beforeMidnightUtc);
        using var _db = db;

        var g1 = await svc.TryConsumeGuestTurnAsync("203.0.113.10", "consume_guest_roleplay");
        var g2 = await svc.TryConsumeGuestTurnAsync("203.0.113.10", "consume_guest_grammar");
        var g3 = await svc.TryConsumeGuestTurnAsync("203.0.113.10", "consume_guest_roleplay");

        g1.Allowed.Should().BeTrue();
        g1.Quota.DailyRemaining.Should().Be(1);
        g1.Quota.IsGuest.Should().BeTrue();

        g2.Allowed.Should().BeTrue();
        g2.Quota.DailyRemaining.Should().Be(0);

        g3.Allowed.Should().BeFalse();
        g3.ErrorCode.Should().Be(AiQuotaService.QuotaExceededCode);
        g3.Quota.IsGuest.Should().BeTrue();
        g3.Quota.TotalAvailable.Should().Be(0);

        // Another IP still has its own 2 turns
        var otherIp = await svc.TryConsumeGuestTurnAsync("203.0.113.20", "consume_guest_roleplay");
        otherIp.Allowed.Should().BeTrue();
        otherIp.Quota.DailyRemaining.Should().Be(1);

        // Crossing 00:00 GMT+7 resets guest IP counter to 2 turns
        clock.Advance(TimeSpan.FromSeconds(20));
        var resetGuest = await svc.GetGuestQuotaAsync("203.0.113.10");
        resetGuest.DailyRemaining.Should().Be(2);
        resetGuest.TotalAvailable.Should().Be(2);
    }

    [Fact]
    public async Task RefundTurnAsync_RestoresConsumedTurnAndIsIdempotent()
    {
        var (svc, db, _, _) = CreateService();
        using var _db = db;
        var userId = Guid.NewGuid();

        var consume = await svc.TryConsumeTurnAsync(userId, "consume_roleplay", "fail_turn_1");
        consume.Quota.DailyRemaining.Should().Be(4);

        var refunded1 = await svc.RefundTurnAsync(userId, consume.ConsumedFromDailyFree, consume.IdempotencyKey);
        refunded1.DailyRemaining.Should().Be(5);

        // Replaying refund with same original key must NOT over-credit
        var refunded2 = await svc.RefundTurnAsync(userId, consume.ConsumedFromDailyFree, consume.IdempotencyKey);
        refunded2.DailyRemaining.Should().Be(5);
    }

    [Fact]
    public void AITutorController_EnforcesAuthorizeOnMainEndpoints_AndOnlyAllowsAnonymousOnGuestRoutes()
    {
        var controllerType = typeof(AITutorController);
        controllerType.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();

        foreach (var methodName in new[] { nameof(AITutorController.GetQuota), nameof(AITutorController.CheckGrammar), nameof(AITutorController.GetScenarios), nameof(AITutorController.ProcessRoleplay) })
        {
            var method = controllerType.GetMethod(methodName);
            method.Should().NotBeNull();
            method!.GetCustomAttribute<AllowAnonymousAttribute>().Should().BeNull($"{methodName} must require authorization");
        }
    }

    [Fact]
    public async Task AITutorController_ProcessRoleplay_IgnoresClientUserId_RefundsOnProviderFailure_AndReturns429WhenExhausted()
    {
        var (quotaSvc, db, _, _) = CreateService();
        using var _db = db;
        var jwtUserId = Guid.NewGuid();

        var tutorMock = new Mock<IAITutorService>();
        string? capturedMezonUserId = null;
        tutorMock
            .Setup(s => s.ProcessRoleplayTurnAsync(It.IsAny<RoleplayTurnRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RoleplayTurnRequest, CancellationToken>((req, _) => capturedMezonUserId = req.MezonUserId)
            .ReturnsAsync(new RoleplayTurnResponse(
                Guid.NewGuid(), "cafe_order", "Cafe", "Barista", "Hello!", false, null, null, ProviderFailed: false));

        var controller = new AITutorController(tutorMock.Object, quotaSvc, new Mock<ILogger<AITutorController>>().Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, jwtUserId.ToString()) },
                    "TestJwt"))
            }
        };

        // Call 1 with spoofed client MezonUserId -> server must overwrite with JWT userId
        var firstRes = await controller.ProcessRoleplay(
            new RoleplayTurnRequest("cafe_order", "Hi", MezonUserId: "spoofed_attacker_id"),
            CancellationToken.None);
        firstRes.Result.Should().BeOfType<OkObjectResult>();
        capturedMezonUserId.Should().Be(jwtUserId.ToString());

        // Provider failure -> must refund turn and return 503
        tutorMock
            .Setup(s => s.ProcessRoleplayTurnAsync(It.IsAny<RoleplayTurnRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleplayTurnResponse(
                Guid.NewGuid(), "cafe_order", "Cafe", "Barista", "Fallback", false, null, null, ProviderFailed: true));
        var failRes = await controller.ProcessRoleplay(new RoleplayTurnRequest("cafe_order", "Trigger fail"), CancellationToken.None);
        var failObj = failRes.Result.Should().BeOfType<ObjectResult>().Subject;
        failObj.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);

        // Quota must still be 4 (refunded)
        var afterFailQuota = await quotaSvc.GetQuotaAsync(jwtUserId);
        afterFailQuota.DailyRemaining.Should().Be(4);

        // Exhaust remaining 4 turns cleanly
        tutorMock
            .Setup(s => s.ProcessRoleplayTurnAsync(It.IsAny<RoleplayTurnRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleplayTurnResponse(
                Guid.NewGuid(), "cafe_order", "Cafe", "Barista", "Great!", false, null, null, ProviderFailed: false));
        for (int i = 0; i < 4; i++)
        {
            var ok = await controller.ProcessRoleplay(new RoleplayTurnRequest("cafe_order", $"Turn {i}"), CancellationToken.None);
            ok.Result.Should().BeOfType<OkObjectResult>();
        }

        // 6th successful attempt -> HTTP 429 AI_QUOTA_EXCEEDED without invoking Gemini again
        tutorMock.Invocations.Clear();
        var exceededRes = await controller.ProcessRoleplay(new RoleplayTurnRequest("cafe_order", "Blocked turn"), CancellationToken.None);
        var exceededObj = exceededRes.Result.Should().BeOfType<ObjectResult>().Subject;
        exceededObj.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        tutorMock.Verify(s => s.ProcessRoleplayTurnAsync(It.IsAny<RoleplayTurnRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
