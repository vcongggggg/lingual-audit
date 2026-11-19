using System;
using System.Collections.Generic;

namespace Lingual.Modules.Gamification.Contracts.DTOs;

public record GameLeaderboardEntryDto(
    int Rank,
    string UserId,
    string DisplayName,
    string? AvatarUrl,
    string GameType,
    int HighScore,
    int TotalWins,
    int TotalXpEarned,
    DateTime LastPlayedAt
);

public record GameLeaderboardResponseDto(
    bool Success,
    List<GameLeaderboardEntryDto> Entries
);
