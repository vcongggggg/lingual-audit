using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Lingual.Modules.Gamification.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lingual.Modules.Gamification.Tests;

public class WordDuelServiceTests
{
    private static (WordDuelService Service, GamificationDbContext DbContext, Mock<IXpService> XpMock, Mock<IDuelClanNotifier> NotifierMock) CreateSut()
    {
        var options = new DbContextOptionsBuilder<GamificationDbContext>()
            .UseInMemoryDatabase($"WordDuelTests_{Guid.NewGuid()}")
            .Options;

        var dbContext = new GamificationDbContext(options);
        var xpMock = new Mock<IXpService>();
        xpMock
            .Setup(x => x.AwardXpAsync(It.IsAny<AwardXpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AwardXpRequest req, CancellationToken _) => new AwardXpResult(
                IsSuccess: true,
                AwardedXp: req.XpDelta,
                TotalXp: req.XpDelta,
                CurrentStreak: 1,
                IsStreakActiveToday: true,
                AlreadyAwarded: false,
                Message: "OK"
            ));

        var notifierMock = new Mock<IDuelClanNotifier>();
        notifierMock
            .Setup(n => n.PublishMatchHonorCardAsync(It.IsAny<DuelMatchDto>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var services = new ServiceCollection();
        services.AddSingleton(notifierMock.Object);
        var provider = services.BuildServiceProvider();

        var service = new WordDuelService(
            dbContext,
            xpMock.Object,
            provider,
            NullLogger<WordDuelService>.Instance
        );

        return (service, dbContext, xpMock, notifierMock);
    }

    [Theory]
    [InlineData(false, 1000, 0)]
    [InlineData(true, 0, 200)]
    [InlineData(true, 5000, 150)]
    [InlineData(true, 10000, 100)]
    [InlineData(true, 12000, 100)]
    public void CalculateRoundScore_ShouldComputeSpeedBonusCorrectly(bool isCorrect, int responseMs, decimal expectedScore)
    {
        var score = WordDuelService.CalculateRoundScore(isCorrect, responseMs);
        score.Should().Be(expectedScore);
    }

    [Fact]
    public async Task CreateChallengeAsync_ShouldEnforceDistinctChallengerAndOpponentIds()
    {
        var (service, _, _, _) = CreateSut();
        var challengerId = Guid.NewGuid();

        var match = await service.CreateChallengeAsync(new CreateDuelChallengeRequest(
            ChallengerId: challengerId,
            ChallengerName: "ChallengerOne",
            OpponentId: Guid.Empty,
            OpponentName: "OpenOpponent",
            QuestionCount: 5,
            ClanChannelId: 998877,
            AutoAccept: false
        ));

        match.Should().NotBeNull();
        match.ChallengerId.Should().Be(challengerId);
        match.OpponentId.Should().NotBe(challengerId);
        match.Status.Should().Be("pending");
        match.QuestionCount.Should().Be(5);
    }

    [Fact]
    public async Task AcceptDeclineAndExpireChallenge_ShouldTransitionStatesProperly()
    {
        var (service, _, _, _) = CreateSut();
        var challengerId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();

        // 1. Accept flow
        var m1 = await service.CreateChallengeAsync(new CreateDuelChallengeRequest(
            ChallengerId: challengerId,
            ChallengerName: "Alice",
            OpponentId: Guid.Empty,
            OpponentName: "Anyone"
        ));
        var accepted = await service.AcceptChallengeAsync(m1.Id, opponentId, "Bob");
        accepted.Should().NotBeNull();
        accepted!.Status.Should().Be("accepted");
        accepted.OpponentId.Should().Be(opponentId);
        accepted.OpponentName.Should().Be("Bob");

        // 2. Decline flow
        var m2 = await service.CreateChallengeAsync(new CreateDuelChallengeRequest(
            ChallengerId: challengerId,
            ChallengerName: "Alice",
            OpponentId: opponentId,
            OpponentName: "Bob"
        ));
        var declined = await service.DeclineChallengeAsync(m2.Id, opponentId);
        declined.Should().NotBeNull();
        declined!.Status.Should().Be("declined");

        // 3. Expire flow
        var m3 = await service.CreateChallengeAsync(new CreateDuelChallengeRequest(
            ChallengerId: challengerId,
            ChallengerName: "Alice",
            OpponentId: opponentId,
            OpponentName: "Bob"
        ));
        var expired = await service.ExpireNoShowMatchAsync(m3.Id);
        expired.Should().NotBeNull();
        expired!.Status.Should().Be("expired");
    }

    [Fact]
    public async Task FiveRoundMatch_ShouldGradeAnswersAndAwardXpAndNotifyClanIdempotently()
    {
        var (service, _, xpMock, notifierMock) = CreateSut();
        var challengerId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();

        var match = await service.CreateChallengeAsync(new CreateDuelChallengeRequest(
            ChallengerId: challengerId,
            ChallengerName: "PlayerA",
            OpponentId: opponentId,
            OpponentName: "PlayerB",
            QuestionCount: 5,
            ClanChannelId: 123456,
            AutoAccept: true
        ));

        await service.StartMatchInProgressAsync(match.Id);

        for (int round = 1; round <= 5; round++)
        {
            var question = await service.GetRoundQuestionAsync(match.Id, round);
            question.Should().NotBeNull();
            question!.RoundNumber.Should().Be(round);
            question.Options.Should().HaveCountGreaterOrEqualTo(4);

            // Challenger submits answer fast (2000ms)
            var resA = await service.SubmitAnswerAsync(new SubmitDuelAnswerRequest(
                MatchId: match.Id,
                UserId: challengerId,
                QuestionId: question.Id,
                SelectedKey: "B",
                ResponseMs: 2000
            ));
            resA.Accepted.Should().BeTrue();

            // Duplicate answer for same question should be rejected
            var dupA = await service.SubmitAnswerAsync(new SubmitDuelAnswerRequest(
                MatchId: match.Id,
                UserId: challengerId,
                QuestionId: question.Id,
                SelectedKey: "B",
                ResponseMs: 2500
            ));
            dupA.Accepted.Should().BeFalse();

            // Opponent submits slower answer (8000ms)
            var resB = await service.SubmitAnswerAsync(new SubmitDuelAnswerRequest(
                MatchId: match.Id,
                UserId: opponentId,
                QuestionId: question.Id,
                SelectedKey: resA.CorrectKey, // Ensure Opponent gets it right too, or test scoring
                ResponseMs: 8000
            ));
            resB.Accepted.Should().BeTrue();
            resB.BothAnsweredCurrentRound.Should().BeTrue();
        }

        var completed = await service.GetMatchAsync(match.Id);
        completed.Should().NotBeNull();
        completed!.Status.Should().Be("completed");
        completed.WinnerId.Should().NotBeNull();
        completed.QuestionReviews.Should().NotBeNull();
        completed.QuestionReviews!.Count.Should().Be(5);

        // Verify XP was awarded to both Challenger and Opponent
        xpMock.Verify(x => x.AwardXpAsync(
            It.Is<AwardXpRequest>(r => r.UserId == challengerId && (r.XpDelta == 30 || r.XpDelta == 10 || r.XpDelta == 15)),
            It.IsAny<CancellationToken>()), Times.Once);

        xpMock.Verify(x => x.AwardXpAsync(
            It.Is<AwardXpRequest>(r => r.UserId == opponentId && (r.XpDelta == 30 || r.XpDelta == 10 || r.XpDelta == 15)),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify Clan Honor Card was published exactly once even if CompleteMatchAsync is called again
        await service.CompleteMatchAsync(match.Id);
        notifierMock.Verify(n => n.PublishMatchHonorCardAsync(
            It.Is<DuelMatchDto>(d => d.Id == match.Id),
            It.IsAny<long?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForfeitMatchAsync_ShouldDeclareRemainingPlayerWinnerAndAward30Xp()
    {
        var (service, _, xpMock, notifierMock) = CreateSut();
        var challengerId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();

        var match = await service.CreateChallengeAsync(new CreateDuelChallengeRequest(
            ChallengerId: challengerId,
            ChallengerName: "Challenger",
            OpponentId: opponentId,
            OpponentName: "DisconnectingOpponent",
            QuestionCount: 5,
            ClanChannelId: 555666,
            AutoAccept: true
        ));

        var forfeited = await service.ForfeitMatchAsync(match.Id, opponentId, "disconnect_timeout");
        forfeited.Should().NotBeNull();
        forfeited!.Status.Should().Be("completed");
        forfeited.WinnerId.Should().Be(challengerId);
        forfeited.ChallengerXpAwarded.Should().Be(30);
        forfeited.OpponentXpAwarded.Should().Be(10);

        xpMock.Verify(x => x.AwardXpAsync(
            It.Is<AwardXpRequest>(r => r.UserId == challengerId && r.XpDelta == 30),
            It.IsAny<CancellationToken>()), Times.Once);

        notifierMock.Verify(n => n.PublishMatchHonorCardAsync(
            It.Is<DuelMatchDto>(d => d.Id == match.Id && d.WinnerId == challengerId),
            It.IsAny<long?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
