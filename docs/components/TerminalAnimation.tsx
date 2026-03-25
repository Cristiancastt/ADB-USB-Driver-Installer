'use client';

import { useEffect, useRef, useState, useCallback } from 'react';

const termLines = [
  { text: '<span class="text-blue-400">$</span> <span class="font-semibold text-white">./adb-installer</span>', delay: 400 },
  { text: '<span class="text-gray-600">\u2500\u2500\u2500 ADB/USB Latest Driver Installer \u2500\u2500\u2500</span>', delay: 300 },
  { text: '<span class="text-gray-600">  Android Debug Bridge &amp; Fastboot \u2014 Automatic Installer</span>', delay: 200 },
  { text: '<span class="text-gray-600">  Language detected: English</span>', delay: 200 },
  { text: '', delay: 300 },
  { text: '<span class="text-gray-600">\u2500\u2500\u2500 [1] Detecting platform \u2500\u2500\u2500</span>', delay: 400 },
  { text: '  OS: <span class="text-cyan-400">Windows</span>  Arch: <span class="text-cyan-400">x64</span>', delay: 300 },
  { text: '', delay: 300 },
  { text: '<span class="text-gray-600">\u2500\u2500\u2500 [6] Installing \u2500\u2500\u2500</span>', delay: 400 },
  { text: '<span class="text-green-400">&#10003;</span> Downloading Platform Tools <span class="text-gray-600">100%</span>', delay: 600 },
  { text: '<span class="text-green-400">&#10003;</span> Extracting files <span class="text-gray-600">100%</span>', delay: 400 },
  { text: '<span class="text-green-400">&#10003;</span> Configuring PATH <span class="text-gray-600">done</span>', delay: 350 },
  { text: '<span class="text-green-400">&#10003;</span> Verifying installation <span class="text-gray-600">done</span>', delay: 350 },
  { text: '', delay: 300 },
  { text: '<span class="text-green-400 font-semibold">&#10003; Installation Successful</span>', delay: 500 },
  { text: '  ADB: <span class="text-green-400">35.0.2</span>  Fastboot: <span class="text-green-400">35.0.2</span>', delay: 200 },
];

export default function TerminalAnimation() {
  const containerRef = useRef<HTMLDivElement>(null);
  const [started, setStarted] = useState(false);

  const runAnimation = useCallback(() => {
    const container = containerRef.current;
    if (!container || started) return;
    setStarted(true);
    container.innerHTML = '';

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
    <div className="bg-black rounded-xl overflow-hidden shadow-2xl shadow-black/20">
      <div className="flex items-center gap-2 px-4 py-3">
        <div className="h-3 w-3 rounded-full bg-red-500" />
        <div className="h-3 w-3 rounded-full bg-yellow-500" />
        <div className="h-3 w-3 rounded-full bg-green-500" />
        <span className="ml-2 text-[11px] text-gray-600 font-medium">adb-installer</span>
      </div>
      <div
        ref={containerRef}
        className="px-5 pb-5 font-mono text-[12px] sm:text-[13px] leading-[1.9] text-gray-300 min-h-80"
      />
    </div>
  );
}
