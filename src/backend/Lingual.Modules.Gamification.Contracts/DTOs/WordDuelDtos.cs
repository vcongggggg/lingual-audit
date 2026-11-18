using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lingual.Modules.Gamification.Contracts.DTOs;

public record DuelOptionDto(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("text")] string Text
);

/// <summary>
/// Câu hỏi phát xuống client trong vòng đấu (TUYỆT ĐỐI KHÔNG chứa đáp án đúng để chống gian lận client-side).
/// </summary>
public record DuelRoundQuestionDto(
    Guid Id,
    int RoundNumber,
    int TotalRounds,
    string Prompt,
    string Level,
    int Difficulty,
    IReadOnlyList<DuelOptionDto> Options,
    int DurationSeconds = 10
);

/// <summary>
/// Chi tiết 1 câu trả lời lưu trong mảng JSONB challenger_answers / opponent_answers của bảng duel_matches.
/// </summary>
public record DuelAnswerEntryDto(
    [property: JsonPropertyName("question_id")] Guid QuestionId,
    [property: JsonPropertyName("round")] int Round,
    [property: JsonPropertyName("selected_key")] string SelectedKey,
    [property: JsonPropertyName("is_correct")] bool IsCorrect,
    [property: JsonPropertyName("response_ms")] int ResponseMs,
    [property: JsonPropertyName("score")] decimal Score,
    [property: JsonPropertyName("answered_at")] DateTime AnsweredAt
);

/// <summary>
/// Tổng kết chi tiết 1 câu hỏi sau khi vòng đấu hoặc trận đấu đã khép lại.
/// </summary>
public record DuelQuestionReviewDto(
    Guid QuestionId,
    int RoundNumber,
    string Prompt,
    string Level,
    IReadOnlyList<DuelOptionDto> Options,
    string CorrectKey,
    string CorrectText,
    string Explanation,
    string? ChallengerSelectedKey,
    bool ChallengerCorrect,
    int ChallengerResponseMs,
    decimal ChallengerRoundScore,
    string? OpponentSelectedKey,
    bool OpponentCorrect,
    int OpponentResponseMs,
    decimal OpponentRoundScore
);

public record CreateDuelChallengeRequest(
    Guid ChallengerId,
    string ChallengerName,
    Guid OpponentId,
    string OpponentName,
    short QuestionCount = 5,
    long? ClanChannelId = null,
    bool AutoAccept = false
);

public record DuelMatchDto(
    Guid Id,
    Guid ChallengerId,
    string ChallengerName,
    Guid OpponentId,
    string OpponentName,
    string Status,
    short QuestionCount,
    decimal ChallengerScore,
    decimal OpponentScore,
    short ChallengerCorrect,
    short OpponentCorrect,
    Guid? WinnerId,
    int ChallengerXpAwarded,
    int OpponentXpAwarded,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int CurrentRound,
    bool IsBotMatch,
    long? ClanChannelId = null,
    IReadOnlyList<DuelQuestionReviewDto>? QuestionReviews = null
);

public record SubmitDuelAnswerRequest(
    Guid MatchId,
    Guid UserId,
    Guid QuestionId,
    string SelectedKey,
    int ResponseMs
);

public record SubmitDuelAnswerResultDto(
    bool Accepted,
    string Message,
    int RoundNumber,
    bool IsCorrect,
    string SelectedKey,
    string CorrectKey,
    string CorrectText,
    string Explanation,
    decimal PointsEarned,
    decimal PlayerTotalScore,
    decimal OpponentTotalScore,
    int PlayerComboStreak,
    bool BothAnsweredCurrentRound,
    bool MatchCompleted,
    DuelMatchDto? MatchSnapshot
);

