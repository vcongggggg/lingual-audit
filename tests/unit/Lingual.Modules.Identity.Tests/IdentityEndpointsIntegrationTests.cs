using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lingual.Modules.AITutor.Infrastructure.Data;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Lingual.Modules.Identity.Contracts.DTOs;
using Lingual.Modules.Identity.Infrastructure.Data;
using Lingual.Modules.Learning.Infrastructure.Data;
using Lingual.Modules.MezonBot.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


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
                ["MezonOAuth2:AuthorizeUrl"] = "https://oauth2.mezon.ai/oauth2/auth",
                ["Mezon:Enabled"] = "false",
                ["ScheduledQuiz:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove background hosted services that require live PostgreSQL / Mezon Gateway
            var hostedDescriptors = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var hd in hostedDescriptors)
            {
                services.Remove(hd);
            }

            void ReplaceDbContext<TContext>(string suffix) where TContext : DbContext
            {
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<TContext>) ||
                    d.ServiceType == typeof(TContext) ||
                    (d.ServiceType.FullName != null && d.ServiceType.FullName.Contains(typeof(TContext).Name))).ToList();

                foreach (var d in descriptors)
                {
                    services.Remove(d);
                }

                services.AddDbContext<TContext>(options => options.UseInMemoryDatabase($"{_dbName}_{suffix}"));
            }

            ReplaceDbContext<IdentityDbContext>("identity");
            ReplaceDbContext<GamificationDbContext>("gamification");
            ReplaceDbContext<LearningDbContext>("learning");
            ReplaceDbContext<MezonBotDbContext>("mezonbot");
            ReplaceDbContext<AITutorDbContext>("aitutor");
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

    [Fact]
    public async Task Get_SwaggerAndContentRoutes_HaveNoAmbiguousRouteCollisions()
    {
        var swaggerRes = await _client.GetAsync("/swagger/v1/swagger.json");
        swaggerRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var vocabRes = await _client.GetAsync("/api/v1/content/vocabulary?limit=5");
        vocabRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var quizzesRes = await _client.GetAsync("/api/v1/content/quizzes?mode=sprint10");
        quizzesRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
