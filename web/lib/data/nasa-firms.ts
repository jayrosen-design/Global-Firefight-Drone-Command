/**
 * NASA FIRMS active-fire feed.
 *
 * The browser calls our own /api/firms route, which proxies the FIRMS area CSV
 * API using the server-side MAP_KEY and returns the raw CSV. This module parses
 * the CSV into typed hotspots and converts them to 3D globe positions.
 */
import { latLonToVector3 } from '@/lib/geo/wgs84';
import type { FirmsHotspot } from './types';
import { Vector3 } from 'three';

const CONFIDENCE_MAP: Record<string, number> = { l: 30, low: 30, n: 60, nominal: 60, h: 90, high: 90 };

export function parseFirmsCsv(csv: string, limit = 6000): FirmsHotspot[] {
  const lines = csv.trim().split(/\r?\n/);
  if (lines.length < 2) return [];
  const header = lines[0].split(',').map((h) => h.trim().toLowerCase());
  const col = (name: string) => header.indexOf(name);
  const iLat = col('latitude'), iLon = col('longitude'), iFrp = col('frp'), iConf = col('confidence');
  const iDate = col('acq_date'), iTime = col('acq_time'), iSat = col('satellite'), iDn = col('daynight');
  if (iLat < 0 || iLon < 0) throw new Error('FIRMS CSV missing latitude/longitude columns');

  const out: FirmsHotspot[] = [];
  // Sort by FRP descending so the strongest fires survive the limit.
  const rows = lines.slice(1).map((l) => l.split(','));
  rows.sort((a, b) => (parseFloat(b[iFrp]) || 0) - (parseFloat(a[iFrp]) || 0));
  for (let i = 0; i < rows.length && out.length < limit; i++) {
    const r = rows[i];
    const latitude = parseFloat(r[iLat]);
    const longitude = parseFloat(r[iLon]);
    if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) continue;
    const rawConf = (r[iConf] ?? '').trim().toLowerCase();
    const confidence = CONFIDENCE_MAP[rawConf] ?? (Number.isFinite(parseFloat(rawConf)) ? parseFloat(rawConf) : 50);
    out.push({
      id: `firms-${i}-${latitude.toFixed(3)}-${longitude.toFixed(3)}`,
      latitude,
      longitude,
      frp: Math.max(0.5, parseFloat(r[iFrp]) || 1),
      confidence,
      acqDate: iDate >= 0 ? r[iDate] : undefined,
      acqTime: iTime >= 0 ? r[iTime] : undefined,
      satellite: iSat >= 0 ? r[iSat] : undefined,
      daynight: iDn >= 0 ? (r[iDn] as 'D' | 'N') : undefined,
    });
  }
  return out;
}

export async function fetchFirmsHotspots(signal?: AbortSignal): Promise<FirmsHotspot[]> {
  const res = await fetch('/api/firms', { signal, cache: 'no-store' });
  if (!res.ok) throw new Error(`FIRMS proxy responded ${res.status}`);
  const text = await res.text();
  if (text.startsWith('{')) {
    const j = JSON.parse(text) as { error?: string };
    throw new Error(j.error ?? 'FIRMS proxy error');
  }
  return parseFirmsCsv(text);
}

/** WGS84 → globe-space Cartesian (X, Y, Z) for a hotspot. */
export function hotspotToVector3(h: FirmsHotspot, out = new Vector3()): Vector3 {
  return latLonToVector3(h.latitude, h.longitude, 0, out);
}

/** Normalised fire intensity 0..1 on a log scale — FRP spans ~1 MW to >5000 MW. */
export function frpToIntensity(frp: number): number {
  return Math.min(1, Math.max(0, Math.log10(frp + 1) / Math.log10(2500)));
}
