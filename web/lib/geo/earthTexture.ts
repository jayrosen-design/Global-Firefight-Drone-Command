/**
 * Builds the dark tactical basemap for the God Eye globe as an equirectangular
 * canvas texture from Natural Earth (world-atlas, 1:110m) TopoJSON. This is the
 * offline base; when NEXT_PUBLIC_BASEMAP_TILE_URL is set, streamed raster
 * tiles are layered on top (see TileBasemap.tsx).
 */
import { feature } from 'topojson-client';
import type { Topology, GeometryCollection } from 'topojson-specification';
import type { FeatureCollection, Geometry, Position } from 'geojson';
import { CanvasTexture, SRGBColorSpace } from 'three';
import land110 from 'world-atlas/land-110m.json';
import countries110 from 'world-atlas/countries-110m.json';

let cached: CanvasTexture | null = null;

function project(w: number, h: number, [lon, lat]: Position): [number, number] {
  return [((lon + 180) / 360) * w, ((90 - lat) / 180) * h];
}

function tracePolygon(ctx: CanvasRenderingContext2D, w: number, h: number, rings: Position[][]) {
  for (const ring of rings) {
    ring.forEach((p, i) => {
      const [x, y] = project(w, h, p);
      if (i === 0) ctx.moveTo(x, y);
      else ctx.lineTo(x, y);
    });
    ctx.closePath();
  }
}

function traceGeometry(ctx: CanvasRenderingContext2D, w: number, h: number, g: Geometry) {
  if (g.type === 'Polygon') tracePolygon(ctx, w, h, g.coordinates);
  else if (g.type === 'MultiPolygon') for (const poly of g.coordinates) tracePolygon(ctx, w, h, poly);
}

export function buildEarthTexture(width = 4096): CanvasTexture {
  if (cached) return cached;
  const height = width / 2;
  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  const ctx = canvas.getContext('2d')!;

  // Ocean
  const ocean = ctx.createLinearGradient(0, 0, 0, height);
  ocean.addColorStop(0, '#04101c');
  ocean.addColorStop(0.5, '#071a2c');
  ocean.addColorStop(1, '#04101c');
  ctx.fillStyle = ocean;
  ctx.fillRect(0, 0, width, height);

  // Graticule
  ctx.strokeStyle = 'rgba(94,242,255,0.07)';
  ctx.lineWidth = 1;
  for (let lon = -180; lon <= 180; lon += 15) {
    const [x] = project(width, height, [lon, 0]);
    ctx.beginPath(); ctx.moveTo(x, 0); ctx.lineTo(x, height); ctx.stroke();
  }
  for (let lat = -75; lat <= 75; lat += 15) {
    const [, y] = project(width, height, [0, lat]);
    ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(width, y); ctx.stroke();
  }

  // Land
  const landTopo = land110 as unknown as Topology<{ land: GeometryCollection }>;
  const land = feature(landTopo, landTopo.objects.land) as unknown as FeatureCollection;
  ctx.beginPath();
  for (const f of land.features) traceGeometry(ctx, width, height, f.geometry);
  ctx.fillStyle = '#0f2735';
  ctx.fill('evenodd');
  ctx.strokeStyle = 'rgba(120,220,240,0.42)';
  ctx.lineWidth = 1.6;
  ctx.stroke();

  // Country borders
  const cTopo = countries110 as unknown as Topology<{ countries: GeometryCollection }>;
  const countries = feature(cTopo, cTopo.objects.countries) as unknown as FeatureCollection;
  ctx.beginPath();
  for (const f of countries.features) traceGeometry(ctx, width, height, f.geometry);
  ctx.strokeStyle = 'rgba(94,242,255,0.22)';
  ctx.lineWidth = 0.9;
  ctx.stroke();

  const tex = new CanvasTexture(canvas);
  tex.colorSpace = SRGBColorSpace;
  tex.anisotropy = 8;
  cached = tex;
  return tex;
}
