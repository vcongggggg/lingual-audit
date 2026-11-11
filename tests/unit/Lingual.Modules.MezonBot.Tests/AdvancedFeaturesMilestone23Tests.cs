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
using Lingual.Modules.Gamification.Infrastructure.Data;
using Lingual.Modules.Gamification.Services;
using Lingual.Modules.MezonBot;
using Lingual.Modules.MezonBot.Services;
using Mezon.Net.Client;
using Mezon.Net.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lingual.Modules.MezonBot.Tests;

public class AdvancedFeaturesMilestone23Tests
{
    private static GamificationDbContext CreateGamificationDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GamificationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GamificationDbContext(options);
    }

    private static (ServiceProvider ServiceProvider, GamificationDbContext DbContext) CreateTestServices(string dbName)
    {
        var services = new ServiceCollection();
        var db = CreateGamificationDbContext(dbName);
        services.AddSingleton(db);

        var streakServiceMock = new Mock<IStreakService>();
        var sampleStreak = new Lingual.Modules.Gamification.Contracts.DTOs.UserStreakDto(
            Guid.NewGuid(),
            5,
            10,
            DateOnly.FromDateTime(DateTime.UtcNow),
            1,
            true,
            20,
            0,
            DateTime.UtcNow);

        streakServiceMock.Setup(s => s.GetStreakAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleStreak);
        streakServiceMock.Setup(s => s.RecordXpActivityAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleStreak);

        var leaderboardServiceMock = new Mock<ILeaderboardService>();

        services.AddSingleton<IStreakService>(streakServiceMock.Object);
        services.AddSingleton<ILeaderboardService>(leaderboardServiceMock.Object);
        services.AddSingleton<ILogger<XpService>>(new NullLogger<XpService>());
        services.AddScoped<IXpService, XpService>();
        services.AddScoped<IGamificationService, GamificationService>();

        var userResolverMock = new Mock<IMezonUserResolver>();
        userResolverMock.Setup(u => u.ResolveUserIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        services.AddSingleton<IMezonUserResolver>(userResolverMock.Object);

        return (services.BuildServiceProvider(), db);
    }

    #region ADV-04: Clan Voice Study Room & Speaking Club Tracker Tests

    [Fact]
    public void VoiceSession_JoinAndLeave_RecordsAccurateDuration()
    {
        // Arrange
        var tracker = new VoiceStudyRoomTracker(new NullLogger<VoiceStudyRoomTracker>());
        long clanId = 1001;
        long channelId = 2002;
        long userId = 3003;
        var joinedAt = new DateTime(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);
        var leftAt = new DateTime(2026, 11, 12, 10, 35, 0, DateTimeKind.Utc);

        // Act
        var joinSession = tracker.OnUserJoinedVoice(clanId, channelId, userId, "NguyenVanA", joinedAt);
        var leaveSession = tracker.OnUserLeftVoice(channelId, userId, leftAt);

        // Assert
        joinSession.Should().NotBeNull();
        joinSession.SpeakingDuration.Should().Be(TimeSpan.Zero);
        joinSession.LeftAtUtc.Should().BeNull();

        leaveSession.Should().NotBeNull();
        leaveSession!.LeftAtUtc.Should().Be(leftAt);
        leaveSession.SpeakingDuration.TotalMinutes.Should().Be(35.0);
    }

    [Fact]
    public void VoiceStudyRoom_ActiveSpeakers_AccumulatesSpeakingDuration()
    {
        // Arrange
        var tracker = new VoiceStudyRoomTracker(new NullLogger<VoiceStudyRoomTracker>());
        long clanId = 1001;
        long channelId = 2002;
        long userId = 4004;

        tracker.OnUserJoinedVoice(clanId, channelId, userId, "LearnerB");

        // Act
        tracker.RecordSpeakingTick(channelId, userId, TimeSpan.FromMinutes(5));
        tracker.RecordSpeakingTick(channelId, userId, TimeSpan.FromMinutes(12));

        var session = tracker.GetSession(channelId, userId);

        // Assert
        session.Should().NotBeNull();
        session!.SpeakingDuration.TotalMinutes.Should().Be(17.0);
    }

    [Fact]
    public async Task VoiceStudyRoom_AwardSpeakingXp_Awards50Xp_Every15Minutes_ServerSide()
    {
        // Arrange
        var (sp, db) = CreateTestServices("VoiceXpTestDb_1");
        var tracker = new VoiceStudyRoomTracker(new NullLogger<VoiceStudyRoomTracker>());
        long clanId = 1001;
        long channelId = 2002;
        long userId = 5005;

        var joinedAt = new DateTime(2026, 11, 12, 8, 0, 0, DateTimeKind.Utc);
        var leftAt = new DateTime(2026, 11, 12, 8, 35, 0, DateTimeKind.Utc); // 35 phút = 2 buckets (15m, 30m)

        tracker.OnUserJoinedVoice(clanId, channelId, userId, "LearnerC", joinedAt);
        tracker.OnUserLeftVoice(channelId, userId, leftAt);

        // Act
        var results = await tracker.AwardSpeakingXpAsync(channelId, userId, sp);

        // Assert
        results.Should().HaveCount(2, "35 phút luyện nói phải tạo 2 mốc 15 phút");
        results[0].BucketIndex.Should().Be(1);
        results[0].AwardedXp.Should().Be(50);
        results[0].AlreadyAwarded.Should().BeFalse();

        results[1].BucketIndex.Should().Be(2);
        results[1].AwardedXp.Should().Be(50);
        results[1].AlreadyAwarded.Should().BeFalse();

        // Kiểm tra database ledger
        var ledgerEntries = await db.XpLedgers.Where(x => x.SourceType == "voice_study_room").ToListAsync();
        ledgerEntries.Should().HaveCount(2);
        ledgerEntries.Sum(x => x.XpDelta).Should().Be(100);
    }

    [Fact]
    public async Task VoiceStudyRoom_AwardSpeakingXp_IsStrictlyIdempotent_DoesNotAwardTwiceForSameBucket()
    {
        // Arrange
        var (sp, db) = CreateTestServices("VoiceXpTestDb_Idempotent");
        var tracker = new VoiceStudyRoomTracker(new NullLogger<VoiceStudyRoomTracker>());
        long clanId = 1001;
        long channelId = 2002;
        long userId = 6006;

        var joinedAt = new DateTime(2026, 11, 12, 9, 0, 0, DateTimeKind.Utc);
        var leftAt = new DateTime(2026, 11, 12, 9, 20, 0, DateTimeKind.Utc); // 20 phút = 1 bucket

        tracker.OnUserJoinedVoice(clanId, channelId, userId, "LearnerD", joinedAt);
        tracker.OnUserLeftVoice(channelId, userId, leftAt);

        // Act 1: Lần gọi thứ nhất
        var firstCall = await tracker.AwardSpeakingXpAsync(channelId, userId, sp);

        // Act 2: Lần gọi thứ hai với cùng phiên/mốc
        var secondCall = await tracker.AwardSpeakingXpAsync(channelId, userId, sp);

        // Assert
        firstCall.Should().HaveCount(1);
        firstCall[0].AwardedXp.Should().Be(50);
        firstCall[0].AlreadyAwarded.Should().BeFalse();

        secondCall.Should().HaveCount(1);
        secondCall[0].AwardedXp.Should().Be(0, "Lần gọi thứ hai không được cộng thêm XP");
        secondCall[0].AlreadyAwarded.Should().BeTrue("Lần gọi thứ hai phải phát hiện trùng lặp Idempotency");

        // Database chỉ có duy nhất 1 bản ghi XP
        var totalLedgerCount = await db.XpLedgers.CountAsync(x => x.SourceType == "voice_study_room");
        totalLedgerCount.Should().Be(1);
    }

    [Fact]
    public void VoiceStudyRoom_HasDocumentedBlockedEvidence_ForSfuWebRtcAudioStreaming()
    {
        // Assert
        VoiceStudyRoomTracker.SfuAudioStreamingBlockedEvidence.Should().NotBeNullOrWhiteSpace();
        VoiceStudyRoomTracker.SfuAudioStreamingBlockedEvidence.Should().Contain("Mezon.Net.Sdk 1.11.0 does not include a WebRTC SFU client or Opus encoder");
        VoiceStudyRoomTracker.SfuAudioStreamingBlockedEvidence.Should().Contain("GenerateMeetTokenAsync");
        VoiceStudyRoomTracker.SfuAudioStreamingBlockedEvidence.Should().Contain("ListChannelVoiceUsersAsync");
    }

    #endregion

    #region ADV-06: AI Essay & Writing Coach Tests

    [Theory]
    [InlineData("/grade")]
    [InlineData("/GRADE")]
    [InlineData("/grade My English essay...")]
    [InlineData("/essay")]
    [InlineData("/ESSAY")]
    [InlineData("  /essay Some topic  ")]
    public void ResolveCommandType_GradeAndEssay_ReturnsEssay(string input)
    {
        // Act
        var result = MezonBotWorker.ResolveCommandType(input);

        // Assert
        result.Should().Be("essay");
    }

    [Theory]
    [InlineData("/voice")]
    [InlineData("/VOICE")]
    [InlineData("/meet")]
    [InlineData("/MEET")]
    [InlineData("  /voice room  ")]
    public void ResolveCommandType_VoiceAndMeet_ReturnsVoice(string input)
    {
        // Act
        var result = MezonBotWorker.ResolveCommandType(input);

        // Assert
        result.Should().Be("voice");
    }

    [Fact]
    public void BuildEssayInputCard_UsesButtonBuilderAddInput_WithTextareaAndSubmitButton()
    {
        // Arrange
        var service = new AiEssayCoachService(new NullLogger<AiEssayCoachService>());

        // Act
        var card = service.BuildEssayInputCard();
        var json = card.ToJson();

        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("essay_text_input", "Card phải chứa id của input component");
        json.Should().Contain("btn_submit_essay", "Card phải chứa nút bấm nộp bài");
        json.Should().Contain("textarea", "Input component phải là loại textarea");
        json.Should().Contain("AI ESSAY COACH", "Card phải có tiêu đề AI Essay Coach");
        json.Should().Contain("0 XP Awarded", "Card phải thể hiện quy định không cộng XP cho bài viết");
    }

    [Fact]
    public async Task AiEssayCoach_Evaluates4AcademicCriteria_TaskLexicalGrammarNative()
    {
        // Arrange
        var service = new AiEssayCoachService(new NullLogger<AiEssayCoachService>());
        var essay =
            "Artificial intelligence is rapidly transforming modern society and language education. " +
            "First of all, students can practice conversational English anytime with automated AI tutors. " +
            "Furthermore, personalized feedback helps learners identify grammatical flaws and enhance vocabulary effectively. " +
            "In conclusion, while teachers remain indispensable, AI provides very good support for self-directed study.";

        // Act
        var result = await service.EvaluateEssayAsync(essay, topic: "Impact of AI in Education");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.WordCount.Should().BeGreaterThan(40);
        result.OverallBandScore.Should().BeInRange(5.0, 9.0);
        result.CefrLevel.Should().MatchRegex("^(A1|A2|B1|B2|C1|C2)$");

        // 4 Tiêu chí học thuật
        result.TaskAchievement.CriteriaName.Should().Contain("Task Achievement");
        result.TaskAchievement.Score.Should().BeGreaterThan(0);
        result.TaskAchievement.Strengths.Should().NotBeEmpty();

        result.LexicalResource.CriteriaName.Should().Contain("Lexical Resource");
        result.LexicalResource.Score.Should().BeGreaterThan(0);

        result.GrammaticalAccuracy.CriteriaName.Should().Contain("Grammatical Range & Accuracy");
        result.GrammaticalAccuracy.Score.Should().BeGreaterThan(0);

        result.NativeSuggestions.CriteriaName.Should().Contain("Native Suggestions");
        result.NativeSuggestions.Score.Should().BeGreaterThan(0);

        // Kiểm tra quy định KHÔNG cộng XP cho bài viết
        result.AwardedXp.Should().Be(0, "Theo yêu cầu nghiệp vụ, không cộng XP cho bài viết");
    }

    [Fact]
    public async Task AiEssayCoach_EmptyOrWhitespace_ReturnsGracefulFailure()
    {
        // Arrange
        var service = new AiEssayCoachService(new NullLogger<AiEssayCoachService>());

        // Act
        var result = await service.EvaluateEssayAsync("   ");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.OverallBandScore.Should().Be(0.0);
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void EssayRateLimiter_AllowsMax3PerMinute_BlocksFourth()
    {
        // Arrange
        var limiter = new EssayRateLimiter(maxSubmissionsPerMinute: 3);
        long userId = 7777;

        // Act
        var pass1 = limiter.TryAcquire(userId, out var retry1);
        var pass2 = limiter.TryAcquire(userId, out var retry2);
        var pass3 = limiter.TryAcquire(userId, out var retry3);
        var pass4 = limiter.TryAcquire(userId, out var retry4);

        // Assert
        pass1.Should().BeTrue();
        retry1.Should().Be(0);

        pass2.Should().BeTrue();
        retry2.Should().Be(0);

        pass3.Should().BeTrue();
        retry3.Should().Be(0);

        pass4.Should().BeFalse("Lần thứ 4 trong vòng 1 phút phải bị chặn bởi Rate Limiter");
        retry4.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task HandleEssayCommandAsync_WhenEmptyContent_SendsInteractiveFormCard()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new MezonBotOptions
        {
            BotId = "123456",
            Token = "test-token",
            Enabled = false
        });

        var loggerMock = new Mock<ILogger<MezonBotWorker>>();
        var worker = new MezonBotWorker(loggerMock.Object, options, scopeFactory);

        long channelId = 8888;
        long clanId = 9999;
        long senderId = 1111;
        var fakeData = default(ChannelMessageResponse);

        // Act: Gõ /grade không có nội dung
        await worker.HandleEssayCommandAsync(channelId, clanId, senderId, "", isDm: false, fakeData);

        // Assert: Phải gửi Ephemeral Card chứa Form nhập bài
        worker.SentEphemeralContents.Should().Contain(c =>
            c.ChannelId == channelId &&
            c.UserId == senderId &&
            c.Content.ToJson().Contains("essay_text_input"));
    }

    [Fact]
    public async Task HandleEssayCommandAsync_WhenProvidedText_EvaluatesAndSendsEphemeral()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new MezonBotOptions
        {
            BotId = "123456",
            Token = "test-token",
            Enabled = false
        });

        var loggerMock = new Mock<ILogger<MezonBotWorker>>();
        var worker = new MezonBotWorker(loggerMock.Object, options, scopeFactory);

        long channelId = 8888;
        long clanId = 9999;
        long senderId = 2222;
        var fakeData = default(ChannelMessageResponse);

        var essayText = "Learning English opens up many global career opportunities. It helps students connect internationally.";

        // Act: Gõ /grade kèm nội dung
        await worker.HandleEssayCommandAsync(channelId, clanId, senderId, essayText, isDm: false, fakeData);

        // Assert: Kết quả phải gửi Ephemeral riêng tư với 4 tiêu chí
        worker.SentEphemeralContents.Should().Contain(c =>
            c.ChannelId == channelId &&
            c.UserId == senderId &&
            c.Content.ToJson().Contains("Task Achievement") &&
            c.Content.ToJson().Contains("Lexical Resource") &&
            c.Content.ToJson().Contains("Grammatical Range") &&
            c.Content.ToJson().Contains("Native Suggestions"));
    }

    #endregion
}
