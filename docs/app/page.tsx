import Navbar from '@/components/Navbar';
import Hero from '@/components/Hero';
import QuickStart from '@/components/QuickStart';
import CommandsReference from '@/components/CommandsReference';
import DotnetSection from '@/components/DotnetSection';
import DownloadSection from '@/components/DownloadSection';
import Footer from '@/components/Footer';

export default function Home() {
  return (
    <>
      <Navbar />
      <Hero />
      <QuickStart />
      <CommandsReference />
      <DotnetSection />
      <DownloadSection />
      <Footer />
    </>
  );
}
