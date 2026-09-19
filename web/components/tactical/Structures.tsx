'use client';
import { useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Html } from '@react-three/drei';
import { Group, Mesh } from 'three';
import type { VisionMode } from '@/store/gameStore';
import { useTacticalTerrain } from './TacticalWorld';

export interface LocalStructure {
  id: string;
  label: string;
  x: number;
  y: number;
  z: number;
  valueUSD: number;
  critical: boolean;
}

/**
 * Protected structures (hospitals, towns, refuges). Over real-world tiles the
 * imagery already shows the town, so only the landmark block, hazard markers
 * and label are drawn; the procedural fallback adds a scattered village.
 */
export function Structures({ structures, vision }: { structures: LocalStructure[]; vision: VisionMode }) {
  const terrain = useTacticalTerrain();
  return (
    <group>
      {!terrain.real && <Village structures={structures} vision={vision} />}
      {structures.map((s) => (
        <Landmark key={s.id} s={s} vision={vision} />
      ))}
    </group>
  );
}

function Landmark({ s, vision }: { s: LocalStructure; vision: VisionMode }) {
  const terrain = useTacticalTerrain();
  const g = useRef<Group>(null);
  const next = useRef(0);
  useFrame(({ clock }) => {
    if (!g.current) return;
    if (clock.elapsedTime < next.current) return;
    next.current = clock.elapsedTime + 1;
    g.current.position.set(s.x, terrain.real ? terrain.height(s.x, s.z) : s.y, s.z);
  });
  const wall = vision === 'ir' ? '#8a8a8a' : vision === 'lidar' ? '#5ef2ff' : '#e8e4dc';
  return (
    <group ref={g} position={[s.x, s.y, s.z]}>
      <mesh position={[0, 9, 0]}>
        <boxGeometry args={[70, 18, 46]} />
        <meshStandardMaterial color={wall} wireframe={vision === 'lidar'} transparent={terrain.real} opacity={terrain.real ? 0.55 : 1} emissive={vision === 'ir' ? '#ffffff' : '#000000'} emissiveIntensity={vision === 'ir' ? 0.25 : 0} />
      </mesh>
      <mesh position={[0, 18.5, 0]}>
        <boxGeometry args={[24, 1, 24]} />
        <meshStandardMaterial color={vision === 'ir' ? '#ffffff' : '#c8102e'} emissive={vision === 'ir' ? '#ffffff' : '#c8102e'} emissiveIntensity={0.7} />
      </mesh>
      {/* Critical valve / utility hazard markers */}
      {[[-30, 20], [34, -18]].map(([dx, dz], i) => (
        <mesh key={i} position={[dx, 3, dz]}>
          <cylinderGeometry args={[1.6, 1.6, 6, 8]} />
          <meshStandardMaterial color={vision === 'ir' ? '#ffffff' : '#ffb000'} emissive={vision === 'ir' ? '#ffffff' : '#ffb000'} emissiveIntensity={vision === 'ir' ? 1 : 0.3} />
        </mesh>
      ))}
      {/* Protection perimeter ring */}
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 1.5, 0]}>
        <ringGeometry args={[60, 64, 48]} />
        <meshBasicMaterial color={vision === 'ir' ? '#ffffff' : vision === 'lidar' ? '#7dffb3' : '#ffd54f'} transparent opacity={0.6} />
      </mesh>
      <Html position={[0, 40, 0]} center zIndexRange={[20, 0]} style={{ pointerEvents: 'none' }}>
        <div className={`hud-label ${vision === 'ir' ? 'hud-label--ir' : vision === 'lidar' ? 'hud-label--lidar' : 'hud-label--structure'}`}>
          <div className="hud-label__title">{vision === 'ir' ? '⚠ CRITICAL STRUCTURE' : '◇ PROTECT'} · {s.label}</div>
          <div className="hud-label__meta">{vision === 'ir' ? 'VALVE / UTILITY HAZARD ×2' : `$${(s.valueUSD / 1e6).toFixed(0)}M AT RISK`}</div>
        </div>
      </Html>
    </group>
  );
}

function Village({ structures, vision }: { structures: LocalStructure[]; vision: VisionMode }) {
  const terrain = useTacticalTerrain();
  const boxes = useMemo(() => {
    const out: { x: number; z: number; w: number; d: number; h: number }[] = [];
    let rnd = 7919 + structures.length * 13;
    const r = () => ((rnd = (rnd * 9301 + 49297) % 233280) / 233280);
    for (const s of structures) {
      for (let i = 0; i < 26; i++) {
        const a = r() * Math.PI * 2, rad = 40 + r() * 220;
        out.push({ x: s.x + Math.cos(a) * rad, z: s.z + Math.sin(a) * rad, w: 8 + r() * 10, d: 8 + r() * 10, h: 5 + r() * 6 });
      }
    }
    return out;
  }, [structures]);
  const refs = useRef<(Mesh | null)[]>([]);
  const next = useRef(0);
  useFrame(({ clock }) => {
    if (clock.elapsedTime < next.current) return;
    next.current = clock.elapsedTime + 2;
    boxes.forEach((b, i) => {
      const m = refs.current[i];
      if (m) m.position.set(b.x, terrain.height(b.x, b.z) + b.h / 2, b.z);
    });
  });
  const wallColor = vision === 'ir' ? '#5a5a5a' : vision === 'lidar' ? '#5ef2ff' : '#c9c2b4';
  return (
    <group>
      {boxes.map((b, i) => (
        <mesh key={i} ref={(m) => { refs.current[i] = m; }} position={[b.x, b.h / 2, b.z]}>
          <boxGeometry args={[b.w, b.h, b.d]} />
          <meshStandardMaterial color={wallColor} wireframe={vision === 'lidar'} roughness={0.9} />
        </mesh>
      ))}
    </group>
  );
}
