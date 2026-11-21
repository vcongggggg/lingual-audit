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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lingual.Modules.Gamification.Tests;

public class GameServiceTests
{
    private static GamificationDbContext CreateInMemoryDb()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<GamificationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new GamificationDbContext(options);
    }

    [Fact]
    public async Task GetGameDataAsync_InvalidType_ThrowsArgumentException()
    {
        var db = CreateInMemoryDb();
        var gMock = new Mock<IGamificationService>();
        var lMock = new Mock<ILogger<GameService>>();
        var service = new GameService(db, gMock.Object, lMock.Object);

        var act = async () => await service.GetGameDataAsync("invalid_type", null, Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*không hợp lệ*");
    }

    [Fact]
    public async Task GetGameDataAsync_WordMatch_CreatesAttemptAndMasksData()
    {
        var db = CreateInMemoryDb();
        db.GameWordPools.AddRange(
            new GameWordPool { Id = "w1", TargetText = "Cat", Translation = "Con mèo", Topic = "animals", CefrLevel = "A1" },
            new GameWordPool { Id = "w2", TargetText = "Dog", Translation = "Con chó", Topic = "animals", CefrLevel = "A1" },
            new GameWordPool { Id = "w3", TargetText = "Bird", Translation = "Con chim", Topic = "animals", CefrLevel = "A1" },
            new GameWordPool { Id = "w4", TargetText = "Fish", Translation = "Con cá", Topic = "animals", CefrLevel = "A1" }
        );
        await db.SaveChangesAsync();

        var gMock = new Mock<IGamificationService>();
        var lMock = new Mock<ILogger<GameService>>();
        var service = new GameService(db, gMock.Object, lMock.Object);

        var userId = Guid.NewGuid();
        var result = await service.GetGameDataAsync("word_match", "A1", userId, CancellationToken.None);

        result.Should().NotBeNull();
        result.GameType.Should().Be("word_match");
        result.AttemptId.Should().NotBeNullOrWhiteSpace();

        // Verify active attempt was stored in database
        var savedAttempt = await db.ActiveGameAttempts.FirstOrDefaultAsync(a => a.AttemptId == result.AttemptId);
        savedAttempt.Should().NotBeNull();
        savedAttempt!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task SubmitGameAsync_SuspiciouslyFast_ThrowsAntiCheatException()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();

        // Attempt started right now (0 seconds elapsed)
        var attempt = new ActiveGameAttempt
        {
            AttemptId = "fast-001",
            UserId = userId,
            GameType = "word_match",
            StartedAt = DateTime.UtcNow,
            QuestionsPayloadJson = "[{\"id\":\"w1\",\"targetText\":\"Cat\",\"translation\":\"Con mèo\"}]"
        };
        db.ActiveGameAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var gMock = new Mock<IGamificationService>();
        var lMock = new Mock<ILogger<GameService>>();
        var service = new GameService(db, gMock.Object, lMock.Object);

        var request = new GameSubmitRequestDto
        {
            AttemptId = "fast-001",
            GameType = "word_match",
            TimeRemainingSeconds = 59,
            Answers = new List<GameAnswerSubmissionDto>
            {
                new() { QuestionId = "w1", Answer = "Con mèo" }
            }
        };

        var act = async () => await service.SubmitGameAsync(request, userId, "TestUser", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*quá nhanh*");
    }

    [Fact]
    public async Task SubmitGameAsync_ValidAttempt_CalculatesScoreAndAwardsXp()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();

        // Attempt started 10s ago
        var attempt = new ActiveGameAttempt
        {
            AttemptId = "valid-001",
            UserId = userId,
            GameType = "word_match",
            StartedAt = DateTime.UtcNow.AddSeconds(-10),
            QuestionsPayloadJson = "[{\"id\":\"w1\",\"targetText\":\"Cat\",\"translation\":\"Con mèo\"}]"
        };
        db.ActiveGameAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var gMock = new Mock<IGamificationService>();
        var lMock = new Mock<ILogger<GameService>>();
        var service = new GameService(db, gMock.Object, lMock.Object);

        var request = new GameSubmitRequestDto
        {
            AttemptId = "valid-001",
            GameType = "word_match",
            TimeRemainingSeconds = 50,
            Answers = new List<GameAnswerSubmissionDto>
            {
                new() { QuestionId = "w1", Answer = "Con mèo" }
            }
        };

        var result = await service.SubmitGameAsync(request, userId, "Nguyen Van A", CancellationToken.None);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Score.Should().BeGreaterThan(0);
        result.XpEarned.Should().BeGreaterThan(0);
        result.CorrectCount.Should().Be(1);
        result.Accuracy.Should().Be(1.0);
        result.IsNewHighScore.Should().BeTrue();

        // Verify session saved
        var session = await db.GameSessions.FirstOrDefaultAsync(s => s.AttemptId == "valid-001");
        session.Should().NotBeNull();
        session!.UserId.Should().Be(userId);

        // Verify user stat updated and DisplayName persisted
        var stat = await db.UserGameStats.FirstOrDefaultAsync(s => s.UserId == userId && s.GameType == "word_match");
        stat.Should().NotBeNull();
        stat!.DisplayName.Should().Be("Nguyen Van A");
        stat.TotalGamesPlayed.Should().Be(1);
        stat.HighScore.Should().Be(result.Score);

        // Verify attempt cleaned up
        var cleaned = await db.ActiveGameAttempts.FirstOrDefaultAsync(a => a.AttemptId == "valid-001");
        cleaned.Should().BeNull();

        // Verify AwardXpAsync was invoked
        gMock.Verify(g => g.AwardXpAsync(It.Is<AwardXpRequest>(r => r.UserId == userId && r.XpDelta > 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitGameAsync_Wordle_Won_Awards50Points()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();

        var attempt = new ActiveGameAttempt
        {
            AttemptId = "wordle-001",
            UserId = userId,
            GameType = "lingo_wordle",
            StartedAt = DateTime.UtcNow.AddSeconds(-15),
            QuestionsPayloadJson = "{\"id\":\"q1\",\"secretWord\":\"APPLE\"}"
        };
        db.ActiveGameAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var gMock = new Mock<IGamificationService>();
        var lMock = new Mock<ILogger<GameService>>();
        var service = new GameService(db, gMock.Object, lMock.Object);

        var request = new GameSubmitRequestDto
        {
            AttemptId = "wordle-001",
            GameType = "lingo_wordle",
            TimeRemainingSeconds = 0,
            Answers = new List<GameAnswerSubmissionDto>
            {
                new() { QuestionId = "q1", Answer = "APPLE" }
            }
        };

        var result = await service.SubmitGameAsync(request, userId, "WordleMaster", CancellationToken.None);

        result.Score.Should().Be(50);
        result.XpEarned.Should().Be(12); // max(5, round(50 / 4)) = 12 (Banker's rounding)
        result.CorrectCount.Should().Be(1);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ReturnsSortedEntries()
    {
        var db = CreateInMemoryDb();
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();

        var stat1 = new UserGameStat(u1, "word_match", "Player One") { HighScore = 150, TotalXpEarned = 100 };
        var stat2 = new UserGameStat(u2, "word_match", "Player Two") { HighScore = 320, TotalXpEarned = 250 };
        db.UserGameStats.AddRange(stat1, stat2);
        await db.SaveChangesAsync();

        var gMock = new Mock<IGamificationService>();
        var lMock = new Mock<ILogger<GameService>>();
        var service = new GameService(db, gMock.Object, lMock.Object);

        var leaderboard = await service.GetLeaderboardAsync("word_match", 10, CancellationToken.None);

        leaderboard.Should().NotBeNull();
        leaderboard.Entries.Should().HaveCount(2);
        leaderboard.Entries[0].DisplayName.Should().Be("Player Two");
        leaderboard.Entries[0].HighScore.Should().Be(320);
        leaderboard.Entries[1].DisplayName.Should().Be("Player One");
        leaderboard.Entries[1].HighScore.Should().Be(150);
    }

    [Fact]
    public async Task GetUserStatsAsync_AggregatesAccurately()
    {
        var db = CreateInMemoryDb();
        var u = Guid.NewGuid();

        var stat1 = new UserGameStat(u, "word_match", "MySelf") { HighScore = 200, TotalGamesPlayed = 3, TotalWins = 2, TotalXpEarned = 120, AvgAccuracy = 0.85 };
        var stat2 = new UserGameStat(u, "typing_race", "MySelf") { HighScore = 450, TotalGamesPlayed = 5, TotalWins = 4, TotalXpEarned = 300, AvgAccuracy = 0.92 };
        db.UserGameStats.AddRange(stat1, stat2);
        await db.SaveChangesAsync();

        var gMock = new Mock<IGamificationService>();
        var lMock = new Mock<ILogger<GameService>>();
        var service = new GameService(db, gMock.Object, lMock.Object);

        var stats = await service.GetUserStatsAsync(u, CancellationToken.None);

        stats.Should().NotBeNull();
        stats.TotalGamesPlayed.Should().Be(8);
        stats.TotalWins.Should().Be(6);
        stats.TotalXpEarned.Should().Be(420);
        stats.ByGame.Should().ContainKey("word_match");
        stats.ByGame.Should().ContainKey("typing_race");
    }

    [Fact]
    public async Task SubmitGameAsync_ClientClaims60Remaining_CalculatesBonusBasedOnRealRemainingDuration()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();

        // Physical duration is 50 seconds (StartedAt 50s ago)
        var attempt = new ActiveGameAttempt
        {
            AttemptId = "time-bonus-001",
            UserId = userId,
            GameType = "word_match",
            StartedAt = DateTime.UtcNow.AddSeconds(-50),
            QuestionsPayloadJson = "[{\"id\":\"w1\",\"targetText\":\"Cat\",\"translation\":\"Con mèo\"}]"
        };
        db.ActiveGameAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var gMock = new Mock<IGamificationService>();
        var lMock = new Mock<ILogger<GameService>>();
        var service = new GameService(db, gMock.Object, lMock.Object);

        // Client falsely claims 60 seconds remaining
        var request = new GameSubmitRequestDto
        {
            AttemptId = "time-bonus-001",
            GameType = "word_match",
            TimeRemainingSeconds = 60,
            Answers = new List<GameAnswerSubmissionDto>
            {
                new() { QuestionId = "w1", Answer = "Con mèo" }
            }
        };

        var result = await service.SubmitGameAsync(request, userId, "HonestServer", CancellationToken.None);

        // Real remaining is 60 - 50 = 10s -> timeBonus is floor(10 * 0.8) = 8, NOT floor(60 * 0.8) = 48!
        result.TimeBonus.Should().Be(8);
    }

    [Fact]
    public async Task SubmitGameAsync_DurationExceeds75Seconds_ThrowsTimeoutException()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();

        // Physical duration is 76 seconds (exceeds 60s + 15s grace)
        var attempt = new ActiveGameAttempt
        {
            AttemptId = "timeout-001",
            UserId = userId,
            GameType = "word_match",
            StartedAt = DateTime.UtcNow.AddSeconds(-76),
            QuestionsPayloadJson = "[{\"id\":\"w1\",\"targetText\":\"Cat\",\"translation\":\"Con mèo\"}]"
        };
        db.ActiveGameAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var gMock = new Mock<IGamificationService>();
        var lMock = new Mock<ILogger<GameService>>();
        var service = new GameService(db, gMock.Object, lMock.Object);

        var request = new GameSubmitRequestDto
        {
            AttemptId = "timeout-001",
            GameType = "word_match",
            TimeRemainingSeconds = 0,
            Answers = new List<GameAnswerSubmissionDto>
            {
                new() { QuestionId = "w1", Answer = "Con mèo" }
            }
        };

        var act = async () => await service.SubmitGameAsync(request, userId, "SlowPlayer", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Hết giờ*");
    }

    [Fact]
    public async Task EvaluateWordleGuessAsync_ExactMatch_MarksAllCorrectAndWins()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();

        var attempt = new ActiveGameAttempt
        {
            AttemptId = "wordle-eval-001",
            UserId = userId,
            GameType = "lingo_wordle",
            StartedAt = DateTime.UtcNow,
            QuestionsPayloadJson = "{\"secretWord\":\"SMART\",\"guesses\":[]}"
        };
        db.ActiveGameAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var gMock = new Mock<IGamificationService>();
        var lMock = new Mock<ILogger<GameService>>();
        var service = new GameService(db, gMock.Object, lMock.Object);

        var request = new WordleGuessRequestDto("wordle-eval-001", "SMART");
        var result = await service.EvaluateWordleGuessAsync(request, userId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Evaluations.Should().Equal("correct", "correct", "correct", "correct", "correct");
        result.IsWon.Should().BeTrue();
        result.IsGameOver.Should().BeTrue();
        result.RemainingAttempts.Should().Be(5);
    }

    [Fact]
    public async Task EvaluateWordleGuessAsync_DuplicateLetters_EvaluatesCorrectFrequencies()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();

        // Secret: APPLE (A:1, P:2, L:1, E:1). Guess: PUPPY (P:3, U:1, Y:1).
        // Index 2 is 'P' == 'P' -> correct.
        // Index 0 is 'P' in target -> present (consumed 2nd P).
        // Index 3 is 'P' -> absent (no more P's left!).
        var attempt = new ActiveGameAttempt
        {
            AttemptId = "wordle-eval-002",
            UserId = userId,
            GameType = "lingo_wordle",
            StartedAt = DateTime.UtcNow,
            QuestionsPayloadJson = "{\"secretWord\":\"APPLE\",\"guesses\":[]}"
        };
        db.ActiveGameAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var gMock = new Mock<IGamificationService>();
        var lMock = new Mock<ILogger<GameService>>();
        var service = new GameService(db, gMock.Object, lMock.Object);

        var request = new WordleGuessRequestDto("wordle-eval-002", "PUPPY");
        var result = await service.EvaluateWordleGuessAsync(request, userId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Evaluations.Should().Equal("present", "absent", "correct", "absent", "absent");
        result.IsWon.Should().BeFalse();
        result.IsGameOver.Should().BeFalse();
        result.RemainingAttempts.Should().Be(5);
    }

    [Fact]
    public async Task EvaluateWordleGuessAsync_ExceedsMax6Guesses_ThrowsInvalidOperationException()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();

        var attempt = new ActiveGameAttempt
        {
            AttemptId = "wordle-eval-003",
            UserId = userId,
            GameType = "lingo_wordle",
            StartedAt = DateTime.UtcNow,
            QuestionsPayloadJson = "{\"secretWord\":\"SMART\",\"guesses\":[\"CRANE\",\"AUDIO\",\"LIGHT\",\"PLAZA\",\"GHOST\",\"FLUTE\"]}"
        };
        db.ActiveGameAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var gMock = new Mock<IGamificationService>();
        var lMock = new Mock<ILogger<GameService>>();
        var service = new GameService(db, gMock.Object, lMock.Object);

        var request = new WordleGuessRequestDto("wordle-eval-003", "BRAIN");
        var act = async () => await service.EvaluateWordleGuessAsync(request, userId, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*vượt quá*");
    }

    [Fact]
    public async Task GetGameDataAsync_EmptyWordPool_ThrowsKeyNotFoundException()
    {
        var db = CreateInMemoryDb();
        var gMock = new Mock<IGamificationService>();
        var lMock = new Mock<ILogger<GameService>>();
        var service = new GameService(db, gMock.Object, lMock.Object);

        var act = async () => await service.GetGameDataAsync("word_match", null, Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*từ vựng*");
    }

    [Fact]
    public async Task SubmitGameAsync_AwardedStamps_RecordedInSessionMetadataJson()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();

        var attempt = new ActiveGameAttempt
        {
            AttemptId = "stamp-001",
            UserId = userId,
            GameType = "word_match",
            StartedAt = DateTime.UtcNow.AddSeconds(-10),
            QuestionsPayloadJson = "[{\"id\":\"w1\",\"targetText\":\"Cat\",\"translation\":\"Con mèo\"}]"
        };
        db.ActiveGameAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var gMock = new Mock<IGamificationService>();
        var lMock = new Mock<ILogger<GameService>>();
        var service = new GameService(db, gMock.Object, lMock.Object);

        var request = new GameSubmitRequestDto
        {
            AttemptId = "stamp-001",
            GameType = "word_match",
            TimeRemainingSeconds = 50,
            Answers = new List<GameAnswerSubmissionDto>
            {
                new() { QuestionId = "w1", Answer = "Con mèo" }
            }
        };

        var result = await service.SubmitGameAsync(request, userId, "StampUser", CancellationToken.None);

        result.StampsAwarded.Should().Contain("PERFECT_SCORE");

        var session = await db.GameSessions.FirstOrDefaultAsync(s => s.AttemptId == "stamp-001");
        session.Should().NotBeNull();
        session!.MetadataJson.Should().Contain("PERFECT_SCORE");
        session.MetadataJson.Should().Contain("stamps");
    }

    [Fact]
    public async Task SubmitGameAsync_DifferentUser_ThrowsInvalidOperationException()
    {
        var db = CreateInMemoryDb();
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();

        db.ActiveGameAttempts.Add(new ActiveGameAttempt
        {
            AttemptId = "idor-attempt-001",
            UserId = ownerId,
            GameType = "word_match",
            StartedAt = DateTime.UtcNow.AddSeconds(-10),
            QuestionsPayloadJson = "[{\"id\":\"w1\",\"targetText\":\"Cat\",\"translation\":\"Con mèo\"}]"
        });
        await db.SaveChangesAsync();

        var service = new GameService(db, new Mock<IGamificationService>().Object, new Mock<ILogger<GameService>>().Object);

        var act = async () => await service.SubmitGameAsync(new GameSubmitRequestDto
        {
            AttemptId = "idor-attempt-001",
            GameType = "word_match",
            TimeRemainingSeconds = 40,
            Answers = new List<GameAnswerSubmissionDto> { new() { QuestionId = "w1", Answer = "Con mèo" } }
        }, attackerId, "Attacker", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*không thuộc về*");
    }

    [Fact]
    public async Task SubmitGameAsync_DuplicateQuestionIds_CountedOnlyOnce()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();

        db.ActiveGameAttempts.Add(new ActiveGameAttempt
        {
            AttemptId = "dup-attempt-001",
            UserId = userId,
            GameType = "word_match",
            StartedAt = DateTime.UtcNow.AddSeconds(-10),
            QuestionsPayloadJson = "[{\"id\":\"w1\",\"targetText\":\"Cat\",\"translation\":\"Con mèo\"},{\"id\":\"w2\",\"targetText\":\"Dog\",\"translation\":\"Con chó\"}]"
        });
        await db.SaveChangesAsync();

        var service = new GameService(db, new Mock<IGamificationService>().Object, new Mock<ILogger<GameService>>().Object);

        var result = await service.SubmitGameAsync(new GameSubmitRequestDto
        {
            AttemptId = "dup-attempt-001",
            GameType = "word_match",
            TimeRemainingSeconds = 50,
            Answers = new List<GameAnswerSubmissionDto>
            {
                new() { QuestionId = "w1", Answer = "Con mèo" },
                new() { QuestionId = "w1", Answer = "Con mèo" },
                new() { QuestionId = "w1", Answer = "Con mèo" }
            }
        }, userId, "Tester", CancellationToken.None);

        result.CorrectCount.Should().Be(1);
        result.TotalQuestions.Should().Be(2);
    }
}
