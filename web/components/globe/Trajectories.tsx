'use client';
import { useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { AdditiveBlending, BufferAttribute, BufferGeometry, Line as ThreeLine, ShaderMaterial } from 'three';
import { trajectoryManager } from '@/lib/geo/trajectory';
import type { DroneUnit } from '@/lib/engine/DroneUnit';
import { useGame } from '@/store/gameStore';

/**
 * White/cyan dispatch arcs. Each arc is a great-circle polyline with a shader
 * that lights the segment already flown in cyan and fades the segment ahead,
 * with a travelling pulse toward the target.
 */
const VERT = /* glsl */ `
attribute float aT;
varying float vT;
void main() {
  vT = aT;
  gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
}`;
const FRAG = /* glsl */ `
uniform float uProgress;
uniform float uTime;
uniform vec3 uFlown;
uniform vec3 uAhead;
varying float vT;
void main() {
  float flown = step(vT, uProgress);
  vec3 col = mix(uAhead, uFlown, flown);
  float pulse = smoothstep(0.06, 0.0, abs(fract(vT * 3.0 - uTime * 0.7) - 0.5) - 0.42);
  float a = mix(0.28, 0.95, flown) + pulse * 0.6;
  gl_FragColor = vec4(col + pulse * 0.4, a);
}`;

function Arc({ drone }: { drone: DroneUnit }) {
  const lineRef = useRef<ThreeLine>(null);
  const uniforms = useMemo(
    () => ({ uProgress: { value: 0 }, uTime: { value: 0 }, uFlown: { value: [0.37, 0.95, 1.0] }, uAhead: { value: [0.95, 0.97, 1.0] } }),
    [],
  );
  const material = useMemo(() => new ShaderMaterial({ vertexShader: VERT, fragmentShader: FRAG, uniforms, transparent: true, depthWrite: false, blending: AdditiveBlending }), [uniforms]);
  const geometry = useMemo(() => {
    const traj = trajectoryManager.build(drone.id, drone.origin, drone.destination);
    const g = new BufferGeometry().setFromPoints(traj.points);
    const t = new Float32Array(traj.points.length);
    for (let i = 0; i < t.length; i++) t[i] = i / (t.length - 1);
    g.setAttribute('aT', new BufferAttribute(t, 1));
    return g;
  }, [drone.id, drone.origin, drone.destination]);

  useFrame(({ clock }) => {
    const live = useGame.getState().drones.find((d) => d.id === drone.id);
    uniforms.uTime.value = clock.elapsedTime;
    uniforms.uProgress.value = live ? (live.state === 'returning' ? 1 : live.t) : 1;
  });

  if (drone.swarmIndex !== 0) return null;
  return <primitive ref={lineRef} object={new ThreeLine(geometry, material)} raycast={() => null} />;
}

export function Trajectories() {
  const drones = useGame((s) => s.drones);
  return (
    <>
      {drones.map((d) => (
        <Arc key={d.id} drone={d} />
      ))}
    </>
  );
}
