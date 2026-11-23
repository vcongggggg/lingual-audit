import type { Metadata } from "next";
import { Be_Vietnam_Pro, Geist_Mono } from "next/font/google";
import AppNavigation from "@/components/AppNavigation";
import SectionSubNav from "@/components/SectionSubNav";
import OnboardingTour from "@/components/OnboardingTour";
import "./globals.css";
import { AuthProvider } from "@/lib/auth-context";
import { LanguageProvider } from "@/lib/language-context";

const beVietnamPro = Be_Vietnam_Pro({
  variable: "--font-be-vietnam-pro",
  subsets: ["latin", "vietnamese"],
  weight: ["400", "500", "600", "700", "800", "900"],
  display: "swap",
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "LinguaMezon | Học từ vựng",
  description: "Không gian học từ vựng tiếng Anh cùng LinguaMezon.",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html
      lang="vi"
      className={`${beVietnamPro.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col bg-[var(--paper)] text-[var(--ink)]">
        <LanguageProvider>
          <AuthProvider>
            <AppNavigation />
            <SectionSubNav />
            <div className="app-route-content">{children}</div>
            <OnboardingTour />
          </AuthProvider>
        </LanguageProvider>
      </body>
    </html>
  );
}
