'use client';

import Link from 'next/link';
import { ArrowLeft, Shield } from 'lucide-react';

export default function AdminAuthPage() {
  return (
    <div className="w-full flex-1 min-h-[calc(100svh-68px)] bg-[var(--paper,#f5f2eb)] text-[var(--ink,#252820)] flex items-center justify-center p-4">
      <div className="max-w-md w-full p-8 bg-[var(--surface,#fffefa)] border border-[var(--line,#e5e0d5)] rounded-2xl shadow-xl text-center space-y-4">
        <div className="w-12 h-12 bg-[#eff1e8] text-[var(--olive,#69734f)] rounded-2xl border border-[#d7ddca] flex items-center justify-center mx-auto">
          <Shield className="w-6 h-6" />
        </div>
        <h1 className="text-xl font-bold text-[var(--ink,#252820)]">Cổng Quản Trị Viên (Admin)</h1>
        <p className="text-xs text-[var(--muted,#77786f)]">
          Vui lòng sử dụng cổng đăng nhập Mezon SSO với tài khoản có quyền Quản trị viên.
        </p>
        <Link
          href="/auth"
          className="inline-flex items-center gap-2 px-5 py-2.5 bg-[var(--olive,#69734f)] hover:opacity-90 text-white rounded-xl text-xs font-semibold shadow-xs transition-all"
        >
          <ArrowLeft className="w-4 h-4" /> Về trang đăng nhập Mezon
        </Link>
      </div>
    </div>
  );
}
