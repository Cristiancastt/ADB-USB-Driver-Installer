export default function Footer() {
  const links = [
    { href: 'https://github.com/Cristiancastt/ADB-USB-Driver-Installer', label: 'GitHub' },
    { href: 'https://github.com/Cristiancastt/ADB-USB-Driver-Installer/releases', label: 'Releases' },
    { href: 'https://github.com/Cristiancastt/ADB-USB-Driver-Installer/issues', label: 'Issues' },
    { href: 'https://github.com/Cristiancastt/ADB-USB-Driver-Installer/blob/master/LICENSE', label: 'License' },
  ];

  return (
    <footer className="border-t border-gray-100 py-8" role="contentinfo">
      <div className="max-w-6xl mx-auto px-5 sm:px-8 flex flex-col sm:flex-row items-center justify-between gap-4 text-xs">
        <span className="text-gray-400">
          AGPL-3.0 &middot;{' '}
          <a
            href="https://github.com/Cristiancastt"
            className="text-gray-500 hover:text-gray-700 transition-colors"
            target="_blank"
            rel="noopener noreferrer"
          >
            Cristian Arana Castiñeiras
          </a>
        </span>

        <nav className="flex gap-5" aria-label="Footer">
          {links.map(({ href, label }) => (
            <a
              key={label}
              href={href}
              className="text-gray-400 hover:text-gray-600 transition-colors"
              target="_blank"
              rel="noopener noreferrer"
            >
              {label}
            </a>
          ))}
        </nav>
      </div>
    </footer>
  );
}
