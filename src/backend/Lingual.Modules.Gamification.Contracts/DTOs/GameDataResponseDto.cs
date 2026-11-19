namespace Lingual.Modules.Gamification.Contracts.DTOs;

public record GameDataResponseDto(
    string AttemptId,
    string GameType,
    object Data
);
