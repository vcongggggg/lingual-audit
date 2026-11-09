using System.Text.Json.Serialization;

namespace Lingual.Modules.Gamification.Contracts.DTOs;

public sealed record LeaderboardResponseDto(
    [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt,
    [property: JsonPropertyName("entries")] IReadOnlyList<LeaderboardEntryDto> Entries,
    [property: JsonPropertyName("my_rank")] LeaderboardEntryDto? MyRank = null,
    [property: JsonPropertyName("total_participants")] long TotalParticipants = 0);

