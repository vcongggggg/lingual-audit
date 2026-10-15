using FluentAssertions;
using Lingual.Modules.Identity.Contracts.DTOs;
using Lingual.Modules.Identity.Features.Auth.LoginWithMezon;
using Lingual.Modules.Identity.Infrastructure.Services;
using Moq;

namespace Lingual.Modules.Identity.Tests;

public class LoginWithMezonCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_CallsMezonAuthServiceAndReturnsResult()
    {
        // Arrange
        var mezonAuthMock = new Mock<IMezonAuthService>();
        var request = new MezonAuthRequest
        {
            Code = "valid_auth_code",
            State = "valid_state_123"
        };

        var expectedPayload = new AuthResponseDto
        {
            Token = "mock.jwt.token",
            RefreshToken = "mock_refresh_token",
            ExpiresIn = 3600
        };

        var expectedResult = MezonCallbackResult.Success(expectedPayload);

        mezonAuthMock
            .Setup(m => m.HandleCallbackAsync(request))
            .ReturnsAsync(expectedResult);

        var handler = new LoginWithMezonCommandHandler(mezonAuthMock.Object);
        var command = new LoginWithMezonCommand(request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(200);
        result.Payload.Should().Be(expectedPayload);
        mezonAuthMock.Verify(m => m.HandleCallbackAsync(request), Times.Once);
    }
}
