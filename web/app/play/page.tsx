import type { Metadata } from 'next';
import { CommandCenter } from '@/components/CommandCenter';

export const metadata: Metadata = {
  title: 'Play — Global Firefight: Drone Command',
};

export default function PlayPage() {
  return <CommandCenter />;
}
