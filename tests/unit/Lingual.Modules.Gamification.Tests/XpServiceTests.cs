using FluentAssertions;
using Lingual.Infrastructure.Cache.Redis;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Lingual.Modules.Gamification.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lingual.Modules.Gamification.Tests;

public class XpServiceTests
{
    private readonly GamificationDbContext _dbContext;
    private readonly Mock<IStreakService> _streakServiceMock;
    private readonly Mock<ILeaderboardService> _leaderboardServiceMock;
    private readonly XpService _xpService;

    public XpServiceTests()
    {
        var options = new DbContextOptionsBuilder<GamificationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new GamificationDbContext(options);
        _streakServiceMock = new Mock<IStreakService>();
        _leaderboardServiceMock = new Mock<ILeaderboardService>();

        _xpService = new XpService(
            _dbContext,
            _streakServiceMock.Object,
            _leaderboardServiceMock.Object,
            NullLogger<XpService>.Instance);
    }

    [Fact]
    public async Task AwardXpAsync_ValidRequest_AwardsXpAndRecordsInLedger()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AwardXpRequest(
            UserId: userId,
            SourceType: "new_vocabulary",
            XpDelta: 5,
            SourceId: Guid.NewGuid(),
            IdempotencyKey: "test-key-1");

        _streakServiceMock
            .Setup(s => s.RecordXpActivityAsync(userId, 5, 0, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserStreakDto(userId, 1, 1, null, 1, false, 5, 15, DateTime.UtcNow));

        // Act
        var result = await _xpService.AwardXpAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.AwardedXp.Should().Be(5);
        result.TotalXp.Should().Be(5);
        result.AlreadyAwarded.Should().BeFalse();

        var ledgerEntry = await _dbContext.XpLedgers.FirstOrDefaultAsync(x => x.IdempotencyKey == "test-key-1");
        ledgerEntry.Should().NotBeNull();
        ledgerEntry!.XpDelta.Should().Be(5);

        _leaderboardServiceMock.Verify(
            l => l.IncrementXpAsync(userId.ToString(), 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AwardXpAsync_DuplicateIdempotencyKey_AcknowledgesWithoutAwardingTwice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AwardXpRequest(
            UserId: userId,
            SourceType: "srs_review",
            XpDelta: 20,
            IdempotencyKey: "unique-postman-key");

        _streakServiceMock
            .Setup(s => s.RecordXpActivityAsync(userId, 20, 0, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserStreakDto(userId, 1, 1, null, 1, true, 20, 0, DateTime.UtcNow));

        _streakServiceMock
            .Setup(s => s.GetStreakAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserStreakDto(userId, 1, 1, null, 1, true, 20, 0, DateTime.UtcNow));

        // First call
        var firstResult = await _xpService.AwardXpAsync(request);
        firstResult.AwardedXp.Should().Be(20);
        firstResult.AlreadyAwarded.Should().BeFalse();

        // Act - Repeat same call
        var secondResult = await _xpService.AwardXpAsync(request);

        // Assert
        secondResult.IsSuccess.Should().BeTrue();
        secondResult.AwardedXp.Should().Be(0);
        secondResult.AlreadyAwarded.Should().BeTrue();

        var totalEntries = await _dbContext.XpLedgers.CountAsync(x => x.IdempotencyKey == "unique-postman-key");
        totalEntries.Should().Be(1);

        // Leaderboard only incremented once
        _leaderboardServiceMock.Verify(
            l => l.IncrementXpAsync(userId.ToString(), 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AwardXpAsync_DuplicateSourceRef_AcknowledgesWithoutAwardingTwice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var request1 = new AwardXpRequest(
            UserId: userId,
            SourceType: "lesson_complete",
            XpDelta: 25,
            SourceId: sourceId);

        var request2 = new AwardXpRequest(
            UserId: userId,
            SourceType: "lesson_complete",
            XpDelta: 25,
            SourceId: sourceId);

        _streakServiceMock
            .Setup(s => s.RecordXpActivityAsync(userId, 25, 0, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserStreakDto(userId, 1, 1, null, 1, true, 25, 0, DateTime.UtcNow));

        _streakServiceMock
            .Setup(s => s.GetStreakAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserStreakDto(userId, 1, 1, null, 1, true, 25, 0, DateTime.UtcNow));

        // First call
        await _xpService.AwardXpAsync(request1);

        // Act - Second call
        var secondResult = await _xpService.AwardXpAsync(request2);

        // Assert
        secondResult.AlreadyAwarded.Should().BeTrue();
        secondResult.AwardedXp.Should().Be(0);

        var totalEntries = await _dbContext.XpLedgers.CountAsync(x => x.SourceType == "lesson_complete" && x.SourceId == sourceId);
        totalEntries.Should().Be(1);
    }
}

