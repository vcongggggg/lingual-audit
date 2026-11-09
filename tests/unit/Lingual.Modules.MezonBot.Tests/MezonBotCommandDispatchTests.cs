using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.MezonBot;
using Lingual.Modules.MezonBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lingual.Modules.MezonBot.Tests;

public class MezonBotCommandDispatchTests
{
    [Theory]
    [InlineData("/streak")]
    [InlineData("/STREAK")]
    [InlineData("/Streak")]
    [InlineData("  /streak  ")]
    public void ResolveCommandType_Streak_ReturnsStreak(string input)
    {
        // Act
        var commandType = MezonBotWorker.ResolveCommandType(input);

        // Assert
        commandType.Should().Be("streak");
    }

    [Theory]
    [InlineData("/daily")]
    [InlineData("/DAILY")]
    [InlineData("/Daily")]
    [InlineData("/checkin")]
    [InlineData("/CHECKIN")]
    [InlineData("  /checkin  ")]
    public void ResolveCommandType_DailyAndCheckin_ReturnsDaily(string input)
    {
        // Act
        var commandType = MezonBotWorker.ResolveCommandType(input);

        // Assert
        commandType.Should().Be("daily");
    }

    [Fact]
    public void ResolveCommandType_Streak_IsNeverShadowedByDaily()
    {
        // Arrange
        var streakCommand = "/streak";

        // Act
        var streakResult = MezonBotWorker.ResolveCommandType(streakCommand);
        var dailyResult = MezonBotWorker.ResolveCommandType("/daily");

        // Assert
        streakResult.Should().Be("streak");
        dailyResult.Should().Be("daily");
        streakResult.Should().NotBe(dailyResult);
    }

    [Theory]
    [InlineData("/ping", "ping")]
    [InlineData("/help", "help")]
    [InlineData("/learn", "learn")]
    [InlineData("/quiz", "quiz")]
    [InlineData("/roleplay greeting", "roleplay")]
    [InlineData("/scenario coffee", "roleplay")]
    [InlineData("/top", "leaderboard")]
    [InlineData("/leaderboard", "leaderboard")]
    [InlineData("/bxh", "leaderboard")]
    [InlineData("/ask I like English", "ask")]
    [InlineData("/check I like English", "ask")]
    public void ResolveCommandType_OtherSupportedCommands_ResolveExpectedTypes(string input, string expected)
    {
        // Act
        var result = MezonBotWorker.ResolveCommandType(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("hello bot")]
    [InlineData("/unknown_command")]
    public void ResolveCommandType_Unrecognized_ReturnsUnknown(string? input)
    {
        // Act
        var result = MezonBotWorker.ResolveCommandType(input);

        // Assert
        result.Should().Be("unknown");
    }

    [Fact]
    public async Task HandleStreakCommandAsync_OnlyQueriesStreak_DoesNotCallProcessDailyCheckin()
    {
        // Arrange
        var services = new ServiceCollection();
        var userResolverMock = new Mock<IMezonUserResolver>();
        var gamificationServiceMock = new Mock<IGamificationService>();
        var userStreakServiceMock = new Mock<IUserStreakService>();

        var testUserId = Guid.NewGuid();
        var mezonUserId = "test_user_streak_01";
        var displayName = "TestLearner";

        userResolverMock.Setup(u => u.ResolveUserIdAsync(mezonUserId, displayName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testUserId);

        gamificationServiceMock.Setup(g => g.GetUserStreakAsync(testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Lingual.Modules.Gamification.Contracts.DTOs.UserStreakDto(
                testUserId,
                5,
                10,
                DateOnly.FromDateTime(DateTime.UtcNow),
                2,
                true,
                20,
                0,
                DateTime.UtcNow));

        services.AddSingleton(userResolverMock.Object);
        services.AddSingleton(gamificationServiceMock.Object);
        services.AddSingleton(userStreakServiceMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new MezonBotOptions
        {
            BotId = "1234567890",
            Token = "fake-token",
            Enabled = false
        });

        var worker = new MezonBotWorker(NullLogger<MezonBotWorker>.Instance, options, scopeFactory);

        // Act - Gọi trực tiếp HandleStreakCommandAsync
        await worker.HandleStreakCommandAsync(1001, mezonUserId, displayName);

        // Assert:
        // 1. Phải tra cứu User ID và lấy Streak DTO
        userResolverMock.Verify(u => u.ResolveUserIdAsync(mezonUserId, displayName, It.IsAny<CancellationToken>()), Times.Once);
        gamificationServiceMock.Verify(g => g.GetUserStreakAsync(testUserId, It.IsAny<CancellationToken>()), Times.Once);

        // 2. TUYỆT ĐỐI KHÔNG gọi ProcessDailyCheckin (không cộng XP hay kích hoạt check-in)
        userStreakServiceMock.Verify(s => s.ProcessDailyCheckinAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        gamificationServiceMock.Verify(g => g.CheckInAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleDailyCommandAsync_CallsProcessDailyCheckin_ToAwardXp()
    {
        // Arrange
        var services = new ServiceCollection();
        var userStreakServiceMock = new Mock<IUserStreakService>();

        var mezonUserId = "test_user_daily_01";
        var displayName = "TestLearnerDaily";

        userStreakServiceMock.Setup(s => s.ProcessDailyCheckinAsync(mezonUserId, displayName))
            .ReturnsAsync(new DailyCheckinResultDto(
                IsSuccess: true,
                AlreadyCheckedInToday: false,
                CurrentDays: 1,
                BestDays: 1,
                XpAwarded: 20,
                FreezeBalance: 1,
                FreezeUsed: false,
                Message: "Điểm danh hàng ngày thành công! Giữ vững ngọn lửa học tập nhé."));

        services.AddSingleton(userStreakServiceMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new MezonBotOptions
        {
            BotId = "1234567890",
            Token = "fake-token",
            Enabled = false
        });

        var worker = new MezonBotWorker(NullLogger<MezonBotWorker>.Instance, options, scopeFactory);

        // Act - Gọi HandleDailyCommandAsync
        await worker.HandleDailyCommandAsync(1001, mezonUserId, displayName);

        // Assert:
        // Phải gọi ProcessDailyCheckinAsync để tiến hành điểm danh và nhận +20 XP
        userStreakServiceMock.Verify(s => s.ProcessDailyCheckinAsync(mezonUserId, displayName), Times.Once);
    }
}
