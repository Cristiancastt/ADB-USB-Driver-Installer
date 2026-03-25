'use client';

import { useTranslation } from '@/lib/i18n-context';
import TerminalAnimation from './TerminalAnimation';

export default function Hero() {
  const { t } = useTranslation();

  return (
    <header id="top" className="pt-24 pb-16 sm:pt-28 sm:pb-20 lg:pt-32 lg:pb-24">
      <div className="max-w-5xl mx-auto px-5 sm:px-6 grid grid-cols-1 lg:grid-cols-2 gap-10 lg:gap-14 items-center">
        <div className="order-2 lg:order-1">
          <span className="inline-block px-3.5 py-1 rounded-full text-xs font-semibold text-accent-500 bg-accent-50 border border-accent-200 mb-5 tracking-wide">
            {t('badge')}
          </span>
          <h1
            className="text-3xl sm:text-4xl lg:text-[2.75rem] font-bold leading-tight tracking-tight text-gray-900 mb-5"
            dangerouslySetInnerHTML={{ __html: t('heroTitle') }}
          />
          <p className="text-base sm:text-lg text-gray-500 max-w-md mb-7 leading-relaxed">
            {t('heroSub')}
          </p>
          <div className="flex flex-wrap gap-3">
            <a
              href="https://github.com/Cristiancastt/ADB-USB-Driver-Installer/releases/latest"
              className="inline-flex items-center gap-2 px-6 py-3 bg-linear-to-br from-accent-400 to-accent-500 text-white font-semibold rounded-xl shadow-md shadow-accent-500/20 hover:shadow-lg hover:shadow-accent-500/30 hover:-translate-y-0.5 transition-all text-sm"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4" />
                <polyline points="7 10 12 15 17 10" />
                <line x1="12" y1="15" x2="12" y2="3" />
              </svg>
              {t('downloadLatest')}
            </a>
            <a
              href="#quickstart"
              className="inline-flex items-center gap-2 px-6 py-3 border-[1.5px] border-gray-200 text-gray-600 font-semibold rounded-xl hover:border-accent-300 hover:text-accent-600 hover:bg-accent-50 transition-all text-sm"
            >
              {t('quickStartGuide')}
            </a>
          </div>
          <p className="mt-5 text-xs text-gray-400">{t('heroNote')}</p>
        </div>

        <div className="order-1 lg:order-2">
          <TerminalAnimation />
        </div>
      </div>
    </header>
  );
}
