"use client";

import React from "react";
import { CornerDownLeft, Delete } from "lucide-react";

interface LingoWordleGameProps {
  prompt: string;
  category: string;
  meaningVi: string;
  guesses: string[];
  evaluations?: string[][];
  currentGuess: string;
  keyColors: Record<string, string>;
  gameOver: boolean;
  onKeyPress: (key: string) => void;
}

const KEYBOARD_ROWS = [
  ["Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P"],
  ["A", "S", "D", "F", "G", "H", "J", "K", "L"],
  ["ENTER", "Z", "X", "C", "V", "B", "N", "M", "BACKSPACE"],
];

export const LingoWordleGame: React.FC<LingoWordleGameProps> = ({
  prompt,
  category,
  meaningVi,
  guesses,
  evaluations = [],
  currentGuess,
  keyColors,
  gameOver,
  onKeyPress,
}) => {
  const maxAttempts = 6;
  const wordLength = 5;

  const rows = Array.from({ length: maxAttempts }, (_, rowIndex) => {
    if (rowIndex < guesses.length) {
      return {
        word: guesses[rowIndex],
        isCommitted: true,
      };
    } else if (rowIndex === guesses.length) {
      return {
        word: currentGuess.padEnd(wordLength, " "),
        isCommitted: false,
      };
    } else {
      return {
        word: " ".repeat(wordLength),
        isCommitted: false,
      };
    }
  });

  return (
    <div className="wordle-board-container">
      <div className="wordle-hint-card">
        <div className="wordle-hint-meta">
          <span className="hint-tag">Chủ đề: {category}</span>
          <span className="attempts-tag">Lượt thử: {guesses.length}/{maxAttempts}</span>
        </div>
        {prompt && <p className="wordle-clue">Gợi ý: {prompt}</p>}
        {meaningVi && <p className="wordle-meaning-vi">Nghĩa: {meaningVi}</p>}
      </div>

      <div className="wordle-grid">
        {rows.map((row, rIdx) => (
          <div key={rIdx} className="wordle-row">
            {Array.from({ length: wordLength }, (_, cIdx) => {
              const char = row.word[cIdx] || "";
              const displayChar = char.trim();

              const tileEval = row.isCommitted && evaluations[rIdx]?.[cIdx] ? evaluations[rIdx][cIdx] : "";
              const evalClass = tileEval ? (tileEval.startsWith("is-") ? tileEval : `is-${tileEval}`) : "";

              return (
                <div
                  key={cIdx}
                  className={`wordle-tile ${evalClass} ${displayChar ? "has-letter" : ""} ${
                    row.isCommitted ? "cell-committed" : ""
                  }`.trim()}
                >
                  {displayChar}
                </div>
              );
            })}
          </div>
        ))}
      </div>

      <div className="wordle-keyboard">
        {KEYBOARD_ROWS.map((row, rIdx) => (
          <div key={rIdx} className="wordle-key-row keyboard-row">
            {row.map((key) => {
              const isSpecial = key === "ENTER" || key === "BACKSPACE";
              const rawStatus = keyColors[key] || "";
              const keyStatus = rawStatus ? (rawStatus.startsWith("is-") ? rawStatus : `is-${rawStatus}`) : "";

              return (
                <button
                  key={key}
                  disabled={gameOver}
                  onClick={() => onKeyPress(key)}
                  className={`wordle-key key-btn ${isSpecial ? "special-key" : ""} ${keyStatus}`.trim()}
                >
                  {key === "BACKSPACE" ? (
                    <Delete size={18} />
                  ) : key === "ENTER" ? (
                    <CornerDownLeft size={18} />
                  ) : (
                    key
                  )}
                </button>
              );
            })}
          </div>
        ))}
      </div>
    </div>
  );
};
