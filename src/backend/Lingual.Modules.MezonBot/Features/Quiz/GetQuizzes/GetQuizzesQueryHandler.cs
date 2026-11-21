using Lingual.Modules.MezonBot.Contracts;
using Lingual.Modules.MezonBot.Contracts.DTOs;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.MezonBot.Features.Quiz.GetQuizzes;

public class GetQuizzesQueryHandler(IQuizService quizService)
    : IQueryHandler<GetQuizzesQuery, QuizListResponseDto>
{
    public async Task<QuizListResponseDto> Handle(
        GetQuizzesQuery query,
        CancellationToken cancellationToken)
    {
        return await quizService.GetQuizQuestionsAsync(
            new QuizQueryParameters(query.Limit, query.Level),
            cancellationToken);
    }
}
