using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Lingual.Modules.Identity.Domain;
using Lingual.Modules.Identity.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lingual.Modules.Identity.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public void CreateAccessToken_ProducesValidJwtWithExpectedClaims()
    {
        // Arrange
        const string secretKey = "LingualSuperSecretKeyForDevelopment2026McsStudioAuth!";
        const string issuer = "Lingual.Api";
        const string audience = "Lingual.Clients";

        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:Key", secretKey},
            {"Jwt:Issuer", issuer},
            {"Jwt:Audience", audience},
            {"Jwt:ExpiryDays", "7"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var service = new TokenService(NullLogger<TokenService>.Instance, configuration);

        var user = new User
        {
            Id = Guid.NewGuid(),
            MezonUserId = "2100896006294999040",
            DisplayName = "Ngo Van Cong",
            Role = "learner",
            Status = "active"
        };

        // Act
        var result = service.CreateAccessToken(user);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresIn.Should().BeGreaterThan(0);

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(result.Token).Should().BeTrue();

        var jwt = handler.ReadJwtToken(result.Token);
        jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => (c.Type == "role" || c.Type == ClaimTypes.Role) && c.Value == "learner");
        var payloadJson = Encoding.UTF8.GetString(Microsoft.IdentityModel.Tokens.Base64UrlEncoder.DecodeBytes(result.Token.Split('.')[1]));
        payloadJson.Should().Contain($"\"iss\":\"{issuer}\"");
        payloadJson.Should().Contain($"\"aud\":\"{audience}\"");


        payloadJson.Should().Contain("\"mezon_user_id\":\"2100896006294999040\"");
    }
}

