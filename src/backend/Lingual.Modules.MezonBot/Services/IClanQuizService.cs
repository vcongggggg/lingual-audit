using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lingual.Modules.MezonBot.Services;

public enum QuizSubmissionStatus
{
    Success,
    AlreadyAnswered,
    SessionExpired,
    SessionNotFound,
    InvalidOption
}

public class QuizOptionDto
{
    public string Key { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class ClanQuizSessionDto
{
    public Guid SessionId { get; set; }
    public long ChannelId { get; set; }
    public string QuestionPrompt { get; set; } = string.Empty;
    public string? Level { get; set; }
    public short? Difficulty { get; set; }
    public List<QuizOptionDto> Options { get; set; } = new();
    public int DurationSeconds { get; set; }
    public DateTime ClosesAt { get; set; }
    public bool IsPractice { get; set; }
}

public class QuizSubmissionResultDto
{
    public QuizSubmissionStatus Status { get; set; }
    public bool IsCorrect { get; set; }
    public bool IsWinner { get; set; }
    public int ResponseMs { get; set; }
    public int XpAwarded { get; set; }
    public bool IsPractice { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class QuizParticipantSummaryDto
{
    public string MezonUserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string SelectedKey { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int ResponseMs { get; set; }
    public int XpAwarded { get; set; }
}

public class ClanQuizConcludeDto
{
    public Guid SessionId { get; set; }
    public long ChannelId { get; set; }
    public string QuestionPrompt { get; set; } = string.Empty;
    public string CorrectKey { get; set; } = string.Empty;
    public string CorrectText { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string? WinnerMezonUserId { get; set; }
    public string? WinnerUsername { get; set; }
    public int? WinningResponseMs { get; set; }
    public int WinnerXpAwarded { get; set; }
    public int TotalResponses { get; set; }
    public int CorrectResponses { get; set; }
    public List<QuizParticipantSummaryDto> Participants { get; set; } = new();
    public bool IsPractice { get; set; }
    public string? SlotKey { get; set; }
}

public interface IClanQuizService
{
    Task<bool> HasActiveSessionAsync(long channelId, bool isPractice = false);
    Task<ClanQuizSessionDto?> StartSessionAsync(long channelId, string? startedByMezonUserId = null, int durationSeconds = 30, bool isPractice = false, string? slotKey = null);
    Task<QuizSubmissionResultDto> SubmitAnswerAsync(Guid sessionId, long channelId, string mezonUserId, string username, string selectedKey);
    Task<ClanQuizConcludeDto?> ConcludeSessionAsync(Guid sessionId);
}
