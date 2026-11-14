import Link from "next/link";
import { ArrowLeft, BookOpen } from "lucide-react";
import { useLanguage } from "@/lib/language-context";

export default function NotFound() {
  const { translate: t } = useLanguage();
  return (
    <main className="app-not-found">
      <span className="not-found-mark"><BookOpen aria-hidden="true" /></span>
      <p className="study-eyebrow">404 · {t("Không tìm thấy trang", "Page not found")}</p>
      <h1>{t("Trang này chưa có trong lộ trình.", "This page is not on your learning path.")}</h1>
      <p>{t("Hãy quay về tổng quan hoặc tiếp tục một phiên học từ vựng.", "Return to the overview or continue a vocabulary session.")}</p>
      <Link href="/" className="not-found-link"><ArrowLeft aria-hidden="true" /> {t("Về tổng quan", "Back to overview")}</Link>
    </main>
  );
}
