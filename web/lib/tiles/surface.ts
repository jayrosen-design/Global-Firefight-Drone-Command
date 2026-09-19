'use client';
import { Intersection, Raycaster, Vector3 } from 'three';
import type { TilesRenderer } from '3d-tiles-renderer/three';
import { latLonToVector3 } from '@/lib/geo/wgs84';

const raycaster = new Raycaster();
raycaster.firstHitOnly = true;
const origin = new Vector3();
const dir = new Vector3();
const surface = new Vector3();

/**
 * Terrain height (km above the ellipsoid) at lat/lon on the globe-view tiles,
 * by casting a ray from 30 km up straight down. Returns null when no tile is
 * loaded under the point yet.
 */
export function sampleSurfaceKm(tiles: TilesRenderer, lat: number, lon: number): number | null {
  latLonToVector3(lat, lon, 30, origin);
  dir.copy(origin).normalize().negate();
  raycaster.set(origin, dir);
  raycaster.near = 0;
  raycaster.far = 40 / 6371;
  const hits: Intersection[] = [];
  raycaster.intersectObject(tiles.group, true, hits);
  if (!hits.length) return null;
  latLonToVector3(lat, lon, 0, surface);
  const km = (hits[0].point.length() - surface.length()) * 6371;
  return Math.max(-0.5, Math.min(9, km));
}

/** Throttled per-entity surface height tracker for globe-view markers. */
export class SurfaceTracker {
  km = 0;
  private next = 0;
  update(tiles: TilesRenderer | null, lat: number, lon: number, time: number, interval = 0.75) {
    if (!tiles || time < this.next) return this.km;
    this.next = time + interval + Math.random() * 0.25;
    const h = sampleSurfaceKm(tiles, lat, lon);
    if (h !== null) this.km = h;
    return this.km;
  }
}
