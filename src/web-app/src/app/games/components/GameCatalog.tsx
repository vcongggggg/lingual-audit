"use client";

import React from "react";
import { Play, Sparkles } from "lucide-react";
import type { MyGameStatsDto } from "@/lib/api";

export interface GameCatalogItem {
  id: string;
  name: string;
  tagline: string;
  description: string;
  difficulty: "A1" | "A2" | "B1" | "B2" | "Tất cả";
  timeLimit: number;
  rewardBase: string;
  icon: React.ReactNode;
}

interface GameCatalogProps {
  catalog: GameCatalogItem[];
  selectedDifficulty: string;
  onDifficultyChange: (diff: string) => void;
  onStartGame: (type: string) => void;
  gameLoading: boolean;
  stats: MyGameStatsDto | null;
}

export const GameCatalog: React.FC<GameCatalogProps> = ({
  catalog,
  selectedDifficulty,
  onDifficultyChange,
  onStartGame,
  gameLoading,
  stats,
}) => {
  return (
    <>
      <div className="games-filter-row">
        <span className="filter-label">Độ khó từ vựng:</span>
        <div className="filter-options">
          {["all", "A1", "A2", "B1", "B2"].map((diff) => (
            <button
              key={diff}
              onClick={() => onDifficultyChange(diff)}
              className={`filter-btn ${selectedDifficulty === diff ? "is-selected" : ""}`}
            >
              {diff === "all" ? "Mọi cấp độ" : `Cấp độ ${diff}`}
            </button>
          ))}
        </div>
      </div>

      <div className="games-grid">
        {catalog.map((game) => {
          const gameStat = stats?.byGame?.[game.id];

          return (
            <div key={game.id} className="game-card">
              <div className="game-card-header">
                <div className="game-icon-badge">{game.icon}</div>
                <div className="game-meta">
                  <span className="game-level-tag">{game.difficulty}</span>
                  <span className="game-time-tag">
                    {game.timeLimit > 0 ? `${game.timeLimit}s` : "Không giới hạn"}
                  </span>
                </div>
              </div>

              <div className="game-card-body">
                <h3 className="game-title">{game.name}</h3>
                <p className="game-tagline">{game.tagline}</p>
                <p className="game-desc">{game.description}</p>
              </div>

              <div className="game-card-footer">
                <div className="game-reward-hint">
                  <Sparkles size={14} className="text-olive" />
                  <span>{game.rewardBase}</span>
                  {gameStat && gameStat.highScore > 0 && (
                    <span className="game-best-score">
                      Kỷ lục: <strong>{gameStat.highScore}</strong> pts
                    </span>
                  )}
                </div>

                <button
                  onClick={() => onStartGame(game.id)}
                  disabled={gameLoading}
                  className="game-launch-btn"
                >
                  <Play size={16} fill="currentColor" />
                  <span>Chơi ngay</span>
                </button>
              </div>
            </div>
          );
        })}
      </div>
    </>
  );
};
