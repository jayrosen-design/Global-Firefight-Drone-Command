/**
 * TrajectoryManager — great-circle flight paths with a parabolic altitude
 * profile ("arc trajectories" in the pitch deck). Produces a sampled polyline
 * in globe space for rendering and a sampler for animating a drone along it.
 */
import { Vector3 } from 'three';
import { greatCirclePoint, haversineKm, latLonToVector3, type LatLon } from './wgs84';

export interface Trajectory {
  id: string;
  from: LatLon;
  to: LatLon;
  distanceKm: number;
  /** Peak altitude in km above the surface at the midpoint. */
  apexKm: number;
  points: Vector3[];
}

export function apexForDistance(distanceKm: number): number {
  // Short hops stay low; intercontinental ferries arc high for visual clarity.
  return Math.min(1400, 60 + distanceKm * 0.16);
}

export function altitudeAt(t: number, apexKm: number): number {
  // Parabolic: 0 at endpoints, apex at t=0.5
  return apexKm * 4 * t * (1 - t);
}

export class TrajectoryManager {
  private cache = new Map<string, Trajectory>();

  build(id: string, from: LatLon, to: LatLon, segments = 96): Trajectory {
    const key = `${id}:${from.lat.toFixed(4)},${from.lon.toFixed(4)}>${to.lat.toFixed(4)},${to.lon.toFixed(4)}`;
    const hit = this.cache.get(key);
    if (hit) return hit;
    const distanceKm = haversineKm(from, to);
    const apexKm = apexForDistance(distanceKm);
    const points: Vector3[] = [];
    for (let i = 0; i <= segments; i++) {
      const t = i / segments;
      const p = greatCirclePoint(from, to, t);
      points.push(latLonToVector3(p.lat, p.lon, altitudeAt(t, apexKm)));
    }
    const traj = { id, from, to, distanceKm, apexKm, points };
    this.cache.set(key, traj);
    return traj;
  }

  /** Position and heading (forward vector) at fraction t along the trajectory. */
  static sample(traj: Trajectory, t: number, outPos = new Vector3(), outFwd = new Vector3()) {
    const tc = Math.min(1, Math.max(0, t));
    const p = greatCirclePoint(traj.from, traj.to, tc);
    latLonToVector3(p.lat, p.lon, altitudeAt(tc, traj.apexKm), outPos);
    const t2 = Math.min(1, tc + 0.002);
    const q = greatCirclePoint(traj.from, traj.to, t2);
    const next = latLonToVector3(q.lat, q.lon, altitudeAt(t2, traj.apexKm));
    outFwd.copy(next).sub(outPos).normalize();
    return { position: outPos, forward: outFwd, latLon: p, altitudeKm: altitudeAt(tc, traj.apexKm) };
  }

  clear() {
    this.cache.clear();
  }
}

export const trajectoryManager = new TrajectoryManager();
