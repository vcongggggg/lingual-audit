"use client";

import React from "react";
import { Trophy } from "lucide-react";
import type { GameLeaderboardEntry } from "@/lib/api";

interface GameLeaderboardTabProps {
  leaderboard: GameLeaderboardEntry[];
  loading: boolean;
  filterType: string;
  onFilterChange: (type: string) => void;
  onPlayGame: () => void;
}

export const GameLeaderboardTab: React.FC<GameLeaderboardTabProps> = ({
  leaderboard,
  loading,
  filterType,
  onFilterChange,
  onPlayGame,
}) => {
  return (
    <div className="game-leaderboard-section">
      <div className="leaderboard-filter-bar">
        <span className="filter-title">Lọc theo trò chơi:</span>
        <div className="filter-buttons">
          {[
            { id: "all", label: "Tất cả trò chơi" },
            { id: "word_match", label: "Nối từ" },
            { id: "sentence_scramble", label: "Xếp câu" },
            { id: "typing_race", label: "Đua gõ phím" },
            { id: "fill_blitz", label: "Điền chỗ trống" },
            { id: "sound_reflex", label: "Phản xạ nghe" },
            { id: "lingo_wordle", label: "Lingo Wordle" },
          ].map((btn) => (
            <button
              key={btn.id}
              onClick={() => onFilterChange(btn.id)}
              className={`filter-tag ${filterType === btn.id ? "is-active" : ""}`}
            >
              {btn.label}
            </button>
          ))}
        </div>
      </div>

      {loading ? (
        <div className="leaderboard-empty-state">
          <p>Đang tải bảng xếp hạng...</p>
        </div>
      ) : leaderboard.length === 0 ? (
        <div className="leaderboard-empty-state">
          <Trophy size={48} className="empty-icon" />
          <h3>Chưa có người chơi nào ghi danh</h3>
          <p>Hãy là người đầu tiên tham gia thử thách để xác lập kỷ lục điểm số cao nhất!</p>
          <button onClick={onPlayGame} className="empty-action-btn">
            Tham gia màn chơi ngay
          </button>
        </div>
      ) : (
        <div className="leaderboard-table-container">
          <table className="game-leaderboard-table">
            <thead>
              <tr>
                <th className="col-rank">Hạng</th>
                <th className="col-player">Học viên</th>
                <th className="col-game">Trò chơi</th>
                <th className="col-score">Điểm cao nhất</th>
                <th className="col-wins">Trận thắng</th>
                <th className="col-xp">XP tích lũy</th>
              </tr>
            </thead>
            <tbody>
              {leaderboard.map((entry) => (
                <tr key={`${entry.userId}-${entry.gameType}-${entry.rank}`}>
                  <td className="col-rank">
                    {entry.rank === 1 ? (
                      <span className="rank-badge rank-1">🥇 1</span>
                    ) : entry.rank === 2 ? (
                      <span className="rank-badge rank-2">🥈 2</span>
                    ) : entry.rank === 3 ? (
                      <span className="rank-badge rank-3">🥉 3</span>
                    ) : (
                      <span className="rank-badge rank-normal">#{entry.rank}</span>
                    )}
                  </td>
                  <td className="col-player">
                    <div className="player-cell">
                      <div className="player-avatar-placeholder">
                        {entry.displayName ? entry.displayName.charAt(0).toUpperCase() : "U"}
                      </div>
                      <span className="player-name">{entry.displayName || "Học viên ẩn danh"}</span>
                    </div>
                  </td>
                  <td className="col-game">
                    <span className="game-type-chip">{formatGameName(entry.gameType)}</span>
                  </td>
                  <td className="col-score">
                    <span className="score-val">{entry.highScore}</span>
                  </td>
                  <td className="col-wins">{entry.totalWins}</td>
                  <td className="col-xp">
                    <span className="xp-val">+{entry.totalXpEarned} XP</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

function formatGameName(type: string): string {
  switch (type) {
    case "word_match":
      return "Nối từ";
    case "sentence_scramble":
      return "Xếp câu";
    case "typing_race":
      return "Đua gõ từ";
    case "fill_blitz":
      return "Điền chỗ trống";
    case "sound_reflex":
      return "Phản xạ nghe";
    case "lingo_wordle":
      return "Lingo Wordle";
    default:
      return type;
  }
}
