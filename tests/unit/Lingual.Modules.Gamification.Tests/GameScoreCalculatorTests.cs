using FluentAssertions;
using Lingual.Modules.Gamification.Domain;
using Xunit;

namespace Lingual.Modules.Gamification.Tests;

public class GameScoreCalculatorTests
{
    [Fact]
    public void Calculate_ShouldMatchSpecExample_FiveOutOfFiveInTwentySeconds()
    {
        // Spec example: 5/5 dung trong 20 giay (60 - 20 = 40s con lai)
        // base 100, combo 2.0, timeBonus 32, streak 25 -> finalScore 257, XP 64
        var input = new GameScoreInput(
            CorrectCount: 5,
            TotalQuestions: 5,
            TimeRemainingSeconds: 40,
            ConsecutiveCorrect: 5,
            Mistakes: 0,
            HasTimer: true);

        var result = GameScoreCalculator.Calculate(input);

        result.FinalScore.Should().Be(257);
        result.XpEarned.Should().Be(64);
        result.Accuracy.Should().Be(1.0);
        result.ComboMultiplier.Should().Be(2.0);
        result.TimeBonus.Should().Be(32);
        result.StreakBonus.Should().Be(25);
        result.MistakePenalty.Should().Be(0);
    }

    [Fact]
    public void Calculate_ComboMultiplier_ShouldReflectActualConsecutiveCorrect()
    {
        // 4 correct total, but consecutive correct was only 2 (due to mistake in between)
        var input = new GameScoreInput(
            CorrectCount: 4,
            TotalQuestions: 5,
            TimeRemainingSeconds: 10,
            ConsecutiveCorrect: 2,
            Mistakes: 1,
            HasTimer: true);

        var result = GameScoreCalculator.Calculate(input);

        // comboMultiplier = min(5, 1 + floor(2 / 2) * 0.5) = 1.5
        result.ComboMultiplier.Should().Be(1.5);
        result.StreakBonus.Should().Be(0); // Not 100%
        result.MistakePenalty.Should().Be(5); // 1 mistake * 5
    }

    [Fact]
    public void Calculate_WhenZeroCorrect_ShouldAwardZeroXpAndZeroScore()
    {
        var input = new GameScoreInput(
            CorrectCount: 0,
            TotalQuestions: 5,
            TimeRemainingSeconds: 20,
            ConsecutiveCorrect: 0,
            Mistakes: 5,
            HasTimer: true);

        var result = GameScoreCalculator.Calculate(input);

        result.FinalScore.Should().Be(0);
        result.XpEarned.Should().Be(0);
        result.Accuracy.Should().Be(0.0);
    }

    [Fact]
    public void Calculate_MaxTimeBonus_ShouldBeCappedAtFifty()
    {
        var input = new GameScoreInput(
            CorrectCount: 5,
            TotalQuestions: 5,
            TimeRemainingSeconds: 100, // 100 * 0.8 = 80 -> capped at 50
            ConsecutiveCorrect: 5,
            Mistakes: 0,
            HasTimer: true);

        var result = GameScoreCalculator.Calculate(input);

        result.TimeBonus.Should().Be(50);
    }

    [Fact]
    public void Calculate_WithoutTimer_ShouldHaveZeroTimeBonus()
    {
        var input = new GameScoreInput(
            CorrectCount: 1,
            TotalQuestions: 1,
            TimeRemainingSeconds: 0,
            ConsecutiveCorrect: 1,
            Mistakes: 0,
            HasTimer: false);

        var result = GameScoreCalculator.Calculate(input);

        result.TimeBonus.Should().Be(0);
    }
}
