namespace Lingual.Modules.AITutor.Contracts.DTOs;

public record GrammarAnalysisRequest(
    string Text,
    string? Context = null
);
