'use client';
import { useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Html } from '@react-three/drei';
import { Color, Group, Mesh } from 'three';
import type { ThreeEvent } from '@react-three/fiber';
import { latLonToVector3, surfaceFrame } from '@/lib/geo/wgs84';
import { intensity, type Fire } from '@/lib/engine/fire';
import { useGame } from '@/store/gameStore';
import { useGlobeTiles } from '@/lib/tiles/TilesContext';
import { SurfaceTracker } from '@/lib/tiles/surface';
import { FLEETS } from '@/lib/config/fleets';

/**
 * Engaged fires (scenario presets or promoted hotspots): pulsing emissive
 * flame column + dynamic point light scaled by FRP, plus a hover/selection
 * label and containment ring.
 */
function FireEntity({ fire }: { fire: Fire }) {
  const group = useRef<Group>(null);
  const core = useRef<Mesh>(null);
  const label = useRef<HTMLDivElement>(null);
  const tiles = useGlobeTiles();
  const tracker = useMemo(() => new SurfaceTracker(), []);
  const select = useGame((s) => s.select);
  const selected = useGame((s) => s.selection?.type === 'fire' && s.selection.id === fire.id);
  const hovered = useGame((s) => s.hoverFireId === fire.id);
  const { position, quaternion } = useMemo(() => {
    const p = latLonToVector3(fire.lat, fire.lon, 0);
    const { up, north, east } = surfaceFrame(fire.lat, fire.lon);
    const g = new Group();
    g.matrix.makeBasis(east, up, north.clone().negate());
    g.quaternion.setFromRotationMatrix(g.matrix);
    return { position: p, quaternion: g.quaternion.clone() };
  }, [fire.lat, fire.lon]);

  const inten = intensity(fire);
  const color = useMemo(() => new Color().setHSL(0.06 - inten * 0.04, 1, 0.45 + inten * 0.15), [inten]);

  useFrame(({ clock, camera }) => {
    if (group.current) {
      const d = camera.position.length() - 1;
      group.current.scale.setScalar(Math.max(0.00035, Math.min(0.004, d * 0.012)));
      const km = tracker.update(tiles, fire.lat, fire.lon, clock.elapsedTime);
      latLonToVector3(fire.lat, fire.lon, km, group.current.position);
      const facing = position.clone().normalize().dot(camera.position.clone().normalize());
      group.current.visible = facing > -0.05;
      if (label.current) label.current.style.opacity = facing > 0.08 ? '1' : '0';
    }
    if (!core.current) return;
    const p = fire.extinguished ? 0.2 : 0.9 + 0.25 * Math.sin(clock.elapsedTime * (2 + inten * 4));
    core.current.scale.set(p, 0.6 + inten * 2.4 * p, p);
  });

  const onClick = (e: ThreeEvent<MouseEvent>) => {
    e.stopPropagation();
    const st = useGame.getState();
    if (st.selection?.type === 'carrier' && !fire.extinguished) st.dispatch(st.selection.id, fire.id);
    else select({ type: 'fire', id: fire.id });
  };

  const scale = 0.001;
  const showLabel = selected || hovered || fire.source === 'scenario';
  return (
    <group ref={group} position={position} quaternion={quaternion}>
      {!fire.extinguished && (
        <>
          <mesh ref={core} position={[0, 0.5, 0]} onClick={onClick} onPointerOver={() => useGame.getState().setHoverFire(fire.id)} onPointerOut={() => useGame.getState().setHoverFire(null)}>
            <coneGeometry args={[0.55, 1.6, 8]} />
            <meshBasicMaterial color={color} transparent opacity={0.9} toneMapped={false} />
          </mesh>
          <pointLight color={color} intensity={0.002 + inten * 0.01} distance={0.03 / scale} decay={2} position={[0, 1.2, 0]} />
        </>
      )}
      {/* Ground ring: threat radius (red) → contained (cyan) → extinguished (grey) */}
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.02, 0]} onClick={onClick}>
        <ringGeometry args={[1.4 + inten * 2, 1.7 + inten * 2, 48]} />
        <meshBasicMaterial color={fire.extinguished ? '#5a6b75' : fire.contained ? '#5ef2ff' : selected ? '#ffffff' : '#ff5a1f'} transparent opacity={selected || hovered ? 0.95 : 0.55} toneMapped={false} />
      </mesh>
      {showLabel && (
        <Html ref={label} position={[0, 3.6 + inten * 2, 0]} center zIndexRange={[20, 0]} style={{ pointerEvents: 'none' }}>
          <div className={`hud-label ${fire.extinguished ? 'hud-label--out' : selected ? 'hud-label--selected' : ''}`}>
            <div className="hud-label__title">{fire.label ?? 'ACTIVE FIRE'}</div>
            <div className="hud-label__meta">
              {fire.extinguished ? 'EXTINGUISHED' : `${fire.frp.toFixed(0)} MW${fire.contained ? ' · CONTAINED' : ''}`}
            </div>
          </div>
        </Html>
      )}
    </group>
  );
}

export function FireEntities() {
  const fires = useGame((s) => s.fires);
  return (
    <>
      {fires.map((f) => (
        <FireEntity key={f.id} fire={f} />
      ))}
    </>
  );
}

export function fleetColor(code: keyof typeof FLEETS) {
  return FLEETS[code].drone.livery.accent;
}
