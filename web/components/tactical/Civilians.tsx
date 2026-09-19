'use client';
import { useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Html } from '@react-three/drei';
import { Group } from 'three';
import type { VisionMode } from '@/store/gameStore';
import { useTacticalTerrain } from './TacticalWorld';

export interface Civilian {
  id: number;
  x: number;
  z: number;
  rescued: boolean;
}

/** Trapped civilians: visible as tiny figures normally; IR paints them white-hot with PERSONNEL DETECTED tags. */
export function Civilians({ civilians, vision }: { civilians: Civilian[]; vision: VisionMode }) {
  return (
    <group>
      {civilians.filter((c) => !c.rescued).map((c) => (
        <CivilianFigure key={c.id} c={c} vision={vision} />
      ))}
    </group>
  );
}

function CivilianFigure({ c, vision }: { c: Civilian; vision: VisionMode }) {
  const g = useRef<Group>(null);
  const terrain = useTacticalTerrain();
  const ground = useRef(0);
  const next = useRef(0);
  useFrame(({ clock }) => {
    if (!g.current) return;
    const t = clock.elapsedTime + c.id * 1.7;
    const x = c.x + Math.sin(t * 0.6) * 6, z = c.z + Math.cos(t * 0.45) * 6;
    if (clock.elapsedTime >= next.current) {
      next.current = clock.elapsedTime + 0.3;
      ground.current = terrain.height(x, z);
    }
    g.current.position.set(x, ground.current, z);
  });
  const color = vision === 'ir' ? '#ffffff' : vision === 'lidar' ? '#ffd54f' : '#ffb27a';
  return (
    <group ref={g}>
      <mesh position={[0, 1.7, 0]}>
        <capsuleGeometry args={[0.6, 1.6, 4, 8]} />
        <meshStandardMaterial color={color} emissive={color} emissiveIntensity={vision === 'ir' ? 1.4 : 0.2} />
      </mesh>
      {/* Beacon so survivors are findable over photoreal terrain */}
      <mesh position={[0, 12, 0]}>
        <cylinderGeometry args={[0.15, 0.15, 20, 6]} />
        <meshBasicMaterial color={color} transparent opacity={0.35} />
      </mesh>
      {vision !== 'standard' && (
        <Html position={[0, 24, 0]} center zIndexRange={[20, 0]} style={{ pointerEvents: 'none' }}>
          <div className={`hud-label ${vision === 'ir' ? 'hud-label--ir' : 'hud-label--lidar'}`}>
            <div className="hud-label__title">{vision === 'ir' ? '▣ PERSONNEL DETECTED' : '▣ SURVIVOR'}</div>
            <div className="hud-label__meta">HOLD R WITHIN 40 m · ALT &lt; 30 m</div>
          </div>
        </Html>
      )}
    </group>
  );
}
