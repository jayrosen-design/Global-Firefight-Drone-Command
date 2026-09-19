'use client';
import { useMemo } from 'react';
import { BackSide, Color, Ray, Vector3 } from 'three';
import type { ThreeEvent } from '@react-three/fiber';
import { GLOBE_RADIUS, vector3ToLatLon } from '@/lib/geo/wgs84';
import { buildEarthTexture } from '@/lib/geo/earthTexture';
import { useGame } from '@/store/gameStore';
import { TileBasemap } from './TileBasemap';

const ATMO_VERT = /* glsl */ `
varying vec3 vNormal;
varying vec3 vView;
void main() {
  vNormal = normalize(normalMatrix * normal);
  vec4 mv = modelViewMatrix * vec4(position, 1.0);
  vView = normalize(-mv.xyz);
  gl_Position = projectionMatrix * mv;
}`;
const ATMO_FRAG = /* glsl */ `
uniform vec3 uColor;
varying vec3 vNormal;
varying vec3 vView;
void main() {
  float rim = pow(1.0 - abs(dot(vNormal, vView)), 3.2);
  gl_FragColor = vec4(uColor, rim * 0.85);
}`;

/**
 * God Eye globe adapter: a WGS84 unit sphere with a dark tactical basemap,
 * fresnel atmosphere and optional streamed raster tiles. All click handling
 * on the bare globe (carrier placement, carrier MOVE orders) lives here.
 */
/** Ray ∩ sphere of radius r about the origin (closest hit), or null. */
function hitSphere(ray: Ray, r: number, out: Vector3): Vector3 | null {
  const o = ray.origin, d = ray.direction;
  const b = o.dot(d);
  const c = o.lengthSq() - r * r;
  const disc = b * b - c;
  if (disc < 0) return null;
  const t = -b - Math.sqrt(disc);
  if (t < 0) return null;
  return out.copy(d).multiplyScalar(t).add(o);
}

export function Globe({ tilesActive = false }: { tilesActive?: boolean }) {
  const texture = useMemo(() => buildEarthTexture(), []);
  const atmoUniforms = useMemo(() => ({ uColor: { value: new Color('#1f8fd8') } }), []);

  const onClick = (e: ThreeEvent<MouseEvent>) => {
    e.stopPropagation();
    const st = useGame.getState();
    // With streamed tiles the pick point comes from the sunken fallback sphere; re-intersect at the true radius.
    const p = hitSphere(e.ray, GLOBE_RADIUS + 0.0005, new Vector3()) ?? e.point;
    const ll = vector3ToLatLon(p);
    if (st.placingCarrier) return st.placeCarrierAt(ll);
    if (st.selection?.type === 'carrier' && st.moveArmed) {
      st.moveCarrier(st.selection.id, ll);
      st.setMoveArmed(false);
      return;
    }
    if (st.selection) st.select(null);
  };

  return (
    <group>
      <mesh onClick={onClick} onPointerMissed={() => undefined} scale={tilesActive ? 0.9965 : 1}>
        <sphereGeometry args={[GLOBE_RADIUS, 128, 96]} />
        <meshStandardMaterial map={texture} roughness={0.85} metalness={0.05} emissive={new Color('#06141f')} emissiveIntensity={0.5} />
      </mesh>
      {!tilesActive && <TileBasemap />}
      {/* Inner rim glow */}
      <mesh scale={1.004}>
        <sphereGeometry args={[GLOBE_RADIUS, 96, 64]} />
        <shaderMaterial vertexShader={ATMO_VERT} fragmentShader={ATMO_FRAG} uniforms={atmoUniforms} transparent depthWrite={false} />
      </mesh>
      {/* Outer atmosphere halo */}
      <mesh scale={1.06}>
        <sphereGeometry args={[GLOBE_RADIUS, 96, 64]} />
        <shaderMaterial vertexShader={ATMO_VERT} fragmentShader={ATMO_FRAG} uniforms={atmoUniforms} transparent depthWrite={false} side={BackSide} />
      </mesh>
    </group>
  );
}
