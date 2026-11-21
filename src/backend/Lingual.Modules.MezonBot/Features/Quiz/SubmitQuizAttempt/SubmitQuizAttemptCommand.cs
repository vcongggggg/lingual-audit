using Lingual.Modules.MezonBot.Contracts.DTOs;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.MezonBot.Features.Quiz.SubmitQuizAttempt;

public record SubmitQuizAttemptCommand(
    QuizAttemptRequestDto Request,
    Guid? UserId) : ICommand<QuizAttemptResultDto>;
