'use client';
import { useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Html } from '@react-three/drei';
import { Group } from 'three';
import { terrainHeight } from '@/lib/tactical/local';
import type { VisionMode } from '@/store/gameStore';

export interface Civilian {
  id: number;
  x: number;
  z: number;
  rescued: boolean;
}

/** Trapped civilians: visible as tiny figures normally; IR paints them white-hot with PERSONNEL DETECTED tags. */
export function Civilians({ civilians, seed, vision }: { civilians: Civilian[]; seed: number; vision: VisionMode }) {
  return (
    <group>
      {civilians.filter((c) => !c.rescued).map((c) => (
        <CivilianFigure key={c.id} c={c} seed={seed} vision={vision} />
      ))}
    </group>
  );
}

function CivilianFigure({ c, seed, vision }: { c: Civilian; seed: number; vision: VisionMode }) {
  const g = useRef<Group>(null);
  const base = useMemo(() => terrainHeight(c.x, c.z, seed), [c.x, c.z, seed]);
  useFrame(({ clock }) => {
    if (!g.current) return;
    const t = clock.elapsedTime + c.id * 1.7;
    g.current.position.set(c.x + Math.sin(t * 0.6) * 6, base, c.z + Math.cos(t * 0.45) * 6);
  });
  const color = vision === 'ir' ? '#ffffff' : vision === 'lidar' ? '#ffd54f' : '#ffb27a';
  return (
    <group ref={g}>
      <mesh position={[0, 1.7, 0]}>
        <capsuleGeometry args={[0.6, 1.6, 4, 8]} />
        <meshStandardMaterial color={color} emissive={color} emissiveIntensity={vision === 'ir' ? 1.4 : 0.2} />
      </mesh>
      {vision !== 'standard' && (
        <Html position={[0, 8, 0]} center zIndexRange={[20, 0]} style={{ pointerEvents: 'none' }}>
          <div className={`hud-label ${vision === 'ir' ? 'hud-label--ir' : 'hud-label--lidar'}`}>
            <div className="hud-label__title">{vision === 'ir' ? '▣ PERSONNEL DETECTED' : '▣ SURVIVOR'}</div>
            <div className="hud-label__meta">HOLD R WITHIN 40 m · ALT &lt; 30 m</div>
          </div>
        </Html>
      )}
    </group>
  );
}
