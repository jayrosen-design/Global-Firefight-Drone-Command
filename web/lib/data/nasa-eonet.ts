/**
 * NASA EONET v3 — curated, named wildfire events (GeoJSON).
 * Proxied via /api/eonet so the browser gets a same-origin, cacheable response.
 */
import type { EonetEvent } from './types';

interface EonetGeometry { date?: string; type: string; coordinates: number[] | number[][] | number[][][] }
interface EonetRawEvent { id: string; title: string; link: string; geometry: EonetGeometry[] }

function centroid(geom: EonetGeometry): [number, number] | null {
  if (geom.type === 'Point') {
    const c = geom.coordinates as number[];
    return [c[0], c[1]];
  }
  if (geom.type === 'Polygon') {
    const ring = (geom.coordinates as number[][][])[0] ?? [];
    if (!ring.length) return null;
    const s = ring.reduce((a, p) => [a[0] + p[0], a[1] + p[1]], [0, 0]);
    return [s[0] / ring.length, s[1] / ring.length];
  }
  return null;
}

export function parseEonet(json: { events?: EonetRawEvent[] }): EonetEvent[] {
  const out: EonetEvent[] = [];
  for (const ev of json.events ?? []) {
    const g = ev.geometry?.[ev.geometry.length - 1];
    if (!g) continue;
    const c = centroid(g);
    if (!c) continue;
    out.push({ id: ev.id, title: ev.title, link: ev.link, longitude: c[0], latitude: c[1], date: g.date });
  }
  return out;
}

export async function fetchEonetWildfires(signal?: AbortSignal): Promise<EonetEvent[]> {
  const res = await fetch('/api/eonet', { signal, cache: 'no-store' });
  if (!res.ok) throw new Error(`EONET proxy responded ${res.status}`);
  const json = await res.json();
  if (json.error) throw new Error(json.error);
  return parseEonet(json);
}
