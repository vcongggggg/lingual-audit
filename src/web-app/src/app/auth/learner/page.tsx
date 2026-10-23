'use client';

import Link from 'next/link';
import { ArrowLeft, GraduationCap } from 'lucide-react';

export default function LearnerAuthPage() {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex items-center justify-center p-4">
      <div className="max-w-md w-full p-6 bg-slate-900 border border-slate-800 rounded-2xl shadow-xl text-center space-y-4">
        <div className="w-12 h-12 bg-indigo-500/20 text-indigo-400 rounded-xl flex items-center justify-center mx-auto">
          <GraduationCap className="w-6 h-6" />
        </div>
        <h1 className="text-xl font-bold">Cổng Học Viên (Learner)</h1>
        <p className="text-xs text-slate-400">
          Vui lòng sử dụng trang kiểm thử xác thực Mezon OAuth để đăng nhập tài khoản học viên.
        </p>
        <Link
          href="/auth"
          className="inline-flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white rounded-lg text-xs font-semibold"
        >
          <ArrowLeft className="w-4 h-4" /> Về trang kiểm thử Mezon Auth
        </Link>
      </div>
    </div>
  );
}
