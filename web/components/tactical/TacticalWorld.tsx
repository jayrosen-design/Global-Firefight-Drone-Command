'use client';
import { createContext, useContext, useEffect, useMemo, useRef, useState } from 'react';
import { Intersection, Raycaster, Vector3 } from 'three';
import type { TilesRenderer as TilesRendererImpl } from '3d-tiles-renderer/three';
import { WorldTiles, type TilesStatus } from '@/components/tiles/WorldTiles';
import { useTelemetry } from '@/store/telemetryStore';
import { HAS_TACTICAL_TILES } from '@/lib/config/tiles';
import { latLonToEcef, type LatLon } from '@/lib/geo/wgs84';
import { terrainHeight, toLocal } from '@/lib/tactical/local';
import { setLidarElevationRange } from '@/lib/tiles/vision';
import type { VisionMode } from '@/store/gameStore';

export interface TacticalTerrain {
  /** True when real-world tiles are the terrain source (vs procedural fallback). */
  real: boolean;
  /** Root tileset loaded and reoriented; projections are trustworthy. */
  ready: boolean;
  /** Ground height in metres at local (x, z). */
  height: (x: number, z: number) => number;
  /** Project a geodetic point into local metres (x east, y up, z south). */
  project: (p: LatLon, heightM?: number) => Vector3;
  /** Drone speed multiplier (real distances need faster airframes). */
  speedScale: number;
}

const noopTerrain: TacticalTerrain = {
  real: false,
  ready: true,
  height: () => 0,
  project: () => new Vector3(),
  speedScale: 1,
};

export const TacticalTerrainContext = createContext<TacticalTerrain>(noopTerrain);
export const useTacticalTerrain = () => useContext(TacticalTerrainContext);

const raycaster = new Raycaster();
raycaster.firstHitOnly = true;
const DOWN = new Vector3(0, -1, 0);
const rayOrigin = new Vector3();
const ecef = new Vector3();

/**
 * Provides the tactical terrain source. With a map route configured this
 * streams the same real-world tiles as the globe, reoriented so `origin` is
 * at (0,0,0) with +Y up; otherwise it falls back to the procedural heightmap.
 */
export function TacticalWorld({ origin, seed, vision, children }: { origin: LatLon; seed: number; vision: VisionMode; children: React.ReactNode }) {
  const [tiles, setTiles] = useState<TilesRendererImpl | null>(null);
  const [status, setStatus] = useState<TilesStatus>(HAS_TACTICAL_TILES ? 'loading' : 'failed');
  const cache = useRef(new Map<string, number>());
  const useReal = HAS_TACTICAL_TILES && status !== 'failed';
  const ready = !useReal || status === 'ready';

  useEffect(() => {
    if (status === 'ready' && tiles) tiles.group.updateMatrixWorld(true);
    useTelemetry.getState().set({ mapStatus: HAS_TACTICAL_TILES ? status : 'off' });
  }, [status, tiles]);

  const terrain = useMemo<TacticalTerrain>(() => {
    if (!useReal) {
      return {
        real: false,
        ready: true,
        speedScale: 1,
        height: (x, z) => terrainHeight(x, z, seed),
        project: (p, h = 0) => {
          const [x, z] = toLocal(origin, p);
          return new Vector3(x, terrainHeight(x, z, seed) + h, z);
        },
      };
    }
    const height = (x: number, z: number) => {
      const key = `${Math.round(x / 8)}:${Math.round(z / 8)}`;
      if (!tiles || !tiles.root) return cache.current.get(key) ?? 0;
      rayOrigin.set(x, 9000, z);
      raycaster.set(rayOrigin, DOWN);
      raycaster.near = 0;
      raycaster.far = 20000;
      const hits: Intersection[] = [];
      raycaster.intersectObject(tiles.group, true, hits);
      if (hits.length) {
        cache.current.set(key, hits[0].point.y);
        return hits[0].point.y;
      }
      return cache.current.get(key) ?? 0;
    };
    return {
      real: true,
      ready,
      speedScale: 2.2,
      height,
      project: (p, h = 0) => {
        if (tiles && tiles.root) {
          latLonToEcef(p.lat, p.lon, 0, ecef);
          tiles.group.updateMatrixWorld();
          const v = ecef.clone().applyMatrix4(tiles.group.matrixWorld);
          // Snap to the streamed surface (ellipsoid height ≠ terrain height), then lift.
          const ground = height(v.x, v.z);
          v.y = (Number.isFinite(ground) ? ground : v.y) + h;
          return v;
        }
        const [x, z] = toLocal(origin, p, 1, 60_000);
        return new Vector3(x, h, z);
      },
    };
  }, [tiles, ready, useReal, origin, seed]);

  // LIDAR colour ramp relative to the ground at the origin.
  useEffect(() => {
    if (!terrain.real || !ready) return;
    const h0 = terrain.height(0, 0);
    setLidarElevationRange(h0 - 120, 800);
  }, [terrain, ready]);

  return (
    <TacticalTerrainContext.Provider value={terrain}>
      {useReal && <WorldTiles mode="tactical" origin={origin} vision={vision} onTiles={setTiles} onStatus={setStatus} errorTarget={vision === 'lidar' ? 16 : 8} />}
      {children}
    </TacticalTerrainContext.Provider>
  );
}
