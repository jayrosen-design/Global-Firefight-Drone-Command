'use client';
import { useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { AdditiveBlending, BufferAttribute, BufferGeometry, Color, Group, NormalBlending, Points, ShaderMaterial } from 'three';
import { intensity, type Fire } from '@/lib/engine/fire';
import type { VisionMode } from '@/store/gameStore';
import { useGame } from '@/store/gameStore';

export interface LocalFire {
  fire: Fire;
  x: number;
  y: number;
  z: number;
}

const FLAME_VERT = /* glsl */ `
attribute float aSeed;
attribute float aSize;
uniform float uTime;
uniform float uRadius;
uniform float uHeight;
uniform vec2 uWind;
varying float vLife;
void main() {
  float life = fract(uTime * (0.35 + aSeed * 0.5) + aSeed * 7.0);
  vLife = life;
  float ang = aSeed * 6.2831 + uTime * 0.4;
  float r = uRadius * (1.0 - life * 0.6) * (0.3 + fract(aSeed * 13.7) * 0.7);
  vec3 p = position;
  p.x += cos(ang) * r + uWind.x * life * life * 60.0;
  p.z += sin(ang) * r + uWind.y * life * life * 60.0;
  p.y += life * uHeight + sin(uTime * 3.0 + aSeed * 20.0) * 3.0;
  vec4 mv = modelViewMatrix * vec4(p, 1.0);
  gl_PointSize = aSize * (1.0 - life * 0.5) * (600.0 / -mv.z);
  gl_Position = projectionMatrix * mv;
}`;
const FLAME_FRAG = /* glsl */ `
uniform vec3 uCold;
uniform vec3 uHot;
uniform float uWhiteHot;
varying float vLife;
void main() {
  float d = length(gl_PointCoord - 0.5) * 2.0;
  if (d > 1.0) discard;
  float a = (1.0 - d) * (1.0 - vLife);
  vec3 col = mix(uHot, uCold, vLife);
  col = mix(col, vec3(1.0), uWhiteHot);
  gl_FragColor = vec4(col, a * (0.6 + uWhiteHot * 0.4));
}`;

function FlameSystem({ lf, vision, windDir }: { lf: LocalFire; vision: VisionMode; windDir: [number, number] }) {
  const inten = intensity(lf.fire);
  const count = 60 + Math.round(inten * 260);
  const geometry = useMemo(() => {
    const g = new BufferGeometry();
    const pos = new Float32Array(count * 3);
    const seed = new Float32Array(count);
    const size = new Float32Array(count);
    for (let i = 0; i < count; i++) {
      seed[i] = Math.random();
      size[i] = 8 + Math.random() * 16;
    }
    g.setAttribute('position', new BufferAttribute(pos, 3));
    g.setAttribute('aSeed', new BufferAttribute(seed, 1));
    g.setAttribute('aSize', new BufferAttribute(size, 1));
    return g;
  }, [count]);
  const uniforms = useMemo(
    () => ({
      uTime: { value: 0 },
      uRadius: { value: 20 },
      uHeight: { value: 60 },
      uWind: { value: windDir },
      uCold: { value: new Color('#a01a05') },
      uHot: { value: new Color('#ffd27a') },
      uWhiteHot: { value: 0 },
    }),
    [windDir],
  );
  const smokeUniforms = useMemo(
    () => ({
      uTime: { value: 0 },
      uRadius: { value: 30 },
      uHeight: { value: 260 },
      uWind: { value: windDir },
      uCold: { value: new Color('#2a2a2e') },
      uHot: { value: new Color('#55555c') },
      uWhiteHot: { value: 0 },
    }),
    [windDir],
  );
  const ref = useRef<Points>(null);
  useFrame(({ clock }) => {
    const live = useGame.getState().fires.find((f) => f.id === lf.fire.id) ?? lf.fire;
    const it = intensity(live);
    uniforms.uTime.value = clock.elapsedTime;
    smokeUniforms.uTime.value = clock.elapsedTime * 0.35;
    uniforms.uRadius.value = 12 + it * 55;
    uniforms.uHeight.value = 30 + it * 90;
    smokeUniforms.uRadius.value = 20 + it * 70;
    smokeUniforms.uHeight.value = 120 + it * 320;
    uniforms.uWhiteHot.value = vision === 'ir' ? 1 : 0;
    if (ref.current) ref.current.visible = !live.extinguished;
  });
  if (lf.fire.extinguished) return null;
  return (
    <group position={[lf.x, lf.y, lf.z]}>
      <points ref={ref} geometry={geometry} frustumCulled={false}>
        <shaderMaterial vertexShader={FLAME_VERT} fragmentShader={FLAME_FRAG} uniforms={uniforms} transparent depthWrite={false} blending={AdditiveBlending} />
      </points>
      {vision === 'standard' && (
        <points geometry={geometry} frustumCulled={false}>
          <shaderMaterial vertexShader={FLAME_VERT} fragmentShader={FLAME_FRAG} uniforms={smokeUniforms} transparent depthWrite={false} blending={NormalBlending} />
        </points>
      )}
      {vision === 'lidar' && (
        <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 2, 0]}>
          <ringGeometry args={[uniforms.uRadius.value, uniforms.uRadius.value + 4, 40]} />
          <meshBasicMaterial color="#ff5a1f" transparent opacity={0.8} />
        </mesh>
      )}
      <pointLight color={vision === 'ir' ? '#ffffff' : '#ff7a2a'} intensity={vision === 'lidar' ? 0 : 3000 + inten * 20000} distance={300 + inten * 500} decay={2} position={[0, 20, 0]} />
    </group>
  );
}

export function TacticalFires({ fires, vision, windDirectionDeg }: { fires: LocalFire[]; vision: VisionMode; windDirectionDeg: number }) {
  const windDir = useMemo<[number, number]>(() => {
    const a = ((windDirectionDeg + 180) * Math.PI) / 180; // direction the wind blows toward
    return [Math.sin(a), -Math.cos(a)];
  }, [windDirectionDeg]);
  const group = useRef<Group>(null);
  return (
    <group ref={group}>
      {fires.map((lf) => (
        <FlameSystem key={lf.fire.id} lf={lf} vision={vision} windDir={windDir} />
      ))}
    </group>
  );
}
