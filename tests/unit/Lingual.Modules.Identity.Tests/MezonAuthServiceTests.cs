using FluentAssertions;
using Lingual.Modules.Identity.Contracts.DTOs;
using Lingual.Modules.Identity.Infrastructure.Data;
using Lingual.Modules.Identity.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lingual.Modules.Identity.Tests;

public class MezonAuthServiceTests
{
    private static IdentityDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new IdentityDbContext(options);
    }

    [Fact]
    public async Task GetAuthorizeUrlAsync_ReturnsSuccessWithAuthorizeUrlAndState()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var tokenMock = new Mock<ITokenService>();
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var httpContextMock = new Mock<IHttpContextAccessor>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            {"MezonOAuth2:ClientId", "2036383630950928384"},
            {"MezonOAuth2:AuthorizeUrl", "https://oauth2.mezon.ai/oauth2/auth"},
            {"MezonOAuth2:RedirectUri", "http://localhost:3000/oauth/mezon/callback"},
            {"MezonOAuth2:Scope", "openid offline"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var service = new MezonAuthService(
            db,
            tokenMock.Object,
            NullLogger<MezonAuthService>.Instance,
            httpFactoryMock.Object,
            configuration,
            memoryCache,
            httpContextMock.Object);

        // Act
        var result = await service.GetAuthorizeUrlAsync();

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(200);

        var json = System.Text.Json.JsonSerializer.Serialize(result.Payload);
        json.Should().Contain("client_id=2036383630950928384");
        json.Should().Contain("response_type=code");
    }

    [Fact]
    public async Task HandleCallbackAsync_MissingCode_ReturnsBadRequest()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var tokenMock = new Mock<ITokenService>();
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var httpContextMock = new Mock<IHttpContextAccessor>();
        var configuration = new ConfigurationBuilder().Build();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var service = new MezonAuthService(
            db,
            tokenMock.Object,
            NullLogger<MezonAuthService>.Instance,
            httpFactoryMock.Object,
            configuration,
            memoryCache,
            httpContextMock.Object);

        var request = new MezonAuthRequest
        {
            Code = "",
            State = "valid_state"
        };

        // Act
        var result = await service.HandleCallbackAsync(request);

        // Assert
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleCallbackAsync_MissingState_ReturnsBadRequest()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var tokenMock = new Mock<ITokenService>();
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var httpContextMock = new Mock<IHttpContextAccessor>();
        var configuration = new ConfigurationBuilder().Build();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var service = new MezonAuthService(
            db,
            tokenMock.Object,
            NullLogger<MezonAuthService>.Instance,
            httpFactoryMock.Object,
            configuration,
            memoryCache,
            httpContextMock.Object);

        var request = new MezonAuthRequest
        {
            Code = "valid_code",
            State = ""
        };

        // Act
        var result = await service.HandleCallbackAsync(request);

        // Assert
        result.StatusCode.Should().Be(400);
    }
}
