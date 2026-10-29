import Link from "next/link";
import { ArrowLeft, BookOpen } from "lucide-react";

export default function NotFound() {
  return (
    <main className="app-not-found">
      <span className="not-found-mark"><BookOpen aria-hidden="true" /></span>
      <p className="study-eyebrow">404 · Không tìm thấy trang</p>
      <h1>Trang này chưa có trong lộ trình.</h1>
      <p>Hãy quay về tổng quan hoặc tiếp tục một phiên học từ vựng.</p>
      <Link href="/" className="not-found-link"><ArrowLeft aria-hidden="true" /> Về tổng quan</Link>
    </main>
  );
}
