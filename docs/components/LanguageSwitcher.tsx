'use client';

import { useEffect, useRef, useState, useCallback } from 'react';
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

  const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
    if (e.key === 'Escape') setOpen(false);
  }, []);

  return (
    <div className="relative" ref={ref} onKeyDown={handleKeyDown}>
      <button
        onClick={() => setOpen((v) => !v)}
        aria-label="Change language"
        aria-expanded={open}
        aria-haspopup="listbox"
        className="inline-flex items-center gap-1.5 h-8 px-3 border border-gray-200 rounded-lg bg-white text-xs font-medium text-gray-500 hover:border-gray-300 hover:text-gray-700 transition-colors cursor-pointer"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
          <circle cx="12" cy="12" r="10" />
          <line x1="2" y1="12" x2="22" y2="12" />
          <path d="M12 2a15.3 15.3 0 014 10 15.3 15.3 0 01-4 10 15.3 15.3 0 01-4-10 15.3 15.3 0 014-10z" />
        </svg>
        <span>{langLabels[lang]}</span>
        <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" aria-hidden="true">
          <polyline points="6 9 12 15 18 9" />
        </svg>
      </button>

      {open && (
        <div
          role="listbox"
          aria-label="Select language"
          className="absolute right-0 top-full mt-1.5 bg-white border border-gray-200 rounded-lg shadow-lg overflow-hidden min-w-[8rem] z-50"
        >
          {supportedLangs.map((l) => (
            <button
              key={l}
              role="option"
              aria-selected={l === lang}
              onClick={() => {
                setLang(l);
                setOpen(false);
              }}
              className={`block w-full text-left px-4 py-2.5 text-sm transition-colors cursor-pointer ${
                l === lang
                  ? 'text-gray-900 font-medium bg-gray-50'
                  : 'text-gray-500 hover:bg-gray-50 hover:text-gray-700'
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
