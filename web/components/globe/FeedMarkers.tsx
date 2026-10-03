'use client';
import { useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Html } from '@react-three/drei';
import { Group, Matrix4 } from 'three';
import { latLonToVector3, surfaceFrame } from '@/lib/geo/wgs84';
import { useGame, type LiveFeed } from '@/store/gameStore';
import { useGlobeTiles } from '@/lib/tiles/TilesContext';
import { SurfaceTracker } from '@/lib/tiles/surface';

/** Cyan reticle on the globe at each open feed, numbered to match its window. */
function Marker({ feed, slot }: { feed: LiveFeed; slot: number }) {
  const g = useRef<Group>(null);
  const label = useRef<HTMLDivElement>(null);
  const tiles = useGlobeTiles();
  const tracker = useMemo(() => new SurfaceTracker(), []);
  const m = useMemo(() => new Matrix4(), []);
  useFrame(({ camera, clock }) => {
    const node = g.current;
    if (!node) return;
    latLonToVector3(feed.lat, feed.lon, tracker.update(tiles, feed.lat, feed.lon, clock.elapsedTime), node.position);
    const { up, north, east } = surfaceFrame(feed.lat, feed.lon);
    m.makeBasis(east, up, north.clone().negate());
    node.quaternion.setFromRotationMatrix(m);
    const d = camera.position.length() - 1;
    node.scale.setScalar(Math.max(0.0006, Math.min(0.012, d * 0.02)));
    const facing = node.position.clone().normalize().dot(camera.position.clone().normalize());
    node.visible = facing > -0.05;
    if (label.current) label.current.style.opacity = facing > 0.08 ? '1' : '0';
  });
  return (
    <group ref={g}>
      {[0, 1, 2, 3].map((i) => (
        <mesh key={i} rotation={[-Math.PI / 2, 0, (i * Math.PI) / 2]} position={[0, 0.05, 0]}>
          <ringGeometry args={[1.6, 1.85, 16, 1, -0.45, 0.9]} />
          <meshBasicMaterial color="#5ef2ff" transparent opacity={0.95} toneMapped={false} depthWrite={false} />
        </mesh>
      ))}
      <Html ref={label} position={[0, 2.4, 0]} center zIndexRange={[35, 0]} style={{ pointerEvents: 'none' }}>
        <div className="hud-label hud-label--feed">FEED {slot}</div>
      </Html>
    </group>
  );
}

export function FeedMarkers() {
  const feeds = useGame((s) => s.feeds);
  return (
    <>
      {feeds.map((f, i) => (
        <Marker key={f.id} feed={f} slot={i + 1} />
      ))}
    </>
  );
}
