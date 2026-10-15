'use client';

import React, { useState } from 'react';
import { useAuth } from '@/lib/auth-context';
import {
  API_URL,
  getMezonAuthorizeUrl,
  loginWithMezon,
  getCurrentUser,
  fetchApiResponse,
} from '@/lib/api';
import {
  ShieldCheck,
  LogIn,
  LogOut,
  KeyRound,
  UserCheck,
  RefreshCw,
  Terminal,
  ExternalLink,
  CheckCircle2,
  XCircle,
  Copy,
  AlertTriangle,
  Zap,
} from 'lucide-react';

interface ApiLogItem {
  id: string;
  timestamp: string;
  method: string;
  endpoint: string;
  status: number | string;
  durationMs: number;
  requestPayload?: unknown;
  responsePayload?: unknown;
  error?: string;
}

export default function AuthTestPage() {
  const { user, token, isAuthenticated, login, logout, loading, updateUser } = useAuth();

  // Test state
  const [logs, setLogs] = useState<ApiLogItem[]>([]);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [lastRevokedToken, setLastRevokedToken] = useState<string | null>(null);
  const [copiedToken, setCopiedToken] = useState(false);

  // OAuth Redirect URI
  const [oauthRedirectUri, setOauthRedirectUri] = useState('http://localhost:3000/oauth/mezon/callback');

  // Mock handshake form
  const [mockUserId, setMockUserId] = useState('2036383630950928384');
  const [mockDisplayName, setMockDisplayName] = useState('Nguyễn Văn Mezon');
  const [mockTokenCode, setMockTokenCode] = useState('mezon_dev_code_test_999');

  const addLog = (item: Omit<ApiLogItem, 'id' | 'timestamp'>) => {
    const newLog: ApiLogItem = {
      ...item,
      id: Math.random().toString(36).substring(2, 9),
      timestamp: new Date().toLocaleTimeString(),
    };
    setLogs((prev) => [newLog, ...prev.slice(0, 19)]);
  };

  // 1. Mezon OAuth 2.0 Real Flow
  const handleStartOAuth = async () => {
    setActionLoading('oauth');
    const startTime = performance.now();
    try {
      const uriToSend = oauthRedirectUri.trim() || `${window.location.origin}/oauth/mezon/callback`;
      if (typeof window !== 'undefined') {
        sessionStorage.setItem('mezon_oauth_redirect_uri', uriToSend);
      }

      const result = await getMezonAuthorizeUrl(uriToSend);

      addLog({
        method: 'GET',
        endpoint: `/auth/mezon/authorize?redirectUri=${encodeURIComponent(uriToSend)}`,
        status: 200,
        durationMs: Math.round(performance.now() - startTime),
        responsePayload: result,
      });

      // Redirect user to Mezon OAuth authorize URL
      window.location.href = result.authorizeUrl;
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Không thể lấy URL ủy quyền Mezon';
      addLog({
        method: 'GET',
        endpoint: '/auth/mezon/authorize',
        status: 'ERROR',
        durationMs: Math.round(performance.now() - startTime),
        error: msg,
      });
      alert(`Lỗi khởi tạo OAuth: ${msg}`);
    } finally {
      setActionLoading(null);
    }
  };

  // 2. Dev Mock / Fast Handshake Login
  const handleDevLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setActionLoading('dev-login');
    const startTime = performance.now();

    const payload = {
      code: mockTokenCode,
      state: 'dev_local_test_state',
      mezonUserId: mockUserId,
      displayName: mockDisplayName,
    };

    try {
      const result = await loginWithMezon(payload);

      addLog({
        method: 'POST',
        endpoint: '/auth/mezon',
        status: 200,
        durationMs: Math.round(performance.now() - startTime),
        requestPayload: payload,
        responsePayload: result,
      });

      login(result);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Đăng nhập dev thất bại';
      addLog({
        method: 'POST',
        endpoint: '/auth/mezon',
        status: (err as { status?: number })?.status || 'ERROR',
        durationMs: Math.round(performance.now() - startTime),
        requestPayload: payload,
        error: msg,
      });
      alert(`Đăng nhập thất bại: ${msg}`);
    } finally {
      setActionLoading(null);
    }
  };

  // 3. Test Active Token with Protected API
  const handleTestProtectedApi = async () => {
    setActionLoading('verify-active');
    const startTime = performance.now();
    try {
      const data = (await getCurrentUser()) as { avatarUrl?: string; name?: string } | undefined;
      if (data?.avatarUrl && data.avatarUrl !== user?.avatarUrl) {
        updateUser({ avatarUrl: data.avatarUrl });
      }

      addLog({
        method: 'GET',
        endpoint: '/auth/me',
        status: 200,
        durationMs: Math.round(performance.now() - startTime),
        responsePayload: data,
      });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Lỗi gọi API bảo mật';
      addLog({
        method: 'GET',
        endpoint: '/auth/me',
        status: (err as { status?: number })?.status || 'FAIL',
        durationMs: Math.round(performance.now() - startTime),
        error: msg,
      });
    } finally {
      setActionLoading(null);
    }
  };

  // 4. Logout & Token Revocation
  const handleLogout = async () => {
    if (!token) return;
    setActionLoading('logout');
    const startTime = performance.now();
    const tokenToRemember = token;

    try {
      await logout();
      setLastRevokedToken(tokenToRemember);

      addLog({
        method: 'POST',
        endpoint: '/auth/logout',
        status: 200,
        durationMs: Math.round(performance.now() - startTime),
        responsePayload: {
          success: true,
          message: 'Đăng xuất và thu hồi token thành công trên backend',
        },
      });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Lỗi khi đăng xuất';
      addLog({
        method: 'POST',
        endpoint: '/auth/logout',
        status: 'ERROR',
        durationMs: Math.round(performance.now() - startTime),
        error: msg,
      });
    } finally {
      setActionLoading(null);
    }
  };

  // 5. Test Revoked Token (must return 401 Unauthorized)
  const handleTestRevokedToken = async () => {
    if (!lastRevokedToken) return;
    setActionLoading('verify-revoked');
    const startTime = performance.now();

    try {
      const response = await fetchApiResponse('/auth/me', {
        method: 'GET',
        explicitToken: lastRevokedToken,
      });

      const data = await response.json().catch(() => ({}));
      addLog({
        method: 'GET',
        endpoint: '/auth/me [WITH REVOKED TOKEN]',
        status: response.status,
        durationMs: Math.round(performance.now() - startTime),
        responsePayload: data,
      });
    } catch (err: unknown) {
      const status = (err as { status?: number })?.status || 401;
      const msg = err instanceof Error ? err.message : 'Token không hợp lệ';
      addLog({
        method: 'GET',
        endpoint: '/auth/me [WITH REVOKED TOKEN]',
        status: status,
        durationMs: Math.round(performance.now() - startTime),
        error: `Mong đợi 401 Unauthorized: ${msg}`,
      });
    } finally {
      setActionLoading(null);
    }
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    setCopiedToken(true);
    setTimeout(() => setCopiedToken(false), 2000);
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 p-4 md:p-8">
      <div className="max-w-6xl mx-auto space-y-6">
        {/* Header */}
        <header className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4 p-6 bg-slate-900/80 border border-slate-800 rounded-2xl shadow-xl backdrop-blur-md">
          <div className="flex items-center gap-4">
            <div className="w-12 h-12 rounded-xl bg-gradient-to-tr from-indigo-600 to-violet-500 flex items-center justify-center shadow-lg shadow-indigo-500/30">
              <KeyRound className="w-6 h-6 text-white" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-white flex items-center gap-2">
                Mezon OAuth 2.0 & Token Revocation
                <span className="text-xs px-2.5 py-0.5 rounded-full bg-indigo-500/20 text-indigo-400 border border-indigo-500/30 font-medium">
                  Lingual Identity
                </span>
              </h1>
              <p className="text-sm text-slate-400 mt-1">
                Giao diện kiểm thử luồng Đăng nhập SSO Mezon, Xác thực JWT Bearer và Thu hồi Token
              </p>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <div className="text-right">
              <span className="text-xs text-slate-500 block">Trạng thái phiên</span>
              <span
                className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold ${
                  isAuthenticated
                    ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/40'
                    : 'bg-slate-800 text-slate-400 border border-slate-700'
                }`}
              >
                <span
                  className={`w-2 h-2 rounded-full ${
                    isAuthenticated ? 'bg-emerald-400 animate-pulse' : 'bg-slate-500'
                  }`}
                />
                {isAuthenticated ? 'Đã xác thực' : 'Chưa đăng nhập'}
              </span>
            </div>
          </div>
        </header>

        {/* Environment Info */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs font-mono">
          <div className="p-3 bg-slate-900/50 border border-slate-800 rounded-xl flex items-center justify-between">
            <span className="text-slate-400">Backend API URL:</span>
            <span className="text-indigo-300 font-semibold">{API_URL}</span>
          </div>
          <div className="p-3 bg-slate-900/50 border border-slate-800 rounded-xl flex items-center justify-between">
            <span className="text-slate-400">OAuth Callback URI:</span>
            <span className="text-violet-300 font-semibold">
              {typeof window !== 'undefined'
                ? `${window.location.origin}/oauth/mezon/callback`
                : 'http://localhost:3000/oauth/mezon/callback'}
            </span>
          </div>
        </div>

        {/* Main Grid: Auth Actions & Profile */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          {/* Left Column: Login / Profile Actions (7 cols) */}
          <div className="lg:col-span-7 space-y-6">
            {/* Authenticated State */}
            {isAuthenticated && user ? (
              <div className="p-6 bg-slate-900/90 border border-emerald-500/30 rounded-2xl shadow-xl space-y-5">
                <div className="flex items-center justify-between border-b border-slate-800 pb-4">
                  <div className="flex items-center gap-3">
                    <div className="relative w-12 h-12 rounded-full overflow-hidden border border-emerald-500/40 bg-emerald-500/20 flex-shrink-0 flex items-center justify-center">
                      {user.avatarUrl ? (
                        <img
                          src={user.avatarUrl}
                          alt={user.displayName}
                          className="w-full h-full object-cover"
                          referrerPolicy="no-referrer"
                          onError={(e) => {
                            (e.currentTarget as HTMLElement).style.display = 'none';
                            const fallback = document.getElementById(`avatar-fallback-${user.id}`);
                            if (fallback) fallback.style.display = 'flex';
                          }}
                        />
                      ) : null}
                      <span
                        id={`avatar-fallback-${user.id}`}
                        className={`text-emerald-400 font-bold text-lg ${user.avatarUrl ? 'hidden' : 'flex'}`}
                      >
                        {user.displayName.charAt(0).toUpperCase()}
                      </span>
                    </div>

                    <div>
                      <h2 className="text-lg font-bold text-white flex items-center gap-2">
                        {user.displayName}
                        <span className="text-xs px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/30 uppercase">
                          {user.role}
                        </span>
                      </h2>
                      <p className="text-xs text-slate-400">Mezon ID: {user.mezonUserId}</p>
                    </div>
                  </div>

                  <span className="px-2.5 py-1 text-xs rounded-md bg-slate-800 text-slate-300 border border-slate-700">
                    Status: {user.status || 'active'}
                  </span>
                </div>

                {/* User Details */}
                <div className="grid grid-cols-2 gap-3 text-xs">
                  <div className="p-2.5 bg-slate-950/60 rounded-lg border border-slate-800">
                    <span className="text-slate-500 block">User Internal ID:</span>
                    <span className="text-slate-200 font-mono break-all">{user.id}</span>
                  </div>
                  <div className="p-2.5 bg-slate-950/60 rounded-lg border border-slate-800">
                    <span className="text-slate-500 block">Locale & Timezone:</span>
                    <span className="text-slate-200">
                      {user.preferredLocale || 'vi-VN'} • {user.timezone || 'Asia/Ho_Chi_Minh'}
                    </span>
                  </div>
                  {user.avatarUrl && (
                    <div className="p-2.5 bg-slate-950/60 rounded-lg border border-slate-800 col-span-2">
                      <span className="text-slate-500 block">Avatar URL:</span>
                      <a href={user.avatarUrl} target="_blank" rel="noreferrer" className="text-emerald-400 hover:underline break-all">
                        {user.avatarUrl}
                      </a>
                    </div>
                  )}
                </div>


                {/* Token Box */}
                {token && (
                  <div className="p-3 bg-slate-950/80 rounded-xl border border-slate-800 space-y-2">
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-slate-400 font-semibold flex items-center gap-1.5">
                        <KeyRound className="w-3.5 h-3.5 text-indigo-400" /> JWT Bearer Token:
                      </span>
                      <button
                        onClick={() => copyToClipboard(token)}
                        className="inline-flex items-center gap-1 text-xs text-indigo-400 hover:text-indigo-300 transition-colors"
                      >
                        <Copy className="w-3 h-3" />
                        {copiedToken ? 'Đã sao chép' : 'Sao chép'}
                      </button>
                    </div>
                    <div className="p-2 bg-slate-900 rounded font-mono text-[11px] text-slate-300 break-all max-h-20 overflow-y-auto">
                      {token}
                    </div>
                  </div>
                )}

                {/* Authenticated Action Buttons */}
                <div className="pt-2 flex flex-wrap gap-3">
                  <button
                    onClick={handleTestProtectedApi}
                    disabled={actionLoading !== null}
                    className="flex-1 min-w-[180px] inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-indigo-600 hover:bg-indigo-500 text-white rounded-xl text-sm font-semibold transition-all shadow-lg shadow-indigo-600/30 disabled:opacity-50"
                  >
                    {actionLoading === 'verify-active' ? (
                      <RefreshCw className="w-4 h-4 animate-spin" />
                    ) : (
                      <ShieldCheck className="w-4 h-4" />
                    )}
                    Kiểm tra phiên (GET /auth/me)
                  </button>

                  <button
                    onClick={handleLogout}
                    disabled={actionLoading !== null}
                    className="flex-1 min-w-[180px] inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-rose-600/90 hover:bg-rose-500 text-white rounded-xl text-sm font-semibold transition-all shadow-lg shadow-rose-600/30 disabled:opacity-50"
                  >
                    {actionLoading === 'logout' ? (
                      <RefreshCw className="w-4 h-4 animate-spin" />
                    ) : (
                      <LogOut className="w-4 h-4" />
                    )}
                    Đăng xuất (Thu hồi token)
                  </button>
                </div>
              </div>
            ) : (
              /* Unauthenticated State: Login Options */
              <div className="space-y-6">
                {/* 1. Official Mezon OAuth 2.0 Flow */}
                <div className="p-6 bg-slate-900/90 border border-slate-800 rounded-2xl shadow-xl space-y-4">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 rounded-lg bg-indigo-500/20 text-indigo-400 flex items-center justify-center">
                      <Zap className="w-5 h-5" />
                    </div>
                    <div>
                      <h2 className="text-lg font-bold text-white">
                        Luồng Thực tế: Mezon OAuth 2.0
                      </h2>
                      <p className="text-xs text-slate-400">
                        Chuyển hướng tới trang đăng nhập Mezon và nhận callback về client
                      </p>
                    </div>
                  </div>

                  <p className="text-xs text-slate-300 leading-relaxed">
                    Gọi endpoint{' '}
                    <code className="px-1.5 py-0.5 bg-slate-800 rounded text-indigo-300 font-mono">
                      GET /api/v1/auth/mezon/authorize
                    </code>{' '}
                    sinh URL ủy quyền kèm state 11 ký tự chống CSRF, sau đó chuyển hướng tới Mezon SSO.
                  </p>

                  <div className="space-y-2 pt-1">
                    <div className="flex items-center justify-between">
                      <label className="text-xs text-slate-400 font-semibold">
                        Redirect URI (phải khớp với cổng Mezon Developer Portal):
                      </label>
                      <div className="flex gap-1.5 text-[10px]">
                        <button
                          type="button"
                          onClick={() => setOauthRedirectUri('http://localhost:3000/oauth/mezon/callback')}
                          className="px-2 py-0.5 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded border border-slate-700"
                        >
                          Localhost
                        </button>
                        <button
                          type="button"
                          onClick={() => setOauthRedirectUri('https://right-thoroughly-amoeba.ngrok-free.app/oauth/mezon/callback')}
                          className="px-2 py-0.5 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded border border-slate-700"
                        >
                          Ngrok mẫu
                        </button>
                      </div>
                    </div>
                    <input
                      type="text"
                      value={oauthRedirectUri}
                      onChange={(e) => setOauthRedirectUri(e.target.value)}
                      className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-xs font-mono text-slate-200 focus:outline-none focus:border-indigo-500"
                      placeholder="https://your-domain/oauth/mezon/callback"
                    />
                  </div>

                  <button
                    onClick={handleStartOAuth}
                    disabled={actionLoading !== null || loading}
                    className="w-full inline-flex items-center justify-center gap-2 px-5 py-3.5 bg-gradient-to-r from-indigo-600 to-violet-600 hover:from-indigo-500 hover:to-violet-500 text-white rounded-xl text-sm font-bold transition-all shadow-xl shadow-indigo-600/25 disabled:opacity-50"
                  >
                    {actionLoading === 'oauth' ? (
                      <RefreshCw className="w-4 h-4 animate-spin" />
                    ) : (
                      <LogIn className="w-4 h-4" />
                    )}
                    Đăng nhập bằng Mezon OAuth 2.0
                    <ExternalLink className="w-4 h-4 ml-1 opacity-70" />
                  </button>
                </div>

                {/* 2. Fast Dev Handshake Login */}
                <div className="p-6 bg-slate-900/90 border border-slate-800 rounded-2xl shadow-xl space-y-4">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 rounded-lg bg-emerald-500/20 text-emerald-400 flex items-center justify-center">
                      <UserCheck className="w-5 h-5" />
                    </div>
                    <div>
                      <h2 className="text-lg font-bold text-white">
                        Luồng Thử nghiệm: Dev SSO Handshake
                      </h2>
                      <p className="text-xs text-slate-400">
                        Kiểm tra nhanh đăng nhập / tự động tạo người dùng không cần mở Mezon web
                      </p>
                    </div>
                  </div>

                  {/* Preset quick buttons */}
                  <div className="flex gap-2">
                    <button
                      type="button"
                      onClick={() => {
                        setMockUserId('2036383630950928384');
                        setMockDisplayName('Học viên Nguyễn Văn A');
                      }}
                      className="px-3 py-1 bg-slate-800 hover:bg-slate-700 text-xs text-slate-300 rounded-lg border border-slate-700 transition-colors"
                    >
                      Học viên mẫu
                    </button>
                    <button
                      type="button"
                      onClick={() => {
                        setMockUserId('1840677019319275520');
                        setMockDisplayName('Mentor Trần Thị B');
                      }}
                      className="px-3 py-1 bg-slate-800 hover:bg-slate-700 text-xs text-slate-300 rounded-lg border border-slate-700 transition-colors"
                    >
                      Mentor mẫu
                    </button>
                  </div>

                  <form onSubmit={handleDevLogin} className="space-y-3">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                      <div>
                        <label className="text-xs text-slate-400 block mb-1">Mezon User ID</label>
                        <input
                          type="text"
                          value={mockUserId}
                          onChange={(e) => setMockUserId(e.target.value)}
                          className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-xs text-slate-200 focus:outline-none focus:border-indigo-500"
                          required
                        />
                      </div>
                      <div>
                        <label className="text-xs text-slate-400 block mb-1">Tên hiển thị</label>
                        <input
                          type="text"
                          value={mockDisplayName}
                          onChange={(e) => setMockDisplayName(e.target.value)}
                          className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-xs text-slate-200 focus:outline-none focus:border-indigo-500"
                          required
                        />
                      </div>
                    </div>

                    <div>
                      <label className="text-xs text-slate-400 block mb-1">
                        Authorization Code / Test Token
                      </label>
                      <input
                        type="text"
                        value={mockTokenCode}
                        onChange={(e) => setMockTokenCode(e.target.value)}
                        className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-xs font-mono text-slate-200 focus:outline-none focus:border-indigo-500"
                        required
                      />
                    </div>

                    <button
                      type="submit"
                      disabled={actionLoading !== null}
                      className="w-full inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-500 text-white rounded-xl text-sm font-semibold transition-all shadow-lg shadow-emerald-600/20 disabled:opacity-50"
                    >
                      {actionLoading === 'dev-login' ? (
                        <RefreshCw className="w-4 h-4 animate-spin" />
                      ) : (
                        <LogIn className="w-4 h-4" />
                      )}
                      Gửi yêu cầu đăng nhập trực tiếp (POST /auth/mezon)
                    </button>
                  </form>
                </div>
              </div>
            )}

            {/* Test Token Revocation Box (Visible after logout or if revoked token available) */}
            {lastRevokedToken && (
              <div className="p-5 bg-amber-950/20 border border-amber-500/40 rounded-2xl shadow-xl space-y-3">
                <div className="flex items-center gap-2 text-amber-400 font-semibold text-sm">
                  <AlertTriangle className="w-4 h-4" />
                  Xác minh Thu hồi Token (Server Revocation)
                </div>
                <p className="text-xs text-slate-300">
                  Token vừa đăng xuất đã được lưu vào bộ nhớ tạm. Hãy thử gửi request đến endpoint bảo mật
                  với token này để kiểm chứng backend từ chối với{' '}
                  <span className="font-semibold text-rose-400">401 Unauthorized</span>.
                </p>

                <div className="p-2 bg-slate-950 rounded font-mono text-[11px] text-amber-200/80 break-all">
                  {lastRevokedToken.substring(0, 40)}...{lastRevokedToken.slice(-20)}
                </div>

                <button
                  onClick={handleTestRevokedToken}
                  disabled={actionLoading !== null}
                  className="w-full inline-flex items-center justify-center gap-2 px-4 py-2 bg-amber-600 hover:bg-amber-500 text-slate-950 rounded-xl text-xs font-bold transition-all disabled:opacity-50"
                >
                  {actionLoading === 'verify-revoked' ? (
                    <RefreshCw className="w-3.5 h-3.5 animate-spin" />
                  ) : (
                    <ShieldCheck className="w-3.5 h-3.5" />
                  )}
                  Thử nghiệm gọi API với token đã thu hồi (Mong đợi 401)
                </button>
              </div>
            )}
          </div>

          {/* Right Column: Live API Log & Diagnostic Console (5 cols) */}
          <div className="lg:col-span-5 space-y-4">
            <div className="p-6 bg-slate-900/90 border border-slate-800 rounded-2xl shadow-xl flex flex-col h-full min-h-[500px]">
              <div className="flex items-center justify-between border-b border-slate-800 pb-3 mb-4">
                <div className="flex items-center gap-2">
                  <Terminal className="w-4 h-4 text-indigo-400" />
                  <h3 className="text-sm font-bold text-white uppercase tracking-wider">
                    Nhật ký Yêu cầu API
                  </h3>
                </div>
                {logs.length > 0 && (
                  <button
                    onClick={() => setLogs([])}
                    className="text-xs text-slate-400 hover:text-slate-200 transition-colors"
                  >
                    Xóa nhật ký
                  </button>
                )}
              </div>

              {logs.length === 0 ? (
                <div className="flex-1 flex flex-col items-center justify-center text-center text-slate-500 text-xs py-12">
                  <Terminal className="w-10 h-10 mb-2 opacity-30" />
                  Chưa có cuộc gọi API nào được thực hiện.
                  <br />
                  Hãy bấm vào các nút bên trái để kiểm thử.
                </div>
              ) : (
                <div className="space-y-3 overflow-y-auto max-h-[600px] pr-1">
                  {logs.map((log) => {
                    const isSuccess =
                      typeof log.status === 'number' && log.status >= 200 && log.status < 300;
                    const isExpected401 =
                      log.status === 401 && log.endpoint.includes('[WITH REVOKED TOKEN]');

                    return (
                      <div
                        key={log.id}
                        className={`p-3 rounded-xl border text-xs font-mono space-y-2 ${
                          isExpected401
                            ? 'bg-amber-950/20 border-amber-500/40 text-amber-200'
                            : isSuccess
                            ? 'bg-slate-950/70 border-emerald-500/30 text-slate-200'
                            : 'bg-rose-950/20 border-rose-500/30 text-rose-200'
                        }`}
                      >
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-2">
                            <span
                              className={`px-1.5 py-0.5 rounded font-bold text-[10px] ${
                                log.method === 'GET'
                                  ? 'bg-blue-500/20 text-blue-400'
                                  : 'bg-emerald-500/20 text-emerald-400'
                              }`}
                            >
                              {log.method}
                            </span>
                            <span className="font-semibold text-[11px] truncate max-w-[180px]">
                              {log.endpoint}
                            </span>
                          </div>

                          <div className="flex items-center gap-1.5">
                            <span
                              className={`px-1.5 py-0.2 rounded font-bold text-[10px] ${
                                isExpected401
                                  ? 'bg-amber-500/20 text-amber-300'
                                  : isSuccess
                                  ? 'bg-emerald-500/20 text-emerald-400'
                                  : 'bg-rose-500/20 text-rose-400'
                              }`}
                            >
                              {log.status}
                            </span>
                            <span className="text-[10px] text-slate-500">{log.durationMs}ms</span>
                          </div>
                        </div>

                        {log.error ? (
                          <div className="text-rose-400 text-[11px] bg-rose-950/40 p-2 rounded border border-rose-900/50">
                            {log.error}
                          </div>
                        ) : null}

                        {log.responsePayload !== undefined && log.responsePayload !== null ? (
                          <div className="p-2 bg-slate-950 rounded text-[10px] text-slate-300 overflow-x-auto max-h-36">
                            <pre>{JSON.stringify(log.responsePayload, null, 2)}</pre>
                          </div>
                        ) : null}

                        <div className="text-[10px] text-slate-500 text-right">{log.timestamp}</div>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
