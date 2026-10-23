using Lingual.SharedKernel.Entities;

namespace Lingual.Modules.Learning.Domain;

public class SrsCard : EntityBase
{
    public Guid UserId { get; set; }
    public Guid VocabularyId { get; set; }
    public int Repetitions { get; set; }
    public decimal IntervalDays { get; set; }
    public decimal EaseFactor { get; set; } = 2.50m;
    public DateTime NextDueAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastReviewedAt { get; set; }
    public int Lapses { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public string ReviewHistory { get; set; } = "[]";

    public VocabularyItem? VocabularyItem { get; set; }
}
