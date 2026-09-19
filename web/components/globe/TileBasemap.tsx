'use client';
import { useEffect, useMemo, useState } from 'react';
import { BufferAttribute, BufferGeometry, SRGBColorSpace, Texture, TextureLoader } from 'three';
import { latLonToVector3 } from '@/lib/geo/wgs84';

import { BASEMAP_TILE_URL } from '@/lib/config/tiles';

const TILE_URL = BASEMAP_TILE_URL;
const ZOOM = 4;

function tile2lon(x: number, z: number) {
  return (x / 2 ** z) * 360 - 180;
}
function tile2lat(y: number, z: number) {
  const n = Math.PI - (2 * Math.PI * y) / 2 ** z;
  return (180 / Math.PI) * Math.atan(0.5 * (Math.exp(n) - Math.exp(-n)));
}
function mercY(lat: number) {
  const r = (lat * Math.PI) / 180;
  return Math.log(Math.tan(Math.PI / 4 + r / 2));
}

/** Sphere patch for one Web-Mercator tile with mercator-correct UVs. */
function tileGeometry(x: number, y: number, z: number, seg = 10): BufferGeometry {
  const lon0 = tile2lon(x, z), lon1 = tile2lon(x + 1, z);
  const lat0 = tile2lat(y + 1, z), lat1 = tile2lat(y, z);
  const my0 = mercY(lat0), my1 = mercY(lat1);
  const pos: number[] = [], uv: number[] = [], idx: number[] = [];
  for (let j = 0; j <= seg; j++) {
    const lat = lat0 + ((lat1 - lat0) * j) / seg;
    const v = (mercY(lat) - my0) / (my1 - my0);
    for (let i = 0; i <= seg; i++) {
      const lon = lon0 + ((lon1 - lon0) * i) / seg;
      const p = latLonToVector3(lat, lon, 2); // 2 km above the base sphere to avoid z-fighting
      pos.push(p.x, p.y, p.z);
      uv.push(i / seg, v);
    }
  }
  for (let j = 0; j < seg; j++) {
    for (let i = 0; i < seg; i++) {
      const a = j * (seg + 1) + i;
      const b = a + seg + 1;
      idx.push(a, b, a + 1, b, b + 1, a + 1);
    }
  }
  const g = new BufferGeometry();
  g.setAttribute('position', new BufferAttribute(new Float32Array(pos), 3));
  g.setAttribute('uv', new BufferAttribute(new Float32Array(uv), 2));
  g.setIndex(idx);
  g.computeVertexNormals();
  return g;
}

/**
 * Streams a raster {z}/{x}/{y} basemap onto the globe (God Eye style). Disabled
 * unless NEXT_PUBLIC_BASEMAP_TILE_URL is configured, in which case the vector
 * basemap remains underneath as a fallback while tiles load.
 */
export function TileBasemap() {
  const tiles = useMemo(() => {
    if (!TILE_URL) return [] as { key: string; x: number; y: number; geometry: BufferGeometry }[];
    const n = 2 ** ZOOM;
    const out = [];
    for (let y = 0; y < n; y++) for (let x = 0; x < n; x++) out.push({ key: `${ZOOM}/${x}/${y}`, x, y, geometry: tileGeometry(x, y, ZOOM) });
    return out;
  }, []);
  const [textures, setTextures] = useState<Record<string, Texture>>({});

  useEffect(() => {
    if (!TILE_URL) return;
    const loader = new TextureLoader();
    loader.setCrossOrigin('anonymous');
    let alive = true;
    for (const t of tiles) {
      const url = TILE_URL.replace('{z}', String(ZOOM)).replace('{x}', String(t.x)).replace('{y}', String(t.y));
      loader.load(url, (tex) => {
        if (!alive) return;
        tex.colorSpace = SRGBColorSpace;
        setTextures((prev) => ({ ...prev, [t.key]: tex }));
      });
    }
    return () => {
      alive = false;
    };
  }, [tiles]);

  if (!TILE_URL) return null;
  return (
    <group>
      {tiles.map((t) =>
        textures[t.key] ? (
          <mesh key={t.key} geometry={t.geometry} raycast={() => null}>
            <meshStandardMaterial map={textures[t.key]} roughness={0.9} transparent opacity={0.85} />
          </mesh>
        ) : null,
      )}
    </group>
  );
}
