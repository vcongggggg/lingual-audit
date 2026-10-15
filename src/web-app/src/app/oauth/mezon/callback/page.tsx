'use client';

import React, { Suspense, useEffect, useRef, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { useAuth } from '@/lib/auth-context';
import { loginWithMezon } from '@/lib/api';
import { CheckCircle2, AlertCircle, Loader2, ArrowRight } from 'lucide-react';
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

        // Automatically navigate to /auth test page after 1.5 seconds
        setTimeout(() => {
          router.push('/auth');
        }, 1500);
      } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Đổi mã xác thực thất bại.';
        setStatus('error');
        setErrorMessage(message);
      }
    }

    handleExchange();
  }, [searchParams, login, router]);

  return (
    <div className="w-full max-w-md p-8 bg-slate-900/90 border border-slate-800 rounded-2xl shadow-2xl backdrop-blur-md text-center">
      {status === 'loading' && (
        <div className="flex flex-col items-center gap-4">
          <div className="relative">
            <div className="w-16 h-16 rounded-full border-4 border-indigo-500/30 border-t-indigo-500 animate-spin" />
            <Loader2 className="w-8 h-8 text-indigo-400 absolute inset-0 m-auto animate-pulse" />
          </div>
          <div>
            <h2 className="text-xl font-bold text-white mb-1">Đang xử lý đăng nhập Mezon</h2>
            <p className="text-sm text-slate-400">
              Đang xác thực authorization code và tạo phiên làm việc...
            </p>
          </div>
        </div>
      )}

      {status === 'success' && (
        <div className="flex flex-col items-center gap-4">
          <div className="w-16 h-16 rounded-full bg-emerald-500/20 border border-emerald-500/40 flex items-center justify-center text-emerald-400">
            <CheckCircle2 className="w-10 h-10 animate-bounce" />
          </div>
          <div>
            <h2 className="text-xl font-bold text-white mb-1">Đăng nhập thành công!</h2>
            <p className="text-sm text-slate-300">
              Chào mừng, <span className="font-semibold text-emerald-400">{userInfo?.displayName}</span> ({userInfo?.role})
            </p>
            <p className="text-xs text-slate-500 mt-2">Đang chuyển hướng về trang điều khiển...</p>
          </div>
          <Link
            href="/auth"
            className="mt-2 inline-flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white text-sm font-medium rounded-lg transition-colors shadow-lg shadow-indigo-500/20"
          >
            Vào bảng kiểm tra <ArrowRight className="w-4 h-4" />
          </Link>
        </div>
      )}

      {status === 'error' && (
        <div className="flex flex-col items-center gap-4">
          <div className="w-16 h-16 rounded-full bg-rose-500/20 border border-rose-500/40 flex items-center justify-center text-rose-400">
            <AlertCircle className="w-10 h-10" />
          </div>
          <div>
            <h2 className="text-xl font-bold text-white mb-1">Xác thực thất bại</h2>
            <p className="text-sm text-rose-300 break-words max-w-sm">{errorMessage}</p>
          </div>
          <Link
            href="/auth"
            className="mt-2 inline-flex items-center gap-2 px-4 py-2 bg-slate-800 hover:bg-slate-700 text-slate-200 text-sm font-medium rounded-lg transition-colors border border-slate-700"
          >
            Quay lại trang thử nghiệm
          </Link>
        </div>
      )}
    </div>
  );
}

export default function MezonCallbackPage() {
  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-radial from-slate-900 via-slate-950 to-black">
      <Suspense
        fallback={
          <div className="w-full max-w-md p-8 bg-slate-900/90 border border-slate-800 rounded-2xl shadow-2xl text-center text-slate-300">
            <Loader2 className="w-8 h-8 animate-spin mx-auto mb-3 text-indigo-400" />
            Đang tải dữ liệu callback...
          </div>
        }
      >
        <MezonCallbackContent />
      </Suspense>
    </div>
  );
}
