using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lingual.Modules.MezonBot.Services;

public record ReactionQuizOption(string Key, string Text);

public record ReactionQuizPromptDto(
    Guid SessionId,
    long ChannelId,
    string QuestionPrompt,
    string? Level,
    short? Difficulty,
    List<ReactionQuizOption> Options,
    DateTime ClosesAt
);

public enum ReactionQuizAnswerStatus
{
    Success,
    WrongAnswer,
    AlreadyAnswered,
    Expired,
    NotAQuizMessage,
    InvalidOption
}

public record ReactionQuizAnswerResult(
    ReactionQuizAnswerStatus Status,
    bool IsCorrect,
    int XpAwarded,
    string CorrectKey,
    string Message
);

public record ActiveReactionQuizInfo(
    bool HasActive,
    Guid? SessionId,
    DateTime? ClosesAt,
    string? QuestionPrompt
);

public record ExpiredQuizConclusionResult(
    bool Success,
    Guid SessionId,
    long MessageId,
    string QuestionPrompt,
    string CorrectKey
);

public interface IReactionQuizService
{
    Task<ReactionQuizPromptDto> CreateQuizAsync(long channelId, long clanId, string? senderId = null, CancellationToken cancellationToken = default);
    Task LinkMessageAsync(Guid sessionId, long messageId, CancellationToken cancellationToken = default);
    Task<ReactionQuizAnswerResult> ProcessReactionAnswerAsync(long messageId, long channelId, string mezonUserId, string username, string emoji, CancellationToken cancellationToken = default);
    Task<bool> IsReactionQuizMessageAsync(long messageId, CancellationToken cancellationToken = default);
    Task<ActiveReactionQuizInfo> GetActiveSessionAsync(long channelId, CancellationToken cancellationToken = default);
    Task<ExpiredQuizConclusionResult?> ConcludeExpiredQuizAsync(long messageId, CancellationToken cancellationToken = default);
}

