import type { Objective } from '@/lib/config/scenarios';
import { haversineKm } from '@/lib/geo/wgs84';
import type { Fire } from './fire';
import type { Ledger } from './economics';

/** The fire closest to a protect-objective's structure — the one the player must put out. */
export function threateningFire(o: Objective, fires: Fire[]): Fire | undefined {
  if (!o.protect) return undefined;
  const p = o.protect;
  return [...fires].sort((a, b) => haversineKm(a, p) - haversineKm(b, p))[0];
}

/** Live objective status, shared by the mission tracker and the debrief. */
export function objectiveDone(o: Objective, fires: Fire[], ledger: Ledger): boolean {
  if (o.civilians) return ledger.civiliansRescued >= o.civilians;
  if (o.line) return fires.some((f) => f.contained || f.extinguished);
  if (o.protect) return !!threateningFire(o, fires)?.extinguished;
  return fires.some((f) => f.extinguished);
}

/** How to complete an objective, in one short line. */
export function objectiveHint(o: Objective, fires: Fire[]): string {
  if (o.civilians) return 'Tactical drone view · fly low over civilians, hold R';
  if (o.line) return 'Contain one fire (retardant) or put one out';
  if (o.protect) {
    const f = threateningFire(o, fires);
    return f?.label ? `Put out ${f.label}` : 'Put out the fire marked THREATENS on the globe';
  }
  return 'Put out any fire';
}
