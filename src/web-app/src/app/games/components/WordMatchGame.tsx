"use client";

import React from "react";
import { Check } from "lucide-react";

interface WordItem {
  id: string;
  text: string;
}

interface WordMatchGameProps {
  matchLeft: WordItem[];
  matchRight: WordItem[];
  selectedLeft: string | null;
  wrongMatch: string | null;
  matchedPairs: Set<string>;
  onSelectLeft: (id: string) => void;
  onSelectRight: (id: string) => void;
}

export const WordMatchGame: React.FC<WordMatchGameProps> = ({
  matchLeft,
  matchRight,
  selectedLeft,
  wrongMatch,
  matchedPairs,
  onSelectLeft,
  onSelectRight,
}) => {
  return (
    <div className="word-match-board">
      <div className="match-column left-column">
        <h4 className="column-title">Tiếng Anh</h4>
        <div className="words-stack">
          {matchLeft.map((item) => {
            const isMatched = matchedPairs.has(item.id);
            const isSelected = selectedLeft === item.id;
            return (
              <button
                key={item.id}
                disabled={isMatched}
                onClick={() => onSelectLeft(item.id)}
                className={`word-card left-card ${
                  isMatched ? "is-matched" : ""
                } ${isSelected ? "is-selected" : ""}`}
              >
                <span>{item.text}</span>
                {isMatched && <Check size={16} className="matched-check" />}
              </button>
            );
          })}
        </div>
      </div>

      <div className="match-column right-column">
        <h4 className="column-title">Nghĩa tiếng Việt</h4>
        <div className="words-stack">
          {matchRight.map((item) => {
            const isMatched = matchedPairs.has(item.id);
            const isWrong = wrongMatch === item.id;
            return (
              <button
                key={item.id}
                disabled={isMatched}
                onClick={() => onSelectRight(item.id)}
                className={`word-card right-card ${
                  isMatched ? "is-matched" : ""
                } ${isWrong ? "is-wrong-shake" : ""}`}
              >
                <span>{item.text}</span>
                {isMatched && <Check size={16} className="matched-check" />}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
};
