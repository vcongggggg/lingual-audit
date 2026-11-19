"use client";

import React, { useRef, useEffect } from "react";
import { CheckCircle2 } from "lucide-react";

interface TypingWord {
  id: string;
  targetText: string;
  phonetic: string;
  translation: string;
  cefrLevel: string;
}

interface TypingRaceGameProps {
  currentWord: TypingWord | null;
  currentIndex: number;
  totalWords: number;
  typingInput: string;
  typingSuccess: boolean;
  onTypingChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

export const TypingRaceGame: React.FC<TypingRaceGameProps> = ({
  currentWord,
  currentIndex,
  totalWords,
  typingInput,
  typingSuccess,
  onTypingChange,
}) => {
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    inputRef.current?.focus();
  }, [currentIndex]);

  if (!currentWord) return null;

  return (
    <div className="typing-race-board">
      <div className="typing-word-display">
        <div className="word-meta-row">
          <span className="question-counter">
            Từ {currentIndex + 1}/{totalWords}
          </span>
          <span className="level-badge">{currentWord.cefrLevel}</span>
        </div>

        <h2 className={`target-word ${typingSuccess ? "text-success-pop" : ""}`}>
          {currentWord.targetText}
        </h2>

        {currentWord.phonetic && (
          <p className="word-phonetic">/{currentWord.phonetic}/</p>
        )}

        <p className="word-translation">Nghĩa: {currentWord.translation}</p>
      </div>

      <div className="typing-input-wrapper">
        <input
          ref={inputRef}
          type="text"
          value={typingInput}
          onChange={onTypingChange}
          placeholder="Gõ lại chính xác từ trên..."
          className={`typing-input-field ${typingSuccess ? "is-correct-border" : ""}`}
          autoComplete="off"
          autoCapitalize="off"
          spellCheck={false}
        />
        {typingSuccess && (
          <div className="typing-success-indicator">
            <CheckCircle2 size={24} className="text-olive" />
          </div>
        )}
      </div>

      <p className="typing-hint">
        Gõ nhanh và chính xác. Hệ thống sẽ tự động chuyển từ tiếp theo khi bạn gõ đúng!
      </p>
    </div>
  );
};
