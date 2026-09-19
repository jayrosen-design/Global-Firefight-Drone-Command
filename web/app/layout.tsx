import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'Global Firefight: Drone Command',
  description: 'Dual-mode RTS and tactical firefighting simulation on a live NASA FIRMS globe (God Eye fork).',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
