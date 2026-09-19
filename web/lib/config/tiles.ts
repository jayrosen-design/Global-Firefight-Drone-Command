/**
 * Real-world 3D map source selection, mirroring God Eye's map startup route
 * (src/maps/google3d.js → selectMapStartupRoute):
 *
 *  google-direct  NEXT_PUBLIC_GOOGLE_MAPS_API_KEY  → Google Photorealistic 3D Tiles (Map Tiles API)
 *  google-ion     NEXT_PUBLIC_CESIUM_ION_TOKEN     → the same Google tiles hosted by Cesium ion (asset 2275207)
 *  keyless        (no credentials)                 → globe: Re:Earth / Mapterhorn quantized-mesh terrain (CC BY 4.0)
 *                                                    draped with Esri World Imagery — God Eye's keyless globe;
 *                                                    tactical drone view: procedural 3D terrain
 *  off            NEXT_PUBLIC_DISABLE_3D_TILES=1   → vector basemap globe + procedural tactical terrain only
 *
 * Both credentials are client-exposed by design (they are used in the browser);
 * restrict them by HTTP referrer / API scope in the provider console.
 */
export type MapRoute =
  | { kind: 'google-direct'; apiToken: string }
  | { kind: 'google-ion'; apiToken: string; assetId: string }
  | { kind: 'keyless'; terrainUrl: string; imageryUrl: string }
  | { kind: 'off' };

export const KEYLESS_TERRAIN_URL = 'https://terrain.reearth.land/cesium-mesh/ellipsoid/';
export const KEYLESS_IMAGERY_URL = 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}';
export const GOOGLE_ION_ASSET_ID = '2275207';

export function selectMapRoute(): MapRoute {
  if (process.env.NEXT_PUBLIC_DISABLE_3D_TILES === '1') return { kind: 'off' };
  const google = (process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY ?? '').trim();
  if (google) return { kind: 'google-direct', apiToken: google };
  const ion = (process.env.NEXT_PUBLIC_CESIUM_ION_TOKEN ?? '').trim();
  if (ion) return { kind: 'google-ion', apiToken: ion, assetId: process.env.NEXT_PUBLIC_CESIUM_ION_ASSET_ID ?? GOOGLE_ION_ASSET_ID };
  return { kind: 'keyless', terrainUrl: KEYLESS_TERRAIN_URL, imageryUrl: KEYLESS_IMAGERY_URL };
}

export const MAP_ROUTE = selectMapRoute();
export const HAS_3D_TILES = MAP_ROUTE.kind !== 'off';
/** The tactical drone view only streams photorealistic tiles when a Google / ion credential is set; keyless uses procedural terrain. */
export const HAS_TACTICAL_TILES = MAP_ROUTE.kind === 'google-direct' || MAP_ROUTE.kind === 'google-ion';

export const MAP_ROUTE_LABEL: Record<MapRoute['kind'], string> = {
  'google-direct': 'GOOGLE 3D TILES',
  'google-ion': 'GOOGLE 3D · CESIUM ION',
  keyless: 'KEYLESS TERRAIN · ESRI IMAGERY',
  off: 'VECTOR BASEMAP',
};

/** Raster basemap for the `off` route: NASA GIBS Blue Marble (no key, CORS enabled). */
export const BASEMAP_TILE_URL =
  process.env.NEXT_PUBLIC_BASEMAP_TILE_URL ??
  'https://gibs.earthdata.nasa.gov/wmts/epsg3857/best/BlueMarble_ShadedRelief_Bathymetry/default/GoogleMapsCompatible_Level8/{z}/{y}/{x}.jpg';
