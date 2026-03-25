'use client';

import { useState, type ReactNode } from 'react';

interface Tab {
  id: string;
  label: string;
  content: ReactNode;
}

export default function CodeTabs({ tabs }: { tabs: Tab[] }) {
  const [active, setActive] = useState(tabs[0]?.id ?? '');

  return (
    <div>
      <div className="inline-flex bg-gray-100 rounded-lg p-0.5 mb-3">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActive(tab.id)}
            className={`tab-btn px-4 py-1.5 text-xs font-semibold rounded-md cursor-pointer transition-all ${
              active === tab.id ? 'active' : ''
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>
      {tabs.map((tab) => (
        <div key={tab.id} style={{ display: active === tab.id ? '' : 'none' }}>
          {tab.content}
        </div>
      ))}
    </div>
  );
}
