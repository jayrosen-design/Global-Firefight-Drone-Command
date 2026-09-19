import { FLEETS, type CountryCode } from '@/lib/config/fleets';

export interface CarrierVehicle {
  id: string;
  country: CountryCode;
  label: string;
  lat: number;
  lon: number;
  /** Drones ready to launch. */
  dronesReady: number;
  /** Seconds remaining before the next rearming drone is ready. */
  rearmQueue: number[];
  deployed: boolean;
  /** Ground-move target, if any. Carriers crawl across the globe when ordered to MOVE. */
  moveTarget?: { lat: number; lon: number };
}

let carrierSeq = 0;

export function createCarrier(country: CountryCode, lat: number, lon: number, label?: string): CarrierVehicle {
  const f = FLEETS[country];
  return {
    id: `carrier-${country}-${++carrierSeq}`,
    country,
    label: label ?? f.carrier.model,
    lat,
    lon,
    dronesReady: f.carrier.droneCapacity,
    rearmQueue: [],
    deployed: true,
  };
}

export function stepCarrier(c: CarrierVehicle, dtSim: number) {
  if (c.rearmQueue.length) {
    for (let i = c.rearmQueue.length - 1; i >= 0; i--) {
      c.rearmQueue[i] -= dtSim;
      if (c.rearmQueue[i] <= 0) {
        c.rearmQueue.splice(i, 1);
        c.dronesReady++;
      }
    }
  }
  if (c.moveTarget) {
    // Carriers relocate at ~90 km/h → degrees per second approx.
    const kmh = 90;
    const degPerSec = kmh / 3600 / 111;
    const dLat = c.moveTarget.lat - c.lat;
    const dLon = c.moveTarget.lon - c.lon;
    const d = Math.hypot(dLat, dLon);
    const step = degPerSec * dtSim;
    if (d <= step) {
      c.lat = c.moveTarget.lat;
      c.lon = c.moveTarget.lon;
      c.moveTarget = undefined;
    } else {
      c.lat += (dLat / d) * step;
      c.lon += (dLon / d) * step;
    }
  }
}
