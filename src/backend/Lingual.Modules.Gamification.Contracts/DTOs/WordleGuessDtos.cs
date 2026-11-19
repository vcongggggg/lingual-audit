using System.Collections.Generic;

namespace Lingual.Modules.Gamification.Contracts.DTOs;

public record WordleGuessRequestDto(string AttemptId, string Guess);

public record WordleGuessResponseDto(
    bool Success,
    List<string> Evaluations,
    int RemainingAttempts,
    bool IsWon,
    bool IsGameOver,
    string? Message = null);
