'use client';

import {
  createContext,
  startTransition,
  useCallback,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from 'react';
import {
  translations,
  htmlKeys,
  langLabels,
  type Lang,
  type TranslationKey,
} from './translations';

interface I18nContextValue {
  lang: Lang;
  setLang: (lang: Lang) => void;
  t: (key: TranslationKey) => string;
  isHtmlKey: (key: TranslationKey) => boolean;
}

const I18nContext = createContext<I18nContextValue | null>(null);

function detectBrowserLang(): Lang {
  if (typeof navigator === 'undefined') return 'en';
  const bl = (navigator.language || '').toLowerCase();
  if (bl.startsWith('es')) return 'es';
  if (bl.startsWith('pt')) return 'pt';
  if (bl.startsWith('ru')) return 'ru';
  if (bl.startsWith('zh')) return 'zh';
  return 'en';
}

export function I18nProvider({ children }: { children: ReactNode }) {
  const [lang, setLangState] = useState<Lang>('en');

  useEffect(() => {
    let saved: string | null = null;
    try {
      saved = localStorage.getItem('adb-lang');
    } catch {}
    const detected = (saved && saved in langLabels ? saved : detectBrowserLang()) as Lang;
    document.documentElement.lang = detected;
    startTransition(() => setLangState(detected));
  }, []);

  const setLang = useCallback((newLang: Lang) => {
    setLangState(newLang);
    document.documentElement.lang = newLang;
    try {
      localStorage.setItem('adb-lang', newLang);
    } catch {}
  }, []);

  const t = useCallback(
    (key: TranslationKey) => translations[lang]?.[key] ?? translations.en[key] ?? key,
    [lang],
  );

  const isHtmlKey = useCallback((key: TranslationKey) => htmlKeys.has(key), []);

  return (
    <I18nContext.Provider value={{ lang, setLang, t, isHtmlKey }}>
      {children}
    </I18nContext.Provider>
  );
}

export function useTranslation() {
  const ctx = useContext(I18nContext);
  if (!ctx) throw new Error('useTranslation must be used within I18nProvider');
  return ctx;
}
