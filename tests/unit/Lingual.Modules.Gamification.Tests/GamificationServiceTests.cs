using FluentAssertions;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Lingual.Modules.Gamification.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lingual.Modules.Gamification.Tests;

public class GamificationServiceTests
{
    private readonly GamificationDbContext _dbContext;
    private readonly Mock<IXpService> _xpServiceMock;
    private readonly Mock<IStreakService> _streakServiceMock;
    private readonly Mock<ILeaderboardService> _leaderboardServiceMock;
    private readonly Mock<IBadgeService> _badgeServiceMock;
    private readonly GamificationService _gamificationService;

    public GamificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<GamificationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new GamificationDbContext(options);
        _xpServiceMock = new Mock<IXpService>();
        _streakServiceMock = new Mock<IStreakService>();
        _leaderboardServiceMock = new Mock<ILeaderboardService>();
        _badgeServiceMock = new Mock<IBadgeService>();

        _gamificationService = new GamificationService(
            _xpServiceMock.Object,
            _streakServiceMock.Object,
            _leaderboardServiceMock.Object,
            _badgeServiceMock.Object,
            _dbContext,
            NullLogger<GamificationService>.Instance);
    }

    [Fact]
    public async Task GetUserProfileSummaryAsync_WhenUserIdIsNull_ReturnsDemoUserSummary()
    {
        // Arrange & Act
        var result = await _gamificationService.GetUserProfileSummaryAsync(null);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be("4943737a-792a-4fca-8315-df58d25c2156");
        result.DisplayName.Should().NotBeNullOrWhiteSpace();
        result.Role.Should().Be("learner");
        result.TotalXp.Should().Be(450);
        result.Level.Should().Be(3);
        result.NextLevelXp.Should().Be(600);
        result.Streak.Should().NotBeNull();
        result.Streak.CurrentDays.Should().Be(5);
        result.Streak.BestDays.Should().Be(12);
        result.Streak.FreezeBalance.Should().Be(2);
        result.Streak.IsActiveToday.Should().BeTrue();
        result.Streak.TodayXp.Should().Be(50);
        result.Streak.XpNeededForStreak.Should().Be(30);
        result.Badges.Should().NotBeEmpty();
        result.Badges.Should().Contain(b => b.Id == "streak_3");
        result.Stats.Should().NotBeNull();
        result.Stats.WordsLearned.Should().Be(45);
        result.Stats.QuizzesCompleted.Should().Be(12);
        result.Stats.AccuracyRate.Should().Be(88);
    }

    [Fact]
    public async Task GetUserProfileSummaryAsync_WhenUserIdProvided_CalculatesLevelAndReturnsSummary()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _xpServiceMock
            .Setup(x => x.GetTotalXpAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(850);

        _streakServiceMock
            .Setup(s => s.GetStreakAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserStreakDto(
                UserId: userId,
                CurrentDays: 7,
                BestDays: 14,
                LastActiveDate: DateOnly.FromDateTime(DateTime.UtcNow),
                FreezeBalance: 3,
                IsActiveToday: true,
                TodayXp: 40,
                XpNeededForStreak: 0,
                UpdatedAt: DateTime.UtcNow));

        _badgeServiceMock
            .Setup(b => b.GetBadgesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BadgesResponseDto(new List<BadgeDto>
            {
                new(
                    Id: "streak_3",
                    Name: "Chuỗi 3 ngày",
                    Category: "streak",
                    Rarity: "common",
                    Icon: "flame",
                    Description: "Duy trì chuỗi học tập 3 ngày liên tục",
                    Unlocked: true,
                    UnlockedDate: "2026-11-18",
                    Progress: 3,
                    MaxProgress: 3,
                    XpReward: 50,
                    Lore: "Khởi đầu ấn tượng!"
                )
            }));

        // Act
        var result = await _gamificationService.GetUserProfileSummaryAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId.ToString());
        result.TotalXp.Should().Be(850);
        result.Level.Should().Be(5); // 850 / 200 + 1 = 5
        result.NextLevelXp.Should().Be(1000); // 5 * 200 = 1000
        result.Streak.CurrentDays.Should().Be(7);
        result.Streak.BestDays.Should().Be(14);
        result.Streak.FreezeBalance.Should().Be(3);
        result.Streak.IsActiveToday.Should().BeTrue();
        result.Streak.TodayXp.Should().Be(40);
        result.Streak.XpNeededForStreak.Should().Be(0);
        result.Badges.Should().Contain(b => b.Id == "streak_3");
    }
}
