using Lingual.Modules.MezonBot.Contracts.DTOs;

namespace Lingual.Modules.MezonBot.Contracts;

public interface IQuizService
{
    Task<QuizListResponseDto> GetQuizQuestionsAsync(
        QuizQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<QuizAttemptResultDto> SubmitQuizAttemptAsync(
        QuizAttemptRequestDto request,
        Guid? userId,
        CancellationToken cancellationToken = default);
}
