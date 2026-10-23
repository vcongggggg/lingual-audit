import Link from "next/link";
import { KeyRound, ArrowRight, ShieldCheck, Zap } from "lucide-react";

export default function Home() {
  return (
    <div className="flex flex-col flex-1 items-center justify-center p-6 bg-radial from-slate-900 via-slate-950 to-black text-slate-100">
      <main className="flex flex-col items-center max-w-2xl w-full text-center space-y-8 p-8 md:p-12 bg-slate-900/60 border border-slate-800 rounded-3xl shadow-2xl backdrop-blur-xl">
        <div className="w-16 h-16 rounded-2xl bg-gradient-to-tr from-indigo-600 to-violet-500 flex items-center justify-center shadow-xl shadow-indigo-500/30">
          <KeyRound className="w-8 h-8 text-white" />
        </div>

        <div className="space-y-3">
          <h1 className="text-3xl md:text-4xl font-extrabold tracking-tight text-white">
            Lingual Campus Studio
          </h1>
          <p className="text-base text-slate-400 max-w-lg mx-auto">
            Hệ thống xác thực người dùng tích hợp Mezon OAuth 2.0 SSO và cơ chế thu hồi token JWT an toàn.
          </p>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 w-full text-left text-xs">
          <div className="p-4 rounded-xl bg-slate-950/80 border border-slate-800 flex items-start gap-3">
            <Zap className="w-5 h-5 text-indigo-400 shrink-0 mt-0.5" />
            <div>
              <span className="font-bold text-white block">Mezon OAuth 2.0</span>
              <span className="text-slate-400">
                Ủy quyền một chạm, state chống CSRF, handshake trao đổi token tự động.
              </span>
            </div>
          </div>
          <div className="p-4 rounded-xl bg-slate-950/80 border border-slate-800 flex items-start gap-3">
            <ShieldCheck className="w-5 h-5 text-emerald-400 shrink-0 mt-0.5" />
            <div>
              <span className="font-bold text-white block">Token Revocation</span>
              <span className="text-slate-400">
                Thu hồi token phía server tức thời khi đăng xuất, đảm bảo an toàn tuyệt đối.
              </span>
            </div>
          </div>
        </div>

        <div className="pt-2 w-full sm:w-auto">
          <Link
            href="/auth"
            className="inline-flex items-center justify-center gap-3 px-8 py-4 bg-gradient-to-r from-indigo-600 to-violet-600 hover:from-indigo-500 hover:to-violet-500 text-white font-bold rounded-2xl text-base transition-all shadow-xl shadow-indigo-600/30 hover:shadow-indigo-600/50"
          >
            Mở Trang Kiểm thử Đăng nhập & Đăng xuất
            <ArrowRight className="w-5 h-5" />
          </Link>
        </div>
      </main>
    </div>
  );
}
