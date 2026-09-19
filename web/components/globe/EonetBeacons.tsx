'use client';
import { useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Html } from '@react-three/drei';
import { Group, Mesh } from 'three';
import type { EonetEvent } from '@/lib/data/types';
import { latLonToVector3, surfaceFrame } from '@/lib/geo/wgs84';
import { useGame } from '@/store/gameStore';
import { useGlobeTiles } from '@/lib/tiles/TilesContext';
import { SurfaceTracker } from '@/lib/tiles/surface';

/** Pulsating beacon for a named EONET wildfire crisis. */
function Beacon({ ev, index }: { ev: EonetEvent; index: number }) {
  const root = useRef<Group>(null);
  const ring = useRef<Mesh>(null);
  const ring2 = useRef<Mesh>(null);
  const label = useRef<HTMLDivElement>(null);
  const tiles = useGlobeTiles();
  const tracker = useMemo(() => new SurfaceTracker(), []);
  const flyTo = useGame((s) => s.flyTo);
  const { position, quaternion } = useMemo(() => {
    const p = latLonToVector3(ev.latitude, ev.longitude, 0);
    const { up, north, east } = surfaceFrame(ev.latitude, ev.longitude);
    const g = new Group();
    g.matrix.makeBasis(east, up, north.clone().negate());
    g.quaternion.setFromRotationMatrix(g.matrix);
    return { position: p, quaternion: g.quaternion.clone() };
  }, [ev.latitude, ev.longitude]);

  useFrame(({ clock, camera }) => {
    if (root.current) {
      const d = camera.position.length() - 1;
      root.current.scale.setScalar(Math.max(0.0006, Math.min(0.01, d * 0.02)));
      latLonToVector3(ev.latitude, ev.longitude, tracker.update(tiles, ev.latitude, ev.longitude, clock.elapsedTime), root.current.position);
      const facing = position.clone().normalize().dot(camera.position.clone().normalize());
      root.current.visible = facing > -0.05;
      if (label.current) label.current.style.opacity = facing > 0.08 ? '1' : '0';
    }
    const t = (clock.elapsedTime * 0.6 + index * 0.37) % 1;
    if (ring.current) {
      ring.current.scale.setScalar(1 + t * 3);
      (ring.current.material as { opacity: number }).opacity = (1 - t) * 0.8;
    }
    if (ring2.current) {
      const t2 = (t + 0.5) % 1;
      ring2.current.scale.setScalar(1 + t2 * 3);
      (ring2.current.material as { opacity: number }).opacity = (1 - t2) * 0.8;
    }
  });

  return (
    <group ref={root} position={position} quaternion={quaternion}>
      <mesh ref={ring} rotation={[-Math.PI / 2, 0, 0]}>
        <ringGeometry args={[1, 1.15, 40]} />
        <meshBasicMaterial color="#ffb347" transparent toneMapped={false} depthWrite={false} />
      </mesh>
      <mesh ref={ring2} rotation={[-Math.PI / 2, 0, 0]}>
        <ringGeometry args={[1, 1.15, 40]} />
        <meshBasicMaterial color="#ff5a1f" transparent toneMapped={false} depthWrite={false} />
      </mesh>
      <mesh position={[0, 1.2, 0]} onClick={(e) => { e.stopPropagation(); flyTo(ev.latitude, ev.longitude, 1.15); }}>
        <cylinderGeometry args={[0.08, 0.08, 2.4, 6]} />
        <meshBasicMaterial color="#ffd79a" toneMapped={false} />
      </mesh>
      <mesh position={[0, 2.6, 0]}>
        <octahedronGeometry args={[0.45]} />
        <meshBasicMaterial color="#ffb347" toneMapped={false} />
      </mesh>
      <Html ref={label} position={[0, 4.2, 0]} center zIndexRange={[10, 0]} style={{ pointerEvents: 'none' }}>
        <div className="hud-label hud-label--beacon">
          <div className="hud-label__title">◈ {ev.title}</div>
          <div className="hud-label__meta">EONET NAMED EVENT</div>
        </div>
      </Html>
    </group>
  );
}

export function EonetBeacons({ events }: { events: EonetEvent[] }) {
  const shown = useMemo(() => events.slice(0, 40), [events]);
  return (
    <>
      {shown.map((ev, i) => (
        <Beacon key={ev.id} ev={ev} index={i} />
      ))}
    </>
  );
}
