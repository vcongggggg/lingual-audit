using Lingual.SharedKernel.Entities;

namespace Lingual.Modules.Learning.Domain;

public class Course : EntityBase, IAggregateRoot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SourceLanguage { get; set; } = "en";
    public string TargetLanguage { get; set; } = "vi";
    public string Level { get; set; } = "A1";
    public string Status { get; set; } = "published";
    public int SortOrder { get; set; }
    public Guid? CreatedBy { get; set; }

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}
