'use client';

import { useEffect, useRef, useState, useCallback } from 'react';

const termLines = [
  { text: '<span class="text-gray-500">$</span> <span class="text-white">./adb-installer</span>', delay: 400 },
  { text: '<span class="text-gray-600">ADB Driver Installer</span>', delay: 300 },
  { text: '<span class="text-gray-600">Language detected: English</span>', delay: 200 },
  { text: '', delay: 250 },
  { text: '<span class="text-gray-500">Detecting platform...</span>', delay: 350 },
  { text: '  OS: <span class="text-sky-400">Windows</span>  Arch: <span class="text-sky-400">x64</span>', delay: 300 },
  { text: '', delay: 250 },
  { text: '<span class="text-gray-500">Installing...</span>', delay: 400 },
  { text: '<span class="text-emerald-400">\u2713</span> Downloading Platform Tools <span class="text-gray-600">100%</span>', delay: 600 },
  { text: '<span class="text-emerald-400">\u2713</span> Extracting files', delay: 400 },
  { text: '<span class="text-emerald-400">\u2713</span> Configuring PATH', delay: 350 },
  { text: '<span class="text-emerald-400">\u2713</span> Verifying installation', delay: 350 },
  { text: '', delay: 300 },
  { text: '<span class="text-emerald-400 font-medium">\u2713 Installation complete</span>', delay: 500 },
  { text: '  ADB <span class="text-emerald-400">35.0.2</span>  Fastboot <span class="text-emerald-400">35.0.2</span>', delay: 200 },
];

export default function TerminalAnimation() {
  const containerRef = useRef<HTMLDivElement>(null);
  const [started, setStarted] = useState(false);

  const runAnimation = useCallback(() => {
    const container = containerRef.current;
    if (!container || started) return;
    setStarted(true);
    container.innerHTML = '';

    const prefersReduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    if (prefersReduced) {
      termLines.forEach((line) => {
        const div = document.createElement('div');
        div.className = 'term-line whitespace-pre';
        div.innerHTML = line.text === '' ? '&nbsp;' : line.text;
        container.appendChild(div);
      });
      return;
    }

    let cumDelay = 0;
    termLines.forEach((line, i) => {
      cumDelay += line.delay;
      setTimeout(() => {
        const div = document.createElement('div');
        div.className = 'term-line whitespace-pre';
        div.innerHTML = line.text === '' ? '&nbsp;' : line.text;

        const prev = container.querySelector('.term-cursor');
        if (prev) prev.remove();

        if (i < termLines.length - 1) {
          const cursor = document.createElement('span');
          cursor.className = 'term-cursor';
          div.appendChild(cursor);
        }
        container.appendChild(div);
      }, cumDelay);
    });
  }, [started]);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting) {
          runAnimation();
          observer.disconnect();
        }
      },
      { threshold: 0.3 },
    );
    observer.observe(container);
    return () => observer.disconnect();
  }, [runAnimation]);

  return (
    <div className="rounded-xl overflow-hidden bg-[#0d1117] ring-1 ring-white/[0.06] shadow-xl">
      <div className="flex items-center gap-1.5 px-4 py-2.5 border-b border-white/[0.06]">
        <span className="h-2.5 w-2.5 rounded-full bg-[#ff5f56]" />
        <span className="h-2.5 w-2.5 rounded-full bg-[#ffbd2e]" />
        <span className="h-2.5 w-2.5 rounded-full bg-[#27c93f]" />
        <span className="ml-2 text-[11px] text-gray-500 font-mono">terminal</span>
      </div>
      <div
        ref={containerRef}
        className="px-5 pb-5 pt-3 font-mono text-[12px] sm:text-[13px] leading-[1.8] text-gray-300 min-h-72"
      />
    </div>
  );
}
