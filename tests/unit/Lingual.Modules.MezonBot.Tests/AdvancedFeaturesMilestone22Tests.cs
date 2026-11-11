using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Domain;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Lingual.Modules.Gamification.Services;
using Lingual.Modules.Learning.Contracts;
using Lingual.Modules.Learning.Contracts.DTOs;
using Lingual.Modules.Learning.Domain;
using Lingual.Modules.Learning.Infrastructure.Data;
using Lingual.Modules.Learning.Services;
using Lingual.Modules.MezonBot.Services;
using Mezon.Net.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lingual.Modules.MezonBot.Tests;

public class AdvancedFeaturesMilestone22Tests
{
    private static LearningDbContext CreateLearningDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<LearningDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new LearningDbContext(options);
    }

    private static GamificationDbContext CreateGamificationDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GamificationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GamificationDbContext(options);
    }

    #region ADV-03: One-Click Reaction Bookmark Tests

    [Theory]
    [InlineData("🔖", true)]
    [InlineData("⭐", true)]
    [InlineData("📌", true)]
    [InlineData("🏷️", true)]
    [InlineData(":bookmark:", true)]
    [InlineData(":star:", true)]
    [InlineData(":pushpin:", true)]
    [InlineData("👍", false)]
    [InlineData("❤️", false)]
    [InlineData("😂", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsBookmarkEmoji_IdentifiesExpectedBookmarkEmojis(string? emoji, bool expected)
    {
        var result = MezonBotWorker.IsBookmarkEmoji(emoji);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("**Meticulous**: Tỉ mỉ, cẩn thận từng chi tiết", "Meticulous", "Tỉ mỉ, cẩn thận từng chi tiết")]
    [InlineData("**Resilient** - Có khả năng phục hồi nhanh", "Resilient", "Có khả năng phục hồi nhanh")]
    [InlineData("Perseverance: Tính kiên trì, bền bỉ", "Perseverance", "Tính kiên trì, bền bỉ")]
    [InlineData("Ambiguity - Sự mơ hồ, không rõ ràng", "Ambiguity", "Sự mơ hồ, không rõ ràng")]
    [InlineData("Ephemeral", "Ephemeral", "Ephemeral")]
    public void VocabularyExtractor_ExtractTermAndMeaning_ParsesCorrectly(string input, string expectedTerm, string expectedMeaning)
    {
        var (term, meaning) = VocabularyExtractor.ExtractTermAndMeaning(input);
        term.Should().Be(expectedTerm);
        meaning.Should().Be(expectedMeaning);
    }

    [Fact]
    public void ReactionBookmarkRateLimiter_AllowsMax10RequestsPerMinute_ThenBlocks()
    {
        var limiter = new ReactionBookmarkRateLimiter(maxRequestsPerMinute: 10);
        var userId = "user_test_rate_limit_001";

        // 10 lần đầu trong vòng 1 phút phải thành công
        for (int i = 0; i < 10; i++)
        {
            limiter.TryAcquire(userId).Should().BeTrue($"Lần thứ {i + 1} phải được phép");
        }

        // Lần thứ 11 trong cùng phút phải bị chặn
        limiter.TryAcquire(userId).Should().BeFalse("Lần thứ 11 phải bị từ chối do vượt rate limit 10/phút");

        // Người dùng khác vẫn thao tác bình thường (rate limit per-user)
        limiter.TryAcquire("another_user_002").Should().BeTrue("User khác không bị ảnh hưởng");
    }

    [Fact]
    public async Task FlashcardService_CreateOrBookmarkFlashcardAsync_InitializesSM2ParametersCorrectly()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateLearningDbContext(dbName);
        var loggerMock = new Mock<ILogger<FlashcardService>>();
        var flashcardService = new FlashcardService(db, loggerMock.Object);

        var userId = Guid.NewGuid();
        var term = "Meticulous";
        var meaning = "Tỉ mỉ, cẩn thận";

        // Thực hiện bookmark
        var cardDto = await flashcardService.CreateOrBookmarkFlashcardAsync(userId, term, meaning);

        // Kiểm tra DTO trả về
        cardDto.Should().NotBeNull();
        cardDto.Term.Should().Be("Meticulous");
        cardDto.Meaning.Should().Be("Tỉ mỉ, cẩn thận");
        cardDto.Repetitions.Should().Be(0, "SM-2 khởi tạo repetitions = 0");
        cardDto.IntervalDays.Should().Be(1.0m, "SM-2 khởi tạo interval = 1 ngày");
        cardDto.EaseFactor.Should().Be(2.50m, "SM-2 khởi tạo ease factor = 2.50");
        cardDto.NextDueAt.Should().BeAfter(DateTime.UtcNow.AddHours(23));

        // Kiểm tra lưu trữ thật trong CSDL
        var cardInDb = await db.SrsCards.FirstOrDefaultAsync(c => c.UserId == userId);
        cardInDb.Should().NotBeNull();
        cardInDb!.Repetitions.Should().Be(0);
        cardInDb.IntervalDays.Should().Be(1.0m);
        cardInDb.EaseFactor.Should().Be(2.50m);

        var vocabInDb = await db.VocabularyItems.FirstOrDefaultAsync(v => v.NormalizedTerm == "meticulous");
        vocabInDb.Should().NotBeNull();
        vocabInDb!.Meaning.Should().Be("Tỉ mỉ, cẩn thận");
    }

    [Fact]
    public async Task FlashcardService_CreateOrBookmarkFlashcardAsync_DoesNotDuplicateCardIfAlreadyExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateLearningDbContext(dbName);
        var loggerMock = new Mock<ILogger<FlashcardService>>();
        var flashcardService = new FlashcardService(db, loggerMock.Object);

        var userId = Guid.NewGuid();
        var term = "Ubiquitous";
        var meaning = "Có mặt khắp nơi";

        // Lần 1
        await flashcardService.CreateOrBookmarkFlashcardAsync(userId, term, meaning);
        // Lần 2 (bookmark lại cùng từ)
        var cardDto2 = await flashcardService.CreateOrBookmarkFlashcardAsync(userId, term, meaning);

        cardDto2.Should().NotBeNull();
        var cardCount = await db.SrsCards.CountAsync(c => c.UserId == userId);
        cardCount.Should().Be(1, "Không được tạo trùng lặp thẻ SRS cho cùng một từ vựng của user");
    }

    #endregion

    #region ADV-05: SRS Reminder & Real Streak Freeze Tests

    [Fact]
    public async Task StreakService_UseStreakFreezeAsync_ConsumesOneFreezeAndPreservesStreakState()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateGamificationDbContext(dbName);
        var loggerMock = new Mock<ILogger<StreakService>>();
        var streakService = new StreakService(db, loggerMock.Object);

        var userId = Guid.NewGuid();
        var initialStreak = new UserStreak
        {
            UserId = userId,
            CurrentDays = 7,
            BestDays = 10,
            FreezeBalance = 2,
            LastActiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(-1)), // Ngày hôm qua
            DailyActivity = "{}",
            FreezeHistory = "[]",
            UpdatedAt = DateTime.UtcNow
        };
        db.UserStreaks.Add(initialStreak);
        await db.SaveChangesAsync();

        // Kích hoạt khiên đóng băng thủ công
        var result = await streakService.UseStreakFreezeAsync(userId);

        // Kiểm tra kết quả trả về
        result.IsSuccess.Should().BeTrue();
        result.RemainingFreezes.Should().Be(1, "Số khiên phải giảm từ 2 xuống 1");
        result.CurrentDays.Should().Be(7, "Chuỗi 7 ngày phải được bảo toàn");

        // Kiểm tra state lưu trữ thật trong CSDL
        var streakInDb = await db.UserStreaks.FirstOrDefaultAsync(s => s.UserId == userId);
        streakInDb.Should().NotBeNull();
        streakInDb!.FreezeBalance.Should().Be(1);
        streakInDb.CurrentDays.Should().Be(7);
        streakInDb.FreezeHistory.Should().Contain("manual_freeze_activated");

        // Gọi lại lần 2 trong cùng ngày -> phải từ chối vì đã đóng băng hôm nay rồi
        var result2 = await streakService.UseStreakFreezeAsync(userId);
        result2.IsSuccess.Should().BeFalse();
        result2.RemainingFreezes.Should().Be(1, "Không bị trừ thêm khiên lần 2 trong ngày");
        result2.Message.Should().Contain("hôm nay");
    }

    [Fact]
    public async Task StreakService_UseStreakFreezeAsync_FailsWhenFreezeBalanceIsZero()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateGamificationDbContext(dbName);
        var loggerMock = new Mock<ILogger<StreakService>>();
        var streakService = new StreakService(db, loggerMock.Object);

        var userId = Guid.NewGuid();
        var streak = new UserStreak
        {
            UserId = userId,
            CurrentDays = 3,
            BestDays = 3,
            FreezeBalance = 0, // Đã hết khiên
            LastActiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(-1)),
            DailyActivity = "{}",
            FreezeHistory = "[]",
            UpdatedAt = DateTime.UtcNow
        };
        db.UserStreaks.Add(streak);
        await db.SaveChangesAsync();

        var result = await streakService.UseStreakFreezeAsync(userId);

        result.IsSuccess.Should().BeFalse();
        result.RemainingFreezes.Should().Be(0);
        result.Message.Should().Contain("không còn");
    }

    [Fact]
    public async Task SrsReminderService_ScanAndSendRemindersAsync_IdentifiesOnlyUsersWithDueCardsAndAtRiskStreaks()
    {
        var flashcardServiceMock = new Mock<IFlashcardService>();
        var gamificationServiceMock = new Mock<IGamificationService>();
        var notifierMock = new Mock<IMezonBotNotifier>();
        var loggerMock = new Mock<ILogger<SrsReminderService>>();

        var userAtRisk = Guid.NewGuid();
        var userAlreadyActive = Guid.NewGuid();

        // 2 user có thẻ đến hạn
        flashcardServiceMock.Setup(f => f.GetUsersWithDueCardsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { userAtRisk, userAlreadyActive });

        // User 1 chưa học hôm nay (nguy cơ đứt chuỗi)
        gamificationServiceMock.Setup(g => g.GetUserStreakAsync(userAtRisk, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Gamification.Contracts.DTOs.UserStreakDto(
                userAtRisk,
                CurrentDays: 5,
                BestDays: 5,
                LastActiveDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                FreezeBalance: 1,
                IsActiveToday: false,
                TodayXp: 0,
                XpNeededForStreak: 20,
                UpdatedAt: DateTime.UtcNow
            ));

        // User 2 ĐÃ học xong hôm nay (không có nguy cơ đứt chuỗi)
        gamificationServiceMock.Setup(g => g.GetUserStreakAsync(userAlreadyActive, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Gamification.Contracts.DTOs.UserStreakDto(
                userAlreadyActive,
                CurrentDays: 12,
                BestDays: 12,
                LastActiveDate: DateOnly.FromDateTime(DateTime.UtcNow),
                FreezeBalance: 2,
                IsActiveToday: true,
                TodayXp: 50,
                XpNeededForStreak: 0,
                UpdatedAt: DateTime.UtcNow
            ));

        // Mapping kênh cho userAtRisk
        notifierMock.Setup(n => n.GetUserChannelMapping(userAtRisk))
            .Returns((987654321L, 11223344L));

        notifierMock.Setup(n => n.SendSrsReminderAsync(
            userAtRisk,
            987654321L,
            11223344L,
            It.IsAny<int>(),
            5,
            1,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        flashcardServiceMock.Setup(f => f.GetDueCardsAsync(userAtRisk, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SrsCardDto>
            {
                new SrsCardDto(userAtRisk, Guid.NewGuid(), "Tenacious", "Kiên cường", 0, 1.0m, 2.50m, DateTime.UtcNow.AddHours(-1), null)
            });

        var reminderService = new SrsReminderService(
            flashcardServiceMock.Object,
            gamificationServiceMock.Object,
            notifierMock.Object,
            loggerMock.Object);

        // Chạy scan
        var scanResult = await reminderService.ScanAndSendRemindersAsync();

        // Kiểm tra
        scanResult.TotalUsersScanned.Should().Be(2, "Có 2 user có thẻ đến hạn");
        scanResult.EligibleUsersCount.Should().Be(1, "Chỉ có 1 user có nguy cơ đứt streak");
        scanResult.RemindersSentCount.Should().Be(1, "1 thông báo nhắc nhở đã được gửi thành công");

        notifierMock.Verify(n => n.SendSrsReminderAsync(
            userAtRisk,
            987654321L,
            11223344L,
            1,
            5,
            1,
            It.IsAny<CancellationToken>()), Times.Once);

        // Đảm bảo KHÔNG gửi nhắc nhở cho user đã active hôm nay
        notifierMock.Verify(n => n.SendSrsReminderAsync(
            userAlreadyActive,
            It.IsAny<long>(),
            It.IsAny<long>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FlashcardService_GetDueCardsAsync_ReturnsOnlyCardsDueBeforeNow()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateLearningDbContext(dbName);
        var loggerMock = new Mock<ILogger<FlashcardService>>();
        var flashcardService = new FlashcardService(db, loggerMock.Object);

        var userId = Guid.NewGuid();
        var v1 = new VocabularyItem { Id = Guid.NewGuid(), Term = "Eloquent", Meaning = "Hùng biện", NormalizedTerm = "eloquent" };
        var v2 = new VocabularyItem { Id = Guid.NewGuid(), Term = "Pragmatic", Meaning = "Thực dụng", NormalizedTerm = "pragmatic" };
        var v3 = new VocabularyItem { Id = Guid.NewGuid(), Term = "FutureWord", Meaning = "Chưa đến hạn", NormalizedTerm = "futureword" };
        db.VocabularyItems.AddRange(v1, v2, v3);

        // 2 thẻ đến hạn (NextDueAt quá khứ)
        db.SrsCards.Add(new SrsCard { UserId = userId, VocabularyId = v1.Id, NextDueAt = DateTime.UtcNow.AddHours(-2), Repetitions = 1, IntervalDays = 1.0m });
        db.SrsCards.Add(new SrsCard { UserId = userId, VocabularyId = v2.Id, NextDueAt = DateTime.UtcNow.AddMinutes(-30), Repetitions = 2, IntervalDays = 3.0m });
        // 1 thẻ chưa đến hạn (NextDueAt ngày mai)
        db.SrsCards.Add(new SrsCard { UserId = userId, VocabularyId = v3.Id, NextDueAt = DateTime.UtcNow.AddDays(1), Repetitions = 0, IntervalDays = 1.0m });
        await db.SaveChangesAsync();

        var dueCards = await flashcardService.GetDueCardsAsync(userId, 5);

        dueCards.Should().HaveCount(2);
        dueCards.Select(c => c.Term).Should().Contain(new[] { "Eloquent", "Pragmatic" });
        dueCards.Select(c => c.Term).Should().NotContain("FutureWord");
    }

    #endregion
}
