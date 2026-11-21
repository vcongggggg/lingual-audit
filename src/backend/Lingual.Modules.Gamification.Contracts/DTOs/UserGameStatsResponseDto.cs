using System;
using System.Collections.Generic;

namespace Lingual.Modules.Gamification.Contracts.DTOs;

public record GameStatDetailDto(
    int HighScore,
    int TotalGames,
    int TotalWins,
    int TotalXp,
    double AvgAccuracy,
    DateTime LastPlayedAt
);

public record UserGameStatsResponseDto(
    bool Success,
    string UserId,
    int TotalGamesPlayed,
    int TotalWins,
    int TotalXpEarned,
    Dictionary<string, GameStatDetailDto> ByGame
);
