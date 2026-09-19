'use client';
import { useEffect, useRef } from 'react';
import { useFrame, useThree } from '@react-three/fiber';
import { OrbitControls } from '@react-three/drei';
import type { OrbitControls as OrbitControlsImpl } from 'three-stdlib';
import { Vector3 } from 'three';
import { latLonToVector3 } from '@/lib/geo/wgs84';
import { useGame } from '@/store/gameStore';

/** Orbit camera that flies smoothly to scenario centres and selected entities. */
export function CameraRig() {
  const controls = useRef<OrbitControlsImpl>(null);
  const { camera } = useThree();
  const request = useGame((s) => s.cameraRequest);
  const target = useRef<Vector3 | null>(null);

  useEffect(() => {
    if (!request) return;
    target.current = latLonToVector3(request.lat, request.lon, 0).normalize().multiplyScalar(request.distance);
  }, [request]);

  useFrame((_, dt) => {
    const t = target.current;
    if (t) {
      const k = 1 - Math.exp(-dt * 3.2);
      camera.position.lerp(t, k);
      // Keep the camera outside the globe during the slerp-ish lerp.
      const minR = 1.012;
      if (camera.position.length() < minR) camera.position.setLength(minR);
      if (camera.position.distanceTo(t) < 0.002) target.current = null;
    }
    const c = controls.current;
    if (c) {
      const d = camera.position.length();
      c.rotateSpeed = Math.max(0.08, Math.min(0.8, (d - 1) * 0.45));
      c.update();
    }
  });

  return <OrbitControls ref={controls} enablePan={false} minDistance={1.012} maxDistance={5.5} enableDamping dampingFactor={0.08} zoomSpeed={0.6} />;
}
