'use client';
import { useMemo } from 'react';
import { AdditiveBlending, BufferAttribute, BufferGeometry } from 'three';

export function Stars({ count = 2500 }: { count?: number }) {
  const geometry = useMemo(() => {
    const pos = new Float32Array(count * 3);
    for (let i = 0; i < count; i++) {
      const r = 40 + Math.random() * 20;
      const u = Math.random() * 2 - 1;
      const th = Math.random() * Math.PI * 2;
      const s = Math.sqrt(1 - u * u);
      pos[i * 3] = r * s * Math.cos(th);
      pos[i * 3 + 1] = r * u;
      pos[i * 3 + 2] = r * s * Math.sin(th);
    }
    const g = new BufferGeometry();
    g.setAttribute('position', new BufferAttribute(pos, 3));
    return g;
  }, [count]);
  return (
    <points geometry={geometry} raycast={() => null}>
      <pointsMaterial size={0.09} color="#9ecfff" transparent opacity={0.7} sizeAttenuation blending={AdditiveBlending} depthWrite={false} />
    </points>
  );
}
