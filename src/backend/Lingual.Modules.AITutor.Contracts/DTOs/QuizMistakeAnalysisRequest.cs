using System.ComponentModel.DataAnnotations;

namespace Lingual.Modules.AITutor.Contracts.DTOs;

public record QuizMistakeAnalysisRequest(
    [property: Required(ErrorMessage = "Nội dung câu hỏi không được để trống.")]
    [property: MaxLength(1000)]
    string QuestionPrompt,

    [property: Required(ErrorMessage = "Đáp án đã chọn không được để trống.")]
    [property: MaxLength(500)]
    string SelectedOptionText,

    [property: Required(ErrorMessage = "Đáp án đúng không được để trống.")]
    [property: MaxLength(500)]
    string CorrectOptionText,

    [property: MaxLength(16)]
    string? Level = "A1",

    [property: MaxLength(1000)]
    string? ExistingExplanation = null
);

