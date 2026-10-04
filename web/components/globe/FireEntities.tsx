'use client';
import { useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Html } from '@react-three/drei';
import { Color, Group, Mesh } from 'three';
import type { ThreeEvent } from '@react-three/fiber';
import { latLonToVector3, surfaceFrame } from '@/lib/geo/wgs84';
import { intensity, type Fire } from '@/lib/engine/fire';
import { useGame } from '@/store/gameStore';
import { isDrag } from '@/lib/geo/clicks';
import { useGlobeTiles } from '@/lib/tiles/TilesContext';
import { SurfaceTracker } from '@/lib/tiles/surface';
import { FLEETS } from '@/lib/config/fleets';
import { threateningFire } from '@/lib/engine/objectives';

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
  const selected = useGame((s) => s.selection?.type === 'fire' && s.selection.id === fire.id);
  const hovered = useGame((s) => s.hoverFireId === fire.id);
  // Name of the structure this fire threatens (protect objectives), so players know which fire matters.
  const threat = useGame((s) => s.scenario?.objectives.find((o) => o.protect && threateningFire(o, s.fires)?.id === fire.id)?.protect?.label);
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
      group.current.scale.setScalar(Math.max(0.00005, Math.min(0.004, d * 0.012)));
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
    if (isDrag(e)) return;
    clickFire(fire.id);
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
      {/* Fixed-size invisible click target: the cone and ring shrink as the fire is knocked down, just when players click again. */}
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.03, 0]} onClick={onClick} onPointerOver={() => useGame.getState().setHoverFire(fire.id)} onPointerOut={() => useGame.getState().setHoverFire(null)}>
        <circleGeometry args={[2.4, 24]} />
        <meshBasicMaterial transparent opacity={0} depthWrite={false} colorWrite={false} />
      </mesh>
      {/* Ground ring: threat radius (red) → contained (cyan) → extinguished (grey) */}
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.02, 0]} onClick={onClick}>
        <ringGeometry args={[1.4 + inten * 2, 1.7 + inten * 2, 48]} />
        <meshBasicMaterial color={fire.extinguished ? '#5a6b75' : fire.contained ? '#5ef2ff' : selected ? '#ffffff' : '#ff5a1f'} transparent opacity={selected || hovered ? 0.95 : 0.55} toneMapped={false} />
      </mesh>
      {showLabel && (
        // Anchored at the fire and lifted in screen space: from the near-overhead campaign camera a 3D "up" offset
        // would draw the label right on top of the fire it names.
        <Html ref={label} position={[0, 0, 0]} center zIndexRange={[20, 0]} style={{ pointerEvents: 'none' }}>
          <div
            className={`hud-label ${fire.extinguished ? 'hud-label--out' : selected ? 'hud-label--selected' : ''}`}
            style={{ transform: 'translateY(calc(-50% - 30px))' }}
          >
            {(fire.label || selected || hovered) && <div className="hud-label__title">{fire.label ?? 'ACTIVE FIRE'}</div>}
            <div className="hud-label__meta">
              {fire.extinguished ? 'EXTINGUISHED' : `${fire.frp.toFixed(0)} MW${fire.contained ? ' · CONTAINED' : fire.holdSec > 0 ? ' · HELD' : ''}`}
            </div>
            {!fire.extinguished && threat && <div className="hud-label__threat">THREATENS {threat.toUpperCase()}</div>}
            {!fire.extinguished && (
              <div className="hud-label__bar">
                <div style={{ width: `${Math.max(0, Math.min(1, 1 - fire.frp / fire.initialFrp)) * 100}%` }} />
              </div>
            )}
          </div>
        </Html>
      )}
    </group>
  );
}

/** One click sends drones (from the selected carrier if it can reach, else the nearest that can) and opens a feed. */
export function clickFire(fireId: string) {
  const st = useGame.getState();
  const fire = st.fires.find((f) => f.id === fireId);
  if (!fire) return;
  const sent = !fire.extinguished && st.dispatchTo(fire.id);
  if (!sent || st.selection?.type !== 'carrier') st.select({ type: 'fire', id: fire.id });
  st.openFeed({ lat: fire.lat, lon: fire.lon }, { fireId: fire.id });
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
