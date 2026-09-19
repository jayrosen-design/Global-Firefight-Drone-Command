/**
 * Local tangent-plane projection for the tactical view. 1 unit = 1 metre,
 * with geography compressed by COMPRESSION so a 15 km fire complex fits within
 * a flyable ~2 km arena.
 */
import { fbm } from './noise';
import type { LatLon } from '@/lib/geo/wgs84';

export const COMPRESSION = 0.12;
export const ARENA = 3000; // metres, half-width
const M_PER_DEG_LAT = 110_540;

export function toLocal(origin: LatLon, p: LatLon): [number, number] {
  const mPerDegLon = 111_320 * Math.cos((origin.lat * Math.PI) / 180);
  const x = (p.lon - origin.lon) * mPerDegLon * COMPRESSION;
  const z = -(p.lat - origin.lat) * M_PER_DEG_LAT * COMPRESSION;
  return [Math.max(-ARENA, Math.min(ARENA, x)), Math.max(-ARENA, Math.min(ARENA, z))];
}

export function terrainHeight(x: number, z: number, seed = 11): number {
  const n = fbm(x / 900 + 10, z / 900 + 10, 5, seed);
  const ridge = fbm(x / 2600, z / 2600, 3, seed + 40);
  return (n - 0.45) * 180 + (ridge - 0.5) * 260;
}
