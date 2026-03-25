'use client';

import { useTranslation } from '@/lib/i18n-context';

export default function DotnetSection() {
  const { t } = useTranslation();

  return (
    <section id="dotnet" className="py-6">
      <div className="max-w-6xl mx-auto px-5 sm:px-8">
        <div className="rounded-xl border border-neutral-200 bg-gray-50/50 p-6 sm:p-8">
          <div className="flex flex-col sm:flex-row items-center gap-5 sm:gap-8">
            <div className="flex-1 text-center sm:text-left">
              <h3 className="text-[0.9375rem] font-semibold text-gray-900 mb-1">
                {t('dotnetTitle')}
              </h3>
              <p
                className="text-sm text-gray-500 leading-relaxed max-w-lg"
                dangerouslySetInnerHTML={{ __html: t('dotnetDesc') }}
              />
            </div>
            <a
              href="https://dotnet.microsoft.com/download/dotnet/10.0"
              target="_blank"
              rel="noopener noreferrer"
              className="shrink-0 inline-flex items-center gap-2 h-10 px-5 bg-gray-900 text-white text-sm font-medium rounded-lg hover:bg-gray-800 transition-colors"
            >
              <svg width="16" height="16" viewBox="0 0 23 23" aria-hidden="true">
                <rect x="1" y="1" width="10" height="10" fill="#f25022" />
                <rect x="12" y="1" width="10" height="10" fill="#7fba00" />
                <rect x="1" y="12" width="10" height="10" fill="#00a4ef" />
                <rect x="12" y="12" width="10" height="10" fill="#ffb900" />
              </svg>
              {t('dotnetBtnTop')} Microsoft
            </a>
          </div>
        </div>
      </div>
    </section>
  );
}
