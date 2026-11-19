"use client";

import React from "react";

interface FillQuestion {
  id: string;
  prompt: string;
  choices: string[];
  level: string;
}

interface FillBlitzGameProps {
  currentQuestion: FillQuestion | null;
  currentIndex: number;
  totalQuestions: number;
  onSelectChoice: (choice: string) => void;
}

export const FillBlitzGame: React.FC<FillBlitzGameProps> = ({
  currentQuestion,
  currentIndex,
  totalQuestions,
  onSelectChoice,
}) => {
  if (!currentQuestion) return null;

  return (
    <div className="fill-blitz-board">
      <div className="fill-prompt-box">
        <div className="fill-meta">
          <span className="question-counter">
            Câu hỏi {currentIndex + 1}/{totalQuestions}
          </span>
          <span className="level-badge">{currentQuestion.level}</span>
        </div>

        <h3 className="fill-prompt-text">{currentQuestion.prompt}</h3>
        <p className="fill-subtext">Chọn đáp án đúng nhất để hoàn thiện câu:</p>
      </div>

      <div className="fill-choices-grid">
        {currentQuestion.choices.map((choice, idx) => (
          <button
            key={`${choice}-${idx}`}
            onClick={() => onSelectChoice(choice)}
            className="fill-choice-card"
          >
            <span className="choice-label">{String.fromCharCode(65 + idx)}</span>
            <span className="choice-text">{choice}</span>
          </button>
        ))}
      </div>
    </div>
  );
};
