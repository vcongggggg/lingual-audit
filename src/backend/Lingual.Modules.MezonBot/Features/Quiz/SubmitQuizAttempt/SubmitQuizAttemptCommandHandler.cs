using Lingual.Modules.MezonBot.Contracts;
using Lingual.Modules.MezonBot.Contracts.DTOs;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.MezonBot.Features.Quiz.SubmitQuizAttempt;

public class SubmitQuizAttemptCommandHandler(IQuizService quizService)
    : ICommandHandler<SubmitQuizAttemptCommand, QuizAttemptResultDto>
{
    public async Task<QuizAttemptResultDto> Handle(
        SubmitQuizAttemptCommand command,
        CancellationToken cancellationToken)
    {
        return await quizService.SubmitQuizAttemptAsync(command.Request, command.UserId, cancellationToken);
    }
}
