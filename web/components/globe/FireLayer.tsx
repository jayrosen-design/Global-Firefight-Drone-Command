'use client';
import { useEffect, useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { AdditiveBlending, Color, InstancedBufferAttribute, InstancedMesh, Matrix4, Object3D, ShaderMaterial } from 'three';
import type { ThreeEvent } from '@react-three/fiber';
import { frpToIntensity, hotspotToVector3 } from '@/lib/data/nasa-firms';
import type { FirmsHotspot } from '@/lib/data/types';
import { useGame } from '@/store/gameStore';

/**
 * Instanced fire shader: one camera-facing quad per FIRMS hotspot. Quad size,
 * emissive colour and pulse rate scale with log(FRP). Instances on the far side
 * of the globe are culled in the fragment shader.
 */
const VERT = /* glsl */ `
attribute float aIntensity;
attribute float aPhase;
attribute float aConfidence;
uniform float uTime;
uniform float uBaseSize;
uniform float uCamDist;
varying vec2 vUv;
varying float vIntensity;
varying float vFacing;
varying float vConfidence;
varying float vPulse;
void main() {
  vUv = uv;
  vIntensity = aIntensity;
  vConfidence = aConfidence;
  vec4 world = modelMatrix * instanceMatrix * vec4(0.0, 0.0, 0.0, 1.0);
  vec3 normal = normalize(world.xyz);
  vec3 toCam = normalize(cameraPosition - world.xyz);
  vFacing = dot(normal, toCam);
  float pulse = 0.85 + 0.15 * sin(uTime * (1.5 + aIntensity * 3.0) + aPhase * 6.2831);
  vPulse = pulse;
  float zoom = clamp((uCamDist - 1.0) * 0.6, 0.05, 1.0);
  float size = uBaseSize * zoom * (0.35 + aIntensity * aIntensity * 3.2) * pulse;
  vec4 mv = viewMatrix * world;
  mv.xy += position.xy * size;
  gl_Position = projectionMatrix * mv;
}`;
const FRAG = /* glsl */ `
varying vec2 vUv;
varying float vIntensity;
varying float vFacing;
varying float vConfidence;
varying float vPulse;
void main() {
  if (vFacing < 0.02) discard;
  vec2 d = vUv - 0.5;
  float r = length(d) * 2.0;
  float core = smoothstep(0.55, 0.0, r);
  float glow = smoothstep(1.0, 0.15, r) * 0.55;
  vec3 hot = vec3(1.0, 0.96, 0.75);
  vec3 orange = vec3(1.0, 0.42, 0.08);
  vec3 red = vec3(0.85, 0.12, 0.02);
  vec3 col = mix(red, orange, vIntensity);
  col = mix(col, hot, core * (0.35 + vIntensity * 0.65));
  float a = (core + glow) * (0.45 + vConfidence * 0.55) * vPulse * smoothstep(0.02, 0.25, vFacing);
  gl_FragColor = vec4(col * (1.2 + vIntensity), a);
}`;

export function FireLayer({ hotspots }: { hotspots: FirmsHotspot[] }) {
  const meshRef = useRef<InstancedMesh>(null);
  const uniforms = useMemo(() => ({ uTime: { value: 0 }, uBaseSize: { value: 0.014 }, uCamDist: { value: 3 } }), []);
  const material = useMemo(
    () => new ShaderMaterial({ vertexShader: VERT, fragmentShader: FRAG, uniforms, transparent: true, depthWrite: false, blending: AdditiveBlending }),
    [uniforms],
  );
  const attrs = useMemo(() => {
    const n = hotspots.length;
    const intensity = new Float32Array(n);
    const phase = new Float32Array(n);
    const conf = new Float32Array(n);
    for (let i = 0; i < n; i++) {
      intensity[i] = frpToIntensity(hotspots[i].frp);
      phase[i] = Math.random();
      conf[i] = hotspots[i].confidence / 100;
    }
    return { intensity, phase, conf };
  }, [hotspots]);

  useEffect(() => {
    const m = meshRef.current;
    if (!m) return;
    const dummy = new Object3D();
    const mat = new Matrix4();
    for (let i = 0; i < hotspots.length; i++) {
      hotspotToVector3(hotspots[i], dummy.position);
      dummy.position.multiplyScalar(1.002);
      dummy.updateMatrix();
      mat.copy(dummy.matrix);
      m.setMatrixAt(i, mat);
    }
    m.instanceMatrix.needsUpdate = true;
    m.geometry.setAttribute('aIntensity', new InstancedBufferAttribute(attrs.intensity, 1));
    m.geometry.setAttribute('aPhase', new InstancedBufferAttribute(attrs.phase, 1));
    m.geometry.setAttribute('aConfidence', new InstancedBufferAttribute(attrs.conf, 1));
    m.count = hotspots.length;
    m.frustumCulled = false;
  }, [hotspots, attrs]);

  useFrame(({ clock, camera }) => {
    uniforms.uTime.value = clock.elapsedTime;
    uniforms.uCamDist.value = camera.position.length();
  });

  const onClick = (e: ThreeEvent<MouseEvent>) => {
    if (e.instanceId === undefined) return;
    e.stopPropagation();
    const st = useGame.getState();
    const fire = st.engageHotspot(hotspots[e.instanceId]);
    if (st.selection?.type === 'carrier') st.dispatch(st.selection.id, fire.id);
    else st.select({ type: 'fire', id: fire.id });
  };
  const onOver = (e: ThreeEvent<PointerEvent>) => {
    if (e.instanceId === undefined) return;
    document.body.style.cursor = 'crosshair';
    useGame.getState().setHoverFire(`live-${hotspots[e.instanceId].id}`);
  };
  const onOut = () => {
    document.body.style.cursor = 'default';
    useGame.getState().setHoverFire(null);
  };

  if (!hotspots.length) return null;
  return (
    <instancedMesh ref={meshRef} args={[undefined, material, hotspots.length]} onClick={onClick} onPointerOver={onOver} onPointerOut={onOut}>
      <planeGeometry args={[1, 1]} />
    </instancedMesh>
  );
}

export const FIRE_COLOR = new Color('#ff5a1f');
