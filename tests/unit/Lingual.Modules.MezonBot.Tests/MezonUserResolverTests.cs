using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Lingual.Modules.MezonBot.Infrastructure.Data;
using Lingual.Modules.MezonBot.Services;

namespace Lingual.Modules.MezonBot.Tests;

public class MezonUserResolverTests
{
    private readonly MezonBotDbContext _dbContext;
    private readonly MezonUserResolver _resolver;

    public MezonUserResolverTests()
    {
        var options = new DbContextOptionsBuilder<MezonBotDbContext>()
            .UseInMemoryDatabase(databaseName: $"MezonBot_Test_{Guid.NewGuid()}")
            .Options;

        _dbContext = new MezonBotDbContext(options);
        _resolver = new MezonUserResolver(_dbContext, NullLogger<MezonUserResolver>.Instance);
    }

    [Fact]
    public async Task ResolveUserIdAsync_WithValidMezonUserId_ReturnsConsistentGuid()
    {
        // Arrange
        const string mezonUserId = "2100896006294999040";

        // Act
        var firstGuid = await _resolver.ResolveUserIdAsync(mezonUserId, "Alice");
        var secondGuid = await _resolver.ResolveUserIdAsync(mezonUserId, "Alice");

        // Assert
        firstGuid.Should().NotBeEmpty();
        secondGuid.Should().Be(firstGuid, "cùng một Mezon User ID phải luôn sinh ra cùng một User GUID");
    }

    [Fact]
    public async Task ResolveUserIdAsync_WithEmptyMezonUserId_ReturnsEmptyGuid()
    {
        // Act
        var result = await _resolver.ResolveUserIdAsync(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveUserIdAsync_WithDifferentMezonIds_ReturnsDifferentGuids()
    {
        // Act
        var user1 = await _resolver.ResolveUserIdAsync("mezon_user_111");
        var user2 = await _resolver.ResolveUserIdAsync("mezon_user_222");

        // Assert
        user1.Should().NotBe(user2);
    }
}

