using FluentAssertions;
using Lingual.Modules.Identity.Contracts.DTOs;
using Lingual.Modules.Identity.Domain;
using Lingual.Modules.Identity.Features.Auth.LoginWithMezon;
using Lingual.Modules.Identity.Infrastructure.Data;
using Lingual.Modules.Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lingual.Modules.Identity.Tests;

public class LoginWithMezonCommandHandlerTests
{
    private static IdentityDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new IdentityDbContext(options);
    }

    [Fact]
    public async Task Handle_NewMezonUser_CreatesUserAndProfile_ReturnsTokenAndIsNewUserTrue()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var mezonAuthMock = new Mock<IMezonAuthService>();
        var jwtMock = new Mock<IJwtTokenService>();

        var mezonUserInfo = new MezonUserInfo("mezon_1001", "Learner Minh", "https://avatar.mezon.ai/1001.png");
        mezonAuthMock.Setup(m => m.ValidateAndGetUserInfoAsync(It.IsAny<LoginWithMezonRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mezonUserInfo);

        jwtMock.Setup(j => j.GenerateToken(It.IsAny<User>()))
            .Returns(("mock.jwt.token", 604800));

        var handler = new LoginWithMezonCommandHandler(
            db, 
            mezonAuthMock.Object, 
            jwtMock.Object, 
            NullLogger<LoginWithMezonCommandHandler>.Instance);

        var request = new LoginWithMezonRequest { Token = "valid_token" };
        var command = new LoginWithMezonCommand(request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsNewUser.Should().BeTrue();
        result.Data.AccessToken.Should().Be("mock.jwt.token");
        result.Data.User.MezonUserId.Should().Be("mezon_1001");
        result.Data.User.DisplayName.Should().Be("Learner Minh");
        result.Data.User.Role.Should().Be("learner");

        // Verify in DB
        var savedUser = await db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.MezonUserId == "mezon_1001");
        savedUser.Should().NotBeNull();
        savedUser!.Profile.Should().NotBeNull();
        savedUser.Profile!.PlacementStatus.Should().Be("pending");
    }

    [Fact]
    public async Task Handle_ExistingMezonUser_UpdatesLastSeenAndInfo_ReturnsIsNewUserFalse()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            MezonUserId = "mezon_2002",
            DisplayName = "Old Name",
            AvatarUrl = "https://avatar.mezon.ai/old.png",
            Status = "active",
            Role = "learner",
            LastSeenAt = DateTime.UtcNow.AddDays(-5),
            Profile = new LearnerProfile
            {
                LearningGoal = "daily_communication",
                DailyCommitmentMinutes = 15,
                ProficiencyLevel = "A1",
                PlacementStatus = "self_selected",
                OnboardingCompletedAt = DateTime.UtcNow.AddDays(-5)
            }
        };
        await db.Users.AddAsync(existingUser);
        await db.SaveChangesAsync();

        var mezonAuthMock = new Mock<IMezonAuthService>();
        var jwtMock = new Mock<IJwtTokenService>();

        var updatedInfo = new MezonUserInfo("mezon_2002", "Updated Display Name", "https://avatar.mezon.ai/new.png");
        mezonAuthMock.Setup(m => m.ValidateAndGetUserInfoAsync(It.IsAny<LoginWithMezonRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedInfo);

        jwtMock.Setup(j => j.GenerateToken(It.IsAny<User>()))
            .Returns(("refreshed.jwt.token", 604800));

        var handler = new LoginWithMezonCommandHandler(
            db, 
            mezonAuthMock.Object, 
            jwtMock.Object, 
            NullLogger<LoginWithMezonCommandHandler>.Instance);

        var request = new LoginWithMezonRequest { Token = "token_2002" };
        var command = new LoginWithMezonCommand(request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsNewUser.Should().BeFalse();
        result.Data.User.DisplayName.Should().Be("Updated Display Name");
        result.Data.OnboardingCompleted.Should().BeTrue();

        var userInDb = await db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.MezonUserId == "mezon_2002");
        userInDb!.DisplayName.Should().Be("Updated Display Name");
        userInDb.AvatarUrl.Should().Be("https://avatar.mezon.ai/new.png");
    }

    [Fact]
    public async Task Handle_InvalidMezonToken_ReturnsFailure()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var mezonAuthMock = new Mock<IMezonAuthService>();
        var jwtMock = new Mock<IJwtTokenService>();

        mezonAuthMock.Setup(m => m.ValidateAndGetUserInfoAsync(It.IsAny<LoginWithMezonRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MezonUserInfo?)null);

        var handler = new LoginWithMezonCommandHandler(
            db, 
            mezonAuthMock.Object, 
            jwtMock.Object, 
            NullLogger<LoginWithMezonCommandHandler>.Instance);

        var request = new LoginWithMezonRequest { Token = "bad_token" };
        var command = new LoginWithMezonCommand(request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}
