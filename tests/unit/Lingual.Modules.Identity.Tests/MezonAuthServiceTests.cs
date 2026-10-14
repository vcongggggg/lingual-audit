using System.Net;
using FluentAssertions;
using Lingual.Modules.Identity.Contracts.DTOs;
using Lingual.Modules.Identity.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace Lingual.Modules.Identity.Tests;

public class MezonAuthServiceTests
{
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<IHostEnvironment> _envMock = new();
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());

    [Fact]
    public void GetAuthorizeUrl_ReturnsValidUrlAndCachesState()
    {
        // Arrange
        _configMock.Setup(c => c["MezonOAuth2:ClientId"]).Returns("2036383630950928384");
        _configMock.Setup(c => c["MezonOAuth2:AuthorizeUrl"]).Returns("https://oauth2.mezon.ai/oauth2/auth");
        _configMock.Setup(c => c["MezonOAuth2:RedirectUri"]).Returns("http://localhost:3000/oauth/mezon/callback");
        _configMock.Setup(c => c["MezonOAuth2:Scope"]).Returns("openid offline");

        var service = new MezonAuthService(new HttpClient(), _configMock.Object, _envMock.Object, _memoryCache, NullLogger<MezonAuthService>.Instance);

        // Act
        var result = service.GetAuthorizeUrl();

        // Assert
        result.Should().NotBeNull();
        result.State.Should().NotBeNullOrWhiteSpace();
        result.AuthorizeUrl.Should().Contain("client_id=2036383630950928384");
        result.AuthorizeUrl.Should().Contain("response_type=code");
        result.AuthorizeUrl.Should().Contain(Uri.EscapeDataString(result.State));

        // State is cached
        _memoryCache.TryGetValue($"mezon_oauth_state:{result.State}", out var cached).Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAndGetUserInfoAsync_WithContextMezonUserId_ReturnsUserInfo()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var service = new MezonAuthService(new HttpClient(), _configMock.Object, _envMock.Object, _memoryCache, NullLogger<MezonAuthService>.Instance);

        var request = new LoginWithMezonRequest
        {
            MezonUserId = "2100896006294999040",
            DisplayName = "Ngo Van Cong",
            AvatarUrl = "https://avatar.mezon.ai/cong.png"
        };

        // Act
        var result = await service.ValidateAndGetUserInfoAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.MezonUserId.Should().Be("2100896006294999040");
        result.DisplayName.Should().Be("Ngo Van Cong");
        result.AvatarUrl.Should().Be("https://avatar.mezon.ai/cong.png");
    }

    [Fact]
    public async Task ValidateAndGetUserInfoAsync_WithDevTokenInDevelopment_ReturnsMockIdentity()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var service = new MezonAuthService(new HttpClient(), _configMock.Object, _envMock.Object, _memoryCache, NullLogger<MezonAuthService>.Instance);

        var request = new LoginWithMezonRequest
        {
            Token = "dev_token_12345",
            DisplayName = "Test Developer"
        };

        // Act
        var result = await service.ValidateAndGetUserInfoAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.MezonUserId.Should().StartWith("dev_mezon_");
        result.DisplayName.Should().Be("Test Developer");
    }

    [Fact]
    public async Task ValidateAndGetUserInfoAsync_WithNumericSnowflakeUserId_ReturnsParsedUserInfo()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        _configMock.Setup(c => c["MezonOAuth2:UserInfoUrl"]).Returns("https://oauth2.mezon.ai/userinfo");

        var mockHttpHandler = new Mock<HttpMessageHandler>();
        // Mezon returns numeric snowflake user_id
        var jsonResponse = """
        {
            "user_id": 2110004994815037440,
            "display_name": "Minh Nguyen",
            "avatar": "https://avatar.mezon.ai/minh.jpg"
        }
        """;

        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        var httpClient = new HttpClient(mockHttpHandler.Object);
        var service = new MezonAuthService(httpClient, _configMock.Object, _envMock.Object, _memoryCache, NullLogger<MezonAuthService>.Instance);

        var request = new LoginWithMezonRequest
        {
            Token = "real_valid_mezon_oauth_access_token"
        };

        // Act
        var result = await service.ValidateAndGetUserInfoAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.MezonUserId.Should().Be("2110004994815037440");
        result.DisplayName.Should().Be("Minh Nguyen");
        result.AvatarUrl.Should().Be("https://avatar.mezon.ai/minh.jpg");
    }
}
