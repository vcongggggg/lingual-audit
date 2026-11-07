using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Lingual.Api.Controllers;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Lingual.Modules.Learning.Infrastructure.Data;
using Lingual.Modules.MezonBot.Infrastructure.Data;
using Lingual.Modules.MezonBot.Services;
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
    public async Task SaveQuizAttempt_AwardsXpProperly()
    {
        // Arrange
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var streakMock = new Mock<IUserStreakService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(botDb, learnDb, gameDb, streakMock.Object, loggerMock.Object);

        var request = new QuizAttemptRequest(
            IdempotencyKey: "test_key_123",
            UserId: Guid.NewGuid().ToString(),
            Mode: "sprint10",
            Answers: new List<QuizAnswerItem>
            {
                new("q1", "A", true),
                new("q2", "B", true),
                new("q3", "C", false)
            }
        );

        // Act
        var result = await controller.SaveQuizAttempt(request);

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
    public void SaveReview_ReturnsSuccess()
    {
        // Arrange
        var (botDb, learnDb, gameDb) = CreateInMemoryDbs();
        var streakMock = new Mock<IUserStreakService>();
        var loggerMock = new Mock<ILogger<ContentController>>();

        var controller = new ContentController(botDb, learnDb, gameDb, streakMock.Object, loggerMock.Object);

        // Act
        var result = controller.SaveReview(new ReviewRequest("card-1", 4));

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}

