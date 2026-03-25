'use client';

import { useEffect, useRef, useState } from 'react';
import { useTranslation } from '@/lib/i18n-context';
import { langLabels, langNames, type Lang } from '@/lib/translations';

const supportedLangs: Lang[] = ['en', 'es', 'pt', 'ru', 'zh'];

export default function LanguageSwitcher() {
  const { lang, setLang } = useTranslation();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener('click', handleClickOutside);
    return () => document.removeEventListener('click', handleClickOutside);
  }, []);

  return (
    <div className="relative" ref={ref}>
      <button
        onClick={() => setOpen((v) => !v)}
        aria-label="Change language"
        className="inline-flex items-center gap-1.5 px-3 py-1.5 border border-gray-200 rounded-lg bg-white text-xs font-semibold text-gray-500 hover:border-accent-300 hover:text-accent-600 hover:bg-accent-50 transition cursor-pointer"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <circle cx="12" cy="12" r="10" />
          <line x1="2" y1="12" x2="22" y2="12" />
          <path d="M12 2a15.3 15.3 0 014 10 15.3 15.3 0 01-4 10 15.3 15.3 0 01-4-10 15.3 15.3 0 014-10z" />
        </svg>
        <span>{langLabels[lang]}</span>
        <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3">
          <polyline points="6 9 12 15 18 9" />
        </svg>
      </button>

      {open && (
        <div className="absolute right-0 top-full mt-1.5 bg-white border border-gray-200 rounded-xl shadow-lg overflow-hidden min-w-30 z-50">
          {supportedLangs.map((l) => (
            <button
              key={l}
              onClick={() => {
                setLang(l);
                setOpen(false);
              }}
              className={`block w-full text-left px-4 py-2 text-sm transition cursor-pointer ${
                l === lang
                  ? 'text-accent-500 font-semibold bg-accent-50'
                  : 'text-gray-500 hover:bg-accent-50 hover:text-accent-600'
              }`}
            >
              {langNames[l]}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
