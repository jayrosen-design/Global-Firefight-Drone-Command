import { FLEETS, type CountryCode } from '@/lib/config/fleets';
import type { LatLon } from '@/lib/geo/wgs84';

export type DroneState = 'enroute' | 'onstation' | 'suppressing' | 'returning' | 'landed';

export interface DroneUnit {
  id: string;
  callSign: string;
  country: CountryCode;
  carrierId: string;
  targetFireId: string;
  state: DroneState;
  /** Departure point (carrier position at launch). */
  origin: LatLon;
  /** Destination (fire position). */
  destination: LatLon;
  /** Progress along the great-circle trajectory 0..1 (enroute) or 1..0 (returning). */
  t: number;
  /** Trajectory length in km. */
  distanceKm: number;
  battery: number; // 0..100
  payloadLitres: number;
  payloadMax: number;
  flightTimeSec: number;
  /** Orbit angle when on station (radians). */
  orbit: number;
  /** Time until the next automatic drop (sim seconds). */
  dropCooldown: number;
  /** Index within a swarm sortie (0..n-1) for formation offsets. */
  swarmIndex: number;
  swarmSize: number;
  drops: number;
}

const PREFIXES = ['GUARDIAN', 'EMBER', 'HAWK', 'ANGEL', 'RESCUE', 'EAGLE', 'TALON', 'PHOENIX'];
let seq = 0;

export function createDrone(
  country: CountryCode,
  carrierId: string,
  origin: LatLon,
  targetFireId: string,
  destination: LatLon,
  distanceKm: number,
  swarmIndex: number,
  swarmSize: number,
): DroneUnit {
  const spec = FLEETS[country].drone;
  seq++;
  return {
    id: `drone-${country}-${seq}`,
    callSign: `${PREFIXES[seq % PREFIXES.length]}-${String((seq % 89) + 10)}`,
    country,
    carrierId,
    targetFireId,
    state: 'enroute',
    origin,
    destination,
    t: 0,
    distanceKm,
    battery: 100,
    payloadLitres: spec.payloadLitres,
    payloadMax: spec.payloadLitres,
    flightTimeSec: 0,
    orbit: (swarmIndex / swarmSize) * Math.PI * 2,
    dropCooldown: 4 + swarmIndex * 1.5,
    swarmIndex,
    swarmSize,
    drops: 0,
  };
}

/** Fraction of the trajectory covered per simulated second at cruise speed. */
export function progressRate(d: DroneUnit) {
  const kmh = FLEETS[d.country].drone.cruiseKmh;
  return kmh / 3600 / Math.max(1, d.distanceKm);
}

export function batteryDrainPerSec(d: DroneUnit) {
  const endurance = FLEETS[d.country].drone.enduranceMin * 60;
  const mult = d.state === 'suppressing' ? 1.6 : d.state === 'onstation' ? 1.2 : 1;
  return (100 / endurance) * mult;
}
