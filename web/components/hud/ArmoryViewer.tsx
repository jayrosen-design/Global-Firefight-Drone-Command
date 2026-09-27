'use client';
import { useRef } from 'react';
import { Canvas, useFrame } from '@react-three/fiber';
import { Group } from 'three';
import type { CountryCode } from '@/lib/config/fleets';
import { FLEETS } from '@/lib/config/fleets';
import { DroneModel } from '@/components/models/DroneModel';
import { CarrierModel } from '@/components/models/CarrierModel';

function Turntable({ country }: { country: CountryCode }) {
  const drone = useRef<Group>(null);
  const rig = useRef<Group>(null);
  useFrame(({ clock }, dt) => {
    if (rig.current) rig.current.rotation.y += dt * 0.35;
    if (drone.current) drone.current.position.y = 3.0 + Math.sin(clock.elapsedTime * 1.4) * 0.18;
  });
  const accent = FLEETS[country].drone.livery.accent;
  return (
    <group ref={rig}>
      <group ref={drone} position={[0, 3.0, 0]} scale={1.0} rotation={[0.08, 0, 0]}>
        <DroneModel country={country} />
      </group>
      <group position={[0, 0, 0]} scale={0.95}>
        <CarrierModel country={country} selected />
      </group>
      {/* Pad */}
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, -0.02, 0]}>
        <ringGeometry args={[3.2, 3.4, 64]} />
        <meshBasicMaterial color={accent} transparent opacity={0.6} toneMapped={false} />
      </mesh>
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, -0.03, 0]}>
        <circleGeometry args={[3.2, 64]} />
        <meshStandardMaterial color="#0a1620" roughness={0.9} metalness={0.2} />
      </mesh>
      <pointLight color={accent} intensity={40} distance={12} position={[0, 2.5, 0]} />
    </group>
  );
}

/** Rotating 3D showcase of a nation's drone and carrier for the country select screen. */
export function ArmoryViewer({ country }: { country: CountryCode }) {
  return (
    <Canvas camera={{ position: [8.5, 5.2, 9.8], fov: 36 }} onCreated={({ camera }) => camera.lookAt(0, 1.7, 0)} dpr={[1, 1.5]} gl={{ antialias: true, alpha: true }} style={{ background: 'transparent' }}>
      <ambientLight intensity={0.45} />
      <directionalLight position={[6, 9, 5]} intensity={2.2} color="#e8f4ff" />
      <directionalLight position={[-6, 3, -4]} intensity={0.8} color="#5ef2ff" />
      <spotLight position={[0, 12, 0]} angle={0.5} penumbra={0.6} intensity={120} color="#ffffff" />
      <Turntable country={country} />
    </Canvas>
  );
}
