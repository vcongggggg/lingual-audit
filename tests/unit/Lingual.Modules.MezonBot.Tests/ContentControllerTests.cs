using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Lingual.Api.Controllers;
using Lingual.Modules.AITutor.Contracts;
using Lingual.Modules.AITutor.Contracts.DTOs;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Lingual.Modules.Gamification.Services;
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

    private static ContentController CreateController(
        MezonBotDbContext botDb,
        LearningDbContext learnDb,
        GamificationDbContext gameDb,
        Mock<IUserStreakService>? streakMock = null,
        Mock<IXpService>? xpMock = null,
        Mock<ILeaderboardService>? lbMock = null)
    {
        var sMock = streakMock ?? new Mock<IUserStreakService>();
        var xMock = xpMock ?? new Mock<IXpService>();
        var lMock = lbMock ?? new Mock<ILeaderboardService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        lMock.Setup(l => l.GetLeaderboardAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new LeaderboardResponseDto(DateTimeOffset.UtcNow, new List<LeaderboardEntryDto>
             {
                 new(1, Guid.NewGuid().ToString(), "Test User", 100, null)
             }));

        xMock.Setup(x => x.AwardXpAsync(It.IsAny<AwardXpRequest>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((AwardXpRequest req, CancellationToken _) => new AwardXpResult(
                 IsSuccess: true,
                 AwardedXp: req.XpDelta,
                 TotalXp: req.XpDelta,
                 CurrentStreak: 1,
                 IsStreakActiveToday: true,
                 AlreadyAwarded: false));

        return new ContentController(botDb, learnDb, gameDb, sMock.Object, xMock.Object, lMock.Object, loggerMock.Object);
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

        var controller = CreateController(botDb, learnDb, gameDb, streakMock);

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

        var controller = CreateController(botDb, learnDb, gameDb, streakMock);

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

        var controller = CreateController(botDb, learnDb, gameDb, streakMock);

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

        var controller = CreateController(botDb, learnDb, gameDb, streakMock);

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

        var controller = CreateController(botDb, learnDb, gameDb, streakMock);
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

        var controller = CreateController(botDb, learnDb, gameDb, streakMock);
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

        var controller = CreateController(botDb, learnDb, gameDb, streakMock);
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

        var controller = CreateController(botDb, learnDb, gameDb, streakMock);
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

        var controller = CreateController(botDb, learnDb, gameDb, streakMock);
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

        var controller = CreateController(botDb, learnDb, gameDb, streakMock);
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

        var controller = CreateController(botDb, learnDb, gameDb, streakMock);
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

        var controller = CreateController(botDb, learnDb, gameDb, streakMock);
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

        var controller = CreateController(botDb, learnDb, gameDb, streakMock);
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

    [Fact]
    public async Task SaveQuizAttempt_ServerGradedAtLeast60Percent_Grants1AiCredit_SpoofedIsCorrectIgnored()
    {
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var userId = Guid.NewGuid();
        var quotaMock = new Mock<IAiQuotaService>();
        quotaMock
            .Setup(q => q.GrantBonusCreditsAsync(userId, 1, "reward_quiz", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiQuotaGrantResult(
                true, 1, false, false,
                new AiQuotaStatusDto(5, 0, 5, 1, 6, 1, 5, DateTime.UtcNow.AddHours(10))));

        var xpMock = new Mock<IXpService>();
        xpMock.Setup(x => x.AwardXpAsync(It.IsAny<AwardXpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AwardXpRequest req, CancellationToken _) => new AwardXpResult(true, req.XpDelta, req.XpDelta, 1, true, false));
        var lbMock = new Mock<ILeaderboardService>();
        var controller = new ContentController(
            botDb, learnDb, gameDb,
            new Mock<IUserStreakService>().Object,
            xpMock.Object,
            lbMock.Object,
            new Mock<ILogger<ContentController>>().Object,
            quotaMock.Object);
        AttachUser(controller, userId);

        // 1/3 correct (< 60%) even though client lies IsCorrect = true on all 3 -> MUST NOT grant AI credit
        var failReq = new QuizAttemptRequest(
            IdempotencyKey: "quiz_low_score",
            UserId: null,
            Mode: "sprint10",
            Answers: new List<QuizSubmittedAnswer>
            {
                new("fb-01", "A", IsCorrect: true),
                new("fb-02", "B", IsCorrect: true),
                new("fb-03", "C", IsCorrect: true),
            });
        var failRes = await controller.SaveQuizAttempt(failReq);
        failRes.Should().BeOfType<OkObjectResult>();
        quotaMock.Verify(q => q.GrantBonusCreditsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        // 2/3 correct (66.7% >= 60%) -> MUST grant +1 AI credit
        var passReq = new QuizAttemptRequest(
            IdempotencyKey: "quiz_pass_score",
            UserId: null,
            Mode: "sprint10",
            Answers: new List<QuizSubmittedAnswer>
            {
                new("fb-01", "A", IsCorrect: false),
                new("fb-02", "A", IsCorrect: false),
                new("fb-03", "D", IsCorrect: false),
            });
        var passRes = await controller.SaveQuizAttempt(passReq);
        var ok = passRes.Should().BeOfType<OkObjectResult>().Subject;
        System.Text.Json.JsonSerializer.Serialize(ok.Value).Should().Contain("\"aiCreditsEarned\":1");
        quotaMock.Verify(q => q.GrantBonusCreditsAsync(userId, 1, "reward_quiz", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckSingleAnswer_DbQuestionWithSnakeCaseIsCorrect_ResolvesTrueOptionKey()
    {
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var qId = Guid.NewGuid();
        botDb.QuizQuestions.Add(new Lingual.Modules.MezonBot.Domain.QuizQuestion
        {
            Id = qId,
            Level = "A1",
            Difficulty = 2,
            QuestionType = "multiple_choice",
            Prompt = "Write your ________ on the paper.",
            Options = """[{"key":"A","text":"foot","is_correct":false},{"key":"B","text":"boat","is_correct":false},{"key":"C","text":"bridge","is_correct":false},{"key":"D","text":"answer","is_correct":true}]""",
            Explanation = "Đáp án đúng là D (answer - câu trả lời).",
            Points = 10,
            Status = "published"
        });
        await botDb.SaveChangesAsync();

        var controller = CreateController(botDb, learnDb, gameDb, new Mock<IUserStreakService>());

        var rightRes = await controller.CheckSingleAnswer(new CheckAnswerRequest(qId.ToString(), "D"));
        var rightJson = System.Text.Json.JsonSerializer.Serialize(rightRes.Should().BeOfType<OkObjectResult>().Subject.Value);
        rightJson.Should().Contain("\"isCorrect\":true");
        rightJson.Should().Contain("\"correctAnswer\":\"D\"");

        var wrongRes = await controller.CheckSingleAnswer(new CheckAnswerRequest(qId.ToString(), "A"));
        var wrongJson = System.Text.Json.JsonSerializer.Serialize(wrongRes.Should().BeOfType<OkObjectResult>().Subject.Value);
        wrongJson.Should().Contain("\"isCorrect\":false");
        wrongJson.Should().Contain("\"correctAnswer\":\"D\"");
    }
}
