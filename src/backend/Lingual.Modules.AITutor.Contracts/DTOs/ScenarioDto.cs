namespace Lingual.Modules.AITutor.Contracts.DTOs;

public record ScenarioDto(
    Guid Id,
    string Code,
    string Title,
    string Description,
    string Level,
    string CharacterRole,
    string GreetingMessage
);

