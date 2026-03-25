'use client';

import { useEffect, useState } from 'react';
import { useTranslation } from '@/lib/i18n-context';
import LanguageSwitcher from './LanguageSwitcher';
import { GitHubIcon } from './icons/GitHubIcon';

export default function Navbar() {
  const { t } = useTranslation();
  const [scrolled, setScrolled] = useState(false);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 8);
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  return (
    <nav
      role="navigation"
      aria-label="Main navigation"
      className={`fixed top-0 inset-x-0 z-50 bg-white/90 backdrop-blur-lg border-b transition-colors ${
        scrolled ? 'border-gray-200/80' : 'border-transparent'
      }`}
    >
      <div className="max-w-6xl mx-auto px-5 sm:px-8 flex items-center justify-between h-14">
        <a
          href="#top"
          className="flex items-center gap-2 font-semibold text-gray-900 text-[0.9375rem] tracking-tight"
        >
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={`${process.env.NEXT_PUBLIC_BASE_PATH ?? ''}/favicon.svg`}
            alt="ADB Driver Installer logo"
            width={24}
            height={24}
            className="rounded"
          />
          <span className="hidden sm:inline">ADB Driver Installer</span>
        </a>

        <div className="flex items-center gap-2 sm:gap-4">
          <a
            href="#quickstart"
            className="hidden sm:inline-block text-[0.8125rem] text-gray-500 hover:text-gray-900 transition-colors font-medium py-1.5 px-2"
          >
            {t('navQuickStart')}
          </a>
          <a
            href="#download"
            className="hidden sm:inline-block text-[0.8125rem] text-gray-500 hover:text-gray-900 transition-colors font-medium py-1.5 px-2"
          >
            {t('navDownload')}
          </a>

          <LanguageSwitcher />

          <a
            href="https://github.com/Cristiancastt/ADB-USB-Driver-Installer"
            className="inline-flex items-center gap-1.5 h-8 px-3 rounded-lg border border-gray-200 text-xs font-medium text-gray-600 hover:border-gray-300 hover:text-gray-900 transition-colors"
            target="_blank"
            rel="noopener noreferrer"
            aria-label="View on GitHub"
          >
            <GitHubIcon />
            <span className="hidden sm:inline">GitHub</span>
          </a>
        </div>
      </div>
    </nav>
  );
}
