"use client";

import React from "react";
import { Volume2 } from "lucide-react";

interface SoundChoice {
  id: string;
  targetText: string;
  translation: string;
}

interface SoundQuestion {
  id: string;
  audioText: string;
  choices: SoundChoice[];
}

interface SoundReflexGameProps {
  currentQuestion: SoundQuestion | null;
  currentIndex: number;
  totalQuestions: number;
  onSelectChoice: (choiceText: string) => void;
  onReplayAudio: (text: string) => void;
}

export const SoundReflexGame: React.FC<SoundReflexGameProps> = ({
  currentQuestion,
  currentIndex,
  totalQuestions,
  onSelectChoice,
  onReplayAudio,
}) => {
  if (!currentQuestion) return null;

  return (
    <div className="sound-reflex-board">
      <div className="sound-audio-box">
        <span className="question-counter">
          Câu {currentIndex + 1}/{totalQuestions}
        </span>

        <button
          onClick={() => onReplayAudio(currentQuestion.audioText)}
          className="audio-play-large-btn"
          title="Bấm để nghe lại phát âm"
        >
          <Volume2 size={40} className="text-olive" />
          <span>Bấm để nghe lại âm thanh</span>
        </button>

        <p className="sound-hint">Lắng nghe phát âm và chọn nghĩa tiếng Việt chính xác nhất</p>
      </div>

      <div className="sound-choices-grid">
        {currentQuestion.choices.map((choice, idx) => (
          <button
            key={`${choice.id}-${idx}`}
            onClick={() => onSelectChoice(choice.targetText)}
            className="sound-choice-btn"
          >
            <span className="choice-number">{idx + 1}</span>
            <div className="choice-content">
              <span className="choice-vi">{choice.translation}</span>
            </div>
          </button>
        ))}
      </div>
    </div>
  );
};
