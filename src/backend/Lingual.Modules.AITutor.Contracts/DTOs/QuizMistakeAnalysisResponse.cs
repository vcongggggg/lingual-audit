using System.Collections.Generic;

namespace Lingual.Modules.AITutor.Contracts.DTOs;

public record QuizMistakeAnalysisResponse(
    string QuestionPrompt,
    string SelectedOptionText,
    string CorrectOptionText,
    string WhyWrong,
    string GrammarAndContextRule,
    IReadOnlyList<string> Synonyms,
    IReadOnlyList<string> Antonyms,
    string MnemonicTip,
    string ExampleSentence
);

