"use client";

import React from "react";
import { Trophy, Zap, Award } from "lucide-react";
import type { MyGameStatsDto } from "@/lib/api";

interface GamePersonalStatsProps {
  stats: MyGameStatsDto | null;
}

export const GamePersonalStats: React.FC<GamePersonalStatsProps> = ({ stats }) => {
  if (!stats) return null;

  return (
    <div className="games-stats-banner">
      <div className="games-stat-card">
        <div className="stat-icon olive">
          <GamepadIcon size={18} />
        </div>
        <div>
          <span className="stat-label">Tổng trận đã chơi</span>
          <span className="stat-value">{stats.totalGamesPlayed || 0}</span>
        </div>
      </div>

      <div className="games-stat-card">
        <div className="stat-icon terracotta">
          <Trophy size={18} />
        </div>
        <div>
          <span className="stat-label">Tổng trận thắng</span>
          <span className="stat-value">{stats.totalWins || 0}</span>
        </div>
      </div>

      <div className="games-stat-card">
        <div className="stat-icon gold">
          <Zap size={18} />
        </div>
        <div>
          <span className="stat-label">Tổng XP thu về</span>
          <span className="stat-value">+{stats.totalXpEarned || 0} XP</span>
        </div>
      </div>

      <div className="games-stat-card">
        <div className="stat-icon blue">
          <Award size={18} />
        </div>
        <div>
          <span className="stat-label">Hạng kỷ lục</span>
          <span className="stat-value">
            {Object.keys(stats.byGame || {}).length > 0 ? "Cao thủ" : "Mới bắt đầu"}
          </span>
        </div>
      </div>
    </div>
  );
};

function GamepadIcon({ size }: { size: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="6" x2="10" y1="12" y2="12" />
      <line x1="8" x2="8" y1="10" y2="14" />
      <line x1="15" x2="15.01" y1="13" y2="13" />
      <line x1="18" x2="18.01" y1="11" y2="11" />
      <rect width="20" height="12" x="2" y="6" rx="6" />
    </svg>
  );
}
