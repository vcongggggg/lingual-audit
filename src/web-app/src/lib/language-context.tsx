"use client";

import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";

export type Language = "vi" | "en";

type LanguageDictionary = {
  navigationLabel: string;
  navigationAriaLabel: string;
  navigationNote: string;
  languageLabel: string;
  languageName: string;
};

const LANGUAGE_STORAGE_KEY = "linguamezon-language";

const DICTIONARY: Record<Language, LanguageDictionary> = {
  vi: {
    navigationLabel: "Điều hướng chính",
    navigationAriaLabel: "Chuyển sang tiếng Anh",
    navigationNote: "Học theo nhịp độ của bạn",
    languageLabel: "Ngôn ngữ",
    languageName: "Tiếng Việt",
  },
  en: {
    navigationLabel: "Main navigation",
    navigationAriaLabel: "Switch to Vietnamese",
    navigationNote: "Learn at your own pace",
    languageLabel: "Language",
    languageName: "English",
  },
};

type LanguageContextValue = {
  language: Language;
  setLanguage: (language: Language) => void;
  toggleLanguage: () => void;
  translate: (vietnamese: string, english: string) => string;
  dictionary: LanguageDictionary;
};

const LanguageContext = createContext<LanguageContextValue | null>(null);

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [language, setLanguageState] = useState<Language>("vi");

  useEffect(() => {
    const savedLanguage = window.localStorage.getItem(LANGUAGE_STORAGE_KEY);
    if (savedLanguage === "vi" || savedLanguage === "en") {
      setLanguageState(savedLanguage);
    }
  }, []);

  useEffect(() => {
    document.documentElement.lang = language;
    window.localStorage.setItem(LANGUAGE_STORAGE_KEY, language);
  }, [language]);

  const value = useMemo<LanguageContextValue>(() => ({
    language,
    setLanguage: setLanguageState,
    toggleLanguage: () => setLanguageState((current) => current === "vi" ? "en" : "vi"),
    translate: (vietnamese, english) => language === "vi" ? vietnamese : english,
    dictionary: DICTIONARY[language],
  }), [language]);

  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>;
}

export function useLanguage() {
  const context = useContext(LanguageContext);
  if (!context) {
    throw new Error("useLanguage must be used within a LanguageProvider");
  }
  return context;
}
