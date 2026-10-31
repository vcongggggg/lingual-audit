using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Lingual.Modules.AITutor.Contracts;
using Lingual.Modules.AITutor.Contracts.DTOs;
using Lingual.Modules.AITutor.Infrastructure.Data;
using Lingual.Modules.AITutor.Options;
using Lingual.Modules.AITutor.Services;

namespace Lingual.Modules.MezonBot.Tests;

public class AITutorRoleplayTests
{
    private static IServiceScopeFactory CreateMockScopeFactory(AITutorDbContext dbContext)
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(AITutorDbContext))).Returns(dbContext);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(sf => sf.CreateScope()).Returns(scopeMock.Object);

        return scopeFactoryMock.Object;
    }

    private static AITutorDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AITutorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AITutorDbContext(options);
    }

    [Fact]
    public async Task GetAvailableScenariosAsync_ReturnsAllThreeDefaultScenarios()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var scopeFactory = CreateMockScopeFactory(dbContext);
        var loggerMock = new Mock<ILogger<GeminiAITutorService>>();
        var options = Options.Create(new GeminiOptions { ApiKey = "dummy_key" });
        var httpClient = new HttpClient();

        var service = new GeminiAITutorService(httpClient, options, scopeFactory, loggerMock.Object);

        // Act
        var scenarios = await service.GetAvailableScenariosAsync();

        // Assert
        scenarios.Should().NotBeNull();
        scenarios.Should().HaveCount(3);
        scenarios.Select(s => s.Code).Should().Contain(new[] { "cafe_order", "airport_checkin", "job_interview" });
    }

    [Fact]
    public async Task ProcessRoleplayTurnAsync_ReturnsFallback_WhenApiKeyMissing()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var scopeFactory = CreateMockScopeFactory(dbContext);
        var loggerMock = new Mock<ILogger<GeminiAITutorService>>();
        var options = Options.Create(new GeminiOptions { ApiKey = "" }); // Không có key
        var httpClient = new HttpClient();

        var service = new GeminiAITutorService(httpClient, options, scopeFactory, loggerMock.Object);

        var request = new RoleplayTurnRequest(
            ScenarioCode: "cafe_order",
            UserMessage: "Can I have an iced latte?",
            MezonUserId: "123456"
        );

        // Act
        var result = await service.ProcessRoleplayTurnAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ScenarioCode.Should().Be("cafe_order");
        result.CharacterRole.Should().Be("Barista LingLing");
        result.Reply.Should().Contain("Barista LingLing");
    }

    [Fact]
    public async Task ProcessRoleplayTurnAsync_ParsesGeminiRoleplayFeedback_Correctly()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var scopeFactory = CreateMockScopeFactory(dbContext);
        var loggerMock = new Mock<ILogger<GeminiAITutorService>>();
        var options = Options.Create(new GeminiOptions { ApiKey = "test_key", Model = "gemini-flash-lite-latest" });

        var geminiResponsePayload = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = "{\"reply\": \"Sure! One regular iced latte coming right up. Would you like whole milk or oat milk?\", \"has_feedback\": true, \"feedback_correction\": \"💡 Bạn nên nói: \\\"I would like an iced latte, please\\\" thay vì \\\"give me latte\\\" nhé!\", \"explanation\": \"Dùng cấu trúc 'I would like' thể hiện sự lịch sự khi gọi món trong tiếng Anh.\"}"
                            }
                        }
                    }
                }
            }
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(geminiResponsePayload))
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new GeminiAITutorService(httpClient, options, scopeFactory, loggerMock.Object);

        var request = new RoleplayTurnRequest(
            ScenarioCode: "cafe_order",
            UserMessage: "give me latte",
            MezonUserId: "user_789"
        );

        // Act
        var result = await service.ProcessRoleplayTurnAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Reply.Should().Contain("regular iced latte");
        result.HasFeedback.Should().BeTrue();
        result.FeedbackCorrection.Should().StartWith("💡 Bạn nên nói:");
        result.FeedbackCorrection.Should().Contain("thay vì");
        result.Explanation.Should().Contain("I would like");
    }
}

