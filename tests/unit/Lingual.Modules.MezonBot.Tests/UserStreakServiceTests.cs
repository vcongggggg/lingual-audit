using System;
using System.Threading.Tasks;
using Lingual.Modules.MezonBot.Infrastructure.Data;
using Lingual.Modules.MezonBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lingual.Modules.MezonBot.Tests;

public class UserStreakServiceTests
{
    private MezonBotDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<MezonBotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MezonBotDbContext(options);
    }

    [Fact]
    public async Task ProcessDailyCheckinAsync_FirstTime_StartsStreakAtOne()
    {
        using var dbContext = CreateInMemoryDbContext();
        var loggerMock = new Mock<ILogger<UserStreakService>>();
        var service = new UserStreakService(dbContext, loggerMock.Object);

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
        using var dbContext = CreateInMemoryDbContext();
        var loggerMock = new Mock<ILogger<UserStreakService>>();
        var service = new UserStreakService(dbContext, loggerMock.Object);

        var mezonUserId = "test_user_" + Guid.NewGuid();
        var firstResult = await service.ProcessDailyCheckinAsync(mezonUserId, "TestLearner");
        Assert.True(firstResult.IsSuccess);

        var secondResult = await service.ProcessDailyCheckinAsync(mezonUserId, "TestLearner");
        Assert.True(secondResult.IsSuccess);
        Assert.True(secondResult.AlreadyCheckedInToday);
        Assert.Equal(1, secondResult.CurrentDays);
        Assert.Equal(0, secondResult.XpAwarded);
    }

    [Fact]
    public async Task GetStreakAsync_AfterCheckin_ReturnsActiveToday()
    {
        using var dbContext = CreateInMemoryDbContext();
        var loggerMock = new Mock<ILogger<UserStreakService>>();
        var service = new UserStreakService(dbContext, loggerMock.Object);

        var mezonUserId = "test_user_" + Guid.NewGuid();
        await service.ProcessDailyCheckinAsync(mezonUserId, "TestLearner");

        var streak = await service.GetStreakAsync(mezonUserId);
        Assert.NotNull(streak);
        Assert.Equal(1, streak.CurrentDays);
        Assert.True(streak.IsActiveToday);
    }
}

