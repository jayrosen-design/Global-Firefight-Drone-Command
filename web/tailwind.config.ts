import type { Config } from 'tailwindcss';

const config: Config = {
  content: ['./app/**/*.{ts,tsx}', './components/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        ember: '#ff5a1f',
        cyan: { glow: '#5ef2ff' },
        panel: 'rgba(8, 14, 22, 0.72)',
      },
      fontFamily: {
        mono: ['"JetBrains Mono"', 'ui-monospace', 'SFMono-Regular', 'Menlo', 'monospace'],
      },
      boxShadow: {
        glass: '0 0 0 1px rgba(94,242,255,0.18), 0 12px 40px rgba(0,0,0,0.55)',
      },
    },
  },
  plugins: [],
};
export default config;
