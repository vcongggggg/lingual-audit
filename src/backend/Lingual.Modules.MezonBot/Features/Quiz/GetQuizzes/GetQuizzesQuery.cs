using Lingual.Modules.MezonBot.Contracts.DTOs;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.MezonBot.Features.Quiz.GetQuizzes;

public record GetQuizzesQuery(int Limit = 10, string? Level = null) : IQuery<QuizListResponseDto>;
