using Lingual.SharedKernel.Entities;
using Lingual.SharedKernel.Events;

namespace Lingual.Modules.AITutor.Domain;

public class AiConversation : HasDomainEventsBase, IAggregateRoot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? ScenarioId { get; set; }
    public string ConversationType { get; set; } = "free_chat";
    public string? Title { get; set; }
    public string Status { get; set; } = "active";
    public string Messages { get; set; } = "[]";
    public string? ModelName { get; set; }
    public int TotalTokens { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    public AiScenario? Scenario { get; set; }
}
