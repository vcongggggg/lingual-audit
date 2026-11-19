'use client';

import React, { Suspense, useEffect, useRef, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { useAuth } from '@/lib/auth-context';
import { loginWithMezon } from '@/lib/api';
import { CheckCircle2, AlertCircle, Loader2, ArrowRight, BookOpen } from 'lucide-react';
import Link from 'next/link';

function MezonCallbackContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { login } = useAuth();

  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
  const [errorMessage, setErrorMessage] = useState<string>('');
  const [userInfo, setUserInfo] = useState<{ displayName: string; role: string } | null>(null);
  const hasExchangedRef = useRef(false);

  useEffect(() => {
    if (hasExchangedRef.current) return;
    hasExchangedRef.current = true;

    const code = searchParams.get('code');
    const state = searchParams.get('state');
    const errorParam = searchParams.get('error');
    const errorDesc = searchParams.get('error_description');

    if (errorParam) {
      setStatus('error');
      setErrorMessage(errorDesc || errorParam || 'Mezon từ chối ủy quyền.');
      return;
    }

    if (!code) {
      setStatus('error');
      setErrorMessage('Không tìm thấy authorization code trong phản hồi từ Mezon.');
      return;
    }

    async function handleExchange() {
      try {
        const savedRedirectUri =
          typeof window !== 'undefined'
            ? sessionStorage.getItem('mezon_oauth_redirect_uri') || undefined
            : undefined;

        const result = await loginWithMezon({
          code: code || undefined,
          state: state || undefined,
          redirectUri: savedRedirectUri,
        });

        login(result);
        setUserInfo({
          displayName: result.user.displayName,
          role: result.user.role || 'learner',
        });
        setStatus('success');

        // Tự động chuyển hướng về Trang chủ sau 1.2 giây
        setTimeout(() => {
          router.push('/');
        }, 1200);
      } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Đổi mã xác thực thất bại.';
        setStatus('error');
        setErrorMessage(message);
      }
    }

    handleExchange();
  }, [searchParams, login, router]);

  return (
    <div className="w-full max-w-md p-8 bg-[var(--surface,#fffefa)] border border-[var(--line,#e5e0d5)] rounded-2xl shadow-xl text-center flex flex-col items-center gap-6">
      <div className="w-12 h-12 rounded-2xl bg-[#eff1e8] border border-[#d7ddca] flex items-center justify-center text-[var(--olive,#69734f)]">
        <BookOpen className="w-6 h-6" />
      </div>

      {status === 'loading' && (
        <div className="flex flex-col items-center gap-4">
          <div className="relative">
            <div className="w-14 h-14 rounded-full border-4 border-[#d7ddca] border-t-[var(--olive,#69734f)] animate-spin" />
            <Loader2 className="w-7 h-7 text-[var(--olive,#69734f)] absolute inset-0 m-auto animate-pulse" />
          </div>
          <div>
            <h2 className="text-xl font-bold text-[var(--ink,#252820)] mb-1">
              Đang xác thực tài khoản Mezon
            </h2>
            <p className="text-xs sm:text-sm text-[var(--muted,#77786f)]">
              Đang tạo phiên đăng nhập an toàn cùng LinguaMezon...
            </p>
          </div>
        </div>
      )}

      {status === 'success' && (
        <div className="flex flex-col items-center gap-4">
          <div className="w-14 h-14 rounded-full bg-emerald-50 border border-emerald-200 flex items-center justify-center text-emerald-600">
            <CheckCircle2 className="w-8 h-8 animate-bounce" />
          </div>
          <div>
            <h2 className="text-xl font-bold text-[var(--ink,#252820)] mb-1">
              Đăng nhập thành công!
            </h2>
            <p className="text-xs sm:text-sm text-[var(--ink,#252820)]">
              Chào mừng, <span className="font-bold text-[var(--olive,#69734f)]">{userInfo?.displayName}</span>
            </p>
            <p className="text-xs text-[var(--muted,#77786f)] mt-2">
              Đang chuyển hướng vào Trang chủ...
            </p>
          </div>
          <Link
            href="/"
            className="mt-2 inline-flex items-center gap-2 px-5 py-2.5 bg-[var(--olive,#69734f)] hover:opacity-90 text-white text-xs sm:text-sm font-semibold rounded-xl transition-all shadow-md shadow-[#69734f33]"
          >
            Vào Trang chủ ngay <ArrowRight className="w-4 h-4" />
          </Link>
        </div>
      )}

      {status === 'error' && (
        <div className="flex flex-col items-center gap-4">
          <div className="w-14 h-14 rounded-full bg-red-50 border border-red-200 flex items-center justify-center text-red-600">
            <AlertCircle className="w-8 h-8" />
          </div>
          <div>
            <h2 className="text-xl font-bold text-[var(--ink,#252820)] mb-1">
              Xác thực không thành công
            </h2>
            <p className="text-xs sm:text-sm text-red-700 break-words max-w-sm">
              {errorMessage}
            </p>
          </div>
          <Link
            href="/auth"
            className="mt-2 inline-flex items-center gap-2 px-5 py-2.5 bg-[var(--surface,#fffefa)] hover:bg-[#eeece4] border border-[var(--line,#e5e0d5)] text-[var(--ink,#252820)] text-xs sm:text-sm font-semibold rounded-xl transition-all"
          >
            Thử đăng nhập lại
          </Link>
        </div>
      )}
    </div>
  );
}

export default function MezonCallbackPage() {
  return (
    <div className="w-full flex-1 min-h-[calc(100svh-68px)] flex items-center justify-center p-4 bg-[var(--paper,#f5f2eb)] text-[var(--ink,#252820)]">
      <Suspense
        fallback={
          <div className="w-full max-w-md p-8 bg-[var(--surface,#fffefa)] border border-[var(--line,#e5e0d5)] rounded-2xl shadow-xl text-center text-[var(--muted,#77786f)]">
            <Loader2 className="w-8 h-8 animate-spin mx-auto mb-3 text-[var(--olive,#69734f)]" />
            Đang tải dữ liệu xác thực...
          </div>
        }
      >
        <MezonCallbackContent />
      </Suspense>
    </div>
  );
}
