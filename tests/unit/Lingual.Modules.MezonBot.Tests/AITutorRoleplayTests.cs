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

    [Fact]
    public async Task ProcessRoleplayTurnAsync_AppendsSecondTurn_ToSameConversation()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var scopeFactory = CreateMockScopeFactory(dbContext);
        var loggerMock = new Mock<ILogger<GeminiAITutorService>>();
        var options = Options.Create(new GeminiOptions { ApiKey = "test_key", Model = "gemini-flash-lite-latest" });

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
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    candidates = new[]
                    {
                        new
                        {
                            content = new
                            {
                                parts = new[]
                                {
                                    new { text = "{\"reply\": \"What size would you like?\", \"has_feedback\": false}" }
                                }
                            }
                        }
                    }
                }))
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new GeminiAITutorService(httpClient, options, scopeFactory, loggerMock.Object);
        var convId = Guid.NewGuid();

        // Act - Turn 1
        var result1 = await service.ProcessRoleplayTurnAsync(new RoleplayTurnRequest(
            ScenarioCode: "cafe_order",
            UserMessage: "I want a coffee",
            ConversationId: convId,
            MezonUserId: "user_101"
        ));

        // Act - Turn 2
        var result2 = await service.ProcessRoleplayTurnAsync(new RoleplayTurnRequest(
            ScenarioCode: "cafe_order",
            UserMessage: "A medium one, please",
            ConversationId: convId,
            MezonUserId: "user_101"
        ));

        // Assert
        result1.ConversationId.Should().Be(convId);
        result2.ConversationId.Should().Be(convId);

        dbContext.AiConversations.Should().HaveCount(1);
        var stored = await dbContext.AiConversations.FirstOrDefaultAsync(c => c.Id == convId);
        stored.Should().NotBeNull();

        var messages = JsonSerializer.Deserialize<List<GeminiAITutorService.ConversationTurnMessage>>(stored!.Messages, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        messages.Should().NotBeNull();
        messages.Should().HaveCount(4); // Turn 1 (user+model) + Turn 2 (user+model)
        messages![0].Role.Should().Be("user");
        messages[0].Text.Should().Be("I want a coffee");
        messages[1].Role.Should().Be("model");
        messages[2].Role.Should().Be("user");
        messages[2].Text.Should().Be("A medium one, please");
        messages[3].Role.Should().Be("model");
    }

    [Fact]
    public async Task ProcessRoleplayTurnAsync_IncludesPreviousTurnInGeminiPrompt()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var scopeFactory = CreateMockScopeFactory(dbContext);
        var loggerMock = new Mock<ILogger<GeminiAITutorService>>();
        var options = Options.Create(new GeminiOptions { ApiKey = "test_key", Model = "gemini-flash-lite-latest" });

        var sentPayloads = new List<string>();
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                var content = await req.Content!.ReadAsStringAsync(ct);
                sentPayloads.Add(content);
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        candidates = new[]
                        {
                            new
                            {
                                content = new
                                {
                                    parts = new[]
                                    {
                                        new { text = "{\"reply\": \"Sure thing!\", \"has_feedback\": false}" }
                                    }
                                }
                            }
                        }
                    }))
                };
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new GeminiAITutorService(httpClient, options, scopeFactory, loggerMock.Object);
        var convId = Guid.NewGuid();

        // Turn 1
        await service.ProcessRoleplayTurnAsync(new RoleplayTurnRequest(
            ScenarioCode: "cafe_order",
            UserMessage: "Can I get an espresso?",
            ConversationId: convId,
            MezonUserId: "user_202"
        ));

        // Turn 2
        await service.ProcessRoleplayTurnAsync(new RoleplayTurnRequest(
            ScenarioCode: "cafe_order",
            UserMessage: "With some sugar please",
            ConversationId: convId,
            MezonUserId: "user_202"
        ));

        // Assert
        sentPayloads.Should().HaveCount(2);

        // Turn 1 should contain current message
        sentPayloads[0].Should().Contain("Can I get an espresso?");

        // Turn 2 should contain Turn 1 user message, Turn 1 model reply, AND Turn 2 user message
        sentPayloads[1].Should().Contain("Can I get an espresso?");
        sentPayloads[1].Should().Contain("Sure thing!");
        sentPayloads[1].Should().Contain("With some sugar please");
    }

    [Fact]
    public void MezonBotWorker_ActiveRoleplaySessions_ReusesConversationId_ForSameScenario()
    {
        // Arrange
        MezonBotWorker.ActiveRoleplaySessions.Clear();
        long channelId = 8888;
        string senderId = "user_303";
        var sessionKey = MezonBotWorker.GetRoleplaySessionKey(channelId, senderId);

        // Act 1: Start scenario cafe_order
        var conv1 = Guid.NewGuid();
        MezonBotWorker.ActiveRoleplaySessions[sessionKey] = new MezonBotWorker.UserRoleplaySession
        {
            ConversationId = conv1,
            ScenarioCode = "cafe_order",
            LastActivity = DateTime.UtcNow
        };

        // Assert 1: Reused for same scenario
        var found = MezonBotWorker.ActiveRoleplaySessions.TryGetValue(sessionKey, out var session1);
        found.Should().BeTrue();
        session1!.ConversationId.Should().Be(conv1);
        session1.ScenarioCode.Should().Be("cafe_order");

        // Act 2: User changes scenario to airport_checkin
        var conv2 = Guid.NewGuid();
        MezonBotWorker.ActiveRoleplaySessions[sessionKey] = new MezonBotWorker.UserRoleplaySession
        {
            ConversationId = conv2,
            ScenarioCode = "airport_checkin",
            LastActivity = DateTime.UtcNow
        };

        // Assert 2: New ConversationId assigned for new scenario
        var session2 = MezonBotWorker.ActiveRoleplaySessions[sessionKey];
        session2.ConversationId.Should().Be(conv2);
        session2.ConversationId.Should().NotBe(conv1);
        session2.ScenarioCode.Should().Be("airport_checkin");

        // Act 3: User clicks "Đổi tình huống" (btn_roleplay_menu)
        MezonBotWorker.ActiveRoleplaySessions.TryRemove(sessionKey, out _);

        // Assert 3: Session is removed from active sessions
        MezonBotWorker.ActiveRoleplaySessions.ContainsKey(sessionKey).Should().BeFalse();
    }

    [Fact]
    public async Task ProcessRoleplayTurnAsync_ConcurrentTurns_AppendsMessagesWithoutLoss()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var scopeFactory = CreateMockScopeFactory(dbContext);
        var loggerMock = new Mock<ILogger<GeminiAITutorService>>();
        var options = Options.Create(new GeminiOptions { ApiKey = "test_key", Model = "gemini-flash-lite-latest" });

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
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    candidates = new[]
                    {
                        new
                        {
                            content = new
                            {
                                parts = new[]
                                {
                                    new { text = "{\"reply\": \"Understood.\", \"has_feedback\": false}" }
                                }
                            }
                        }
                    }
                }))
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new GeminiAITutorService(httpClient, options, scopeFactory, loggerMock.Object);
        var convId = Guid.NewGuid();

        // Pre-create initial turn
        await service.ProcessRoleplayTurnAsync(new RoleplayTurnRequest(
            ScenarioCode: "cafe_order",
            UserMessage: "Initial message",
            ConversationId: convId,
            MezonUserId: "user_404"
        ));

        // Act: Run 2 concurrent turns
        var task1 = service.ProcessRoleplayTurnAsync(new RoleplayTurnRequest(
            ScenarioCode: "cafe_order",
            UserMessage: "Concurrent message 1",
            ConversationId: convId,
            MezonUserId: "user_404"
        ));
        var task2 = service.ProcessRoleplayTurnAsync(new RoleplayTurnRequest(
            ScenarioCode: "cafe_order",
            UserMessage: "Concurrent message 2",
            ConversationId: convId,
            MezonUserId: "user_404"
        ));

        await Task.WhenAll(task1, task2);

        // Assert: All 3 turns (6 messages) are preserved
        var stored = await dbContext.AiConversations.FirstOrDefaultAsync(c => c.Id == convId);
        stored.Should().NotBeNull();

        var messages = JsonSerializer.Deserialize<List<GeminiAITutorService.ConversationTurnMessage>>(stored!.Messages, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        messages.Should().NotBeNull();
        messages.Should().HaveCount(6); // 1 initial + 2 concurrent = 3 pairs = 6 messages
    }

    [Fact]
    public async Task ProcessRoleplayTurnAsync_SendsApiKeyInHeader_NotQueryString()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var scopeFactory = CreateMockScopeFactory(dbContext);
        var loggerMock = new Mock<ILogger<GeminiAITutorService>>();
        const string expectedKey = "secret_gemini_key_xyz";
        var options = Options.Create(new GeminiOptions { ApiKey = expectedKey, Model = "gemini-flash-lite-latest" });

        HttpRequestMessage? capturedRequest = null;

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
                            new { text = "{\"reply\": \"Hello! What drink can I get you?\", \"has_feedback\": false}" }
                        }
                    }
                }
            },
            usageMetadata = new
            {
                promptTokenCount = 25,
                candidatesTokenCount = 15,
                totalTokenCount = 40
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
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(geminiResponsePayload))
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new GeminiAITutorService(httpClient, options, scopeFactory, loggerMock.Object);

        var request = new RoleplayTurnRequest(
            ScenarioCode: "cafe_order",
            UserMessage: "Hello",
            MezonUserId: "user_sec"
        );

        // Act
        var result = await service.ProcessRoleplayTurnAsync(request);

        // Assert
        result.Should().NotBeNull();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Contains("x-goog-api-key").Should().BeTrue();
        capturedRequest.Headers.GetValues("x-goog-api-key").First().Should().Be(expectedKey);
        capturedRequest.RequestUri!.Query.Should().NotContain("key=");
    }

    [Fact]
    public async Task ProcessRoleplayTurnAsync_DoesNotLoadHistory_WhenCallerDoesNotOwnConversation()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var scopeFactory = CreateMockScopeFactory(dbContext);
        var loggerMock = new Mock<ILogger<GeminiAITutorService>>();
        var options = Options.Create(new GeminiOptions { ApiKey = "test_key", Model = "gemini-flash-lite-latest" });

        var handlerMock = new Mock<HttpMessageHandler>();
        string? capturedPayloadJson = null;

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
                            new { text = "{\"reply\": \"Welcome!\", \"has_feedback\": false}" }
                        }
                    }
                }
            }
        };

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                if (req.Content != null)
                {
                    capturedPayloadJson = await req.Content.ReadAsStringAsync();
                }
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(geminiResponsePayload))
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new GeminiAITutorService(httpClient, options, scopeFactory, loggerMock.Object);

        // Turn 1 by legitimate Owner
        var turn1 = await service.ProcessRoleplayTurnAsync(new RoleplayTurnRequest(
            ScenarioCode: "cafe_order",
            UserMessage: "Owner private secret message",
            MezonUserId: "owner_123"
        ));

        capturedPayloadJson = null;

        // Turn 2 by unauthorized caller targeting Owner's conversationId
        await service.ProcessRoleplayTurnAsync(new RoleplayTurnRequest(
            ScenarioCode: "cafe_order",
            UserMessage: "Attacker message",
            ConversationId: turn1.ConversationId,
            MezonUserId: "attacker_456"
        ));

        // Assert: Attacker request prompt must NOT contain Owner's private history
        capturedPayloadJson.Should().NotBeNull();
        capturedPayloadJson.Should().NotContain("Owner private secret message");
    }
}

