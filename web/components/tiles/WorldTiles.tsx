'use client';
import { useEffect, useMemo, useRef, useState } from 'react';
import { TilesRenderer, TilesPlugin, TilesAttributionOverlay } from '3d-tiles-renderer/r3f';
import type { TilesRenderer as TilesRendererImpl } from '3d-tiles-renderer/three';
import {
  CesiumIonAuthPlugin,
  GLTFExtensionsPlugin,
  GoogleCloudAuthPlugin,
  ImageOverlayPlugin,
  QuantizedMeshPlugin,
  ReorientationPlugin,
  TileCompressionPlugin,
  XYZTilesOverlay,
} from '3d-tiles-renderer/plugins';
import type { Object3D } from 'three';
import { MAP_ROUTE } from '@/lib/config/tiles';
import { buildTileBvh, enableBvhRaycast, getDracoLoader } from '@/lib/tiles/setup';
import { applyVisionToTile, disposeVisionForTile } from '@/lib/tiles/vision';
import type { VisionMode } from '@/store/gameStore';

const M_PER_UNIT = 6371.0088 * 1000;

export type TilesStatus = 'loading' | 'ready' | 'failed';

export interface WorldTilesProps {
  /** globe: ECEF tiles scaled into the unit-sphere frame. tactical: metres, target lat/lon at origin, +Y up. */
  mode: 'globe' | 'tactical';
  /** Tactical origin (degrees). */
  origin?: { lat: number; lon: number };
  vision?: VisionMode;
  errorTarget?: number;
  onTiles?: (tiles: TilesRendererImpl | null) => void;
  /** loading → ready once the root tileset arrives, or failed (root unreachable / no root within 20 s). */
  onStatus?: (status: TilesStatus) => void;
  /** Fired after every tile model loads (BVH already built). */
  onModel?: (scene: Object3D) => void;
  children?: React.ReactNode;
}

/**
 * Real-world 3D tiles (God Eye's map stack) for both views. Route is chosen
 * from env at build time — see lib/config/tiles.ts.
 */
export function WorldTiles({ mode, origin, vision = 'standard', errorTarget, onTiles, onStatus, onModel, children }: WorldTilesProps) {
  const tilesRef = useRef<TilesRendererImpl | null>(null);
  const visionRef = useRef<VisionMode>(vision);
  visionRef.current = vision;

  useEffect(() => {
    enableBvhRaycast();
  }, []);

  // Root-tileset watchdog: report ready / failed so the views can fall back (God Eye's keyless-globe recovery).
  const [tiles, setTilesState] = useState<TilesRendererImpl | null>(null);
  useEffect(() => {
    if (!tiles) return;
    let settled = false;
    const ready = () => {
      if (settled) return;
      settled = true;
      onStatus?.('ready');
    };
    const failed = () => {
      if (settled) return;
      settled = true;
      onStatus?.('failed');
    };
    const onError = (e: { tile: unknown }) => {
      if (e.tile === null) failed();
    };
    onStatus?.('loading');
    tiles.addEventListener('load-root-tileset', ready);
    tiles.addEventListener('load-error', onError);
    if (tiles.root) ready();
    const timer = window.setTimeout(() => {
      if (!tiles.root) failed();
    }, 20_000);
    return () => {
      window.clearTimeout(timer);
      tiles.removeEventListener('load-root-tileset', ready);
      tiles.removeEventListener('load-error', onError);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tiles]);

  // Re-apply the vision mode to everything already streamed in.
  useEffect(() => {
    const t = tilesRef.current;
    if (!t) return;
    t.forEachLoadedModel((scene) => applyVisionToTile(scene, vision));
  }, [vision]);

  const imagery = useMemo(() => {
    if (MAP_ROUTE.kind !== 'keyless') return null;
    return new XYZTilesOverlay({ url: MAP_ROUTE.imageryUrl, levels: 19 });
  }, []);

  const gltfArgs = useMemo(() => ({ dracoLoader: getDracoLoader(), autoDispose: false }), []);
  const reorientArgs = useMemo(
    () => (mode === 'tactical' && origin ? { lat: (origin.lat * Math.PI) / 180, lon: (origin.lon * Math.PI) / 180, height: 0, recenter: true } : null),
    [mode, origin],
  );

  if (MAP_ROUTE.kind === 'off') return null;

  const handleRef = (t: TilesRendererImpl | null) => {
    if (tilesRef.current === t) return;
    tilesRef.current = t;
    setTilesState(t);
    onTiles?.(t);
  };

  const renderer = (
    <TilesRenderer
      ref={handleRef}
      url={MAP_ROUTE.kind === 'keyless' ? MAP_ROUTE.terrainUrl : undefined}
      errorTarget={errorTarget}
      onLoadModel={({ scene }) => {
        buildTileBvh(scene);
        applyVisionToTile(scene, visionRef.current);
        onModel?.(scene);
      }}
      onDisposeModel={({ scene }) => disposeVisionForTile(scene)}
    >
      {MAP_ROUTE.kind === 'google-direct' && <TilesPlugin plugin={GoogleCloudAuthPlugin} args={[{ apiToken: MAP_ROUTE.apiToken, autoRefreshToken: true }]} />}
      {MAP_ROUTE.kind === 'google-ion' && <TilesPlugin plugin={CesiumIonAuthPlugin} args={[{ apiToken: MAP_ROUTE.apiToken, assetId: MAP_ROUTE.assetId, autoRefreshToken: true }]} />}
      {MAP_ROUTE.kind === 'keyless' && <TilesPlugin plugin={QuantizedMeshPlugin} args={[{ solid: false }]} />}
      {MAP_ROUTE.kind === 'keyless' && imagery && <TilesPlugin plugin={ImageOverlayPlugin} args={[{ overlays: [imagery], resolution: 256 }]} />}
      <TilesPlugin plugin={GLTFExtensionsPlugin} args={[gltfArgs]} />
      <TilesPlugin plugin={TileCompressionPlugin} args={[{ generateNormals: false, disableMipmaps: false }]} />
      {reorientArgs && <TilesPlugin plugin={ReorientationPlugin} args={[reorientArgs]} />}
      <TilesAttributionOverlay style={ATTRIBUTION_STYLE} />
      {children}
    </TilesRenderer>
  );

  if (mode === 'globe') {
    // ECEF (X, Y, Z) → globe space (X, Z, −Y) / R  ==  rotate −90° about X, then scale.
    return (
      <group rotation={[-Math.PI / 2, 0, 0]} scale={1 / M_PER_UNIT}>
        {renderer}
      </group>
    );
  }
  return renderer;
}

const ATTRIBUTION_STYLE: React.CSSProperties = {
  position: 'absolute',
  left: 12,
  bottom: 176,
  fontSize: 10,
  color: 'rgba(255,255,255,0.75)',
  background: 'rgba(4,10,16,0.6)',
  padding: '3px 6px',
  borderRadius: 3,
  pointerEvents: 'none',
  zIndex: 25,
  maxWidth: 420,
};
