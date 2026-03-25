'use client';

import { useTranslation } from '@/lib/i18n-context';
import TerminalAnimation from './TerminalAnimation';

export default function Hero() {
  const { t } = useTranslation();

  return (
    <header id="top" className="pt-24 pb-12 sm:pt-32 sm:pb-16 lg:pt-36 lg:pb-20">
      <div className="max-w-6xl mx-auto px-5 sm:px-8 grid grid-cols-1 lg:grid-cols-2 gap-12 lg:gap-16 items-center">
        {/* Copy */}
        <div className="order-2 lg:order-1">
          <p className="text-[0.8125rem] font-medium text-accent-600 tracking-wide mb-4">
            {t('badge')}
          </p>
          <h1
            className="text-[2rem] sm:text-[2.5rem] lg:text-[2.75rem] font-bold leading-[1.15] tracking-tight text-gray-950 mb-5"
            dangerouslySetInnerHTML={{ __html: t('heroTitle') }}
          />
          <p className="text-base sm:text-lg text-gray-500 max-w-lg mb-8 leading-relaxed">
            {t('heroSub')}
          </p>

          <div className="flex flex-wrap gap-3">
            <a
              href="https://github.com/Cristiancastt/ADB-USB-Driver-Installer/releases/latest"
              className="inline-flex items-center gap-2 h-11 px-6 bg-gray-900 text-white font-medium rounded-lg text-sm hover:bg-gray-800 transition-colors"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4" />
                <polyline points="7 10 12 15 17 10" />
                <line x1="12" y1="15" x2="12" y2="3" />
              </svg>
              {t('downloadLatest')}
            </a>
            <a
              href="#quickstart"
              className="inline-flex items-center h-11 px-6 border border-gray-200 text-gray-700 font-medium rounded-lg text-sm hover:border-gray-300 hover:bg-gray-50 transition-colors"
            >
              {t('quickStartGuide')}
            </a>
          </div>

          <p className="mt-5 text-xs text-gray-400 tracking-wide">{t('heroNote')}</p>
        </div>

        {/* Terminal */}
        <div className="order-1 lg:order-2" aria-hidden="true">
          <TerminalAnimation />
        </div>
      </div>
    </header>
  );
}
