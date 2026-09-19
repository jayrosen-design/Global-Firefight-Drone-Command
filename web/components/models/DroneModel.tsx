'use client';
import { useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Group } from 'three';
import { FLEETS, type CountryCode } from '@/lib/config/fleets';
import type { VisionMode } from '@/store/gameStore';

/**
 * Procedural drone bodies with per-country liveries. Six silhouettes:
 *  USA fixed-wing VTOL, CAN quad "wasp", BRA long-wing ISR, CHN 16-rotor
 *  EHang-style pod, DEU boxy hex, AUS swept coastal ferry.
 * `vision` swaps materials for IR white-hot / LIDAR wireframe.
 */
export function DroneModel({ country, vision = 'standard', spin = true }: { country: CountryCode; vision?: VisionMode; spin?: boolean }) {
  const rotors = useRef<Group>(null);
  const { primary, secondary, accent } = FLEETS[country].drone.livery;
  const mat = useMemo(() => {
    if (vision === 'ir') return { p: '#d9d9d9', s: '#bdbdbd', a: '#ffffff', emissive: '#ffffff', ei: 0.35, wire: false };
    if (vision === 'lidar') return { p: '#5ef2ff', s: '#7dffb3', a: '#5ef2ff', emissive: '#5ef2ff', ei: 0.9, wire: true };
    return { p: primary, s: secondary, a: accent, emissive: accent, ei: 0.25, wire: false };
  }, [vision, primary, secondary, accent]);

  useFrame((_, dt) => {
    if (spin && rotors.current) rotors.current.rotation.y += dt * 40;
  });

  const M = ({ c, e }: { c: string; e?: boolean }) => (
    <meshStandardMaterial color={c} metalness={0.35} roughness={0.45} emissive={e ? mat.emissive : '#000000'} emissiveIntensity={e ? mat.ei : 0} wireframe={mat.wire} />
  );

  switch (country) {
    case 'USA':
      return (
        <group>
          <mesh><capsuleGeometry args={[0.22, 1.4, 6, 12]} /><M c={mat.p} /></mesh>
          <mesh rotation={[0, 0, Math.PI / 2]}><boxGeometry args={[0.08, 3.2, 0.5]} /><M c={mat.s} /></mesh>
          <mesh position={[0, 0.15, -0.9]}><boxGeometry args={[0.9, 0.35, 0.06]} /><M c={mat.a} e /></mesh>
          <mesh position={[0, -0.3, 0]}><boxGeometry args={[0.35, 0.3, 0.9]} /><M c={mat.a} /></mesh>
          <group ref={rotors}>{[-1.2, 1.2].map((x) => (<mesh key={x} position={[x, 0.2, 0]} rotation={[Math.PI / 2, 0, 0]}><cylinderGeometry args={[0.55, 0.55, 0.02, 12]} /><M c={mat.s} e /></mesh>))}</group>
        </group>
      );
    case 'CAN':
      return (
        <group>
          <mesh><sphereGeometry args={[0.45, 16, 12]} /><M c={mat.p} /></mesh>
          <mesh position={[0, -0.45, 0]}><cylinderGeometry args={[0.3, 0.4, 0.5, 12]} /><M c={mat.s} e /></mesh>
          {[[1, 1], [-1, 1], [1, -1], [-1, -1]].map(([x, z]) => (<mesh key={`${x}${z}`} position={[x * 0.55, 0, z * 0.55]} rotation={[0, Math.atan2(z, x), 0]}><boxGeometry args={[0.9, 0.08, 0.12]} /><M c={mat.s} /></mesh>))}
          <group ref={rotors}>{[[1, 1], [-1, 1], [1, -1], [-1, -1]].map(([x, z]) => (<mesh key={`${x}${z}`} position={[x * 0.95, 0.08, z * 0.95]} rotation={[Math.PI / 2, 0, 0]}><cylinderGeometry args={[0.42, 0.42, 0.02, 12]} /><M c={mat.a} e /></mesh>))}</group>
        </group>
      );
    case 'BRA':
      return (
        <group>
          <mesh rotation={[Math.PI / 2, 0, 0]}><cylinderGeometry args={[0.16, 0.22, 2.2, 10]} /><M c={mat.p} /></mesh>
          <mesh rotation={[0, 0, Math.PI / 2]} position={[0, 0.05, 0.2]}><boxGeometry args={[0.06, 4.2, 0.45]} /><M c={mat.s} /></mesh>
          <mesh position={[0, 0.3, -1.05]}><boxGeometry args={[0.7, 0.5, 0.05]} /><M c={mat.a} e /></mesh>
          <mesh position={[0, -0.25, 0.2]}><sphereGeometry args={[0.2, 10, 8]} /><M c={mat.a} e /></mesh>
          <group ref={rotors}>{[-1.4, 1.4].map((x) => (<mesh key={x} position={[x, 0.1, 0.2]} rotation={[Math.PI / 2, 0, 0]}><cylinderGeometry args={[0.5, 0.5, 0.02, 12]} /><M c={mat.s} e /></mesh>))}</group>
        </group>
      );
    case 'CHN':
      return (
        <group>
          <mesh><capsuleGeometry args={[0.4, 0.6, 6, 12]} /><M c={mat.p} /></mesh>
          <mesh position={[0, 0.1, 0.45]}><sphereGeometry args={[0.28, 12, 10]} /><M c={mat.s} /></mesh>
          {Array.from({ length: 8 }).map((_, i) => { const a = (i / 8) * Math.PI * 2; return (<mesh key={i} position={[Math.cos(a) * 0.6, -0.1, Math.sin(a) * 0.6]} rotation={[0, -a, 0]}><boxGeometry args={[0.7, 0.06, 0.1]} /><M c={mat.s} /></mesh>); })}
          <group ref={rotors}>{Array.from({ length: 8 }).map((_, i) => { const a = (i / 8) * Math.PI * 2; return (<mesh key={i} position={[Math.cos(a) * 1.0, -0.05, Math.sin(a) * 1.0]} rotation={[Math.PI / 2, 0, 0]}><cylinderGeometry args={[0.3, 0.3, 0.02, 10]} /><M c={mat.a} e /></mesh>); })}</group>
          <mesh position={[0, -0.55, 0]}><cylinderGeometry args={[0.12, 0.12, 0.5, 8]} /><M c={mat.a} e /></mesh>
        </group>
      );
    case 'DEU':
      return (
        <group>
          <mesh><boxGeometry args={[0.9, 0.45, 1.2]} /><M c={mat.p} /></mesh>
          <mesh position={[0, 0.3, 0]}><boxGeometry args={[0.5, 0.15, 0.8]} /><M c={mat.s} e /></mesh>
          <mesh position={[0, -0.35, 0]}><cylinderGeometry args={[0.3, 0.35, 0.3, 10]} /><M c={mat.s} /></mesh>
          {Array.from({ length: 6 }).map((_, i) => { const a = (i / 6) * Math.PI * 2; return (<mesh key={i} position={[Math.cos(a) * 0.75, 0.05, Math.sin(a) * 0.75]} rotation={[0, -a, 0]}><boxGeometry args={[0.8, 0.06, 0.1]} /><M c={mat.p} /></mesh>); })}
          <group ref={rotors}>{Array.from({ length: 6 }).map((_, i) => { const a = (i / 6) * Math.PI * 2; return (<mesh key={i} position={[Math.cos(a) * 1.1, 0.12, Math.sin(a) * 1.1]} rotation={[Math.PI / 2, 0, 0]}><cylinderGeometry args={[0.35, 0.35, 0.02, 10]} /><M c={mat.s} e /></mesh>); })}</group>
        </group>
      );
    case 'AUS':
    default:
      return (
        <group>
          <mesh rotation={[Math.PI / 2, 0, 0]}><capsuleGeometry args={[0.2, 1.6, 6, 12]} /><M c={mat.p} /></mesh>
          <mesh rotation={[0, 0.35, Math.PI / 2]} position={[0.9, 0, 0.2]}><boxGeometry args={[0.06, 1.9, 0.5]} /><M c={mat.s} /></mesh>
          <mesh rotation={[0, -0.35, Math.PI / 2]} position={[-0.9, 0, 0.2]}><boxGeometry args={[0.06, 1.9, 0.5]} /><M c={mat.s} /></mesh>
          <mesh position={[0, 0.35, -0.9]} rotation={[0.4, 0, 0]}><boxGeometry args={[0.5, 0.5, 0.05]} /><M c={mat.a} e /></mesh>
          <group ref={rotors}><mesh position={[0, 0, 1.0]} rotation={[0, 0, 0]}><cylinderGeometry args={[0.5, 0.5, 0.02, 12]} /><M c={mat.s} e /></mesh></group>
        </group>
      );
  }
}
