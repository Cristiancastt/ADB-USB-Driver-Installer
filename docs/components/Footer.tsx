export default function Footer() {
  return (
    <footer className="border-t border-gray-100 py-6">
      <div className="max-w-5xl mx-auto px-5 sm:px-6 flex flex-col sm:flex-row items-center justify-between gap-4 text-xs">
        <div className="flex items-center gap-2 text-gray-400 font-semibold">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={`${process.env.NEXT_PUBLIC_BASE_PATH ?? ''}/favicon.svg`}
            alt=""
            width={20}
            height={20}
            className="rounded"
          />
          ADB/USB Latest Driver Installer
        </div>
        <nav className="flex gap-5" aria-label="Footer links">
          <a href="https://github.com/Cristiancastt/ADB-USB-Driver-Installer" className="text-gray-400 hover:text-accent-600 transition" target="_blank" rel="noopener noreferrer">
            GitHub
          </a>
          <a href="https://github.com/Cristiancastt/ADB-USB-Driver-Installer/releases" className="text-gray-400 hover:text-accent-600 transition" target="_blank" rel="noopener noreferrer">
            Releases
          </a>
          <a href="https://github.com/Cristiancastt/ADB-USB-Driver-Installer/issues" className="text-gray-400 hover:text-accent-600 transition" target="_blank" rel="noopener noreferrer">
            Issues
          </a>
          <a href="https://github.com/Cristiancastt/ADB-USB-Driver-Installer/blob/main/LICENSE" className="text-gray-400 hover:text-accent-600 transition" target="_blank" rel="noopener noreferrer">
            License
          </a>
        </nav>
        <div className="text-gray-300">
          AGPL-3.0 &middot;{' '}
          <a href="https://github.com/Cristiancastt" className="text-gray-400 hover:text-accent-600 transition" target="_blank" rel="noopener noreferrer">
            Cristian Arana Castiñeiras
          </a>
        </div>
      </div>
    </footer>
  );
}
