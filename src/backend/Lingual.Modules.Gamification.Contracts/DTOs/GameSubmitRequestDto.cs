using System.Collections.Generic;

namespace Lingual.Modules.Gamification.Contracts.DTOs;

public class GameSubmitRequestDto
{
    public string AttemptId { get; set; } = string.Empty;
    public string GameType { get; set; } = string.Empty;
    public int TimeRemainingSeconds { get; set; } = 0;
    public List<GameAnswerSubmissionDto> Answers { get; set; } = new();
}

public class GameAnswerSubmissionDto
{
    public string QuestionId { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}
