using System.Text.Json.Serialization;

namespace Lingual.Modules.Learning.Contracts.DTOs;

public sealed record VocabularyItemDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("term")] string Term,
    [property: JsonPropertyName("normalized_term")] string NormalizedTerm,
    [property: JsonPropertyName("source_language")] string SourceLanguage,
    [property: JsonPropertyName("target_language")] string TargetLanguage,
    [property: JsonPropertyName("ipa")] string? Ipa,
    [property: JsonPropertyName("part_of_speech")] string? PartOfSpeech,
    [property: JsonPropertyName("meaning")] string Meaning,
    [property: JsonPropertyName("pronunciation_audio_url")] string? PronunciationAudioUrl,
    [property: JsonPropertyName("image_url")] string? ImageUrl,
    [property: JsonPropertyName("examples")] object Examples);

public sealed record VocabularyListResponseDto(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("words")] IReadOnlyList<VocabularyItemDto> Words);

public sealed record VocabularyQueryParameters(
    int Limit = 50,
    string SourceLanguage = "en",
    string TargetLanguage = "vi",
    string? Level = null);
