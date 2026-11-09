using FluentAssertions;
using Lingual.Modules.Gamification.Common;
using Lingual.Modules.Gamification.Domain;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Lingual.Modules.Gamification.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lingual.Modules.Gamification.Tests;

public class StreakServiceTests
{
    private readonly GamificationDbContext _dbContext;
    private readonly StreakService _streakService;

    public StreakServiceTests()
    {
        var options = new DbContextOptionsBuilder<GamificationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new GamificationDbContext(options);
        _streakService = new StreakService(_dbContext, NullLogger<StreakService>.Instance);
    }

    [Fact]
    public async Task GetStreakAsync_NewUser_InitializesWithDefaultValues()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _streakService.GetStreakAsync(userId);

        // Assert
        result.UserId.Should().Be(userId);
        result.CurrentDays.Should().Be(0);
        result.BestDays.Should().Be(0);
        result.FreezeBalance.Should().Be(1);
        result.IsActiveToday.Should().BeFalse();
        result.TodayXp.Should().Be(0);
        result.XpNeededForStreak.Should().Be(20);
    }

    [Fact]
    public async Task RecordXpActivityAsync_UnderThreshold_DoesNotIncrementStreak()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act - Thưởng 10 XP (chưa đủ 20 XP)
        var result = await _streakService.RecordXpActivityAsync(userId, 10);

        // Assert
        result.CurrentDays.Should().Be(0);
        result.IsActiveToday.Should().BeFalse();
        result.TodayXp.Should().Be(10);
        result.XpNeededForStreak.Should().Be(10);
    }

    [Fact]
    public async Task RecordXpActivityAsync_Reaching20Xp_IncrementsStreakTo1()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act - 10 XP lần đầu, sau đó thêm 10 XP
        await _streakService.RecordXpActivityAsync(userId, 10);
        var result = await _streakService.RecordXpActivityAsync(userId, 10);

        // Assert
        result.CurrentDays.Should().Be(1);
        result.BestDays.Should().Be(1);
        result.IsActiveToday.Should().BeTrue();
        result.TodayXp.Should().Be(20);
        result.XpNeededForStreak.Should().Be(0);
    }

    [Fact]
    public async Task RecordXpActivityAsync_Exceeding20XpSameDay_DoesNotIncrementStreakTwice()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act - Đạt 25 XP sau đó thêm 30 XP trong cùng ngày
        await _streakService.RecordXpActivityAsync(userId, 25);
        var secondResult = await _streakService.RecordXpActivityAsync(userId, 30);

        // Assert
        secondResult.CurrentDays.Should().Be(1);
        secondResult.BestDays.Should().Be(1);
        secondResult.TodayXp.Should().Be(55);
        secondResult.IsActiveToday.Should().BeTrue();
    }

    [Fact]
    public async Task RecordXpActivityAsync_MissingOneDayWithFreeze_ConsumesFreezeAndPreservesStreak()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = TimeZoneHelper.GetCurrentVietnamDate();
        var twoDaysAgo = today.AddDays(-2); // Bỏ lỡ ngày hôm qua (yesterday = today - 1)

        // Khởi tạo user có streak 5 ngày, active lần cuối 2 ngày trước, có 1 freeze ticket
        var initialStreak = new UserStreak
        {
            UserId = userId,
            CurrentDays = 5,
            BestDays = 5,
            LastActiveDate = twoDaysAgo,
            FreezeBalance = 1,
            DailyActivity = "{}",
            FreezeHistory = "[]"
        };
        _dbContext.UserStreaks.Add(initialStreak);
        await _dbContext.SaveChangesAsync();

        // Act - Hôm nay học đạt 20 XP
        var result = await _streakService.RecordXpActivityAsync(userId, 20);

        // Assert
        result.FreezeBalance.Should().Be(0); // Tiêu thụ 1 khiên
        result.CurrentDays.Should().Be(6); // Bảo toàn chuỗi 5 ngày và tăng lên 6
        result.BestDays.Should().Be(6);
        result.IsActiveToday.Should().BeTrue();
    }

    [Fact]
    public async Task RecordXpActivityAsync_MissingMultipleDays_ResetsStreakTo1()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = TimeZoneHelper.GetCurrentVietnamDate();
        var fiveDaysAgo = today.AddDays(-5); // Bỏ lỡ 4 ngày

        var initialStreak = new UserStreak
        {
            UserId = userId,
            CurrentDays = 10,
            BestDays = 10,
            LastActiveDate = fiveDaysAgo,
            FreezeBalance = 1, // Kể cả còn 1 freeze cũng không đủ cứu 4 ngày
            DailyActivity = "{}",
            FreezeHistory = "[]"
        };
        _dbContext.UserStreaks.Add(initialStreak);
        await _dbContext.SaveChangesAsync();

        // Act - Hôm nay đạt 20 XP
        var result = await _streakService.RecordXpActivityAsync(userId, 20);

        // Assert
        result.CurrentDays.Should().Be(1); // Bị reset về 1
        result.BestDays.Should().Be(10); // Kỷ lục cũ 10 ngày vẫn giữ nguyên
        result.IsActiveToday.Should().BeTrue();
    }
}

