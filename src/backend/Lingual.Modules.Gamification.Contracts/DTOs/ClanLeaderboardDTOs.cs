using System.Text.Json.Serialization;

namespace Lingual.Modules.Gamification.Contracts.DTOs;

public sealed record ClanLeaderboardEntryDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("rank")] int Rank,
    [property: JsonPropertyName("previous_rank")] int PreviousRank,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("avatar")] string Avatar,
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("tag")] string Tag,
    [property: JsonPropertyName("words_learned")] int WordsLearned,
    [property: JsonPropertyName("xp")] double Xp,
    [property: JsonPropertyName("member_count")] int MemberCount,
    [property: JsonPropertyName("is_current_user")] bool IsCurrentUser = false);

public sealed record ClanLeaderboardResponseDto(
    [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt,
    [property: JsonPropertyName("entries")] IReadOnlyList<ClanLeaderboardEntryDto> Entries,
    [property: JsonPropertyName("total_clans")] int TotalClans);
