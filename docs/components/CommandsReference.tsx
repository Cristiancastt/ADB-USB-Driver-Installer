'use client';

import { useTranslation } from '@/lib/i18n-context';
import type { TranslationKey } from '@/lib/translations';

const commands: { cmd: string; descKey: TranslationKey }[] = [
  { cmd: 'install', descKey: 'cmdInstall' },
  { cmd: 'install --silent', descKey: 'cmdSilent' },
  { cmd: 'verify', descKey: 'cmdVerify' },
  { cmd: 'update', descKey: 'cmdUpdate' },
  { cmd: 'uninstall', descKey: 'cmdUninstall' },
  { cmd: '--version', descKey: 'cmdVersion' },
];

export default function CommandsReference() {
  const { t } = useTranslation();

  return (
    <div>
      <h3 className="text-lg font-semibold text-gray-950 mb-5 tracking-tight">
        {t('allCommands')}
      </h3>
      <div className="space-y-2">
        {commands.map(({ cmd, descKey }) => (
          <div
            key={cmd}
            className="flex items-baseline justify-between gap-4 px-4 py-3 rounded-lg border border-gray-100 bg-white hover:border-gray-200 transition-colors"
          >
            <code className="text-[0.8125rem] font-mono font-medium text-gray-900 whitespace-nowrap">
              adb-installer {cmd}
            </code>
            <span className="text-gray-400 text-xs text-right leading-snug">{t(descKey)}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
