import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'Global Firefight: Drone Command',
  metadataBase: new URL(process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000'),
  description: 'Dual-mode RTS and tactical firefighting simulation on a live NASA FIRMS globe (God Eye fork).',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
