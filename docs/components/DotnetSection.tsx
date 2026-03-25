'use client';

import { useTranslation } from '@/lib/i18n-context';
import MicrosoftButton from './MicrosoftButton';

export default function DotnetSection() {
  const { t } = useTranslation();

  return (
    <section id="dotnet" className="py-4">
      <div className="max-w-5xl mx-auto px-5 sm:px-6">
        <div className="relative overflow-hidden rounded-2xl border border-gray-100 bg-linear-to-br from-white to-gray-50 p-8 sm:p-10">
          <div className="flex flex-col sm:flex-row items-center gap-6 sm:gap-10">
            <div className="flex-1 text-center sm:text-left">
              <h3 className="text-lg font-bold text-gray-900 mb-1.5">
                {t('dotnetTitle')}
              </h3>
              <p
                className="text-sm text-gray-500 leading-relaxed max-w-lg"
                dangerouslySetInnerHTML={{ __html: t('dotnetDesc') }}
              />
            </div>
            <div className="shrink-0">
              <MicrosoftButton label={t('dotnetBtnTop')} />
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
