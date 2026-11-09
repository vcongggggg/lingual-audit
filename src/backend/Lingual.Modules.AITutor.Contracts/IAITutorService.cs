using Lingual.Modules.AITutor.Contracts.DTOs;

namespace Lingual.Modules.AITutor.Contracts;

public interface IAITutorService
{
    Task<GrammarAnalysisResponse> AnalyzeGrammarAsync(GrammarAnalysisRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScenarioDto>> GetAvailableScenariosAsync(CancellationToken cancellationToken = default);

    Task<RoleplayTurnResponse> ProcessRoleplayTurnAsync(RoleplayTurnRequest request, CancellationToken cancellationToken = default);

    Task<QuizMistakeAnalysisResponse> ExplainQuizMistakeAsync(QuizMistakeAnalysisRequest request, CancellationToken cancellationToken = default);
}
