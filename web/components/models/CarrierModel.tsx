'use client';
import { FLEETS, type CountryCode } from '@/lib/config/fleets';

/** Procedural Mobile Command Carrier (6x6 truck) in each nation's livery. */
export function CarrierModel({ country, selected = false }: { country: CountryCode; selected?: boolean }) {
  const { primary, secondary, accent } = FLEETS[country].carrier.livery;
  return (
    <group>
      {/* chassis */}
      <mesh position={[0, 0.45, 0]}><boxGeometry args={[1.4, 0.5, 3.6]} /><meshStandardMaterial color={primary} metalness={0.3} roughness={0.5} /></mesh>
      {/* cab */}
      <mesh position={[0, 1.05, 1.25]}><boxGeometry args={[1.3, 0.75, 1.0]} /><meshStandardMaterial color={secondary} metalness={0.3} roughness={0.4} /></mesh>
      <mesh position={[0, 1.15, 1.76]}><boxGeometry args={[1.1, 0.4, 0.05]} /><meshStandardMaterial color="#1c3b4d" metalness={0.8} roughness={0.1} /></mesh>
      {/* command module / drone bay */}
      <mesh position={[0, 1.15, -0.55]}><boxGeometry args={[1.4, 0.9, 2.2]} /><meshStandardMaterial color={primary} metalness={0.3} roughness={0.5} /></mesh>
      <mesh position={[0, 1.62, -0.55]}><boxGeometry args={[1.42, 0.06, 2.22]} /><meshStandardMaterial color={accent} emissive={accent} emissiveIntensity={0.5} /></mesh>
      {/* stripe */}
      <mesh position={[0.71, 0.9, -0.55]}><boxGeometry args={[0.02, 0.18, 2.1]} /><meshStandardMaterial color={accent} emissive={accent} emissiveIntensity={0.4} /></mesh>
      <mesh position={[-0.71, 0.9, -0.55]}><boxGeometry args={[0.02, 0.18, 2.1]} /><meshStandardMaterial color={accent} emissive={accent} emissiveIntensity={0.4} /></mesh>
      {/* mast + radar */}
      <mesh position={[0, 2.2, -1.3]}><cylinderGeometry args={[0.04, 0.04, 1.2, 6]} /><meshStandardMaterial color="#888" /></mesh>
      <mesh position={[0, 2.85, -1.3]}><sphereGeometry args={[0.18, 10, 8]} /><meshStandardMaterial color={secondary} emissive="#5ef2ff" emissiveIntensity={0.6} /></mesh>
      {/* wheels 6x6 */}
      {[-1.15, 0, 1.15].map((z) => [-0.75, 0.75].map((x) => (
        <mesh key={`${x}${z}`} position={[x, 0.3, z]} rotation={[0, 0, Math.PI / 2]}><cylinderGeometry args={[0.32, 0.32, 0.3, 14]} /><meshStandardMaterial color="#141414" roughness={0.9} /></mesh>
      )))}
      {/* selection ring */}
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.02, 0]}>
        <ringGeometry args={[2.6, 2.85, 48]} />
        <meshBasicMaterial color={selected ? '#5ef2ff' : accent} transparent opacity={selected ? 0.95 : 0.35} toneMapped={false} />
      </mesh>
    </group>
  );
}
