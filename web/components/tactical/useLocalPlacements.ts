'use client';
import { useMemo, useRef, useState } from 'react';
import { useFrame } from '@react-three/fiber';
import { haversineKm, type LatLon } from '@/lib/geo/wgs84';
import type { Fire } from '@/lib/engine/fire';
import type { Scenario } from '@/lib/config/scenarios';
import type { LocalFire } from './TacticalFires';
import type { LocalStructure } from './Structures';
import { useTacticalTerrain } from './TacticalWorld';

/**
 * Projects nearby fires and the scenario's protected structures into the local
 * arena around `origin`, re-settling them onto the terrain periodically while
 * streamed tiles refine. Shared by the tactical drone view and live feed windows.
 */
export function useLocalPlacements(origin: LatLon, scenario: Scenario | null, allFires: Fire[]) {
  const terrain = useTacticalTerrain();
  const rangeKm = terrain.real ? 25 : 30;

  const nearby = useMemo(
    () => allFires.filter((f) => haversineKm(origin, { lat: f.lat, lon: f.lon }) < rangeKm).sort((a, b) => haversineKm(origin, a) - haversineKm(origin, b)),
    // Only recompute when the set of fires changes, not on every FRP tick.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [origin, rangeKm, allFires.length, allFires.map((f) => f.extinguished).join()],
  );

  const place = () =>
    nearby.map<LocalFire>((f) => {
      const v = terrain.project({ lat: f.lat, lon: f.lon });
      return { fire: f, x: v.x, y: v.y, z: v.z };
    });
  const [fires, setFires] = useState<LocalFire[]>(place);
  const [structures, setStructures] = useState<LocalStructure[]>([]);
  const nextSettle = useRef(0);

  useFrame(({ clock }) => {
    if (clock.elapsedTime < nextSettle.current) return;
    nextSettle.current = clock.elapsedTime + (terrain.real ? 1.5 : 30);
    const next = place();
    if (next.length !== fires.length || next.some((n, i) => Math.abs(n.y - fires[i].y) > 0.5 || n.fire.id !== fires[i].fire.id)) setFires(next);
    if (scenario) {
      const s = scenario.objectives
        .filter((o) => o.protect && haversineKm(origin, o.protect) < rangeKm)
        .map<LocalStructure>((o) => {
          const v = terrain.project({ lat: o.protect!.lat, lon: o.protect!.lon });
          return { id: o.id, label: o.protect!.label, x: v.x, y: v.y, z: v.z, valueUSD: o.protect!.valueUSD, critical: true };
        });
      if (s.length !== structures.length || s.some((n, i) => Math.abs(n.y - structures[i].y) > 0.5)) setStructures(s);
    }
  });

  return { fires, structures, rangeKm };
}
