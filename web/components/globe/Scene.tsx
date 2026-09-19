'use client';
import { Suspense, useState } from 'react';
import type { TilesRenderer as TilesRendererImpl } from '3d-tiles-renderer/three';
import { GlobeTilesContext } from '@/lib/tiles/TilesContext';
import { WorldTiles, type TilesStatus } from '@/components/tiles/WorldTiles';
import { HAS_3D_TILES } from '@/lib/config/tiles';
import { Canvas, useFrame } from '@react-three/fiber';
import { useGame } from '@/store/gameStore';
import { Globe } from './Globe';
import { Stars } from './Stars';
import { FireLayer } from './FireLayer';
import { FireEntities } from './FireEntities';
import { EonetBeacons } from './EonetBeacons';
import { Carriers } from './Carriers';
import { Drones } from './Drones';
import { Trajectories } from './Trajectories';
import { CameraRig } from './CameraRig';

/** Drives the simulation clock from the render loop. */
function Simulation() {
  const tick = useGame((s) => s.tick);
  useFrame((_, dt) => tick(dt));
  return null;
}

function FeedLayers() {
  const feed = useGame((s) => s.feed);
  const show = useGame((s) => s.showLiveFeed);
  if (!feed || !show) return null;
  return (
    <>
      <FireLayer hotspots={feed.hotspots} />
      <EonetBeacons events={feed.events} />
    </>
  );
}

export function GlobeScene() {
  const [tiles, setTiles] = useState<TilesRendererImpl | null>(null);
  const [status, setStatus] = useState<TilesStatus>('loading');
  const active = HAS_3D_TILES && status === 'ready';
  return (
    <Canvas
      className="absolute inset-0"
      camera={{ position: [0, 1.2, 3.2], fov: 42, near: 0.001, far: 200 }}
      gl={{ antialias: true, powerPreference: 'high-performance', alpha: false }}
      dpr={[1, 1.75]}
      onPointerMissed={() => useGame.getState().select(null)}
    >
      <color attach="background" args={['#020509']} />
      <ambientLight intensity={0.3} />
      <directionalLight position={[5, 3, 4]} intensity={1.1} color="#dfefff" />
      <directionalLight position={[-4, -2, -3]} intensity={0.25} color="#5ef2ff" />
      <Stars />
      <GlobeTilesContext.Provider value={active ? tiles : null}>
        <Suspense fallback={null}>
          <Globe tilesActive={active} />
          {HAS_3D_TILES && status !== 'failed' && <WorldTiles mode="globe" onTiles={setTiles} onStatus={setStatus} errorTarget={12} />}
          <FeedLayers />
          <FireEntities />
          <Trajectories />
          <Carriers />
          <Drones />
        </Suspense>
      </GlobeTilesContext.Provider>
      <CameraRig />
      <Simulation />
    </Canvas>
  );
}
