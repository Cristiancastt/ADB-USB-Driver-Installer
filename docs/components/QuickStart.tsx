'use client';

import { useTranslation } from '@/lib/i18n-context';
import CodeTabs from './CodeTabs';
import CopyButton from './CopyButton';
import CommandsReference from './CommandsReference';

function StepNumber({ n }: { n: number }) {
  return (
    <span
      className="shrink-0 w-8 h-8 rounded-full bg-gray-900 text-white text-xs font-semibold flex items-center justify-center"
      aria-hidden="true"
    >
      {n}
    </span>
  );
}

export default function QuickStart() {
  const { t } = useTranslation();

  const winTab = (
    <div className="group relative">
      <code className="text-[0.8125rem] flex items-center bg-gray-900 text-gray-100 rounded-lg p-4 pl-5 font-mono w-full">
        <span className="flex gap-3 flex-1 items-center">
          <span className="text-gray-500 select-none">&gt;</span>
          <span>adb-installer.exe</span>
        </span>
        <CopyButton text="adb-installer.exe" />
      </code>
    </div>
  );

  const linuxTab = (
    <div className="group relative">
      <code className="text-[0.8125rem] flex items-start bg-gray-900 text-gray-100 rounded-lg p-4 pl-5 font-mono w-full">
        <span className="flex gap-3 flex-1 flex-col">
          <span className="flex gap-3">
            <span className="text-gray-500 select-none">$</span>
            <span>chmod +x adb-installer</span>
          </span>
          <span className="flex gap-3">
            <span className="text-gray-500 select-none">$</span>
            <span>./adb-installer</span>
          </span>
        </span>
        <CopyButton text="chmod +x adb-installer && ./adb-installer" className="self-start" />
      </code>
    </div>
  );

  return (
    <section id="quickstart" className="py-16 sm:py-20 lg:py-24 bg-surface">
      <div className="max-w-6xl mx-auto px-5 sm:px-8">
        {/* Section header */}
        <div className="text-center mb-14">
          <h2 className="text-2xl sm:text-3xl font-bold text-gray-950 tracking-tight mb-3">
            {t('quickStartTitle')}
          </h2>
          <p
            className="text-gray-500 text-[0.9375rem]"
            dangerouslySetInnerHTML={{ __html: t('quickStartSub') }}
          />
        </div>

        {/* Two-column: Steps left, Commands right */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-10 lg:gap-16">
          {/* Left: Steps */}
          <div className="space-y-8">
            {/* Step 1 */}
            <div className="flex gap-4">
              <StepNumber n={1} />
              <div>
                <h3 className="font-semibold text-gray-900 text-[0.9375rem] mb-1">{t('step1Title')}</h3>
                <p
                  className="text-gray-500 text-sm leading-relaxed"
                  dangerouslySetInnerHTML={{ __html: t('step1Desc') }}
                />
              </div>
            </div>

            {/* Step 2 */}
            <div className="flex gap-4">
              <StepNumber n={2} />
              <div className="flex-1 min-w-0">
                <h3 className="font-semibold text-gray-900 text-[0.9375rem] mb-2">{t('step2Title')}</h3>
                <CodeTabs
                  tabs={[
                    { id: 'win', label: 'Windows', content: winTab },
                    { id: 'linux', label: 'Linux / macOS', content: linuxTab },
                  ]}
                />
              </div>
            </div>

            {/* Step 3 */}
            <div className="flex gap-4">
              <StepNumber n={3} />
              <div>
                <h3 className="font-semibold text-gray-900 text-[0.9375rem] mb-1">{t('step3Title')}</h3>
                <p className="text-gray-500 text-sm leading-relaxed">{t('step3Desc')}</p>
              </div>
            </div>

            {/* Step 4 */}
            <div className="flex gap-4">
              <StepNumber n={4} />
              <div className="flex-1 min-w-0">
                <h3 className="font-semibold text-gray-900 text-[0.9375rem] mb-2">{t('step4Title')}</h3>
                <div className="group relative">
                  <code className="text-[0.8125rem] flex items-start bg-gray-900 text-gray-100 rounded-lg p-4 pl-5 font-mono w-full">
                    <span className="flex gap-3 flex-1 flex-col leading-relaxed">
                      <span className="flex gap-3">
                        <span className="text-gray-500 select-none">$</span>
                        <span>adb-installer verify</span>
                      </span>
                      <span className="text-emerald-400">&#10003; ADB &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;35.0.2</span>
                      <span className="text-emerald-400">&#10003; Fastboot &nbsp;35.0.2</span>
                      <span className="text-emerald-400">&#10003; In PATH &nbsp;&nbsp;Accessible from any terminal</span>
                    </span>
                    <CopyButton text="adb-installer verify" />
                  </code>
                </div>
              </div>
            </div>
          </div>

          {/* Right: Commands reference */}
          <div className="lg:pt-1">
            <CommandsReference />
          </div>
        </div>
      </div>
    </section>
  );
}
