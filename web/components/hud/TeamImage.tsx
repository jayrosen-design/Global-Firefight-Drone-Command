'use client';
import { useEffect, useRef, useState } from 'react';

/**
 * Image that renders `fallback` while loading or when the file is missing. Used
 * for pitch-deck concept art and crew portraits so the screen degrades
 * gracefully without assets. Handles images that finish loading before React
 * attaches the load handler (cached / fast local files).
 */
export function TeamImage({ src, alt, className, fallback }: { src: string; alt: string; className?: string; fallback: React.ReactNode }) {
  const [state, setState] = useState<'loading' | 'ok' | 'missing'>('loading');
  const ref = useRef<HTMLImageElement>(null);
  useEffect(() => {
    setState('loading');
    const img = ref.current;
    if (img && img.complete) setState(img.naturalWidth > 0 ? 'ok' : 'missing');
  }, [src]);
  if (state === 'missing') return <>{fallback}</>;
  return (
    <>
      {state === 'loading' && fallback}
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img ref={ref} src={src} alt={alt} className={className} style={state === 'ok' ? undefined : { display: 'none' }} onLoad={() => setState('ok')} onError={() => setState('missing')} />
    </>
  );
}
