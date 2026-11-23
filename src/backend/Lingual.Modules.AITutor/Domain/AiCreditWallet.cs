namespace Lingual.Modules.AITutor.Domain;

public class AiCreditWallet
{
    public Guid UserId { get; set; }
    public int Balance { get; set; }
    public int DailyFreeRemaining { get; set; } = 5;
    public DateOnly LastResetDate { get; set; }
    public int DailyBonusEarned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AiCreditTransaction> Transactions { get; set; } = new List<AiCreditTransaction>();
}
