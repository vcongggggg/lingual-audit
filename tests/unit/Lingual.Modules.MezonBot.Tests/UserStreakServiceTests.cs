using System;
using System.Threading.Tasks;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.MezonBot.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lingual.Modules.MezonBot.Tests;

public class UserStreakServiceTests
{
    [Fact]
    public async Task ProcessDailyCheckinAsync_FirstTime_StartsStreakAtOne()
    {
        var gamificationServiceMock = new Mock<IGamificationService>();
        var userResolverMock = new Mock<IMezonUserResolver>();
        var loggerMock = new Mock<ILogger<UserStreakService>>();

        var testUserId = Guid.NewGuid();
        userResolverMock.Setup(u => u.ResolveUserIdAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(testUserId);

        gamificationServiceMock.Setup(s => s.CheckInAsync(testUserId, It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new DailyCheckinResultDto(
                IsSuccess: true,
                AlreadyCheckedInToday: false,
                CurrentDays: 1,
                BestDays: 1,
                XpAwarded: 20,
                FreezeBalance: 1,
                FreezeUsed: false,
                Message: "Điểm danh hàng ngày thành công! Giữ vững ngọn lửa học tập nhé."
            ));

        var service = new UserStreakService(gamificationServiceMock.Object, userResolverMock.Object, loggerMock.Object);

        var mezonUserId = "test_user_" + Guid.NewGuid();
        var result = await service.ProcessDailyCheckinAsync(mezonUserId, "TestLearner");

        Assert.True(result.IsSuccess);
        Assert.False(result.AlreadyCheckedInToday);
        Assert.Equal(1, result.CurrentDays);
        Assert.Equal(1, result.BestDays);
        Assert.Equal(20, result.XpAwarded);
        Assert.False(result.FreezeUsed);
    }

    [Fact]
    public async Task ProcessDailyCheckinAsync_AlreadyCheckedIn_ReturnsAlreadyCheckedIn()
    {
        var gamificationServiceMock = new Mock<IGamificationService>();
        var userResolverMock = new Mock<IMezonUserResolver>();
        var loggerMock = new Mock<ILogger<UserStreakService>>();

        var testUserId = Guid.NewGuid();
        userResolverMock.Setup(u => u.ResolveUserIdAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(testUserId);

        gamificationServiceMock.Setup(s => s.CheckInAsync(testUserId, It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new DailyCheckinResultDto(
                IsSuccess: true,
                AlreadyCheckedInToday: true,
                CurrentDays: 1,
                BestDays: 1,
                XpAwarded: 0,
                FreezeBalance: 1,
                FreezeUsed: false,
                Message: "Hôm nay bạn đã điểm danh giữ streak rồi! Hãy quay lại vào ngày mai nhé."
            ));

        var service = new UserStreakService(gamificationServiceMock.Object, userResolverMock.Object, loggerMock.Object);

        var mezonUserId = "test_user_" + Guid.NewGuid();
        var result = await service.ProcessDailyCheckinAsync(mezonUserId, "TestLearner");

        Assert.True(result.IsSuccess);
        Assert.True(result.AlreadyCheckedInToday);
        Assert.Equal(1, result.CurrentDays);
        Assert.Equal(0, result.XpAwarded);
    }

    [Fact]
    public async Task GetStreakAsync_AfterCheckin_ReturnsActiveToday()
    {
        var gamificationServiceMock = new Mock<IGamificationService>();
        var userResolverMock = new Mock<IMezonUserResolver>();
        var loggerMock = new Mock<ILogger<UserStreakService>>();

        var testUserId = Guid.NewGuid();
        userResolverMock.Setup(u => u.ResolveUserIdAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(testUserId);

        gamificationServiceMock.Setup(s => s.GetUserStreakAsync(testUserId, It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new Lingual.Modules.Gamification.Contracts.DTOs.UserStreakDto(
                testUserId,
                1,
                1,
                DateOnly.FromDateTime(DateTime.UtcNow),
                1,
                true,
                20,
                0,
                DateTime.UtcNow
            ));

        var service = new UserStreakService(gamificationServiceMock.Object, userResolverMock.Object, loggerMock.Object);

        var mezonUserId = "test_user_" + Guid.NewGuid();
        var streak = await service.GetStreakAsync(mezonUserId);

        Assert.NotNull(streak);
        Assert.Equal(1, streak.CurrentDays);
        Assert.True(streak.IsActiveToday);
    }
}
