using Lingual.Modules.AITutor.Contracts.DTOs;

namespace Lingual.Modules.AITutor.Contracts;

public interface IAITutorService
{
    Task<GrammarAnalysisResponse> AnalyzeGrammarAsync(GrammarAnalysisRequest request, CancellationToken cancellationToken = default);
}
