'use client';

import { useTranslation } from '@/lib/i18n-context';
import { WindowsIcon, LinuxIcon, MacOSIcon } from './icons/OsIcons';

const platforms = [
  { icon: <WindowsIcon />, name: 'Windows', detail: 'x64 \u00b7 .zip' },
  { icon: <LinuxIcon />, name: 'Linux', detail: 'x64 / ARM64 \u00b7 .tar.gz' },
  { icon: <MacOSIcon />, name: 'macOS', detail: 'Intel / Apple Silicon \u00b7 .tar.gz' },
];

export default function DownloadSection() {
  const { t } = useTranslation();

  return (
    <section id="download" className="py-16 sm:py-20 lg:py-24 bg-surface">
      <div className="max-w-5xl mx-auto px-5 sm:px-6">
        <h2 className="text-2xl sm:text-3xl font-bold text-center text-gray-900 tracking-tight mb-3">
          {t('downloadTitle')}
        </h2>
        <p className="text-center text-gray-500 text-base mb-12">
          {t('downloadSub')}
        </p>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 max-w-2xl mx-auto mb-8">
          {platforms.map(({ icon, name, detail }) => (
            <a
              key={name}
              href="https://github.com/Cristiancastt/ADB-USB-Driver-Installer/releases/latest"
              className="flex flex-col items-center gap-2.5 px-6 py-8 bg-white border border-gray-100 rounded-2xl text-center hover:border-accent-200 hover:bg-accent-50/30 hover:-translate-y-0.5 hover:shadow-lg hover:shadow-accent-500/5 transition-all group"
              target="_blank"
              rel="noopener noreferrer"
            >
              {icon}
              <strong className="text-gray-900 text-sm">{name}</strong>
              <span className="text-gray-400 text-xs">{detail}</span>
            </a>
          ))}
        </div>

        <p
          className="text-center text-xs text-gray-400"
          dangerouslySetInnerHTML={{ __html: t('downloadNote') }}
        />
      </div>
    </section>
  );
}
