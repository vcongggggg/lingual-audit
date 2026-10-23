using Lingual.SharedKernel.Entities;

namespace Lingual.Modules.Learning.Domain;

public class VocabularyItem : EntityBase, IAggregateRoot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Term { get; set; } = string.Empty;
    public string NormalizedTerm { get; set; } = string.Empty;
    public string SourceLanguage { get; set; } = "en";
    public string TargetLanguage { get; set; } = "vi";
    public string? Ipa { get; set; }
    public string? PartOfSpeech { get; set; }
    public string Meaning { get; set; } = string.Empty;
    public string? PronunciationAudioUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string Examples { get; set; } = "[]";
    public string Status { get; set; } = "published";
    public Guid? CreatedBy { get; set; }

    public ICollection<SrsCard> SrsCards { get; set; } = new List<SrsCard>();
}
