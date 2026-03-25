import type { Metadata } from 'next';
import { Inter } from 'next/font/google';
import { Geist_Mono } from 'next/font/google';
import { I18nProvider } from '@/lib/i18n-context';
import './globals.css';

const inter = Inter({
  variable: '--font-sora',
  subsets: ['latin'],
  weight: ['400', '500', '600', '700'],
  display: 'swap',
});

const geistMono = Geist_Mono({
  variable: '--font-geist-mono',
  subsets: ['latin'],
  weight: ['400', '500'],
  display: 'swap',
});

const siteUrl = 'https://cristiancastt.github.io/ADB-USB-Driver-Installer/';
const ogImage = `${siteUrl}og-image.png`;

export const metadata: Metadata = {
  title: 'ADB/USB Latest Driver Installer — One-command ADB, Fastboot & USB driver setup',
  description:
    'Install ADB, Fastboot, and USB drivers on Windows, Linux, and macOS with a single command. Open-source, cross-platform, interactive wizard.',
  keywords: [
    'ADB',
    'Fastboot',
    'Android',
    'USB drivers',
    'platform-tools',
    'installer',
    'CLI',
    'Windows',
    'Linux',
    'macOS',
    'open source',
  ],
  authors: [{ name: 'Cristian Arana Castineiras' }],
  robots: 'index, follow',
  alternates: { canonical: siteUrl },
  openGraph: {
    type: 'website',
    title: 'ADB Driver Installer',
    description:
      'One command to set up ADB, Fastboot & USB drivers on any platform. No manual downloads, no PATH headaches.',
    url: siteUrl,
    siteName: 'ADB Driver Installer',
    images: [{ url: ogImage }],
  },
  twitter: {
    card: 'summary_large_image',
    title: 'ADB Driver Installer',
    description:
      'One command to set up ADB, Fastboot & USB drivers. Cross-platform, open source.',
    images: [ogImage],
  },
  icons: { icon: '/ADB-USB-Driver-Installer/favicon.svg' },
};

const jsonLd = {
  '@context': 'https://schema.org',
  '@type': 'SoftwareApplication',
  name: 'ADB Driver Installer',
  operatingSystem: 'Windows, Linux, macOS',
  applicationCategory: 'DeveloperApplication',
  description:
    'One command to install ADB, Fastboot, and USB drivers on any platform.',
  url: siteUrl,
  downloadUrl:
    'https://github.com/Cristiancastt/ADB-USB-Driver-Installer/releases/latest',
  softwareVersion: '2.0',
  license: 'https://www.gnu.org/licenses/agpl-3.0.html',
  author: { '@type': 'Person', name: 'Cristian Arana Castineiras' },
  offers: { '@type': 'Offer', price: '0', priceCurrency: 'USD' },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className={`${inter.variable} ${geistMono.variable} scroll-smooth`}>
      <head>
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
        />
      </head>
      <body className="bg-white text-gray-900 antialiased selection:bg-accent-100 selection:text-accent-700">
        <a href="#main" className="skip-link">Skip to content</a>
        <I18nProvider>{children}</I18nProvider>
      </body>
    </html>
  );
}
