using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    private static MessageButtonClickedResponse CreateMockButtonClickedResponse(
        long channelId,
        long userId,
        long senderId,
        string buttonId,
        string extraData)
    {
        var proto = new Mezon.Net.Internal.Realtime.MessageButtonClicked
        {
            ChannelId = channelId,
            UserId = userId,
            SenderId = senderId,
            ButtonId = buttonId,
            ExtraData = extraData
        };
        var ctor = typeof(MessageButtonClickedResponse).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(Mezon.Net.Internal.Realtime.MessageButtonClicked) },
            null)!;
        return (MessageButtonClickedResponse)ctor.Invoke(new object[] { proto });
    }

    private static VoiceInteractiveEventResponse CreateMockVoiceInteractiveResponse(
        long clanId,
        long channelId,
        long senderId)
    {
        var proto = new Mezon.Net.Internal.Realtime.VoiceInteractiveEvent
        {
            ClanId = clanId,
            VoiceChannelId = channelId,
            SenderId = senderId
        };
        var ctor = typeof(VoiceInteractiveEventResponse).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(Mezon.Net.Internal.Realtime.VoiceInteractiveEvent) },
            null)!;
        return (VoiceInteractiveEventResponse)ctor.Invoke(new object[] { proto });
    }

    #region ADV-04: Clan Voice Study Room & Speaking Club Tracker Tests (F1 Fixes)

    [Fact]
    public void VoiceSession_JoinAndLeave_WithoutVoiceActivity_SpeakingDurationIsZero()
    {
        // Arrange - F1: User vao phong tat mic/idle 35 phut -> SpeakingDuration phai la 0, khong duoc gan elapsed
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
        leaveSession.SpeakingDuration.TotalMinutes.Should().Be(0.0, "Khi khong co voice activity, SpeakingDuration phai bang 0 du co mat 35 phut");
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

        // Act - Tich luy thoi gian noi that qua RecordSpeakingTick
        tracker.RecordSpeakingTick(channelId, userId, TimeSpan.FromMinutes(5));
        tracker.RecordSpeakingTick(channelId, userId, TimeSpan.FromMinutes(12));

        var session = tracker.GetSession(channelId, userId);

        // Assert
        session.Should().NotBeNull();
        session!.SpeakingDuration.TotalMinutes.Should().Be(17.0);
    }

    [Fact]
    public async Task VoiceStudyRoom_AwardSpeakingXp_Idle20Minutes_AwardsZeroXp()
    {
        // Arrange - DoD 1: User join voice 20 phut khong co voice activity -> 0 XP
        var (sp, db) = CreateTestServices("VoiceXpTestDb_Idle");
        var tracker = new VoiceStudyRoomTracker(new NullLogger<VoiceStudyRoomTracker>());
        long clanId = 1001;
        long channelId = 2002;
        long userId = 4111;

        var joinedAt = new DateTime(2026, 11, 12, 8, 0, 0, DateTimeKind.Utc);
        var leftAt = new DateTime(2026, 11, 12, 8, 20, 0, DateTimeKind.Utc); // 20 phut ngoi im, khong noi tick nao

        tracker.OnUserJoinedVoice(clanId, channelId, userId, "IdleUser", joinedAt);
        tracker.OnUserLeftVoice(channelId, userId, leftAt);

        // Act
        var results = await tracker.AwardSpeakingXpAsync(channelId, userId, sp);

        // Assert: Khong dat moc 15 phut noi that -> 0 bucket, 0 XP
        results.Should().BeEmpty("Nguoi dung khong co voice activity khong duoc tao bucket thuong XP");

        // Database khong co ban ghi ledger nao
        var count = await db.XpLedgers.CountAsync(x => x.SourceType == "voice_study_room");
        count.Should().Be(0);
    }

    [Fact]
    public async Task VoiceStudyRoom_AwardSpeakingXp_Awards50Xp_Every15Minutes_ServerSide()
    {
        // Arrange - DoD 1: User co voice activity du 35 phut -> +100 XP (2 buckets x 50 XP)
        var (sp, db) = CreateTestServices("VoiceXpTestDb_1");
        var tracker = new VoiceStudyRoomTracker(new NullLogger<VoiceStudyRoomTracker>());
        long clanId = 1001;
        long channelId = 2002;
        long userId = 5005;

        var joinedAt = new DateTime(2026, 11, 12, 8, 0, 0, DateTimeKind.Utc);
        var leftAt = new DateTime(2026, 11, 12, 8, 40, 0, DateTimeKind.Utc);

        tracker.OnUserJoinedVoice(clanId, channelId, userId, "LearnerC", joinedAt);
        tracker.RecordSpeakingTick(channelId, userId, TimeSpan.FromMinutes(35)); // Noi that 35 phut = 2 buckets (15m, 30m)
        tracker.OnUserLeftVoice(channelId, userId, leftAt);

        // Act
        var results = await tracker.AwardSpeakingXpAsync(channelId, userId, sp);

        // Assert
        results.Should().HaveCount(2, "35 phut luyen noi that phai tao 2 moc 15 phut");
        results[0].BucketIndex.Should().Be(1);
        results[0].AwardedXp.Should().Be(50);
        results[0].AlreadyAwarded.Should().BeFalse();

        results[1].BucketIndex.Should().Be(2);
        results[1].AwardedXp.Should().Be(50);
        results[1].AlreadyAwarded.Should().BeFalse();

        // Kiem tra database ledger
        var ledgerEntries = await db.XpLedgers.Where(x => x.SourceType == "voice_study_room").ToListAsync();
        ledgerEntries.Should().HaveCount(2);
        ledgerEntries.Sum(x => x.XpDelta).Should().Be(100);
    }

    [Fact]
    public async Task VoiceStudyRoom_AwardSpeakingXp_IsStrictlyIdempotent_DoesNotAwardTwiceForSameBucket()
    {
        // Arrange - DoD 1: Idempotency van dung
        var (sp, db) = CreateTestServices("VoiceXpTestDb_Idempotent");
        var tracker = new VoiceStudyRoomTracker(new NullLogger<VoiceStudyRoomTracker>());
        long clanId = 1001;
        long channelId = 2002;
        long userId = 6006;

        var joinedAt = new DateTime(2026, 11, 12, 9, 0, 0, DateTimeKind.Utc);
        var leftAt = new DateTime(2026, 11, 12, 9, 25, 0, DateTimeKind.Utc);

        tracker.OnUserJoinedVoice(clanId, channelId, userId, "LearnerD", joinedAt);
        tracker.RecordSpeakingTick(channelId, userId, TimeSpan.FromMinutes(20)); // Noi that 20 phut = 1 bucket
        tracker.OnUserLeftVoice(channelId, userId, leftAt);

        // Act 1: Lan goi thu nhat
        var firstCall = await tracker.AwardSpeakingXpAsync(channelId, userId, sp);

        // Act 2: Lan goi thu hai voi cung phien/moc
        var secondCall = await tracker.AwardSpeakingXpAsync(channelId, userId, sp);

        // Assert
        firstCall.Should().HaveCount(1);
        firstCall[0].AwardedXp.Should().Be(50);
        firstCall[0].AlreadyAwarded.Should().BeFalse();

        secondCall.Should().HaveCount(1);
        secondCall[0].AwardedXp.Should().Be(0, "Lan goi thu hai khong duoc cong them XP");
        secondCall[0].AlreadyAwarded.Should().BeTrue("Lan goi thu hai phai phat hien trung lap Idempotency");

        // Database chi co duy nhat 1 ban ghi XP
        var totalLedgerCount = await db.XpLedgers.CountAsync(x => x.SourceType == "voice_study_room");
        totalLedgerCount.Should().Be(1);
    }

    [Fact]
    public async Task VoiceStudyRoom_AwardSpeakingXp_WithoutGamificationOrUser_ReturnsZeroXpFallback()
    {
        // Arrange - F1 Fallback: Khi khong co gamification service hoac khong resolve duoc user, tra AwardedXp = 0 kem thong bao ro rang
        var services = new ServiceCollection();
        var emptySp = services.BuildServiceProvider();

        var tracker = new VoiceStudyRoomTracker(new NullLogger<VoiceStudyRoomTracker>());
        long clanId = 1001;
        long channelId = 2002;
        long userId = 7007;

        var joinedAt = new DateTime(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);
        tracker.OnUserJoinedVoice(clanId, channelId, userId, "LearnerNoDb", joinedAt);
        tracker.RecordSpeakingTick(channelId, userId, TimeSpan.FromMinutes(20)); // Co noi 20 phut (1 bucket)

        // Act
        var results = await tracker.AwardSpeakingXpAsync(channelId, userId, emptySp);

        // Assert
        results.Should().HaveCount(1);
        results[0].BucketIndex.Should().Be(1);
        results[0].AwardedXp.Should().Be(0, "Khi khong co GamificationService, khong duoc tra 50 XP ao");
        results[0].AlreadyAwarded.Should().BeFalse();
        results[0].Message.Should().Contain("Gamification");

        // Phien khong duoc ghi nhan bucket vao AwardedBucketIndices de co the thu lai khi co service
        var session = tracker.GetSession(channelId, userId);
        session!.AwardedBucketIndices.Should().NotContain(1);
    }

    [Fact]
    public async Task OnVoiceInteractiveReceivedAsync_AccumulatesSpeakingTickInVoiceTracker()
    {
        // Arrange - F1 Production Subscription: VoiceInteractiveReceived phat hien hoan thanh tich luy thoi gian noi
        var tracker = new VoiceStudyRoomTracker(new NullLogger<VoiceStudyRoomTracker>());
        long channelId = 5555;
        long userId = 6666;
        long clanId = 7777;

        tracker.OnUserJoinedVoice(clanId, channelId, userId, "SpeakerUser");

        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        var options = Options.Create(new MezonBotOptions { BotId = "123", Token = "fake", Enabled = false });

        var worker = new MezonBotWorker(new NullLogger<MezonBotWorker>(), options, scopeFactory, voiceTracker: tracker);

        var voiceEvent = CreateMockVoiceInteractiveResponse(clanId, channelId, userId);

        // Act - Bot nhan su kien Gateway VoiceInteractiveReceived
        await worker.OnVoiceInteractiveReceivedAsync(voiceEvent);
        await worker.OnVoiceInteractiveReceivedAsync(voiceEvent);

        // Assert - 2 ticks x 5 giay = 10 giay
        var session = tracker.GetSession(channelId, userId);
        session.Should().NotBeNull();
        session!.SpeakingDuration.TotalSeconds.Should().Be(10.0);
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

    #region ADV-06: AI Essay & Writing Coach Tests (F2 Fixes)

    [Fact]
    public async Task AiEssayCoach_WithAITutorService_Evaluates4AcademicCriteria_UsingGeminiTutor()
    {
        // Arrange - DoD 2: Service voi wiring production (mock IAITutorService) tra ve ket qua tu AI, khong phai heuristic
        var essay =
            "Artificial intelligence is rapidly transforming modern society and language education. " +
            "First of all, students can practice conversational English anytime with automated AI tutors. " +
            "Furthermore, personalized feedback helps learners identify grammatical flaws and enhance vocabulary effectively. " +
            "In conclusion, while teachers remain indispensable, AI provides very good support for self-directed study.";

        var aiTutorMock = new Mock<IAITutorService>();
        aiTutorMock.Setup(a => a.AnalyzeGrammarAsync(It.IsAny<GrammarAnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GrammarAnalysisResponse(
                IsCorrect: true,
                OriginalText: essay,
                CorrectedText: essay,
                Explanation: "Coherence and grammatical structures are accurate.",
                Tips: "Effective use of academic connectors (Furthermore, In conclusion)."
            ));

        var service = new AiEssayCoachService(
            new NullLogger<AiEssayCoachService>(),
            aiTutorService: aiTutorMock.Object);

        // Act
        var result = await service.EvaluateEssayAsync(essay, topic: "Impact of AI in Education");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.WordCount.Should().BeGreaterThan(40);
        result.OverallBandScore.Should().BeInRange(6.0, 9.0, "AI that phai cham diem Band > 0");
        result.CefrLevel.Should().MatchRegex("^(B1|B2|C1|C2)$");
        result.GeneralFeedback.Should().Contain("AI Tutor (Gemini)");

        // 4 Tieu chi hoc thuat AI
        result.TaskAchievement.Score.Should().BeGreaterThan(0);
        result.LexicalResource.Score.Should().BeGreaterThan(0);
        result.GrammaticalAccuracy.Score.Should().BeGreaterThan(0);
        result.NativeSuggestions.Score.Should().BeGreaterThan(0);

        // Khong cong XP cho bai viet
        result.AwardedXp.Should().Be(0);

        aiTutorMock.Verify(a => a.AnalyzeGrammarAsync(It.IsAny<GrammarAnalysisRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AiEssayCoach_WhenNoAiConfigured_ReturnsHeuristicFallback_WithZeroBandScore()
    {
        // Arrange - DoD 2: Khi fallback heuristic (offline/khong co AI): ghi ro rang '[Uoc tinh tho, chua qua AI]' va OverallBandScore = 0.0
        var service = new AiEssayCoachService(new NullLogger<AiEssayCoachService>()); // Khong HttpClient, khong IAITutorService
        var essay = "Technology brings many advantages to students. It makes learning easier and faster.";

        // Act
        var result = await service.EvaluateEssayAsync(essay);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.OverallBandScore.Should().Be(0.0, "Heuristic fallback tuyet doi khong duoc tu trinh bay band score nhu diem cham that");
        result.GeneralFeedback.Should().Contain("AI");

        // 4 tieu chi deu co prefix canh bao
        result.TaskAchievement.Summary.Should().Contain("AI");
        result.LexicalResource.Summary.Should().Contain("AI");
        result.GrammaticalAccuracy.Summary.Should().Contain("AI");
        result.NativeSuggestions.Summary.Should().Contain("AI");
        result.AwardedXp.Should().Be(0);
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

        pass4.Should().BeFalse("Lan thu 4 trong vong 1 phut phai bi chan boi Rate Limiter");
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

        // Act: Go /grade khong co noi dung
        await worker.HandleEssayCommandAsync(channelId, clanId, senderId, "", isDm: false, fakeData);

        // Assert: Phai gui Ephemeral Card chua Form nhap bai
        worker.SentEphemeralContents.Should().Contain(c =>
            c.ChannelId == channelId &&
            c.UserId == senderId &&
            c.Content.ToJson().Contains("essay_text_input") &&
            c.Content.ToJson().Contains("btn_submit_essay"));
    }

    [Fact]
    public async Task HandleEssayCommandAsync_WhenProvidedText_WithAiTutor_EvaluatesAndSendsEmeraldCard()
    {
        // Arrange - DoD 2: Tich hop IAITutorService vao ScopeFactory cho worker
        var services = new ServiceCollection();
        var aiTutorMock = new Mock<IAITutorService>();
        aiTutorMock.Setup(a => a.AnalyzeGrammarAsync(It.IsAny<GrammarAnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GrammarAnalysisResponse(
                IsCorrect: true,
                OriginalText: "Sample essay",
                CorrectedText: "Sample essay",
                Explanation: "Good coherence and grammar.",
                Tips: "Advanced vocabulary detected."
            ));

        services.AddScoped(_ => aiTutorMock.Object);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new MezonBotOptions
        {
            BotId = "123456",
            Token = "test-token",
            Enabled = false
        });

        var loggerMock = new Mock<ILogger<MezonBotWorker>>();
        var essayCoach = new AiEssayCoachService(new NullLogger<AiEssayCoachService>(), scopeFactory: scopeFactory);
        var worker = new MezonBotWorker(loggerMock.Object, options, scopeFactory, essayCoachService: essayCoach);

        long channelId = 8888;
        long clanId = 9999;
        long senderId = 2222;
        var fakeData = default(ChannelMessageResponse);

        var essayText = "Learning English opens up many global career opportunities. It helps students connect internationally.";

        // Act: Go /grade kem noi dung
        await worker.HandleEssayCommandAsync(channelId, clanId, senderId, essayText, isDm: false, fakeData);

        // Assert: Ket qua phai gui Ephemeral rieng tu voi 4 tieu chi AI chuan va mau Emerald
        worker.SentEphemeralContents.Should().Contain(c =>
            c.ChannelId == channelId &&
            c.UserId == senderId &&
            c.Content.ToJson().Contains("#10B981") &&
            c.Content.ToJson().Contains("Overall Band") &&
            c.Content.ToJson().Contains("Task Achievement") &&
            c.Content.ToJson().Contains("Lexical Resource") &&
            c.Content.ToJson().Contains("Grammatical Range") &&
            c.Content.ToJson().Contains("Native Suggestions"));
    }

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
        json.Should().Contain("essay_text_input", "Card phai chua id cua input component");
        json.Should().Contain("btn_submit_essay", "Card phai chua nut bam nop bai");
        json.Should().Contain("textarea", "Input component phai la loai textarea");
        json.Should().Contain("AI ESSAY COACH", "Card phai co tieu de AI Essay Coach");
        json.Should().Contain("0 XP Awarded", "Card phai the hien quy dinh khong cong XP cho bai viet");
    }

    #endregion

    #region Interactive Button Handlers Tests (F3 Fixes)

    [Fact]
    public async Task OnMessageButtonClickedAsync_SubmitEssayButton_ExtractsTextAndSendsEphemeralResult()
    {
        // Arrange - DoD 3: Bam btn_submit_essay voi noi dung form -> goi EvaluateEssayAsync va tra ephemeral
        var services = new ServiceCollection();
        var aiTutorMock = new Mock<IAITutorService>();
        aiTutorMock.Setup(a => a.AnalyzeGrammarAsync(It.IsAny<GrammarAnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GrammarAnalysisResponse(
                IsCorrect: true,
                OriginalText: "English improves future prospects.",
                CorrectedText: "English improves future prospects.",
                Explanation: "Clear grammar.",
                Tips: "Great word choice."
            ));

        services.AddScoped(_ => aiTutorMock.Object);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new MezonBotOptions
        {
            BotId = "123456",
            Token = "test-token",
            Enabled = false
        });

        var loggerMock = new Mock<ILogger<MezonBotWorker>>();
        var essayCoach = new AiEssayCoachService(new NullLogger<AiEssayCoachService>(), scopeFactory: scopeFactory);
        var worker = new MezonBotWorker(loggerMock.Object, options, scopeFactory, essayCoachService: essayCoach);

        long channelId = 7181;
        long userId = 9181;
        long senderId = 9181;
        var extraData = "{\"essay_text_input\":\"English improves future prospects and global communication for students.\"}";

        var btnEvent = CreateMockButtonClickedResponse(channelId, userId, senderId, "btn_submit_essay", extraData);

        // Act - Nguoi dung click nut nop bai tren form
        await worker.OnMessageButtonClickedAsync(btnEvent);

        // Assert - Bot phai tra ket qua danh gia qua Ephemeral cho dung userId
        worker.SentEphemeralContents.Should().Contain(c =>
            c.ChannelId == channelId &&
            c.UserId == userId &&
            c.Content.ToJson().Contains("AI ESSAY COACH") &&
            c.Content.ToJson().Contains("Overall Band"));
    }

    [Fact]
    public async Task OnMessageButtonClickedAsync_SubmitEssayButton_WhenEmptyText_SendsEphemeralWarning()
    {
        // Arrange - DoD 3: Bam btn_submit_essay khong co noi dung -> tra ephemeral canh bao
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new MezonBotOptions { BotId = "123", Token = "fake", Enabled = false });
        var worker = new MezonBotWorker(new NullLogger<MezonBotWorker>(), options, scopeFactory);

        long channelId = 7182;
        long userId = 9182;
        var btnEvent = CreateMockButtonClickedResponse(channelId, userId, userId, "btn_submit_essay", "");

        // Act
        await worker.OnMessageButtonClickedAsync(btnEvent);

        // Assert - Gui ephemeral thong bao nhap bai
        worker.SentEphemeralMessages.Should().Contain(m =>
            m.ChannelId == channelId &&
            m.UserId == userId &&
            m.Text.Length > 0);
    }

    [Fact]
    public async Task OnMessageButtonClickedAsync_VoiceRefreshButton_RerendersVoiceRoomEmbed()
    {
        // Arrange - DoD 3: Bam btn_voice_refresh -> render lai embed phong voice
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new MezonBotOptions { BotId = "123", Token = "fake", Enabled = false });
        var worker = new MezonBotWorker(new NullLogger<MezonBotWorker>(), options, scopeFactory);

        long channelId = 3383;
        long userId = 5583;
        var btnEvent = CreateMockButtonClickedResponse(channelId, userId, userId, "btn_voice_refresh", "");

        // Act
        await worker.OnMessageButtonClickedAsync(btnEvent);

        // Assert - Phai gui embed phong voice cap nhat vao channel
        worker.SentChannelContents.Should().Contain(m =>
            m.ChannelId == channelId &&
            m.Content.ToJson().Contains("VOICE STUDY ROOM") &&
            m.Content.ToJson().Contains("btn_voice_refresh"));
    }

    #endregion
}
