"use client";

import React from "react";
import { Check } from "lucide-react";

interface ScrambleQuestion {
  id: string;
  prompt: string;
  tokens: string[];
  level: string;
}

interface SentenceScrambleGameProps {
  currentQuestion: ScrambleQuestion | null;
  currentIndex: number;
  totalQuestions: number;
  placedTokens: string[] | null;
  availableTokens: string[] | null;
  onPlaceToken: (token: string, index: number) => void;
  onRemoveToken: (token: string, index: number) => void;
  onConfirmScramble: () => void;
}

export const SentenceScrambleGame: React.FC<SentenceScrambleGameProps> = ({
  currentQuestion,
  currentIndex,
  totalQuestions,
  placedTokens,
  availableTokens,
  onPlaceToken,
  onRemoveToken,
  onConfirmScramble,
}) => {
  if (!currentQuestion) return null;

  const placed = placedTokens || [];
  const available = availableTokens || [];

  return (
    <div className="scramble-game-board">
      <div className="scramble-prompt-box">
        <span className="question-counter">
          Câu {currentIndex + 1}/{totalQuestions}
        </span>
        <h3 className="prompt-text">{currentQuestion.prompt}</h3>
        <p className="prompt-guide">
          Bấm chọn các từ bên dưới theo thứ tự để tạo thành câu hoàn chỉnh đúng ngữ pháp.
        </p>
      </div>

      <div className="scramble-drop-zone">
        {placed.length === 0 ? (
          <span className="drop-placeholder">Bấm từ để xếp vào đây...</span>
        ) : (
          placed.map((token, idx) => (
            <button
              key={`${token}-${idx}`}
              onClick={() => onRemoveToken(token, idx)}
              className="token-chip placed-token"
            >
              <span>{token}</span>
            </button>
          ))
        )}
      </div>

      <div className="scramble-tokens-pool">
        {available.map((token, idx) => (
          <button
            key={`${token}-${idx}`}
            onClick={() => onPlaceToken(token, idx)}
            className="token-chip available-token"
          >
            <span>{token}</span>
          </button>
        ))}
      </div>

      <div className="scramble-actions">
        <button
          disabled={placed.length === 0}
          onClick={onConfirmScramble}
          className="scramble-submit-btn"
        >
          <Check size={18} />
          <span>Kiểm tra câu</span>
        </button>
      </div>
    </div>
  );
};
