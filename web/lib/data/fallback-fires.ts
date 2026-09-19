/**
 * Offline fallback used when the FIRMS proxy has no key or the network is
 * blocked. Seeds a plausible global distribution of hotspots derived from the
 * campaign clusters plus historically fire-prone belts, so the globe never
 * renders empty.
 */
import { SCENARIOS } from '@/lib/config/scenarios';
import type { EonetEvent, FirmsHotspot } from './types';

function seeded(seed: number) {
  let s = seed >>> 0;
  return () => {
    s = (s * 1664525 + 1013904223) >>> 0;
    return s / 0xffffffff;
  };
}

const BELTS: { lat: number; lon: number; spread: number; count: number; frp: number }[] = [
  { lat: -8, lon: 22, spread: 12, count: 320, frp: 25 }, // Central Africa savanna burning
  { lat: -14, lon: -55, spread: 9, count: 160, frp: 40 }, // Amazon arc of deforestation
  { lat: 56, lon: 105, spread: 14, count: 180, frp: 60 }, // Siberian taiga
  { lat: 60, lon: -120, spread: 10, count: 110, frp: 55 }, // Canadian boreal
  { lat: 42, lon: -118, spread: 6, count: 90, frp: 70 }, // US West
  { lat: -24, lon: 133, spread: 12, count: 140, frp: 30 }, // Australian interior
  { lat: 40, lon: -6, spread: 4, count: 60, frp: 45 }, // Iberia
  { lat: 20, lon: 100, spread: 6, count: 120, frp: 20 }, // SE Asia
  { lat: 38, lon: 35, spread: 5, count: 50, frp: 40 }, // Anatolia / Greece
  { lat: 25, lon: 80, spread: 8, count: 90, frp: 15 }, // Indo-Gangetic crop burning
];

export function buildFallbackFeed(): { hotspots: FirmsHotspot[]; events: EonetEvent[] } {
  const rnd = seeded(20180808);
  const hotspots: FirmsHotspot[] = [];
  let n = 0;
  for (const b of BELTS) {
    for (let i = 0; i < b.count; i++) {
      // Gaussian-ish scatter
      const g = () => (rnd() + rnd() + rnd() - 1.5) * 1.6;
      const lat = b.lat + g() * b.spread * 0.6;
      const lon = b.lon + g() * b.spread;
      const frp = Math.max(1, b.frp * (0.2 + rnd() * 2.2) * (rnd() < 0.05 ? 8 : 1));
      hotspots.push({
        id: `fb-${n++}`,
        latitude: lat,
        longitude: lon,
        frp,
        confidence: rnd() < 0.2 ? 30 : rnd() < 0.6 ? 60 : 90,
        satellite: 'N',
        daynight: rnd() < 0.7 ? 'D' : 'N',
      });
    }
  }
  const events: EonetEvent[] = SCENARIOS.map((s) => ({
    id: `fb-ev-${s.id}`,
    title: s.eventTitle,
    link: 'https://eonet.gsfc.nasa.gov/',
    latitude: s.center.lat,
    longitude: s.center.lon,
  }));
  for (const s of SCENARIOS) {
    for (const f of s.fires) {
      hotspots.push({ id: `fb-${n++}`, latitude: f.lat, longitude: f.lon, frp: f.frp, confidence: 90, daynight: 'D' });
    }
  }
  return { hotspots, events };
}
