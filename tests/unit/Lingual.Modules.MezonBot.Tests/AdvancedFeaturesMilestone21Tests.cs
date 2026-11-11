using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lingual.Api.Controllers;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Lingual.Modules.Gamification.Services;
using Lingual.Modules.Learning.Infrastructure.Data;
using Lingual.Modules.MezonBot;
using Lingual.Modules.MezonBot.Infrastructure.Data;
using Lingual.Modules.MezonBot.Services;
using Mezon.Net.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lingual.Modules.MezonBot.Tests;

public class AdvancedFeaturesMilestone21Tests
{
    #region ADV-01: Interactive Dropdown Select Menu Tests

    [Theory]
    [InlineData("/vocab")]
    [InlineData("/VOCAB")]
    [InlineData("/vocab deck")]
    [InlineData("  /vocab  ")]
    [InlineData("/deck")]
    [InlineData("/DECK")]
    public void ResolveCommandType_VocabAndDeck_ReturnsVocab(string input)
    {
        // Act
        var result = MezonBotWorker.ResolveCommandType(input);

        // Assert
        result.Should().Be("vocab");
    }

    [Theory]
    [InlineData("/poll")]
    [InlineData("/POLL")]
    [InlineData("/poll quiz")]
    [InlineData("/poll vote")]
    [InlineData("  /poll  ")]
    public void ResolveCommandType_Poll_ReturnsPoll(string input)
    {
        // Act
        var result = MezonBotWorker.ResolveCommandType(input);

        // Assert
        result.Should().Be("poll");
    }

    [Fact]
    public void BuildRoleplayScenarioSelectContent_CreatesValidDropdownSelectComponent()
    {
        // Act
        var content = MezonBotWorker.BuildRoleplayScenarioSelectContent();

        // Assert
        content.Should().NotBeNull();
        content.Text.Should().Contain("Chọn tình huống đàm thoại thực tế");
        content.Components.Should().NotBeEmpty();

        var select = (content.Components ?? Array.Empty<MessageActionRow>())
            .SelectMany(row => row.Components ?? Array.Empty<MessageComponent>())
            .OfType<SelectMessageComponent>()
            .FirstOrDefault(s => s.Id == "select_roleplay_scenario");

        select.Should().NotBeNull();
        select!.SelectType.Should().Be(MessageSelectType.Text);
        select.Placeholder.Should().Be("Chọn tình huống giao tiếp...");
        select.MinOptions.Should().Be(1);
        select.MaxOptions.Should().Be(1);
        select.Options.Should().HaveCount(4);
        select.Options.Select(o => o.Value).Should().Contain(new[]
        {
            "cafe_order",
            "airport_checkin",
            "job_interview",
            "startup_pitch"
        });
    }

    [Fact]
    public void BuildVocabDeckSelectContent_CreatesValidDropdownSelectComponent()
    {
        // Act
        var content = MezonBotWorker.BuildVocabDeckSelectContent();

        // Assert
        content.Should().NotBeNull();
        content.Text.Should().Contain("Chọn bộ từ vựng bạn muốn luyện tập");
        content.Components.Should().NotBeEmpty();

        var select = (content.Components ?? Array.Empty<MessageActionRow>())
            .SelectMany(row => row.Components ?? Array.Empty<MessageComponent>())
            .OfType<SelectMessageComponent>()
            .FirstOrDefault(s => s.Id == "select_vocab_deck");

        select.Should().NotBeNull();
        select!.SelectType.Should().Be(MessageSelectType.Text);
        select.Placeholder.Should().Be("Chọn bộ từ vựng luyện tập...");
        select.MinOptions.Should().Be(1);
        select.MaxOptions.Should().Be(1);
        select.Options.Should().HaveCount(4);
        select.Options.Select(o => o.Value).Should().Contain(new[]
        {
            "oxford_3000",
            "business_english",
            "academic_wordlist",
            "daily_conversation"
        });
    }

    #endregion

    #region ADV-02: Native Clan Poll System Tests

    [Fact]
    public void BuildPollQuizMessage_CreatesNativePollWithCorrectOptionsAndSingleChoice()
    {
        // Act
        var content = MezonBotWorker.BuildPollQuizMessage("poll_vocab_meticulous");

        // Assert
        content.Should().NotBeNull();
        content.Poll.Should().NotBeNull();
        content.Poll!.Question.Should().Contain("Meticulous");
        content.Poll.Answers.Should().HaveCount(4);
        content.Poll.AllowMultipleAnswers.Should().BeFalse();
        content.Poll.ExpireTime.Should().BeGreaterThan(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    [Fact]
    public void BuildPollVoteMessage_CreatesNativeOpinionPollWithMultipleChoices()
    {
        // Act
        var content = MezonBotWorker.BuildPollVoteMessage();

        // Assert
        content.Should().NotBeNull();
        content.Poll.Should().NotBeNull();
        content.Poll!.Question.Should().Contain("BẦU CHỌN CLAN");
        content.Poll.Answers.Should().HaveCount(4);
        content.Poll.AllowMultipleAnswers.Should().BeTrue();
        content.Poll.ExpireTime.Should().BeGreaterThan(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    #endregion

    #region Ràng Buộc XP: Server-side Evaluation & Idempotency Tests

    private static (MezonBotDbContext botDb, LearningDbContext learnDb, GamificationDbContext gameDb) CreateInMemoryDbs()
    {
        var dbName = Guid.NewGuid().ToString();
        var botOptions = new DbContextOptionsBuilder<MezonBotDbContext>()
            .UseInMemoryDatabase("bot_" + dbName)
            .Options;
        var learnOptions = new DbContextOptionsBuilder<LearningDbContext>()
            .UseInMemoryDatabase("learn_" + dbName)
            .Options;
        var gameOptions = new DbContextOptionsBuilder<GamificationDbContext>()
            .UseInMemoryDatabase("game_" + dbName)
            .Options;

        return (
            new MezonBotDbContext(botOptions),
            new LearningDbContext(learnOptions),
            new GamificationDbContext(gameOptions)
        );
    }

    private static ContentController CreateControllerWithUser(
        Guid userId,
        Mock<IXpService> xpMock)
    {
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var sMock = new Mock<IUserStreakService>();
        var lMock = new Mock<ILeaderboardService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(
            botDb,
            learnDb,
            gameDb,
            sMock.Object,
            xpMock.Object,
            lMock.Object,
            loggerMock.Object
        );

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("sub", userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        return controller;
    }

    [Fact]
    public async Task SubmitPollAnswer_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var sMock = new Mock<IUserStreakService>();
        var xMock = new Mock<IXpService>();
        var lMock = new Mock<ILeaderboardService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(
            botDb, learnDb, gameDb, sMock.Object, xMock.Object, lMock.Object, loggerMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } // No identity/claims
        };

        var request = new PollAnswerSubmitRequest("poll_vocab_meticulous", 1);

        // Act
        var result = await controller.SubmitPollAnswer(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task SubmitPollAnswer_WrongAnswer_AwardsZeroXp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var xpMock = new Mock<IXpService>();
        var controller = CreateControllerWithUser(userId, xpMock);

        // Đáp án đúng của poll_vocab_meticulous là index 1, người dùng chọn index 0 (sai)
        var request = new PollAnswerSubmitRequest("poll_vocab_meticulous", 0);

        // Act
        var result = await controller.SubmitPollAnswer(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        // Server KHÔNG gọi AwardXpAsync khi đáp án sai
        xpMock.Verify(x => x.AwardXpAsync(It.IsAny<AwardXpRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitPollAnswer_CorrectAnswer_AwardsExactly10XpServerSide()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var xpMock = new Mock<IXpService>();

        AwardXpRequest? capturedRequest = null;
        xpMock.Setup(x => x.AwardXpAsync(It.IsAny<AwardXpRequest>(), It.IsAny<CancellationToken>()))
              .Callback<AwardXpRequest, CancellationToken>((r, _) => capturedRequest = r)
              .ReturnsAsync(new AwardXpResult(
                  IsSuccess: true,
                  AwardedXp: 10,
                  TotalXp: 50,
                  CurrentStreak: 3,
                  IsStreakActiveToday: true,
                  AlreadyAwarded: false,
                  Message: "Thành công"));

        var controller = CreateControllerWithUser(userId, xpMock);

        // Đáp án đúng của poll_vocab_meticulous là index 1
        var request = new PollAnswerSubmitRequest("poll_vocab_meticulous", 1);

        // Act
        var result = await controller.SubmitPollAnswer(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        // Kiểm tra request gửi sang IXpService
        capturedRequest.Should().NotBeNull();
        capturedRequest!.UserId.Should().Be(userId, "UserId phải lấy từ JWT Claims, không từ client");
        capturedRequest.XpDelta.Should().Be(10, "Ràng buộc XP: đúng câu hỏi nhận chính xác 10 XP");
        capturedRequest.SourceType.Should().Be("clan_poll_quiz");
        capturedRequest.IdempotencyKey.Should().Contain(userId.ToString());
        capturedRequest.IdempotencyKey.Should().Contain("poll_vocab_meticulous");
    }

    [Fact]
    public async Task SubmitPollAnswer_Idempotent_PreventsDuplicateXpOnRepeatedVotes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var xpMock = new Mock<IXpService>();

        // Lần gọi thứ hai: XP Service báo đã được nhận trước đó (Idempotent)
        xpMock.Setup(x => x.AwardXpAsync(It.IsAny<AwardXpRequest>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new AwardXpResult(
                  IsSuccess: true,
                  AwardedXp: 0,
                  TotalXp: 50,
                  CurrentStreak: 3,
                  IsStreakActiveToday: true,
                  AlreadyAwarded: true,
                  Message: "Yêu cầu đã được ghi nhận trước đó (Idempotent)."));

        var controller = CreateControllerWithUser(userId, xpMock);
        var request = new PollAnswerSubmitRequest("poll_vocab_meticulous", 1);

        // Act
        var result = await controller.SubmitPollAnswer(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var json = System.Text.Json.JsonSerializer.Serialize(okResult!.Value);
        json.Should().Contain("\"alreadyAwarded\":true");
        json.Should().Contain("\"xpAwarded\":0");
    }

    #region Fix N1 & F1 Tests: Poll Answer Validation and Automated Settlement

    [Fact]
    public async Task SubmitPollAnswer_UnknownPollId_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var xpMock = new Mock<IXpService>();
        var controller = CreateControllerWithUser(userId, xpMock);

        var unknownPollRequest = new PollAnswerSubmitRequest("poll_non_existent_999", 1);

        // Act
        var result = await controller.SubmitPollAnswer(unknownPollRequest);

        // Assert: N1 - Bắt buộc trả về NotFound khi PollId không tồn tại, tuyệt đối không fallback ngầm
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();

        var json = System.Text.Json.JsonSerializer.Serialize(notFoundResult!.Value);
        json.Should().Contain("poll_non_existent_999");
        xpMock.Verify(x => x.AwardXpAsync(It.IsAny<AwardXpRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mezon.Net.Models.GetPollResponse BuildMockPollResponse(
        long pollId,
        long correctVoterUserId,
        long wrongVoterUserId)
    {
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var voterCtor = typeof(Mezon.Net.Models.PollVoterDetailResponse).GetConstructors(flags)[0];

        // Voter 1: Chọn đáp án đúng (AnswerIndex = 1)
        var rawVoter1 = new Mezon.Net.Internal.Api.PollVoterDetail { AnswerIndex = 1 };
        rawVoter1.UserIds.Add(correctVoterUserId);
        var voter1 = (Mezon.Net.Models.PollVoterDetailResponse)voterCtor.Invoke(new object[] { rawVoter1 });

        // Voter 2: Chọn đáp án sai (AnswerIndex = 0)
        var rawVoter2 = new Mezon.Net.Internal.Api.PollVoterDetail { AnswerIndex = 0 };
        rawVoter2.UserIds.Add(wrongVoterUserId);
        var voter2 = (Mezon.Net.Models.PollVoterDetailResponse)voterCtor.Invoke(new object[] { rawVoter2 });

        var rawPoll = new Mezon.Net.Internal.Api.GetPollResponse
        {
            PollId = pollId,
            Question = "📚 [ĐỐ VUI CLAN] Từ đồng nghĩa của 'Meticulous' là gì?",
            TotalVotes = 2,
            IsClosed = true
        };
        rawPoll.VoterDetails.Add(rawVoter1);
        rawPoll.VoterDetails.Add(rawVoter2);

        var pollCtor = typeof(Mezon.Net.Models.GetPollResponse).GetConstructors(flags)[0];
        return (Mezon.Net.Models.GetPollResponse)pollCtor.Invoke(new object[] { rawPoll });
    }

    [Fact]
    public async Task SettleQuizPollAsync_Awards10XpToCorrectVoter_AndZeroToWrongVoter()
    {
        // Arrange
        var services = new ServiceCollection();
        var userResolverMock = new Mock<IMezonUserResolver>();
        var gamificationServiceMock = new Mock<IGamificationService>();

        long correctPlatformUserId = 111222333L;
        long wrongPlatformUserId = 444555666L;

        var correctInternalId = Guid.NewGuid();
        var wrongInternalId = Guid.NewGuid();

        userResolverMock.Setup(u => u.ResolveUserIdAsync(correctPlatformUserId.ToString(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(correctInternalId);
        userResolverMock.Setup(u => u.ResolveUserIdAsync(wrongPlatformUserId.ToString(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(wrongInternalId);

        AwardXpRequest? capturedAwardRequest = null;
        gamificationServiceMock.Setup(g => g.AwardXpAsync(It.IsAny<AwardXpRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AwardXpRequest, CancellationToken>((req, _) => capturedAwardRequest = req)
            .ReturnsAsync(new AwardXpResult(
                IsSuccess: true,
                AwardedXp: 10,
                TotalXp: 100,
                CurrentStreak: 5,
                IsStreakActiveToday: true,
                AlreadyAwarded: false,
                Message: "Thành công"));

        services.AddSingleton(userResolverMock.Object);
        services.AddSingleton(gamificationServiceMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Microsoft.Extensions.Options.Options.Create(new MezonBotOptions
        {
            BotId = "1234567890",
            Token = "fake-token",
            Enabled = false
        });

        var worker = new MezonBotWorker(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MezonBotWorker>.Instance,
            options,
            scopeFactory);

        var pollSession = new MezonBotWorker.ActiveQuizPollSession(
            pollKey: "poll_vocab_meticulous",
            channelId: 998877L,
            messageId: 554433L,
            correctIndex: 1,
            pollId: 123456L);

        var mockGetPollResponse = BuildMockPollResponse(123456L, correctPlatformUserId, wrongPlatformUserId);

        // Act
        var result = await worker.SettleQuizPollAsync(pollSession, mockGetPollResponse);

        // Assert
        result.Should().NotBeNull();
        result.AlreadySettled.Should().BeFalse();
        result.TotalVoters.Should().Be(2);
        result.CorrectVotersCount.Should().Be(1);
        result.WrongVotersCount.Should().Be(1);
        result.TotalXpAwarded.Should().Be(10);
        result.AwardedUserIds.Should().Contain(correctPlatformUserId.ToString());
        result.AwardedUserIds.Should().NotContain(wrongPlatformUserId.ToString());

        // Kiểm tra đúng voter nhận +10 XP với SourceType và IdempotencyKey chuẩn
        capturedAwardRequest.Should().NotBeNull();
        capturedAwardRequest!.UserId.Should().Be(correctInternalId);
        capturedAwardRequest.XpDelta.Should().Be(10);
        capturedAwardRequest.SourceType.Should().Be("clan_poll_quiz");
        capturedAwardRequest.IdempotencyKey.Should().Be($"clan_poll_poll_vocab_meticulous_{correctInternalId}");

        // GamificationService chỉ được gọi duy nhất 1 lần (cho người đúng, người sai không gọi)
        gamificationServiceMock.Verify(g => g.AwardXpAsync(It.IsAny<AwardXpRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SettleQuizPollAsync_RepeatedRun_IsIdempotent_ReturnsZeroAdditionalXp()
    {
        // Arrange
        var services = new ServiceCollection();
        var userResolverMock = new Mock<IMezonUserResolver>();
        var gamificationServiceMock = new Mock<IGamificationService>();

        long correctPlatformUserId = 888999111L;
        var correctInternalId = Guid.NewGuid();

        userResolverMock.Setup(u => u.ResolveUserIdAsync(correctPlatformUserId.ToString(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(correctInternalId);

        gamificationServiceMock.Setup(g => g.AwardXpAsync(It.IsAny<AwardXpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AwardXpResult(true, 10, 100, 5, true, false, "Thành công"));

        services.AddSingleton(userResolverMock.Object);
        services.AddSingleton(gamificationServiceMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Microsoft.Extensions.Options.Options.Create(new MezonBotOptions
        {
            BotId = "1234567890",
            Token = "fake-token",
            Enabled = false
        });

        var worker = new MezonBotWorker(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MezonBotWorker>.Instance,
            options,
            scopeFactory);

        var pollSession = new MezonBotWorker.ActiveQuizPollSession(
            pollKey: "poll_vocab_meticulous",
            channelId: 998877L,
            messageId: 554433L,
            correctIndex: 1,
            pollId: 123456L);

        var mockGetPollResponse = BuildMockPollResponse(123456L, correctPlatformUserId, 999999L);

        // Act 1: Chạy settlement lần đầu
        var firstResult = await worker.SettleQuizPollAsync(pollSession, mockGetPollResponse);
        firstResult.TotalXpAwarded.Should().Be(10);
        firstResult.AlreadySettled.Should().BeFalse();

        // Act 2: Chạy settlement lần thứ hai trên cùng phiên
        var secondResult = await worker.SettleQuizPollAsync(pollSession, mockGetPollResponse);

        // Assert 2: Idempotent - không được cộng thêm XP, báo AlreadySettled = true
        secondResult.Should().NotBeNull();
        secondResult.AlreadySettled.Should().BeTrue();
        secondResult.TotalXpAwarded.Should().Be(0);
        secondResult.AwardedUserIds.Should().BeEmpty();

        // Không gọi thêm AwardXpAsync ở lần chạy thứ hai
        gamificationServiceMock.Verify(g => g.AwardXpAsync(It.IsAny<AwardXpRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #endregion
}
