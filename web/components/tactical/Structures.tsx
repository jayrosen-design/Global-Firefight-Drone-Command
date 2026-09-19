'use client';
import { useMemo } from 'react';
import { Html } from '@react-three/drei';
import { terrainHeight } from '@/lib/tactical/local';
import type { VisionMode } from '@/store/gameStore';

export interface LocalStructure {
  id: string;
  label: string;
  x: number;
  z: number;
  valueUSD: number;
  critical: boolean;
}

/** Protected structures (hospitals, towns, refuges) plus a scattered village footprint. */
export function Structures({ structures, seed, vision }: { structures: LocalStructure[]; seed: number; vision: VisionMode }) {
  const village = useMemo(() => {
    const out: { x: number; z: number; w: number; d: number; h: number }[] = [];
    let rnd = seed * 7919 + 13;
    const r = () => ((rnd = (rnd * 9301 + 49297) % 233280) / 233280);
    for (const s of structures) {
      for (let i = 0; i < 26; i++) {
        const a = r() * Math.PI * 2, rad = 40 + r() * 220;
        out.push({ x: s.x + Math.cos(a) * rad, z: s.z + Math.sin(a) * rad, w: 8 + r() * 10, d: 8 + r() * 10, h: 5 + r() * 6 });
      }
    }
    return out;
  }, [structures, seed]);

  const wallColor = vision === 'ir' ? '#5a5a5a' : vision === 'lidar' ? '#5ef2ff' : '#c9c2b4';
  return (
    <group>
      {village.map((b, i) => {
        const y = terrainHeight(b.x, b.z, seed);
        return (
          <mesh key={i} position={[b.x, y + b.h / 2, b.z]}>
            <boxGeometry args={[b.w, b.h, b.d]} />
            <meshStandardMaterial color={wallColor} wireframe={vision === 'lidar'} roughness={0.9} />
          </mesh>
        );
      })}
      {structures.map((s) => {
        const y = terrainHeight(s.x, s.z, seed);
        return (
          <group key={s.id} position={[s.x, y, s.z]}>
            <mesh position={[0, 9, 0]}>
              <boxGeometry args={[70, 18, 46]} />
              <meshStandardMaterial color={vision === 'ir' ? '#8a8a8a' : vision === 'lidar' ? '#5ef2ff' : '#e8e4dc'} wireframe={vision === 'lidar'} emissive={vision === 'ir' ? '#ffffff' : '#000000'} emissiveIntensity={vision === 'ir' ? 0.25 : 0} />
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
            <Html position={[0, 40, 0]} center zIndexRange={[20, 0]} style={{ pointerEvents: 'none' }}>
              <div className={`hud-label ${vision === 'ir' ? 'hud-label--ir' : vision === 'lidar' ? 'hud-label--lidar' : 'hud-label--structure'}`}>
                <div className="hud-label__title">{vision === 'ir' ? '⚠ CRITICAL STRUCTURE' : '◇ PROTECT'} · {s.label}</div>
                <div className="hud-label__meta">{vision === 'ir' ? 'VALVE / UTILITY HAZARD ×2' : `$${(s.valueUSD / 1e6).toFixed(0)}M AT RISK`}</div>
              </div>
            </Html>
          </group>
        );
      })}
    </group>
  );
}
