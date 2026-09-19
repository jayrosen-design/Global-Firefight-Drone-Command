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
    burnTime: 0,
    source: 'live',
  };
}

/** Fire growth and damage per simulated second. */
export function stepFire(f: Fire, dtSim: number, windMph: number) {
  if (f.extinguished) return;
  f.burnTime += dtSim;
  if (!f.contained) {
    // Growth: up to +0.06%/s scaled by wind; caps at 3x initial FRP.
    const windFactor = 0.5 + windMph / 40;
    f.frp = Math.min(f.initialFrp * 3, f.frp * (1 + 0.0006 * windFactor * dtSim));
  }
  // Damage: property burns at a rate proportional to intensity.
  const burnRate = 0.00008 * (0.4 + intensity(f)); // fraction per second (~1–2 h to total loss)
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
  if (retardantLine) f.contained = true;
  if (f.frp <= Math.max(0.5, f.initialFrp * 0.02)) {
    f.frp = 0;
    f.extinguished = true;
  }
  return before - f.frp;
}
