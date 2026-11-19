using System.Collections.Generic;

namespace Lingual.Modules.Gamification.Contracts.DTOs;

public record GameSubmitResultDto(
    bool Success,
    int Score,
    int XpEarned,
    double Accuracy,
    double ComboMultiplier,
    int TimeBonus,
    int StreakBonus,
    int DurationSeconds,
    int CorrectCount,
    int TotalQuestions,
    int HighScore,
    bool IsNewHighScore,
    List<string> StampsAwarded
);
