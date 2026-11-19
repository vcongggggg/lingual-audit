using System;
using Lingual.SharedKernel.Entities;

namespace Lingual.Modules.Gamification.Domain;

public class GameSession : EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AttemptId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string GameType { get; set; } = string.Empty;
    public int Score { get; set; }
    public double Accuracy { get; set; }
    public int XpEarned { get; set; }
    public int DurationSeconds { get; set; }
    public int ComboMax { get; set; } = 1;
    public string? MetadataJson { get; set; }

    public GameSession() { }

    public GameSession(
        string attemptId,
        Guid userId,
        string gameType,
        int score,
        double accuracy,
        int xpEarned,
        int durationSeconds,
        int comboMax = 1,
        string? metadataJson = null)
    {
        Id = Guid.NewGuid();
        AttemptId = attemptId;
        UserId = userId;
        GameType = gameType;
        Score = score;
        Accuracy = accuracy;
        XpEarned = xpEarned;
        DurationSeconds = durationSeconds;
        ComboMax = comboMax;
        MetadataJson = metadataJson;
        CreatedAt = DateTime.UtcNow;
    }
}
