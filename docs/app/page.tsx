import Navbar from '@/components/Navbar';
import Hero from '@/components/Hero';
import QuickStart from '@/components/QuickStart';
import DotnetSection from '@/components/DotnetSection';
import DownloadSection from '@/components/DownloadSection';
import Footer from '@/components/Footer';

export default function Home() {
  return (
    <>
      <Navbar />
      <main id="main">
        <Hero />
        <QuickStart />
        <DotnetSection />
        <DownloadSection />
      </main>
      <Footer />
    </>
  );
}
