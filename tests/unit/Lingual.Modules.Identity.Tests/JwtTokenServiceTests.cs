using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
using Lingual.Modules.Identity.Domain;
using Lingual.Modules.Identity.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Lingual.Modules.Identity.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateToken_ProducesValidJwtWithExpectedClaims()
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

        var service = new JwtTokenService(configuration);

        var user = new User
        {
            Id = Guid.NewGuid(),
            MezonUserId = "2100896006294999040",
            DisplayName = "Ngo Van Cong",
            Role = "learner",
            Status = "active"
        };

        // Act
        var (token, expiresIn) = service.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        expiresIn.Should().BeGreaterThan(0);

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();

        var jwt = handler.ReadJwtToken(token);
        jwt.Subject.Should().Be(user.Id.ToString());

        jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "mezonUserId" && c.Value == "2100896006294999040");
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "learner");
        jwt.Claims.Should().Contain(c => c.Type == "iss" && c.Value == issuer);

        // Verify payload JSON structure contains standard claims
        var parts = token.Split('.');
        parts.Length.Should().Be(3);
        var payloadJson = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(parts[1]));
        payloadJson.Should().Contain($"\"iss\":\"{issuer}\"");
        payloadJson.Should().Contain($"\"aud\":\"{audience}\"");
        payloadJson.Should().Contain("\"exp\":");
    }
}
