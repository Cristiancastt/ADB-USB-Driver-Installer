'use client';

import { useEffect, useState } from 'react';
import { useTranslation } from '@/lib/i18n-context';
import LanguageSwitcher from './LanguageSwitcher';
import { GitHubIcon } from './icons/GitHubIcon';

export default function Navbar() {
  const { t } = useTranslation();
  const [shadow, setShadow] = useState(false);

  useEffect(() => {
    function onScroll() {
      setShadow(window.scrollY > 10);
    }
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  return (
    <nav
      className="fixed top-0 inset-x-0 z-50 bg-white/80 backdrop-blur-xl border-b border-gray-100 transition-shadow"
      style={{ boxShadow: shadow ? '0 1px 12px rgba(0,0,0,.06)' : 'none' }}
    >
      <div className="max-w-5xl mx-auto px-5 sm:px-6 flex items-center justify-between h-14">
        <a href="#top" className="flex items-center gap-2.5 font-bold text-gray-900 text-sm">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={`${process.env.NEXT_PUBLIC_BASE_PATH ?? ''}/favicon.svg`} alt="" width={26} height={26} className="rounded-md" />
          ADB/USB Latest Driver Installer
        </a>

        <div className="flex items-center gap-3 sm:gap-5">
          <a href="#quickstart" className="hidden sm:block text-sm text-gray-500 hover:text-gray-900 transition-colors font-medium">
            {t('navQuickStart')}
          </a>
          <a href="#download" className="hidden sm:block text-sm text-gray-500 hover:text-gray-900 transition-colors font-medium">
            {t('navDownload')}
          </a>

          <LanguageSwitcher />

          <a
            href="https://github.com/Cristiancastt/ADB-USB-Driver-Installer"
            className="inline-flex items-center gap-1.5 px-3.5 py-1.5 border border-gray-200 rounded-lg text-xs font-semibold text-gray-600 hover:border-accent-300 hover:text-accent-600 hover:bg-accent-50 transition"
            target="_blank"
            rel="noopener noreferrer"
          >
            <GitHubIcon />
            GitHub
          </a>
        </div>
      </div>
    </nav>
  );
}
