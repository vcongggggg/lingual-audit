using System;
using Lingual.SharedKernel.Entities;

namespace Lingual.Modules.Gamification.Domain;

public class UserGameStat : EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string GameType { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public int HighScore { get; set; }
    public int TotalGamesPlayed { get; set; }
    public int TotalWins { get; set; }
    public int TotalXpEarned { get; set; }
    public double AvgAccuracy { get; set; }
    public DateTime LastPlayedAt { get; set; } = DateTime.UtcNow;

    public UserGameStat() { }

    public UserGameStat(Guid userId, string gameType, string? displayName = null, string? avatarUrl = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        GameType = gameType;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        HighScore = 0;
        TotalGamesPlayed = 0;
        TotalWins = 0;
        TotalXpEarned = 0;
        AvgAccuracy = 0.0;
        LastPlayedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordGame(int score, double accuracy, int xpEarned, string? displayName = null, string? avatarUrl = null)
    {
        if (score > HighScore)
        {
            HighScore = score;
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            DisplayName = displayName;
        }

        if (!string.IsNullOrWhiteSpace(avatarUrl))
        {
            AvatarUrl = avatarUrl;
        }

        TotalGamesPlayed++;
        if (accuracy >= 0.70)
        {
            TotalWins++;
        }

        TotalXpEarned += xpEarned;
        AvgAccuracy = Math.Round(((AvgAccuracy * (TotalGamesPlayed - 1)) + accuracy) / TotalGamesPlayed, 2);
        LastPlayedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
