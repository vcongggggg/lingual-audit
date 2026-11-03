using FluentAssertions;
using Lingual.Infrastructure.Cache.Redis;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Lingual.Modules.Gamification.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lingual.Modules.Gamification.Tests;

public class LeaderboardServiceTests
{
    private readonly Mock<IRedisService> _redisMock;
    private readonly GamificationDbContext _dbContext;
    private readonly LeaderboardService _leaderboardService;

    public LeaderboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<GamificationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new GamificationDbContext(options);
        _redisMock = new Mock<IRedisService>();

        _leaderboardService = new LeaderboardService(
            _redisMock.Object,
            _dbContext,
            NullLogger<LeaderboardService>.Instance);
    }

    [Fact]
    public async Task IncrementXpAsync_CallsRedisSortedSetIncrement()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        const double xp = 25;

        // Act
        await _leaderboardService.IncrementXpAsync(userId, xp);

        // Assert
        _redisMock.Verify(
            r => r.SortedSetIncrementAsync("leaderboard:all-time", userId, xp),
            Times.Once);

        _redisMock.Verify(
            r => r.SortedSetIncrementAsync(
                It.Is<string>(k => k.StartsWith("leaderboard:weekly:")),
                userId,
                xp),
            Times.Once);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ClampsZeroLimitToOne()
    {
        // Arrange
        _redisMock
            .Setup(r => r.SortedSetRangeByRankWithScoresAsync(
                It.IsAny<string>(),
                0,
                0, // 1 - 1 = 0
                true))
            .ReturnsAsync(new List<(string Member, double Score)>
            {
                ("lingual-local-demo-user", 100)
            });

        // Act - limit = 0 (phải bị clamp lên 1)
        var result = await _leaderboardService.GetLeaderboardAsync(limit: 0);

        // Assert
        result.Entries.Should().HaveCount(1);
        result.Entries[0].Rank.Should().Be(1);
        result.Entries[0].Name.Should().Be("You");
        result.Entries[0].Xp.Should().Be(100);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ReturnsExpectedStructure()
    {
        // Arrange
        _redisMock
            .Setup(r => r.SortedSetRangeByRankWithScoresAsync(
                It.IsAny<string>(),
                0,
                19,
                true))
            .ReturnsAsync(new List<(string Member, double Score)>
            {
                ("user-1", 500),
                ("user-2", 300)
            });

        // Act
        var result = await _leaderboardService.GetLeaderboardAsync(limit: 20);

        // Assert
        result.Entries.Should().HaveCount(2);
        result.Entries[0].Rank.Should().Be(1);
        result.Entries[0].UserId.Should().Be("user-1");
        result.Entries[0].Xp.Should().Be(500);

        result.Entries[1].Rank.Should().Be(2);
        result.Entries[1].UserId.Should().Be("user-2");
        result.Entries[1].Xp.Should().Be(300);
    }
}

