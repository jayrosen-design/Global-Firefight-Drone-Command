import { frpToIntensity } from '@/lib/data/nasa-firms';
import type { FirmsHotspot } from '@/lib/data/types';
import type { Scenario } from '@/lib/config/scenarios';

export interface Fire {
  id: string;
  lat: number;
  lon: number;
  label?: string;
  /** Current fire radiative power (MW). Acts as the fire's "health". */
  frp: number;
  /** FRP at ignition, for progress bars. */
  initialFrp: number;
  /** Property value still standing (USD). */
  propertyRemainingUSD: number;
  propertyInitialUSD: number;
  /** People still in the threat zone. */
  populationRemaining: number;
  populationInitial: number;
  extinguished: boolean;
  /** Retardant line laid (halts growth). */
  contained: boolean;
  /** Sim seconds of knock-down left: a fire stops spreading while drones keep working it. */
  holdSec: number;
  /** Seconds burning (sim time). */
  burnTime: number;
  /** Whether this fire came from a scenario preset (vs promoted live hotspot). */
  source: 'scenario' | 'live';
}

export function intensity(f: Fire) {
  return frpToIntensity(f.frp);
}

export function createScenarioFires(s: Scenario): Fire[] {
  const total = s.fires.reduce((a, f) => a + f.frp, 0);
  return s.fires.map((f, i) => {
    const share = f.frp / total;
    return {
      id: `${s.id}-fire-${i}`,
      lat: f.lat,
      lon: f.lon,
      label: f.label,
      frp: f.frp,
      initialFrp: f.frp,
      propertyRemainingUSD: s.propertyAtRiskUSD * share,
      propertyInitialUSD: s.propertyAtRiskUSD * share,
      populationRemaining: Math.round(s.populationAtRisk * share),
      populationInitial: Math.round(s.populationAtRisk * share),
      extinguished: false,
      contained: false,
      holdSec: 0,
      burnTime: 0,
      source: 'scenario',
    };
  });
}

/** Promote a live FIRMS hotspot into an engageable fire, estimating what is at risk from FRP. */
export function fireFromHotspot(h: FirmsHotspot): Fire {
  const inten = frpToIntensity(h.frp);
  const property = 2_000_000 + inten * inten * 900_000_000;
  const population = Math.round(inten * inten * 4000);
  return {
    id: `live-${h.id}`,
    lat: h.latitude,
    lon: h.longitude,
    label: `Hotspot ${h.latitude.toFixed(2)}°, ${h.longitude.toFixed(2)}° · ${h.frp.toFixed(0)} MW`,
    frp: h.frp,
    initialFrp: h.frp,
    propertyRemainingUSD: property,
    propertyInitialUSD: property,
    populationRemaining: population,
    populationInitial: population,
    extinguished: false,
    contained: false,
    holdSec: 0,
    burnTime: 0,
    source: 'live',
  };
}

/**
 * Game-balance tuning, checked with a headless bot across all seven campaigns: clicking the
 * nearest fire puts the first one out within seconds and wins in ~1–5 min; doing nothing loses
 * ~20% of property per minute at 1×.
 */
export const BALANCE = {
  /** Fire growth per sim second (scaled by wind: ×0.5 calm … ×1.5 at 40 mph). */
  growthRate: 0.0002,
  /** Fires stop growing at this multiple of their starting FRP. */
  growthCap: 2,
  /** Sim seconds a fire stops spreading after any drop — keep drones on it. Retardant contains it for good. */
  holdSec: 600,
  /** Property lost per sim second, scaled by intensity. */
  burnRate: 0.00005,
  /** Carrier rearm time = fleet rearmSeconds × this (sim seconds). */
  rearmSimScale: 20,
  /** A fire knocked below this fraction of its starting FRP is out. */
  outFrac: 0.08,
  /** Globe-scale flight speed multiplier on drone cruise speed. */
  droneSpeed: 2,
} as const;

/** Fire growth and damage per simulated second. */
export function stepFire(f: Fire, dtSim: number, windMph: number) {
  if (f.extinguished) return;
  f.burnTime += dtSim;
  f.holdSec = Math.max(0, f.holdSec - dtSim);
  if (!f.contained && f.holdSec <= 0) {
    const windFactor = 0.5 + windMph / 40;
    f.frp = Math.min(f.initialFrp * BALANCE.growthCap, f.frp * (1 + BALANCE.growthRate * windFactor * dtSim));
  }
  // Damage: property burns at a rate proportional to intensity.
  const burnRate = BALANCE.burnRate * (0.4 + intensity(f)); // fraction per sim second (~3 sim-hours to total loss)
  const loss = f.propertyRemainingUSD * burnRate * dtSim;
  f.propertyRemainingUSD = Math.max(0, f.propertyRemainingUSD - loss);
  f.populationRemaining = Math.max(0, f.populationRemaining - f.populationInitial * burnRate * 0.5 * dtSim);
}

/**
 * Apply a suppressant drop. Returns MW of FRP knocked down.
 * 1 litre of water ~ 0.6 MW knock-down at this game scale.
 */
export function applyDrop(f: Fire, litres: number, effectiveness: number, retardantLine: boolean): number {
  if (f.extinguished) return 0;
  const knock = litres * 0.6 * effectiveness;
  const before = f.frp;
  f.frp = Math.max(0, f.frp - knock);
  f.holdSec = BALANCE.holdSec;
  if (retardantLine) f.contained = true;
  if (f.frp <= Math.max(0.5, f.initialFrp * BALANCE.outFrac)) {
    f.frp = 0;
    f.extinguished = true;
  }
  return before - f.frp;
}
