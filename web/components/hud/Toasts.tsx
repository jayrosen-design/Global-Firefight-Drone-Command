'use client';
import { useEffect, useRef, useState } from 'react';
import { useGame } from '@/store/gameStore';

/** Brief centre-screen banner for wins (fire out), so progress is felt, not just logged. */
export function Toasts() {
  const latest = useGame((s) => s.log[0]);
  const seen = useRef(latest);
  const [toast, setToast] = useState<{ key: number; title: string; sub: string } | null>(null);
  const timer = useRef<number>();
  useEffect(() => () => window.clearTimeout(timer.current), []);

  useEffect(() => {
    if (!latest || latest === seen.current) return;
    seen.current = latest;
    if (latest.kind !== 'success') return;
    const m = latest.text.match(/^(.*) extinguished\. (.*)$/);
    const title = m ? 'FIRE OUT' : latest.text.split(' — ')[0];
    const sub = m ? `${m[1]} · ${m[2]}` : latest.text.split(' — ')[1] ?? '';
    setToast({ key: Date.now(), title, sub });
    window.clearTimeout(timer.current);
    timer.current = window.setTimeout(() => setToast(null), 2600);
  }, [latest]);

  if (!toast) return null;
  return (
    <div key={toast.key} className="toast">
      {toast.title}
      {toast.sub && <span className="toast__sub">{toast.sub}</span>}
    </div>
  );
}
