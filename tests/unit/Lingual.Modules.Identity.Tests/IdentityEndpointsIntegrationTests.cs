using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lingual.Modules.Identity.Contracts.DTOs;
using Lingual.Modules.Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Lingual.Modules.Identity.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MezonOAuth2:ClientId"] = "2036383630950928384",
                ["MezonOAuth2:ClientSecret"] = "test_secret",
                ["MezonOAuth2:RedirectUri"] = "http://localhost:3000/oauth/mezon/callback",
                ["MezonOAuth2:AuthorizeUrl"] = "https://oauth2.mezon.ai/oauth2/auth"
            });
        });

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
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);


        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.ValueKind.Should().Be(JsonValueKind.Object);
        result.TryGetProperty("authorizeUrl", out var authUrlNode).Should().BeTrue();
        authUrlNode.GetString().Should().Contain("https://oauth2.mezon.ai/oauth2/auth");
    }

    [Fact]
    public async Task Post_AuthMezon_MissingParams_ReturnsBadRequest()
    {
        // Arrange
        var request = new MezonAuthRequest
        {
            Code = "",
            State = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/mezon", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
