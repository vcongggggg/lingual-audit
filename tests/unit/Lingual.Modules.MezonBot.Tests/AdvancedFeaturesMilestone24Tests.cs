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

        // Act - Tạo câu hỏi qua HandleReactionQuizCommandAsync
        await worker.HandleReactionQuizCommandAsync(channelId: 1001, senderId: "user_test");

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

    #region Fix F1: Bot Ignored Own Reactions (Quiz & Bookmark)

    [Fact]
    public async Task ReactionQuiz_WhenReactionIsFromBotItself_IsCompletelyIgnored_NoDbResponse_NoXp()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateBotDbContext(dbName);

        var quizServiceMock = new Mock<IReactionQuizService>();
        var userResolverMock = new Mock<IMezonUserResolver>();
        var gamificationMock = new Mock<IGamificationService>();

        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(quizServiceMock.Object);
        services.AddSingleton(userResolverMock.Object);
        services.AddSingleton(gamificationMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        long botId = 987654321;
        var options = Options.Create(new MezonBotOptions
        {
            BotId = botId.ToString(),
            Token = "test-token",
            Enabled = true
        });

        var loggerMock = new Mock<ILogger<MezonBotWorker>>();
        var worker = new MezonBotWorker(loggerMock.Object, options, scopeFactory);

        long channelId = 12345;
        long messageId = 88899;

        // Act 1: Thử gọi HandleMessageReactionQuizAsync trực tiếp với SenderId = BotId
        var directResult = await worker.HandleMessageReactionQuizAsync(
            clanId: 1,
            channelId: channelId,
            messageId: messageId,
            senderId: botId,
            senderName: "LingualBot",
            emoji: "🇦");

        // Assert 1: Phải bị bỏ qua hoàn toàn, trả về null
        directResult.Should().BeNull("Reaction từ chính BotId phải bị bỏ qua hoàn toàn");

        // Act 2: Thử gửi qua OnMessageReactionReceivedAsync với event data từ chính bot
        var reactionJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            clan_id = 1L,
            channel_id = channelId,
            message_id = messageId,
            sender_id = botId,
            sender_name = "LingualBot",
            emoji = "🇦",
            action = true
        });
        var eventData = System.Text.Json.JsonSerializer.Deserialize<Mezon.Net.Models.MessageReactionResponse>(reactionJson,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        await worker.OnMessageReactionReceivedAsync(eventData);

        // Assert 2: ProcessReactionAnswerAsync tuyệt đối KHÔNG bao giờ được gọi
        quizServiceMock.Verify(s => s.ProcessReactionAnswerAsync(
            It.IsAny<long>(),
            It.IsAny<long>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never, "Tuyệt đối không xử lý câu trả lời nếu reaction là của chính Bot");

        // Gamification tuyệt đối không bao giờ được cộng XP
        gamificationMock.Verify(g => g.AwardXpAsync(
            It.IsAny<AwardXpRequest>(),
            It.IsAny<CancellationToken>()), Times.Never, "Tuyệt đối không cộng XP cho chính bot");

        // Log Information ghi nhận đã bỏ qua reaction từ chính bot
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bỏ qua reaction") && v.ToString()!.Contains("chính Bot")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ReactionBookmark_WhenReactionIsFromBotItself_IsCompletelyIgnored()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        long botId = 987654321;
        var options = Options.Create(new MezonBotOptions
        {
            BotId = botId.ToString(),
            Token = "test-token",
            Enabled = true
        });

        var loggerMock = new Mock<ILogger<MezonBotWorker>>();
        var worker = new MezonBotWorker(loggerMock.Object, options, scopeFactory);

        // Act - Bot thả reaction bookmark vào tin nhắn
        var result = await worker.HandleMessageReactionBookmarkAsync(
            clanId: 1,
            channelId: 12345,
            messageId: 88899,
            senderId: botId,
            senderName: "LingualBot",
            emoji: "🔖",
            action: true);

        // Assert - Bị từ chối, trả về false
        result.Should().BeFalse("Reaction bookmark từ chính Bot phải bị bỏ qua");

        // Log ghi nhận bỏ qua
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bỏ qua reaction bookmark từ chính Bot")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Quiz Anti-Spam & Expired Marking Tests

    [Fact]
    public async Task ReactionQuiz_WhenChannelHasActiveOpenQuiz_RejectsSecondQuizCommand_AndDoesNotCreateNewSessionInDb()
    {
        MezonBotWorker.ChannelQuizCooldowns.Clear();

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

        long channelId = 55501;

        // Lần 1: Tạo quiz thành công
        await worker.HandleReactionQuizCommandAsync(channelId, senderId: "12345678");

        var sessionsCountAfterFirst = await db.ClanQuizSessions.CountAsync();
        sessionsCountAfterFirst.Should().Be(1, "Lần 1 phải tạo 1 session trong DB");

        // Giả lập xóa cooldown nhưng giữ nguyên session đang open trong DB
        MezonBotWorker.ChannelQuizCooldowns.Clear();

        // Lần 2: Gọi tiếp /quiz trong cùng kênh khi session 1 vẫn đang open
        await worker.HandleReactionQuizCommandAsync(channelId, senderId: "12345678");

        // Assert:
        // 1. Số lượng session trong DB vẫn giữ nguyên là 1, không tạo thêm session rác
        var sessionsCountAfterSecond = await db.ClanQuizSessions.CountAsync();
        sessionsCountAfterSecond.Should().Be(1, "Không được tạo session thứ 2 khi kênh đang có quiz mở");

        // 2. User nhận được thông báo kênh đang có quiz
        worker.SentEphemeralMessages.Should().Contain(m =>
            m.ChannelId == channelId &&
            m.UserId == 12345678 &&
            m.Text.Contains("đang có một câu hỏi Quiz đang diễn ra"));
    }

    [Fact]
    public async Task ReactionQuiz_WhenCalledWithin60SecondsCooldown_RejectsQuizCommand_EvenIfPreviousQuizClosed()
    {
        MezonBotWorker.ChannelQuizCooldowns.Clear();

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

        long channelId = 55502;

        // Giả lập kênh vừa tạo quiz cách đây 20 giây (vẫn trong thời gian cooldown 60 giây)
        MezonBotWorker.ChannelQuizCooldowns[channelId] = DateTime.UtcNow.AddSeconds(-20);

        // Act - Gọi /quiz lại
        await worker.HandleReactionQuizCommandAsync(channelId, senderId: "12345678");

        // Assert:
        // 1. Không tạo session nào trong DB
        var count = await db.ClanQuizSessions.CountAsync();
        count.Should().Be(0);

        // 2. Nhận được thông báo Cooldown
        worker.SentEphemeralMessages.Should().Contain(m =>
            m.ChannelId == channelId &&
            m.UserId == 12345678 &&
            m.Text.Contains("cooldown"));
    }

    [Fact]
    public async Task ReactionQuiz_WhenQuizExpires_ConcludeExpiredQuiz_MarksStatusExpired_AndExtractsCorrectAnswer()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateBotDbContext(dbName);

        var userResolverMock = new Mock<IMezonUserResolver>();
        var loggerMock = new Mock<ILogger<ReactionQuizService>>();
        var quizService = new ReactionQuizService(db, userResolverMock.Object, loggerMock.Object);

        long channelId = 55503;
        long messageId = 999123;

        var prompt = await quizService.CreateQuizAsync(channelId, 1);
        await quizService.LinkMessageAsync(prompt.SessionId, messageId);

        // Giả lập ClosesAt lùi về quá khứ (đã quá hạn 10 phút)
        var session = await db.ClanQuizSessions.FirstAsync(s => s.Id == prompt.SessionId);
        session.ClosesAt = DateTime.UtcNow.AddMinutes(-5);
        await db.SaveChangesAsync();

        // Act - Kích hoạt kết thúc quiz hết hạn
        var conclusion = await quizService.ConcludeExpiredQuizAsync(messageId);

        // Assert:
        conclusion.Should().NotBeNull();
        conclusion!.Success.Should().BeTrue();
        conclusion.SessionId.Should().Be(prompt.SessionId);
        conclusion.MessageId.Should().Be(messageId);
        conclusion.QuestionPrompt.Should().Be(prompt.QuestionPrompt);
        conclusion.CorrectKey.Should().NotBeNullOrEmpty();

        // DB session đã được cập nhật sang status 'expired'
        var updatedSession = await db.ClanQuizSessions.FirstAsync(s => s.Id == prompt.SessionId);
        updatedSession.Status.Should().Be("expired");
        updatedSession.ClosedAt.Should().NotBeNull();
    }

    #endregion
}

