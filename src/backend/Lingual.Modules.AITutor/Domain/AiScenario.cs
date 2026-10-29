using Lingual.SharedKernel.Entities;

namespace Lingual.Modules.AITutor.Domain;

public class AiScenario : EntityBase, IAggregateRoot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Level { get; set; }
    public string SystemPromptKey { get; set; } = string.Empty;
    public string Status { get; set; } = "published";
    public Guid? CreatedBy { get; set; }

    public ICollection<AiConversation> Conversations { get; set; } = new List<AiConversation>();
}
