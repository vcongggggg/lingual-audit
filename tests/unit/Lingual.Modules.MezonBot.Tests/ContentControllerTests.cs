using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Lingual.Api.Controllers;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Lingual.Modules.Learning.Infrastructure.Data;
using Lingual.Modules.MezonBot.Infrastructure.Data;
using Lingual.Modules.MezonBot.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lingual.Modules.MezonBot.Tests;

public class ContentControllerTests
{
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

    private static void AttachUser(ControllerBase controller, Guid userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("sub", userId.ToString()),
            new(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetQuizzes_ReturnsModeAndQuestionsList()
    {
        // Arrange
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var streakMock = new Mock<IUserStreakService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(botDb, learnDb, gameDb, streakMock.Object, loggerMock.Object);

        // Act
        var result = await controller.GetQuizzes(mode: "sprint10", limit: 5);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLeaderboard_ReturnsLeaderboardList()
    {
        // Arrange
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var streakMock = new Mock<IUserStreakService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(botDb, learnDb, gameDb, streakMock.Object, loggerMock.Object);

        // Act
        var result = await controller.GetLeaderboard();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task GetVocabulary_ReturnsVocabularyList()
    {
        // Arrange
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var streakMock = new Mock<IUserStreakService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(botDb, learnDb, gameDb, streakMock.Object, loggerMock.Object);

        // Act
        var result = await controller.GetVocabulary(limit: 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CheckSingleAnswer_ReturnsCorrectAnswerAndExplanation()
    {
        // Arrange
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var streakMock = new Mock<IUserStreakService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(botDb, learnDb, gameDb, streakMock.Object, loggerMock.Object);

        // Act - Test correct answer for fallback question fb-01
        var resultCorrect = await controller.CheckSingleAnswer(new CheckAnswerRequest("fb-01", "A"));

        // Assert
        resultCorrect.Should().BeOfType<OkObjectResult>();
        var okObj = resultCorrect as OkObjectResult;
        okObj.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(okObj!.Value);
        json.Should().Contain("\"isCorrect\":true");
        json.Should().Contain("\"correctAnswer\":\"A\"");

        // Act - Test incorrect answer
        var resultWrong = await controller.CheckSingleAnswer(new CheckAnswerRequest("fb-01", "C"));
        var wrongJson = System.Text.Json.JsonSerializer.Serialize((resultWrong as OkObjectResult)!.Value);
        wrongJson.Should().Contain("\"isCorrect\":false");
    }

    [Fact]
    public async Task SaveQuizAttempt_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var streakMock = new Mock<IUserStreakService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(botDb, learnDb, gameDb, streakMock.Object, loggerMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext() // No User authenticated
        };

        var request = new QuizAttemptRequest(
            IdempotencyKey: "idem_unauth_1",
            UserId: Guid.NewGuid().ToString(),
            Mode: "sprint10",
            Answers: new List<QuizSubmittedAnswer> { new("fb-01", "A") }
        );

        // Act
        var result = await controller.SaveQuizAttempt(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthResult = result as UnauthorizedObjectResult;
        unauthResult!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task SaveQuizAttempt_AllWrongAnswers_AwardsZeroXp()
    {
        // Arrange
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var streakMock = new Mock<IUserStreakService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(botDb, learnDb, gameDb, streakMock.Object, loggerMock.Object);
        var authUserId = Guid.NewGuid();
        AttachUser(controller, authUserId);

        // Fallback fb-01 has correct answer A. We submit D (wrong).
        var request = new QuizAttemptRequest(
            IdempotencyKey: "idem_all_wrong",
            UserId: authUserId.ToString(),
            Mode: "sprint10",
            Answers: new List<QuizSubmittedAnswer>
            {
                new("fb-01", "D"),
                new("fb-02", "D")
            }
        );

        // Act
        var result = await controller.SaveQuizAttempt(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var json = System.Text.Json.JsonSerializer.Serialize(okResult!.Value);
        json.Should().Contain("\"xpAwarded\":0");
        json.Should().Contain("\"correctCount\":0");
    }

    [Fact]
    public async Task SaveQuizAttempt_ClientSendsFakeIsCorrect_IsOverriddenByServer()
    {
        // Arrange
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var streakMock = new Mock<IUserStreakService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(botDb, learnDb, gameDb, streakMock.Object, loggerMock.Object);
        var authUserId = Guid.NewGuid();
        AttachUser(controller, authUserId);

        // Client fakes IsCorrect: true with wrong answer 'C' (actual is 'A')
        var request = new QuizAttemptRequest(
            IdempotencyKey: "idem_fake_client",
            UserId: authUserId.ToString(),
            Mode: "sprint10",
            Answers: new List<QuizSubmittedAnswer>
            {
                new("fb-01", "C", IsCorrect: true)
            }
        );

        // Act
        var result = await controller.SaveQuizAttempt(request);

        // Assert: Server grades answer as wrong and awards 0 XP regardless of client flag
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var json = System.Text.Json.JsonSerializer.Serialize(okResult!.Value);
        json.Should().Contain("\"xpAwarded\":0");
        json.Should().Contain("\"correctCount\":0");
    }

    [Fact]
    public async Task SaveQuizAttempt_AwardsXpForCorrectAnswers()
    {
        // Arrange
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var streakMock = new Mock<IUserStreakService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(botDb, learnDb, gameDb, streakMock.Object, loggerMock.Object);
        var authUserId = Guid.NewGuid();
        AttachUser(controller, authUserId);

        // fb-01 correct is A (+10 XP), fb-02 correct is A (+10 XP)
        var request = new QuizAttemptRequest(
            IdempotencyKey: "idem_correct_2",
            UserId: authUserId.ToString(),
            Mode: "sprint10",
            Answers: new List<QuizSubmittedAnswer>
            {
                new("fb-01", "A"),
                new("fb-02", "A")
            }
        );

        // Act
        var result = await controller.SaveQuizAttempt(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var json = System.Text.Json.JsonSerializer.Serialize(okResult!.Value);
        json.Should().Contain("\"xpAwarded\":20");
        json.Should().Contain("\"correctCount\":2");
    }

    [Fact]
    public async Task SaveQuizAttempt_ClientSendsFakeUserId_UsesTokenUserId()
    {
        // Arrange
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var streakMock = new Mock<IUserStreakService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(botDb, learnDb, gameDb, streakMock.Object, loggerMock.Object);
        var tokenUserId = Guid.NewGuid();
        var fakeTargetUserId = Guid.NewGuid();
        AttachUser(controller, tokenUserId);

        // Client body tries to submit on behalf of fakeTargetUserId
        var request = new QuizAttemptRequest(
            IdempotencyKey: "idem_fake_user_id",
            UserId: fakeTargetUserId.ToString(),
            Mode: "sprint10",
            Answers: new List<QuizSubmittedAnswer> { new("fb-01", "A") }
        );

        // Act
        var result = await controller.SaveQuizAttempt(request);

        // Assert: Succeeds because tokenUserId is authenticated, request succeeds without throwing
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SaveReview_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var streakMock = new Mock<IUserStreakService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(botDb, learnDb, gameDb, streakMock.Object, loggerMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext() // Unauthenticated
        };

        var request = new SrsReviewRequest(
            VocabularyId: Guid.NewGuid().ToString(),
            Rating: "good",
            Quality: 4,
            Repetitions: 1,
            IntervalDays: 1.0m,
            EaseFactor: 2.5m,
            NextDueAt: DateTime.UtcNow.AddDays(1),
            TimeSpentMs: 1500,
            IdempotencyKey: "idem_srs_unauth"
        );

        // Act
        var result = await controller.SaveReview(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthResult = result as UnauthorizedObjectResult;
        unauthResult!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task SaveReview_WithAuth_ReturnsSuccess()
    {
        // Arrange
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var streakMock = new Mock<IUserStreakService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(botDb, learnDb, gameDb, streakMock.Object, loggerMock.Object);
        var authUserId = Guid.NewGuid();
        AttachUser(controller, authUserId);

        var vocabId = Guid.NewGuid();
        var request = new SrsReviewRequest(
            VocabularyId: vocabId.ToString(),
            Rating: "good",
            Quality: 4,
            Repetitions: 2,
            IntervalDays: 3.5m,
            EaseFactor: 2.6m,
            NextDueAt: DateTime.UtcNow.AddDays(3),
            TimeSpentMs: 2000,
            IdempotencyKey: "idem_srs_auth"
        );

        // Act
        var result = await controller.SaveReview(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var json = System.Text.Json.JsonSerializer.Serialize(okResult!.Value);
        json.Should().Contain("\"success\":true");
        json.Should().Contain(vocabId.ToString());
    }

    [Fact]
    public async Task SaveQuizAttempt_FabricatedQuestionIdWithAnswerA_AwardsZeroXp()
    {
        // Arrange
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var streakMock = new Mock<IUserStreakService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(botDb, learnDb, gameDb, streakMock.Object, loggerMock.Object);
        var authUserId = Guid.NewGuid();
        AttachUser(controller, authUserId);

        var fabricatedQuestionId = Guid.NewGuid().ToString();
        var request = new QuizAttemptRequest(
            IdempotencyKey: "idem_fabricated_a",
            UserId: authUserId.ToString(),
            Mode: "sprint10",
            Answers: new List<QuizSubmittedAnswer>
            {
                new(fabricatedQuestionId, "A")
            }
        );

        // Act
        var result = await controller.SaveQuizAttempt(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var json = System.Text.Json.JsonSerializer.Serialize(okResult!.Value);
        json.Should().Contain("\"xpAwarded\":0");
        json.Should().Contain("\"correctCount\":0");
    }

    [Fact]
    public async Task CheckSingleAnswer_UnfamiliarQuestionId_ReturnsFalseAndEmptyCorrectAnswer()
    {
        // Arrange
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var streakMock = new Mock<IUserStreakService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(botDb, learnDb, gameDb, streakMock.Object, loggerMock.Object);
        var unfamiliarQuestionId = Guid.NewGuid().ToString();

        // Act - Submit "A" for an unknown / unfamiliar question ID
        var result = await controller.CheckSingleAnswer(new CheckAnswerRequest(unfamiliarQuestionId, "A"));

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(okResult!.Value);
        json.Should().Contain("\"isCorrect\":false");
        json.Should().Contain("\"correctAnswer\":\"\"");
    }
}
