/**
 * WGS84 <-> globe-space helpers.
 *
 * The God Eye globe is a unit sphere (radius GLOBE_RADIUS) in Three.js world
 * space. +Y is the north pole, +Z passes through the prime meridian, and
 * longitude increases toward +X (east). All game-space math funnels through
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

/** Convert geodetic lat/lon (degrees) + altitude (km) to a Cartesian point on the globe. */
export function latLonToVector3(lat: number, lon: number, altitudeKm = 0, out = new Vector3()): Vector3 {
  const r = GLOBE_RADIUS + altitudeKm / KM_PER_UNIT;
  const phi = (90 - lat) * DEG;
  const theta = (lon + 180) * DEG;
  out.set(-r * Math.sin(phi) * Math.cos(theta), r * Math.cos(phi), r * Math.sin(phi) * Math.sin(theta));
  return out;
}

/** Inverse of latLonToVector3 (altitude ignored). */
export function vector3ToLatLon(v: Vector3): LatLon {
  const n = v.clone().normalize();
  const lat = 90 - Math.acos(n.y) / DEG;
  const lon = Math.atan2(n.z, -n.x) / DEG - 180;
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
