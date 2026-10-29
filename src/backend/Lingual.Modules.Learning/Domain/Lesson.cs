using Lingual.SharedKernel.Entities;

namespace Lingual.Modules.Learning.Domain;

public class Lesson : EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UnitId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public short? EstimatedMinutes { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = "published";
    public string VocabularyIds { get; set; } = "[]";

    public Unit? Unit { get; set; }
    public ICollection<LessonProgress> Progresses { get; set; } = new List<LessonProgress>();
}
