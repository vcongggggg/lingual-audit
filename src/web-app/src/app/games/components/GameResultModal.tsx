"use client";

import React from "react";
import { Sparkles, Trophy, CheckCircle2, RotateCcw } from "lucide-react";
import type { GameSubmitResult } from "@/lib/api";

interface GameResultModalProps {
  summary: GameSubmitResult | null;
  onClose: () => void;
  onPlayAgain: () => void;
}

export const GameResultModal: React.FC<GameResultModalProps> = ({
  summary,
  onClose,
  onPlayAgain,
}) => {
  if (!summary) return null;

  return (
    <div className="game-modal-backdrop">
      <div className="game-result-card">
        <div className="result-header">
          <div className="trophy-sparkle">
            <Trophy size={48} className="text-olive" />
          </div>
          <h2>Hoàn thành thử thách!</h2>
          <p className="result-tagline">
            Bạn đã nỗ lực hết mình và hoàn thành phiên trò chơi xuất sắc.
          </p>
        </div>

        <div className="result-primary-metrics">
          <div className="primary-metric-box">
            <span className="metric-label">Tổng điểm</span>
            <span className="metric-number score-glow">{summary.score}</span>
          </div>
          <div className="primary-metric-box">
            <span className="metric-label">XP kiếm được</span>
            <span className="metric-number xp-glow">+{summary.xpEarned} XP</span>
          </div>
        </div>

        <div className="result-breakdown-grid">
          <div className="breakdown-item">
            <span className="item-title">Độ chính xác:</span>
            <span className="item-value">{Math.round(summary.accuracy * 100)}%</span>
          </div>
          <div className="breakdown-item">
            <span className="item-title">Hệ số combo:</span>
            <span className="item-value">{summary.comboMultiplier.toFixed(1)}x</span>
          </div>
          <div className="breakdown-item">
            <span className="item-title">Thưởng thời gian:</span>
            <span className="item-value">+{summary.timeBonus} pts</span>
          </div>
          <div className="breakdown-item">
            <span className="item-title">Thưởng chuỗi đúng:</span>
            <span className="item-value">+{summary.streakBonus} pts</span>
          </div>
          <div className="breakdown-item">
            <span className="item-title">Thời gian hoàn thành:</span>
            <span className="item-value">{summary.durationSeconds} giây</span>
          </div>
          <div className="breakdown-item">
            <span className="item-title">Số câu đúng:</span>
            <span className="item-value">
              {summary.correctCount}/{summary.totalQuestions}
            </span>
          </div>
        </div>

        {summary.isNewHighScore && (
          <div className="new-high-score-banner">
            <Sparkles size={16} />
            <span>Kỷ lục mới! Bạn vừa đạt điểm số cao nhất từ trước đến nay!</span>
          </div>
        )}

        {summary.stampsAwarded && summary.stampsAwarded.length > 0 && (
          <div className="stamps-awarded-row">
            <span className="stamps-title">Huy hiệu đạt được:</span>
            <div className="stamps-list">
              {summary.stampsAwarded.map((stamp) => (
                <span key={stamp} className="stamp-chip">
                  <CheckCircle2 size={14} />
                  <span>
                    {stamp === "PERFECT_SCORE"
                      ? "Tuyệt đối 100%"
                      : stamp === "STREAK_7_DAYS"
                      ? "Chuỗi 7 ngày"
                      : stamp}
                  </span>
                </span>
              ))}
            </div>
          </div>
        )}

        <div className="result-modal-actions">
          <button onClick={onClose} className="action-btn-secondary">
            Về Game Center
          </button>
          <button onClick={onPlayAgain} className="action-btn-primary">
            <RotateCcw size={16} />
            <span>Chơi lại màn này</span>
          </button>
        </div>
      </div>
    </div>
  );
};
