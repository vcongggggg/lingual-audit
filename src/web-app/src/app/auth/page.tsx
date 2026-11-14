'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/auth-context';
import { getMezonAuthorizeUrl } from '@/lib/api';
import {
  BookOpen,
  Trophy,
  Zap,
  ShieldCheck,
  ArrowRight,
  Loader2,
  Sparkles,
  UserCheck,
} from 'lucide-react';

export default function LoginPage() {
  const router = useRouter();
  const { isAuthenticated, user, logout } = useAuth();
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const handleMezonLogin = async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const redirectUri = `${window.location.origin}/oauth/mezon/callback`;
      sessionStorage.setItem('mezon_oauth_redirect_uri', redirectUri);

      const result = await getMezonAuthorizeUrl(redirectUri);
      if (result?.authorizeUrl) {
        window.location.href = result.authorizeUrl;
      } else {
        throw new Error('Không nhận được đường dẫn đăng nhập từ máy chủ.');
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Không thể kết nối đến cổng Mezon OAuth.';
      setErrorMessage(msg);
      setIsLoading(false);
    }
  };

  const handleGuestContinue = () => {
    router.push('/');
  };

  return (
    <div className="w-full flex-1 min-h-[calc(100svh-68px)] flex items-center justify-center p-4 sm:p-6 md:p-10 bg-[var(--paper,#f5f2eb)] text-[var(--ink,#252820)]">
      <div className="w-full max-w-lg bg-[var(--surface,#fffefa)] border border-[var(--line,#e5e0d5)] rounded-2xl shadow-xl p-6 sm:p-10 flex flex-col gap-8 transition-all">
        
        {/* Brand Header */}
        <div className="flex flex-col items-center text-center gap-3">
          <div className="w-14 h-14 rounded-2xl bg-[#eff1e8] border border-[#d7ddca] flex items-center justify-center text-[var(--olive,#69734f)] shadow-sm">
            <BookOpen className="w-7 h-7" />
          </div>
          <div>
            <span className="text-[11px] font-bold uppercase tracking-widest text-[var(--olive,#69734f)] bg-[#eff1e8] px-2.5 py-0.5 rounded-full border border-[#d7ddca]">
              LinguaMezon SSO
            </span>
            <h1 className="text-2xl sm:text-3xl font-extrabold tracking-tight mt-2 text-[var(--ink,#252820)]">
              Đăng nhập LinguaMezon
            </h1>
            <p className="text-xs sm:text-sm text-[var(--muted,#77786f)] mt-1 max-w-sm">
              Không gian học từ vựng tiếng Anh tương tác thông minh kết nối cùng nền tảng Mezon.
            </p>
          </div>
        </div>

        {/* Thông báo nếu đã có phiên đăng nhập */}
        {isAuthenticated && user && (
          <div className="p-4 bg-[#eff1e8] border border-[#d7ddca] rounded-xl flex items-center justify-between gap-3 text-xs sm:text-sm">
            <div className="flex items-center gap-2.5 min-w-0">
              <div className="w-8 h-8 rounded-full bg-[var(--olive,#69734f)] text-white flex items-center justify-center font-bold text-xs shrink-0">
                {user.displayName?.charAt(0).toUpperCase() || 'U'}
              </div>
              <div className="min-w-0">
                <p className="font-semibold text-[var(--ink,#252820)] truncate">
                  {user.displayName}
                </p>
                <p className="text-[11px] text-[var(--olive,#69734f)]">
                  Đang hoạt động ({user.role === 'admin' ? 'Quản trị' : 'Học viên'})
                </p>
              </div>
            </div>
            <div className="flex items-center gap-2 shrink-0">
              <button
                onClick={() => router.push('/')}
                className="px-3 py-1.5 bg-[var(--olive,#69734f)] hover:opacity-90 text-white rounded-lg text-xs font-semibold flex items-center gap-1 transition-all"
              >
                Vào học <ArrowRight className="w-3.5 h-3.5" />
              </button>
              <button
                onClick={logout}
                className="px-2.5 py-1.5 border border-[#d7ddca] hover:bg-white text-[var(--muted,#77786f)] rounded-lg text-xs transition-all"
                title="Đăng xuất tài khoản này"
              >
                Đổi tài khoản
              </button>
            </div>
          </div>
        )}

        {/* Feature Highlights */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-2.5 pt-1">
          <div className="p-3 bg-[#fbfaf5] border border-[var(--line,#e5e0d5)] rounded-xl flex flex-col items-center text-center gap-1.5">
            <Zap className="w-4 h-4 text-[var(--terracotta,#bd684d)]" />
            <span className="text-xs font-bold text-[var(--ink,#252820)]">Lặp ngắt quãng</span>
            <span className="text-[10px] text-[var(--muted,#77786f)] leading-tight">SRS SM-2 ghi nhớ lâu bền</span>
          </div>
          <div className="p-3 bg-[#fbfaf5] border border-[var(--line,#e5e0d5)] rounded-xl flex flex-col items-center text-center gap-1.5">
            <Trophy className="w-4 h-4 text-amber-600" />
            <span className="text-xs font-bold text-[var(--ink,#252820)]">Bảng xếp hạng</span>
            <span className="text-[10px] text-[var(--muted,#77786f)] leading-tight">Đua top Cá nhân & Clan</span>
          </div>
          <div className="p-3 bg-[#fbfaf5] border border-[var(--line,#e5e0d5)] rounded-xl flex flex-col items-center text-center gap-1.5">
            <Sparkles className="w-4 h-4 text-[var(--olive,#69734f)]" />
            <span className="text-xs font-bold text-[var(--ink,#252820)]">Đấu trường 1vs1</span>
            <span className="text-[10px] text-[var(--muted,#77786f)] leading-tight">Thử thách tốc độ từ vựng</span>
          </div>
        </div>

        {/* Error alert */}
        {errorMessage && (
          <div className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-xs flex items-center gap-2">
            <span>⚠️</span>
            <span>{errorMessage}</span>
          </div>
        )}

        {/* Primary Action Buttons */}
        <div className="flex flex-col gap-3">
          <button
            onClick={handleMezonLogin}
            disabled={isLoading}
            className="w-full min-h-[50px] px-6 py-3 bg-[var(--olive,#69734f)] hover:opacity-95 text-white font-bold text-sm sm:text-base rounded-xl flex items-center justify-center gap-3 transition-all shadow-md shadow-[#69734f33] active:scale-[0.99] disabled:opacity-50 cursor-pointer"
          >
            {isLoading ? (
              <>
                <Loader2 className="w-5 h-5 animate-spin" />
                <span>Đang kết nối Mezon OAuth...</span>
              </>
            ) : (
              <>
                <ShieldCheck className="w-5 h-5 text-emerald-300" />
                <span>Đăng nhập với tài khoản Mezon</span>
                <ArrowRight className="w-4 h-4 ml-auto" />
              </>
            )}
          </button>

          <button
            onClick={handleGuestContinue}
            className="w-full min-h-[44px] px-4 py-2.5 bg-transparent hover:bg-[#eeece4] border border-[var(--line,#e5e0d5)] text-[var(--ink,#252820)] font-semibold text-xs sm:text-sm rounded-xl flex items-center justify-center gap-2 transition-all cursor-pointer"
          >
            <UserCheck className="w-4 h-4 text-[var(--muted,#77786f)]" />
            <span>Tiếp tục với tư cách Khách trải nghiệm</span>
          </button>
        </div>

        {/* Trust & Security Note */}
        <div className="pt-2 border-t border-[var(--line,#e5e0d5)] flex items-center justify-center gap-2 text-[11px] text-[var(--muted,#77786f)] text-center">
          <ShieldCheck className="w-3.5 h-3.5 text-[var(--olive,#69734f)]" />
          <span>Xác thực an toàn và bảo mật bởi Mezon Platform SSO</span>
        </div>

      </div>
    </div>
  );
}
