'use client';

import { useTranslation } from '@/lib/i18n-context';
import CodeTabs from './CodeTabs';
import CopyButton from './CopyButton';

function StepNumber({ n }: { n: number }) {
  return (
    <div className="shrink-0 w-10 h-10 rounded-xl bg-accent-50 border border-accent-200 text-accent-500 font-bold text-sm flex items-center justify-center">
      {n}
    </div>
  );
}

export default function QuickStart() {
  const { t } = useTranslation();

  const winTab = (
    <div className="group relative">
      <code className="text-sm inline-flex text-left items-center space-x-4 bg-gray-800 text-white rounded-lg p-4 pl-6 w-full">
        <span className="flex gap-4 flex-1">
          <span className="shrink-0 text-gray-500">&gt;</span>
          <span className="flex-1">adb-installer.exe</span>
        </span>
        <CopyButton text="adb-installer.exe" />
      </code>
    </div>
  );

  const linuxTab = (
    <div className="group relative">
      <code className="text-sm inline-flex text-left items-center space-x-4 bg-gray-800 text-white rounded-lg p-4 pl-6 w-full">
        <span className="flex gap-4 flex-1 flex-col">
          <span className="flex gap-4">
            <span className="shrink-0 text-gray-500">$</span>
            <span>chmod +x adb-installer</span>
          </span>
          <span className="flex gap-4">
            <span className="shrink-0 text-gray-500">$</span>
            <span>./adb-installer</span>
          </span>
        </span>
        <CopyButton text="chmod +x adb-installer && ./adb-installer" className="self-start mt-0.5" />
      </code>
    </div>
  );

  return (
    <section id="quickstart" className="py-8">
      <div className="max-w-5xl mx-auto px-5 sm:px-6">
        <h2 className="text-2xl sm:text-3xl font-bold text-center text-gray-900 tracking-tight mb-3">
          {t('quickStartTitle')}
        </h2>
        <p
          className="text-center text-gray-500 text-base mb-12"
          dangerouslySetInnerHTML={{ __html: t('quickStartSub') }}
        />

        <div className="max-w-xl mx-auto space-y-8 mb-14">
          {/* Step 1 */}
          <div className="flex gap-5">
            <StepNumber n={1} />
            <div>
              <h3 className="font-bold text-gray-900 mb-1">{t('step1Title')}</h3>
              <p
                className="text-gray-500 text-sm leading-relaxed"
                dangerouslySetInnerHTML={{ __html: t('step1Desc') }}
              />
            </div>
          </div>

          {/* Step 2 */}
          <div className="flex gap-5">
            <StepNumber n={2} />
            <div className="flex-1 min-w-0">
              <h3 className="font-bold text-gray-900 mb-2">{t('step2Title')}</h3>
              <CodeTabs
                tabs={[
                  { id: 'win', label: 'Windows', content: winTab },
                  { id: 'linux', label: 'Linux / macOS', content: linuxTab },
                ]}
              />
            </div>
          </div>

          {/* Step 3 */}
          <div className="flex gap-5">
            <StepNumber n={3} />
            <div>
              <h3 className="font-bold text-gray-900 mb-1">{t('step3Title')}</h3>
              <p className="text-gray-500 text-sm leading-relaxed">{t('step3Desc')}</p>
            </div>
          </div>

          {/* Step 4 */}
          <div className="flex gap-5">
            <StepNumber n={4} />
            <div className="flex-1 min-w-0">
              <h3 className="font-bold text-gray-900 mb-2">{t('step4Title')}</h3>
              <div className="group relative">
                <code className="text-sm inline-flex text-left items-start space-x-4 bg-gray-800 text-white rounded-lg p-4 pl-6 w-full">
                  <span className="flex gap-4 flex-1 flex-col font-mono leading-relaxed">
                    <span className="flex gap-4">
                      <span className="shrink-0 text-gray-500">$</span>
                      <span>adb-installer verify</span>
                    </span>
                    <span className="text-green-400">&#10003; ADB &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;35.0.2</span>
                    <span className="text-green-400">&#10003; Fastboot &nbsp;35.0.2</span>
                    <span className="text-green-400">&#10003; In PATH &nbsp;&nbsp;Accessible from any terminal</span>
                  </span>
                  <CopyButton text="adb-installer verify" />
                </code>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
