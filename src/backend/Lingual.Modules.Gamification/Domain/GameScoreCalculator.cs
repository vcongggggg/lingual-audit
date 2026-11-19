using System;

namespace Lingual.Modules.Gamification.Domain;

public sealed record GameScoreInput(
    int CorrectCount,
    int TotalQuestions,
    int TimeRemainingSeconds,
    int ConsecutiveCorrect,
    int Mistakes,
    bool HasTimer = true);

public sealed record GameScoreOutput(
    int FinalScore,
    int XpEarned,
    double Accuracy,
    double ComboMultiplier,
    int TimeBonus,
    int StreakBonus,
    int MistakePenalty);

public static class GameScoreCalculator
{
    public static GameScoreOutput Calculate(GameScoreInput input)
    {
        double accuracy = input.TotalQuestions > 0 ? (double)input.CorrectCount / input.TotalQuestions : 0.0;
        accuracy = Math.Round(accuracy, 2);

        // Base score: 20 points per correct answer
        int baseScore = input.CorrectCount * 20;

        // Combo multiplier: min(5, 1 + floor(chuoi dung LIEN TIEP / 2) * 0.5)
        double comboMultiplier = Math.Min(5.0, 1.0 + Math.Floor(input.ConsecutiveCorrect / 2.0) * 0.5);

        // Time bonus: min(50, floor(so giay con lai * 0.8)) (ngan sach 60 giay)
        int timeBonus = 0;
        if (input.HasTimer)
        {
            timeBonus = Math.Min(50, (int)Math.Floor(Math.Max(0, input.TimeRemainingSeconds) * 0.8));
        }

        // Streak bonus: 25 neu dung 100%, khong thi 0
        int streakBonus = Math.Abs(accuracy - 1.0) < 0.001 && input.TotalQuestions > 0 ? 25 : 0;

        // Penalty for mistakes: so cau sai * 5
        int mistakePenalty = input.Mistakes * 5;

        // finalScore = max(0, round(base * combo + timeBonus + streakBonus - penalty))
        int finalScore = Math.Max(0, (int)Math.Round((baseScore * comboMultiplier) + timeBonus + streakBonus - mistakePenalty));

        // xpEarned = co cau dung ? max(5, round(finalScore / 4)) : 0
        int xpEarned = input.CorrectCount > 0 ? Math.Max(5, (int)Math.Round(finalScore / 4.0)) : 0;

        return new GameScoreOutput(
            FinalScore: finalScore,
            XpEarned: xpEarned,
            Accuracy: accuracy,
            ComboMultiplier: comboMultiplier,
            TimeBonus: timeBonus,
            StreakBonus: streakBonus,
            MistakePenalty: mistakePenalty);
    }
}
