'use client';

import { useTranslation } from '@/lib/i18n-context';
import type { TranslationKey } from '@/lib/translations';

const commands: { cmd: string; descKey: TranslationKey }[] = [
  { cmd: 'adb-installer install', descKey: 'cmdInstall' },
  { cmd: 'adb-installer install --silent', descKey: 'cmdSilent' },
  { cmd: 'adb-installer verify', descKey: 'cmdVerify' },
  { cmd: 'adb-installer update', descKey: 'cmdUpdate' },
  { cmd: 'adb-installer uninstall', descKey: 'cmdUninstall' },
  { cmd: 'adb-installer --version', descKey: 'cmdVersion' },
];

export default function CommandsReference() {
  const { t } = useTranslation();

  return (
    <div className="max-w-xl mx-auto px-2">
      <h3 className="text-lg font-bold text-gray-900 text-center mb-4">
        {t('allCommands')}
      </h3>
      <div className="space-y-2">
        {commands.map(({ cmd, descKey }) => (
          <div
            key={cmd}
            className="flex items-center justify-between gap-4 px-4 py-3 bg-white border border-gray-100 rounded-xl hover:border-accent-200 transition"
          >
            <code className="bg-accent-50 text-accent-500 font-semibold text-xs px-2 py-1 rounded-md font-mono whitespace-nowrap">
              {cmd}
            </code>
            <span className="text-gray-400 text-xs text-right">{t(descKey)}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
