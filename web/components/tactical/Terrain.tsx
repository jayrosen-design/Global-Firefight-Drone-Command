'use client';
import { useMemo } from 'react';
import { BufferAttribute, BufferGeometry, Color, DoubleSide, InstancedMesh, Object3D, PlaneGeometry } from 'three';
import { ARENA, terrainHeight } from '@/lib/tactical/local';
import type { VisionMode } from '@/store/gameStore';

const SEG = 140;

export function useTerrainGeometry(seed: number) {
  return useMemo(() => {
    const g = new PlaneGeometry(ARENA * 2, ARENA * 2, SEG, SEG);
    g.rotateX(-Math.PI / 2);
    const pos = g.attributes.position as BufferAttribute;
    const colors = new Float32Array(pos.count * 3);
    const lo = new Color('#243b1c'), hi = new Color('#6b6a4e'), rock = new Color('#7d7f83');
    const c = new Color();
    for (let i = 0; i < pos.count; i++) {
      const x = pos.getX(i), z = pos.getZ(i);
      const h = terrainHeight(x, z, seed);
      pos.setY(i, h);
      const t = Math.min(1, Math.max(0, (h + 80) / 300));
      c.copy(lo).lerp(hi, t);
      if (h > 140) c.lerp(rock, Math.min(1, (h - 140) / 100));
      colors[i * 3] = c.r; colors[i * 3 + 1] = c.g; colors[i * 3 + 2] = c.b;
    }
    g.setAttribute('color', new BufferAttribute(colors, 3));
    g.computeVertexNormals();
    return g;
  }, [seed]);
}

export function Terrain({ geometry, vision }: { geometry: BufferGeometry; vision: VisionMode }) {
  if (vision === 'lidar') return <LidarCloud geometry={geometry} />;
  return (
    <mesh geometry={geometry} receiveShadow>
      {vision === 'ir' ? (
        <meshStandardMaterial color="#2b2b2b" roughness={1} />
      ) : (
        <meshStandardMaterial vertexColors roughness={0.95} metalness={0} side={DoubleSide} />
      )}
    </mesh>
  );
}

/** LIDAR point cloud: terrain vertices coloured by elevation (cyan low → green high). */
function LidarCloud({ geometry }: { geometry: BufferGeometry }) {
  const cloud = useMemo(() => {
    const src = geometry.attributes.position as BufferAttribute;
    const g = new BufferGeometry();
    const pos = new Float32Array(src.count * 3);
    const col = new Float32Array(src.count * 3);
    const lo = new Color('#0aa6c9'), hi = new Color('#7dffb3');
    const c = new Color();
    for (let i = 0; i < src.count; i++) {
      pos[i * 3] = src.getX(i); pos[i * 3 + 1] = src.getY(i); pos[i * 3 + 2] = src.getZ(i);
      const t = Math.min(1, Math.max(0, (src.getY(i) + 80) / 300));
      c.copy(lo).lerp(hi, t);
      col[i * 3] = c.r; col[i * 3 + 1] = c.g; col[i * 3 + 2] = c.b;
    }
    g.setAttribute('position', new BufferAttribute(pos, 3));
    g.setAttribute('color', new BufferAttribute(col, 3));
    return g;
  }, [geometry]);
  return (
    <>
      <points geometry={cloud}>
        <pointsMaterial size={9} vertexColors sizeAttenuation transparent opacity={0.9} depthWrite={false} />
      </points>
      <mesh geometry={geometry}>
        <meshBasicMaterial color="#0d5f6e" wireframe transparent opacity={0.18} />
      </mesh>
    </>
  );
}

/** Instanced conifer canopy. In LIDAR mode the canopy renders as a point cloud showing pathways through smoke. */
export function Trees({ seed, vision, count = 2200 }: { seed: number; vision: VisionMode; count?: number }) {
  const { matrices, points } = useMemo(() => {
    const dummy = new Object3D();
    const matrices: Float32Array = new Float32Array(count * 16);
    const points = new Float32Array(count * 3);
    let rnd = seed * 9301 + 49297;
    const r = () => ((rnd = (rnd * 9301 + 49297) % 233280) / 233280);
    for (let i = 0; i < count; i++) {
      const x = (r() - 0.5) * ARENA * 2 * 0.95, z = (r() - 0.5) * ARENA * 2 * 0.95;
      const h = terrainHeight(x, z, seed);
      const s = 8 + r() * 10;
      dummy.position.set(x, h + s * 0.5, z);
      dummy.scale.set(s * 0.45, s, s * 0.45);
      dummy.rotation.y = r() * Math.PI;
      dummy.updateMatrix();
      dummy.matrix.toArray(matrices, i * 16);
      points[i * 3] = x; points[i * 3 + 1] = h + s; points[i * 3 + 2] = z;
    }
    return { matrices, points };
  }, [seed, count]);

  const pointGeo = useMemo(() => {
    const g = new BufferGeometry();
    g.setAttribute('position', new BufferAttribute(points, 3));
    return g;
  }, [points]);

  if (vision === 'lidar') {
    return (
      <points geometry={pointGeo}>
        <pointsMaterial size={14} color="#7dffb3" sizeAttenuation transparent opacity={0.85} depthWrite={false} />
      </points>
    );
  }
  return (
    <instancedMesh
      args={[undefined, undefined, count]}
      ref={(m: InstancedMesh | null) => {
        if (!m) return;
        (m.instanceMatrix.array as Float32Array).set(matrices);
        m.instanceMatrix.needsUpdate = true;
      }}
      frustumCulled={false}
    >
      <coneGeometry args={[1, 1, 6]} />
      <meshStandardMaterial color={vision === 'ir' ? '#3a3a3a' : '#1f4a22'} roughness={1} />
    </instancedMesh>
  );
}
