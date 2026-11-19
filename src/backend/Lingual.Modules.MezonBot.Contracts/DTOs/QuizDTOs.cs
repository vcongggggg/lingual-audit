using System.Text.Json.Serialization;

namespace Lingual.Modules.MezonBot.Contracts.DTOs;

public sealed record QuizQuestionDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("sort_order")] short SortOrder,
    [property: JsonPropertyName("level")] string Level,
    [property: JsonPropertyName("difficulty")] short Difficulty,
    [property: JsonPropertyName("question_type")] string QuestionType,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("options")] object Options,
    [property: JsonPropertyName("correct_answer")] string CorrectAnswer,
    [property: JsonPropertyName("explanation")] string? Explanation,
    [property: JsonPropertyName("points")] int Points);

public sealed record QuizListResponseDto(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("questions")] IReadOnlyList<QuizQuestionDto> Questions);

public sealed record QuizQueryParameters(
    int Limit = 10,
    string? Level = null);

public sealed record QuizAnswerItemDto(
    [property: JsonPropertyName("questionId")] string? QuestionId,
    [property: JsonPropertyName("selectedKey")] string? SelectedKey,
    [property: JsonPropertyName("isCorrect")] bool IsCorrect);

public sealed record QuizAttemptRequestDto(
    [property: JsonPropertyName("answers")] List<QuizAnswerItemDto>? Answers,
    [property: JsonPropertyName("xp")] int Xp,
    [property: JsonPropertyName("idempotencyKey")] string? IdempotencyKey);

public sealed record QuizAttemptResultDto(
    [property: JsonPropertyName("saved")] bool Saved,
    [property: JsonPropertyName("correct_answers")] int CorrectAnswers,
    [property: JsonPropertyName("xp")] int Xp);
