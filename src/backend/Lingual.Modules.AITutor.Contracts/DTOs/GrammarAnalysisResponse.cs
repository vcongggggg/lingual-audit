namespace Lingual.Modules.AITutor.Contracts.DTOs;

public record GrammarAnalysisResponse(
    bool IsCorrect,
    string OriginalText,
    string CorrectedText,
    string Explanation,
    string Tips
);
