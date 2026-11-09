namespace Lingual.Modules.AITutor.Contracts.DTOs;

public record RoleplayTurnRequest(
    string ScenarioCode,
    string UserMessage,
    Guid? ConversationId = null,
    string? MezonUserId = null,
    string? ChannelId = null
);

