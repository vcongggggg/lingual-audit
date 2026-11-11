using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lingual.Api.Controllers;
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

    #endregion
}
