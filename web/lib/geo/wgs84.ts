/**
 * WGS84 <-> globe-space helpers.
 *
 * The God Eye globe is the WGS84 ellipsoid scaled so one unit ≈ the mean Earth
 * radius. +Y is the north pole, +X passes through the prime meridian and
 * longitude increases toward −Z (east). ECEF ↔ globe space is the rotation
 * (X, Y, Z) → (X, Z, −Y), which is what the 3D Tiles group is wrapped in. All game-space math funnels through
 * these helpers so the fire, drone and trajectory layers agree on placement.
 */
import { Vector3 } from 'three';

export const GLOBE_RADIUS = 1;
export const EARTH_RADIUS_KM = 6371.0088;
export const KM_PER_UNIT = EARTH_RADIUS_KM / GLOBE_RADIUS;

export interface LatLon {
  lat: number;
  lon: number;
}

const DEG = Math.PI / 180;

// WGS84 ellipsoid (metres). Globe space = ECEF rotated so +Y is the pole: (X, Z, −Y) / (EARTH_RADIUS_KM·1000)
export const WGS84_A = 6378137.0;
export const WGS84_F = 1 / 298.257223563;
export const WGS84_B = WGS84_A * (1 - WGS84_F);
const E2 = 1 - (WGS84_B * WGS84_B) / (WGS84_A * WGS84_A);
const M_PER_UNIT = EARTH_RADIUS_KM * 1000;

/** Geodetic lat/lon (degrees) + height (metres) → ECEF metres (X through Greenwich, Z through the north pole). */
export function latLonToEcef(lat: number, lon: number, heightM = 0, out = new Vector3()): Vector3 {
  const φ = lat * DEG, λ = lon * DEG;
  const sφ = Math.sin(φ), cφ = Math.cos(φ);
  const N = WGS84_A / Math.sqrt(1 - E2 * sφ * sφ);
  out.set((N + heightM) * cφ * Math.cos(λ), (N + heightM) * cφ * Math.sin(λ), (N * (1 - E2) + heightM) * sφ);
  return out;
}

/** ECEF metres → geodetic (Bowring's closed form, sub-millimetre for terrestrial points). */
export function ecefToLatLon(x: number, y: number, z: number): LatLon & { heightM: number } {
  const lon = Math.atan2(y, x);
  const p = Math.hypot(x, y);
  const ep2 = (WGS84_A * WGS84_A - WGS84_B * WGS84_B) / (WGS84_B * WGS84_B);
  const θ = Math.atan2(z * WGS84_A, p * WGS84_B);
  const sθ = Math.sin(θ), cθ = Math.cos(θ);
  const lat = Math.atan2(z + ep2 * WGS84_B * sθ * sθ * sθ, p - E2 * WGS84_A * cθ * cθ * cθ);
  const sφ = Math.sin(lat);
  const N = WGS84_A / Math.sqrt(1 - E2 * sφ * sφ);
  const heightM = p / Math.cos(lat) - N;
  return { lat: lat / DEG, lon: lon / DEG, heightM };
}

/** ECEF metres → globe space (unit ≈ Earth radius). */
export function ecefToGlobe(e: Vector3, out = new Vector3()): Vector3 {
  return out.set(e.x / M_PER_UNIT, e.z / M_PER_UNIT, -e.y / M_PER_UNIT);
}

/** Globe space → ECEF metres. */
export function globeToEcef(g: Vector3, out = new Vector3()): Vector3 {
  return out.set(g.x * M_PER_UNIT, -g.z * M_PER_UNIT, g.y * M_PER_UNIT);
}

const _ecef = new Vector3();

/** Convert geodetic lat/lon (degrees) + altitude (km) to a point in globe space on the WGS84 ellipsoid. */
export function latLonToVector3(lat: number, lon: number, altitudeKm = 0, out = new Vector3()): Vector3 {
  latLonToEcef(lat, lon, altitudeKm * 1000, _ecef);
  return ecefToGlobe(_ecef, out);
}

/** Inverse of latLonToVector3 (geodetic; altitude ignored). */
export function vector3ToLatLon(v: Vector3): LatLon {
  globeToEcef(v, _ecef);
  const { lat, lon } = ecefToLatLon(_ecef.x, _ecef.y, _ecef.z);
  return { lat, lon: ((lon + 540) % 360) - 180 };
}

/** Great-circle distance in kilometres (haversine). */
export function haversineKm(a: LatLon, b: LatLon): number {
  const dLat = (b.lat - a.lat) * DEG;
  const dLon = (b.lon - a.lon) * DEG;
  const s =
    Math.sin(dLat / 2) ** 2 + Math.cos(a.lat * DEG) * Math.cos(b.lat * DEG) * Math.sin(dLon / 2) ** 2;
  return 2 * EARTH_RADIUS_KM * Math.asin(Math.min(1, Math.sqrt(s)));
}

/** Initial bearing (degrees, 0 = north, clockwise) from a to b along the great circle. */
export function bearingDeg(a: LatLon, b: LatLon): number {
  const y = Math.sin((b.lon - a.lon) * DEG) * Math.cos(b.lat * DEG);
  const x =
    Math.cos(a.lat * DEG) * Math.sin(b.lat * DEG) -
    Math.sin(a.lat * DEG) * Math.cos(b.lat * DEG) * Math.cos((b.lon - a.lon) * DEG);
  return ((Math.atan2(y, x) / DEG) + 360) % 360;
}

/** Spherical interpolation along the great circle between a and b at fraction t∈[0,1]. */
export function greatCirclePoint(a: LatLon, b: LatLon, t: number): LatLon {
  const φ1 = a.lat * DEG, λ1 = a.lon * DEG;
  const φ2 = b.lat * DEG, λ2 = b.lon * DEG;
  const d =
    2 *
    Math.asin(
      Math.sqrt(
        Math.sin((φ2 - φ1) / 2) ** 2 + Math.cos(φ1) * Math.cos(φ2) * Math.sin((λ2 - λ1) / 2) ** 2,
      ),
    );
  if (d < 1e-9) return { ...a };
  const A = Math.sin((1 - t) * d) / Math.sin(d);
  const B = Math.sin(t * d) / Math.sin(d);
  const x = A * Math.cos(φ1) * Math.cos(λ1) + B * Math.cos(φ2) * Math.cos(λ2);
  const y = A * Math.cos(φ1) * Math.sin(λ1) + B * Math.cos(φ2) * Math.sin(λ2);
  const z = A * Math.sin(φ1) + B * Math.sin(φ2);
  return { lat: Math.atan2(z, Math.sqrt(x * x + y * y)) / DEG, lon: Math.atan2(y, x) / DEG };
}

/** Local east/north/up frame at a surface point, for orienting models flush to the globe. */
export function surfaceFrame(lat: number, lon: number) {
  const up = latLonToVector3(lat, lon).normalize();
  const north = latLonToVector3(Math.min(lat + 0.01, 89.99), lon).normalize().sub(up).normalize();
  const east = new Vector3().crossVectors(north, up).normalize();
  north.crossVectors(up, east).normalize();
  return { up, north, east };
}
