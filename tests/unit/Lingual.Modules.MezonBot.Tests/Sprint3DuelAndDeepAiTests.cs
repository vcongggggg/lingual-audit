using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lingual.Modules.AITutor.Contracts.DTOs;
using Lingual.Modules.AITutor.Options;
using Lingual.Modules.AITutor.Services;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.MezonBot;
using Lingual.Modules.MezonBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lingual.Modules.MezonBot.Tests;

public class Sprint3DuelAndDeepAiTests
{
    [Theory]
    [InlineData("/duel", "duel")]
    [InlineData("/duel @minh", "duel")]
    [InlineData("/duel bot", "duel")]
    [InlineData("  /DUEL @user123  ", "duel")]
    public void ResolveCommandType_ShouldRecognizeDuelCommands(string rawInput, string expectedType)
    {
        var commandType = MezonBotWorker.ResolveCommandType(rawInput);
        commandType.Should().Be(expectedType);
    }

    [Fact]
    public async Task ExplainQuizMistakeAsync_WhenApiKeyMissing_ShouldReturnRichDeterministicFallback()
    {
        var options = Options.Create(new GeminiOptions
        {
            ApiKey = string.Empty,
            Model = "gemini-2.5-flash"
        });
        var httpClient = new HttpClient();
        var scopeFactory = new Mock<IServiceScopeFactory>().Object;
        var service = new GeminiAITutorService(httpClient, options, scopeFactory, NullLogger<GeminiAITutorService>.Instance);

        var request = new QuizMistakeAnalysisRequest(
            QuestionPrompt: "She ___ to the office every morning by metro.",
            SelectedOptionText: "go",
            CorrectOptionText: "goes",
            Level: "A1",
            ExistingExplanation: "Chủ ngữ ngôi thứ 3 số ít thêm -es."
        );

        var response = await service.ExplainQuizMistakeAsync(request);

        response.Should().NotBeNull();
        response.QuestionPrompt.Should().Be(request.QuestionPrompt);
        response.SelectedOptionText.Should().Be("go");
        response.CorrectOptionText.Should().Be("goes");
        response.WhyWrong.Should().NotBeNullOrWhiteSpace();
        response.GrammarAndContextRule.Should().NotBeNullOrWhiteSpace();
        response.Synonyms.Should().NotBeEmpty();
        response.Antonyms.Should().NotBeEmpty();
        response.MnemonicTip.Should().NotBeNullOrWhiteSpace();
        response.ExampleSentence.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExplainQuizMistakeAsync_WhenGeminiReturnsJson_ShouldParseDeepAnalysisCorrectly()
    {
        const string fakeGeminiPayload = """
        {
          "candidates": [
            {
              "content": {
                "parts": [
                  {
                    "text": "{\"why_wrong\":\"Bạn chọn 'borrow' nhưng ngữ cảnh là cho mượn đi.\",\"grammar_and_context_rule\":\"Lend someone something = cho ai mượn; Borrow from = mượn từ ai.\",\"synonyms\":[\"loan\",\"advance\",\"grant\"],\"antonyms\":[\"borrow\",\"take\"],\"mnemonic_tip\":\"Lend = Cho đi (Leave), Borrow = Mang về (Bring).\",\"example_sentence\":\"Could you lend me your umbrella for an hour?\"}"
                  }
                ]
              }
            }
          ]
        }
        """;

        var handler = new StubHttpMessageHandler(fakeGeminiPayload, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new GeminiOptions
        {
            ApiKey = "test-api-key-sprint3",
            Model = "gemini-2.5-flash"
        });
        var scopeFactory = new Mock<IServiceScopeFactory>().Object;

        var service = new GeminiAITutorService(httpClient, options, scopeFactory, NullLogger<GeminiAITutorService>.Instance);
        var result = await service.ExplainQuizMistakeAsync(new QuizMistakeAnalysisRequest(
            QuestionPrompt: "Can you ___ me your pen for a minute?",
            SelectedOptionText: "borrow",
            CorrectOptionText: "lend",
            Level: "A2"
        ));

        result.WhyWrong.Should().Contain("borrow");
        result.GrammarAndContextRule.Should().Contain("Lend");
        result.Synonyms.Should().Contain("loan");
        result.Antonyms.Should().Contain("borrow");
        result.MnemonicTip.Should().Contain("Lend");
        result.ExampleSentence.Should().Contain("umbrella");
    }

    [Fact]
    public async Task DuelClanNotifier_ShouldEnforceIdempotencyPerMatchId()
    {
        var notifier = new DuelClanNotifier(NullLogger<DuelClanNotifier>.Instance);
        var matchId = Guid.NewGuid();
        var match = new DuelMatchDto(
            Id: matchId,
            ChallengerId: Guid.NewGuid(),
            ChallengerName: "Cong",
            OpponentId: Guid.NewGuid(),
            OpponentName: "Tri",
            Status: "completed",
            QuestionCount: 5,
            ChallengerScore: 850,
            OpponentScore: 720,
            ChallengerCorrect: 5,
            OpponentCorrect: 4,
            WinnerId: Guid.NewGuid(),
            ChallengerXpAwarded: 30,
            OpponentXpAwarded: 10,
            CreatedAt: DateTime.UtcNow.AddMinutes(-2),
            StartedAt: DateTime.UtcNow.AddMinutes(-2),
            CompletedAt: DateTime.UtcNow,
            CurrentRound: 5,
            IsBotMatch: false,
            ClanChannelId: 0
        );

        var firstPublish = await notifier.PublishMatchHonorCardAsync(match, preferredChannelId: 0);
        var duplicatePublish = await notifier.PublishMatchHonorCardAsync(match, preferredChannelId: 0);

        firstPublish.Should().BeTrue();
        duplicatePublish.Should().BeFalse();
    }

    private sealed class StubHttpMessageHandler(string json, HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
