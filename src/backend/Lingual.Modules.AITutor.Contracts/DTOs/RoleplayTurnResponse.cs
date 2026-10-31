namespace Lingual.Modules.AITutor.Contracts.DTOs;

public record RoleplayTurnResponse(
    Guid ConversationId,
    string ScenarioCode,
    string ScenarioTitle,
    string CharacterRole,
    string Reply,
    bool HasFeedback,
    string? FeedbackCorrection,
    string? Explanation
);
