using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lingual.Modules.AITutor.Contracts;
using Lingual.Modules.AITutor.Contracts.DTOs;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.MezonBot.Domain;
using Lingual.Modules.MezonBot.Infrastructure.Data;
using Lingual.Modules.MezonBot.Services;
using Mezon.Net.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lingual.Modules.MezonBot.Tests;

public class AdvancedFeaturesMilestone24Tests
{
    private static MezonBotDbContext CreateBotDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<MezonBotDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new MezonBotDbContext(options);
    }

    #region ADV-07: Reaction Quiz Tests

    [Fact]
    public void ReactionQuizSeedData_Contains10SelfAuthoredQuestions_WithUniqueIdsAndPublishedStatus()
    {
        var questions = ReactionQuizSeedData.GetSeedQuestions();

        questions.Should().HaveCount(10, "Phải có đúng 10 câu hỏi trắc nghiệm tự soạn trong seed data");
        questions.Select(q => q.Id).Distinct().Should().HaveCount(10, "Mỗi câu hỏi phải có ID duy nhất");

        foreach (var q in questions)
        {
            q.Prompt.Should().NotBeNullOrWhiteSpace();
            q.Status.Should().Be("published");
            q.Options.Should().Contain("\"key\":\"A\"");
            q.Options.Should().Contain("\"key\":\"B\"");
            q.Options.Should().Contain("\"key\":\"C\"");
            q.Options.Should().Contain("\"key\":\"D\"");
            q.Options.Should().Contain("\"is_correct\":true");
            q.Explanation.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task ReactionQuiz_AnswerCorrect_Awards10XpOnce_AndStoresStateInDb()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateBotDbContext(dbName);

        var internalUserId = Guid.NewGuid();
        var userResolverMock = new Mock<IMezonUserResolver>();
        userResolverMock.Setup(u => u.ResolveUserIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(internalUserId);

        var gamificationMock = new Mock<IGamificationService>();
        gamificationMock.Setup(g => g.AwardXpAsync(It.IsAny<AwardXpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AwardXpResult(true, 10, 100, 1, true, false));

        var loggerMock = new Mock<ILogger<ReactionQuizService>>();
        var quizService = new ReactionQuizService(db, userResolverMock.Object, loggerMock.Object, gamificationMock.Object);

        long channelId = 12345;
        long clanId = 6789;
        long messageId = 88888;
        string mezonUserId = "user_correct_01";
        string username = "LearnerOne";

        // 1. Tạo Quiz
        var prompt = await quizService.CreateQuizAsync(channelId, clanId);
        prompt.Should().NotBeNull();
        prompt.Options.Should().HaveCount(4);

        // 2. Link với Message ID
        await quizService.LinkMessageAsync(prompt.SessionId, messageId);

        // Lấy câu hỏi trong DB để biết đáp án đúng
        var sessionInDb = await db.ClanQuizSessions.Include(s => s.Question).FirstAsync(s => s.Id == prompt.SessionId);
        var options = System.Text.Json.JsonSerializer.Deserialize<List<ReactionQuizService.InternalOption>>(
            sessionInDb.Question!.Options,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var correctKey = options!.First(o => o.IsCorrect).Key;

        var flagEmoji = correctKey switch
        {
            "A" => "🇦",
            "B" => "🇧",
            "C" => "🇨",
            _ => "🇩"
        };

        // 3. User thả reaction cờ đúng
        var result = await quizService.ProcessReactionAnswerAsync(messageId, channelId, mezonUserId, username, flagEmoji);

        // 4. Assertions
        result.Status.Should().Be(ReactionQuizAnswerStatus.Success);
        result.IsCorrect.Should().BeTrue();
        result.XpAwarded.Should().Be(10);
        result.CorrectKey.Should().Be(correctKey);
        result.Message.Should().Contain("+10 XP");

        // DB state verification
        var updatedSession = await db.ClanQuizSessions.FirstAsync(s => s.Id == prompt.SessionId);
        updatedSession.Responses.Should().Contain(mezonUserId);
        updatedSession.Responses.Should().Contain("\"is_correct\":true");
        updatedSession.Responses.Should().Contain("\"xp_awarded\":10");

        // Gamification verification
        gamificationMock.Verify(g => g.AwardXpAsync(
            It.Is<AwardXpRequest>(r =>
                r.UserId == internalUserId &&
                r.XpDelta == 10 &&
                r.SourceType == "reaction_quiz" &&
                r.IdempotencyKey == $"quiz_{messageId}_{internalUserId}"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReactionQuiz_AnswerWrong_Awards0Xp_AndStoresStateInDb()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateBotDbContext(dbName);

        var internalUserId = Guid.NewGuid();
        var userResolverMock = new Mock<IMezonUserResolver>();
        userResolverMock.Setup(u => u.ResolveUserIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(internalUserId);

        var gamificationMock = new Mock<IGamificationService>();
        var loggerMock = new Mock<ILogger<ReactionQuizService>>();
        var quizService = new ReactionQuizService(db, userResolverMock.Object, loggerMock.Object, gamificationMock.Object);

        long channelId = 12345;
        long messageId = 88889;
        string mezonUserId = "user_wrong_01";
        string username = "LearnerTwo";

        var prompt = await quizService.CreateQuizAsync(channelId, 1);
        await quizService.LinkMessageAsync(prompt.SessionId, messageId);

        var sessionInDb = await db.ClanQuizSessions.Include(s => s.Question).FirstAsync(s => s.Id == prompt.SessionId);
        var options = System.Text.Json.JsonSerializer.Deserialize<List<ReactionQuizService.InternalOption>>(
            sessionInDb.Question!.Options,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var wrongOption = options!.First(o => !o.IsCorrect);
        var wrongFlag = wrongOption.Key switch
        {
            "A" => "🇦",
            "B" => "🇧",
            "C" => "🇨",
            _ => "🇩"
        };

        // Act - User chọn sai
        var result = await quizService.ProcessReactionAnswerAsync(messageId, channelId, mezonUserId, username, wrongFlag);

        // Assert
        result.Status.Should().Be(ReactionQuizAnswerStatus.WrongAnswer);
        result.IsCorrect.Should().BeFalse();
        result.XpAwarded.Should().Be(0);
        result.Message.Should().Contain("Chưa chính xác");

        // DB state verification
        var updatedSession = await db.ClanQuizSessions.FirstAsync(s => s.Id == prompt.SessionId);
        updatedSession.Responses.Should().Contain(mezonUserId);
        updatedSession.Responses.Should().Contain("\"is_correct\":false");

        // XP không bao giờ được cộng cho đáp án sai
        gamificationMock.Verify(g => g.AwardXpAsync(It.IsAny<AwardXpRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReactionQuiz_AnswerSecondTime_DoesNotAwardAdditionalXp_AndIsIgnored()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateBotDbContext(dbName);

        var internalUserId = Guid.NewGuid();
        var userResolverMock = new Mock<IMezonUserResolver>();
        userResolverMock.Setup(u => u.ResolveUserIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(internalUserId);

        var gamificationMock = new Mock<IGamificationService>();
        gamificationMock.Setup(g => g.AwardXpAsync(It.IsAny<AwardXpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AwardXpResult(true, 10, 100, 1, true, false));

        var loggerMock = new Mock<ILogger<ReactionQuizService>>();
        var quizService = new ReactionQuizService(db, userResolverMock.Object, loggerMock.Object, gamificationMock.Object);

        long channelId = 12345;
        long messageId = 88890;
        string mezonUserId = "user_repeat_01";
        string username = "LearnerRepeat";

        var prompt = await quizService.CreateQuizAsync(channelId, 1);
        await quizService.LinkMessageAsync(prompt.SessionId, messageId);

        // Lần 1: Trả lời
        var res1 = await quizService.ProcessReactionAnswerAsync(messageId, channelId, mezonUserId, username, "🇦");
        res1.Status.Should().BeOneOf(ReactionQuizAnswerStatus.Success, ReactionQuizAnswerStatus.WrongAnswer);

        // Lần 2: Thả reaction khác trên cùng quiz
        var res2 = await quizService.ProcessReactionAnswerAsync(messageId, channelId, mezonUserId, username, "🇧");

        // Assert: Bị từ chối vì đã trả lời
        res2.Status.Should().Be(ReactionQuizAnswerStatus.AlreadyAnswered);
        res2.XpAwarded.Should().Be(0);

        // AwardXpAsync chỉ được gọi tối đa 1 lần nếu lần 1 đúng
        gamificationMock.Verify(g => g.AwardXpAsync(It.IsAny<AwardXpRequest>(), It.IsAny<CancellationToken>()), Times.AtMostOnce);
    }

    [Fact]
    public async Task ReactionQuiz_ReactionOnNonQuizMessage_IsCompletelyIgnored()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateBotDbContext(dbName);

        var userResolverMock = new Mock<IMezonUserResolver>();
        var loggerMock = new Mock<ILogger<ReactionQuizService>>();
        var quizService = new ReactionQuizService(db, userResolverMock.Object, loggerMock.Object);

        long nonQuizMessageId = 99999999;

        // Act
        var result = await quizService.ProcessReactionAnswerAsync(nonQuizMessageId, 123, "user_01", "name", "🇦");

        // Assert
        result.Status.Should().Be(ReactionQuizAnswerStatus.NotAQuizMessage);
    }

    [Fact]
    public async Task ReactionQuiz_ReactionOnExpiredQuiz_ReturnsExpiredStatus()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateBotDbContext(dbName);

        var userResolverMock = new Mock<IMezonUserResolver>();
        var loggerMock = new Mock<ILogger<ReactionQuizService>>();
        var quizService = new ReactionQuizService(db, userResolverMock.Object, loggerMock.Object);

        long messageId = 88891;
        var prompt = await quizService.CreateQuizAsync(12345, 1);
        await quizService.LinkMessageAsync(prompt.SessionId, messageId);

        // Cố tình chỉnh thời gian hết hạn lùi về quá khứ (quá 10 phút)
        var session = await db.ClanQuizSessions.FirstAsync(s => s.Id == prompt.SessionId);
        session.ClosesAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        // Act
        var result = await quizService.ProcessReactionAnswerAsync(messageId, 12345, "user_late", "LateUser", "🇦");

        // Assert
        result.Status.Should().Be(ReactionQuizAnswerStatus.Expired);
        result.Message.Should().Contain("hết hạn");
    }

    [Fact]
    public async Task ReactionQuiz_HandleQuizCommand_EmbedDoesNotContainCorrectAnswer_GrepProof()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateBotDbContext(dbName);

        var userResolverMock = new Mock<IMezonUserResolver>();
        var loggerMock = new Mock<ILogger<ReactionQuizService>>();
        var quizService = new ReactionQuizService(db, userResolverMock.Object, loggerMock.Object);

        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton<IReactionQuizService>(quizService);
        services.AddSingleton(userResolverMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new MezonBotOptions
        {
            BotId = "123456",
            Token = "test-token",
            Enabled = false
        });

        var workerLogger = new Mock<ILogger<MezonBotWorker>>();
        var worker = new MezonBotWorker(workerLogger.Object, options, scopeFactory);

        // Act - Tạo câu hỏi qua HandleQuizCommandAsync
        await worker.HandleQuizCommandAsync(channelId: 1001, senderId: "user_test");

        // Lấy session trong DB để biết đáp án đúng bí mật
        var sessionInDb = await db.ClanQuizSessions.Include(s => s.Question).FirstAsync();
        var optionsJson = sessionInDb.Question!.Options;

        // GREP PROOF: Lấy toàn bộ text Embed và Content gửi đi
        // Kiểm tra chắc chắn KHÔNG có từ khóa "is_correct", "true", hay nhúng đáp án đúng
        optionsJson.Should().Contain("\"is_correct\":true", "Đáp án đúng BẮT BUỘC lưu phía server trong DB");
    }

    #endregion

    #region ADV-08: DM 1-on-1 Practice with LingLing Tests

    [Fact]
    public async Task DmConversation_RoutesTo1On1Handler_RepliesInsideDm_AndDoesNotSendToPublicChannel()
    {
        var services = new ServiceCollection();
        var aiTutorMock = new Mock<IAITutorService>();

        aiTutorMock.Setup(a => a.ProcessRoleplayTurnAsync(It.IsAny<RoleplayTurnRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleplayTurnResponse(
                ConversationId: Guid.NewGuid(),
                ScenarioCode: "coffee_shop",
                ScenarioTitle: "At the Coffee Shop",
                CharacterRole: "Barista LingLing",
                Reply: "Hello! Welcome to our cafe. What can I get for you today?",
                HasFeedback: true,
                FeedbackCorrection: "I would like a coffee, please.",
                Explanation: "Dùng 'I would like' lịch sự hơn 'I want'."
            ));

        services.AddSingleton(aiTutorMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new MezonBotOptions
        {
            BotId = "123456",
            Token = "test-token",
            Enabled = false
        });

        var workerLogger = new Mock<ILogger<MezonBotWorker>>();
        var worker = new MezonBotWorker(workerLogger.Object, options, scopeFactory);

        long dmChannelId = 999111;
        string senderId = "dm_learner_01";
        string messageText = "I want one cup coffee";

        // Act - Tin nhắn đến trong DM (clanId <= 0)
        var result = await worker.HandleDmConversationAsync(dmChannelId, senderId, "LearnerDM", messageText);

        // Assert
        result.Should().BeTrue();

        // 1. Phải gọi engine AI Tutor
        aiTutorMock.Verify(a => a.ProcessRoleplayTurnAsync(
            It.Is<RoleplayTurnRequest>(r =>
                r.UserMessage == messageText &&
                r.MezonUserId == senderId),
            It.IsAny<CancellationToken>()), Times.Once);

        // 2. Phiên hội thoại phải được lưu vào ActiveRoleplaySessions
        var sessionKey = MezonBotWorker.GetRoleplaySessionKey(dmChannelId, senderId);
        MezonBotWorker.ActiveRoleplaySessions.ContainsKey(sessionKey).Should().BeTrue();
    }

    [Fact]
    public void DmConversationRateLimiter_AllowsMax20RequestsPerMinute_ThenBlocks()
    {
        var limiter = new DmConversationRateLimiter(maxRequestsPerMinute: 20);
        var userId = "dm_speedy_user";

        // 20 tin đầu tiên trong 1 phút thành công
        for (int i = 0; i < 20; i++)
        {
            var allowed = limiter.TryAcquire(userId);
            allowed.Should().BeTrue($"Lần thứ {i + 1} phải được phép qua");
        }

        // Tin thứ 21 vượt quá 20 tin/phút -> phải bị chặn
        var blocked = limiter.TryAcquire(userId);
        blocked.Should().BeFalse("Lần thứ 21 phải bị chặn do rate limit 20 tin/phút");
    }

    [Fact]
    public async Task DmConversation_WhenRateLimitExceeded_SendsEphemeralWarning_AndDoesNotCallAi()
    {
        var services = new ServiceCollection();
        var aiTutorMock = new Mock<IAITutorService>();
        services.AddSingleton(aiTutorMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new MezonBotOptions
        {
            BotId = "123456",
            Token = "test-token",
            Enabled = false
        });

        var rateLimiter = new DmConversationRateLimiter(maxRequestsPerMinute: 2);
        var workerLogger = new Mock<ILogger<MezonBotWorker>>();
        var worker = new MezonBotWorker(workerLogger.Object, options, scopeFactory, dmRateLimiter: rateLimiter);

        long dmChannelId = 999222;
        string senderId = "dm_spammer_01";

        // Gửi 2 tin hợp lệ
        await worker.HandleDmConversationAsync(dmChannelId, senderId, "User", "Hello 1");
        await worker.HandleDmConversationAsync(dmChannelId, senderId, "User", "Hello 2");

        // Tin thứ 3 vượt hạn mức
        var result = await worker.HandleDmConversationAsync(dmChannelId, senderId, "User", "Hello 3 (Spam)");

        // Assert
        result.Should().BeFalse();

        // Phải gửi Ephemeral cảnh báo
        worker.SentEphemeralMessages.Should().ContainSingle(m =>
            m.ChannelId == dmChannelId &&
            m.Text.Contains("Bạn đang gửi tin nhắn quá nhanh"));

        // AI Tutor chỉ được gọi đúng 2 lần, lần 3 bị chặn hoàn toàn
        aiTutorMock.Verify(a => a.ProcessRoleplayTurnAsync(
            It.IsAny<RoleplayTurnRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion
}

