using Lingual.SharedKernel.Events;

namespace Lingual.Modules.Learning.Domain;

public class Unit : HasDomainEventsBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Topic { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = "published";

    public Course? Course { get; set; }
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
