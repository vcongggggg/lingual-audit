using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Lingual.Modules.Identity.Contracts.DTOs;
using Lingual.Modules.Identity.Infrastructure.Data;
using Lingual.SharedKernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lingual.Modules.Identity.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<IdentityDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(IdentityDbContext) ||
                (d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("IdentityDbContext"))).ToList();

            foreach (var d in descriptors)
            {
                services.Remove(d);
            }

            services.AddDbContext<IdentityDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });
        });
    }
}

public class IdentityEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public IdentityEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_AuthMezonAuthorize_Returns200WithAuthorizeUrl()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/auth/mezon/authorize");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<MezonAuthorizeUrlDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AuthorizeUrl.Should().Contain("https://oauth2.mezon.ai/oauth2/auth");
        result.Data.AuthorizeUrl.Should().Contain("client_id=");
        result.Data.AuthorizeUrl.Should().Contain("response_type=code");
        result.Data.State.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Post_AuthMezon_ValidToken_Returns200WithJwtToken()
    {
        // Arrange
        var request = new LoginWithMezonRequest
        {
            Token = "dev_test_token_123",
            MezonUserId = "2100896006294999040",
            DisplayName = "Ngo Van Cong",
            AvatarUrl = "https://avatar.mezon.ai/cong.png"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/mezon", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResultDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Data.User.MezonUserId.Should().Be("2100896006294999040");
        result.Data.User.DisplayName.Should().Be("Ngo Van Cong");
        result.Data.User.Role.Should().Be("learner");
    }

    [Fact]
    public async Task Post_AuthMezonSso_AliasRoute_Returns200WithJwtToken()
    {
        // Arrange
        var request = new LoginWithMezonRequest
        {
            Code = "mezon_oauth_auth_code_xyz",
            MezonUserId = "2110004994815037440",
            DisplayName = "Minh Nguyen"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/mezon-sso", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResultDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.User.MezonUserId.Should().Be("2110004994815037440");
    }

    [Fact]
    public async Task Post_Logout_WithValidToken_RevokesTokenAndBlocksSubsequentRequests()
    {
        // 1. Login to obtain access token
        var loginRequest = new LoginWithMezonRequest
        {
            Token = "dev_logout_token_test",
            MezonUserId = "2036383630950928384",
            DisplayName = "Logout Tester"
        };

        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/mezon", loginRequest);
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var authData = (await loginRes.Content.ReadFromJsonAsync<ApiResponse<AuthResultDto>>())!.Data!;
        var token = authData.AccessToken;

        // 2. Call Logout endpoint with Bearer token
        using var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var logoutRes = await _client.SendAsync(logoutReq);
        logoutRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Attempting to use the revoked token again must be rejected with 401 Unauthorized
        using var secondLogoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        secondLogoutReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var secondLogoutRes = await _client.SendAsync(secondLogoutReq);
        secondLogoutRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
