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
    <section id="download" className="py-16 sm:py-20 lg:py-24">
      <div className="max-w-6xl mx-auto px-5 sm:px-8">
        <div className="text-center mb-12">
          <h2 className="text-2xl sm:text-3xl font-bold text-gray-950 tracking-tight mb-3">
            {t('downloadTitle')}
          </h2>
          <p className="text-gray-500 text-[0.9375rem]">
            {t('downloadSub')}
          </p>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 max-w-2xl mx-auto mb-10">
          {platforms.map(({ icon, name, detail }) => (
            <a
              key={name}
              href="https://github.com/Cristiancastt/ADB-USB-Driver-Installer/releases/latest"
              className="flex flex-col items-center gap-3 px-6 py-8 bg-white border border-neutral-200 rounded-xl text-center hover:border-gray-300 hover:shadow-sm transition-all group"
              target="_blank"
              rel="noopener noreferrer"
              aria-label={`Download for ${name}`}
            >
              <span className="text-gray-400 group-hover:text-gray-600 transition-colors">{icon}</span>
              <strong className="text-gray-900 text-sm font-semibold">{name}</strong>
              <span className="text-gray-400 text-xs">{detail}</span>
            </a>
          ))}
        </div>

        <p
          className="text-center text-xs text-gray-400 leading-relaxed"
          dangerouslySetInnerHTML={{ __html: t('downloadNote') }}
        />
      </div>
    </section>
  );
}
