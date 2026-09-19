'use client';
import { useEffect, useMemo, useRef, useState } from 'react';
import { Canvas, useFrame } from '@react-three/fiber';
import { useGame } from '@/store/gameStore';
import { useTelemetry } from '@/store/telemetryStore';
import { haversineKm, type LatLon } from '@/lib/geo/wgs84';
import type { Fire } from '@/lib/engine/fire';
import type { Scenario } from '@/lib/config/scenarios';
import { Terrain, Trees, useTerrainGeometry } from './Terrain';
import { TacticalFires, type LocalFire } from './TacticalFires';
import { Structures, type LocalStructure } from './Structures';
import { Civilians, type Civilian } from './Civilians';
import { DroneController } from './DroneController';
import { TacticalWorld, useTacticalTerrain } from './TacticalWorld';

function Simulation() {
  const tick = useGame((s) => s.tick);
  useFrame((_, dt) => tick(dt));
  return null;
}

/** Procedural terrain fallback (only when no map route is configured). */
function ProceduralGround({ seed, vision }: { seed: number; vision: 'standard' | 'ir' | 'lidar' }) {
  const geometry = useTerrainGeometry(seed);
  return (
    <>
      <Terrain geometry={geometry} vision={vision} />
      <Trees seed={seed} vision={vision} />
    </>
  );
}

/**
 * Everything that needs the terrain provider: fires, structures and civilians
 * are projected from their real coordinates and re-settled onto the streamed
 * surface as tiles load in.
 */
function TacticalContent({ origin, seed, scenario, allFires, droneId }: { origin: LatLon; seed: number; scenario: Scenario | null; allFires: Fire[]; droneId: string }) {
  const terrain = useTacticalTerrain();
  const vision = useGame((s) => s.visionMode);
  const windDeg = useGame((s) => s.windDirectionDeg);
  const tacticalDrop = useGame((s) => s.tacticalDrop);
  const drone = useGame((s) => s.drones.find((d) => d.id === droneId));
  const rangeKm = terrain.real ? 25 : 30;

  const nearby = useMemo(
    () => allFires.filter((f) => haversineKm(origin, { lat: f.lat, lon: f.lon }) < rangeKm).sort((a, b) => haversineKm(origin, a) - haversineKm(origin, b)),
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

  // Re-settle placements periodically while tiles stream (heights change as LOD refines).
  useFrame(({ clock }) => {
    if (clock.elapsedTime < nextSettle.current) return;
    nextSettle.current = clock.elapsedTime + (terrain.real ? 1.5 : 30);
    const next = place();
    if (next.length !== fires.length || next.some((n, i) => Math.abs(n.y - fires[i].y) > 0.5 || n.fire.id !== fires[i].fire.id)) setFires(next);
    if (scenario) {
      const s = scenario.objectives
        .filter((o) => o.protect)
        .map<LocalStructure>((o) => {
          const v = terrain.project({ lat: o.protect!.lat, lon: o.protect!.lon });
          return { id: o.id, label: o.protect!.label, x: v.x, y: v.y, z: v.z, valueUSD: o.protect!.valueUSD, critical: true };
        });
      if (s.length !== structures.length || s.some((n, i) => Math.abs(n.y - structures[i].y) > 0.5)) setStructures(s);
    }
  });

  const [civilians, setCivilians] = useState<Civilian[]>([]);
  useEffect(() => {
    const n = scenario?.objectives.find((o) => o.civilians)?.civilians ?? 4;
    const first = fires[0];
    const cx = first?.x ?? 0, cz = first?.z ?? 0;
    let rnd = seed + 1;
    const r = () => ((rnd = (rnd * 9301 + 49297) % 233280) / 233280);
    setCivilians(Array.from({ length: n }, (_, i) => ({ id: i, x: cx + (r() - 0.5) * 700, z: cz + (r() - 0.5) * 700, rescued: false })));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [scenario?.id, seed]);

  const onDrop = (fireId: string, litres: number) => {
    const knocked = tacticalDrop(fireId, litres, false);
    useTelemetry.getState().set({ lastDropKnockdownMW: knocked });
  };

  if (!drone) return null;
  const photoreal = terrain.real && terrain.ready;
  const bg = vision === 'standard' ? (photoreal ? '#7a5a4a' : terrain.real ? '#0a0f14' : '#3a2a24') : '#000000';
  const fogColor = vision === 'standard' ? (photoreal ? '#8c6a58' : terrain.real ? '#0a0f14' : '#6b4a3a') : vision === 'ir' ? '#050505' : '#00161c';
  return (
    <>
      <color attach="background" args={[bg]} />
      <fog attach="fog" args={[fogColor, terrain.real ? 900 : 400, vision === 'standard' ? (terrain.real ? 9000 : 3800) : 12000]} />
      <ambientLight intensity={vision === 'ir' ? 0.9 : terrain.real ? 0.7 : 0.35} color={vision === 'ir' ? '#ffffff' : '#ffd0a0'} />
      {!terrain.real && <ProceduralGround seed={seed} vision={vision} />}
      <TacticalFires fires={fires} vision={vision} windDirectionDeg={windDeg} />
      <Structures structures={structures} vision={vision} />
      <Civilians civilians={civilians} vision={vision} />
      <DroneController drone={drone} fires={fires} civilians={civilians} setCivilians={setCivilians} vision={vision} onDrop={onDrop} />
    </>
  );
}

/**
 * Third-person tactical view centred on the controlled drone's target fire,
 * rendered over the real-world 3D tiles of that location (God Eye map stack).
 */
export function TacticalScene() {
  const droneId = useGame((s) => s.tacticalDroneId);
  const drone = useGame((s) => s.drones.find((d) => d.id === droneId));
  const scenario = useGame((s) => s.scenario);
  const vision = useGame((s) => s.visionMode);
  const allFires = useGame((s) => s.fires);

  const origin = useMemo(() => (drone ? drone.destination : { lat: 0, lon: 0 }), [drone]);
  const seed = useMemo(() => Math.abs(Math.round(origin.lat * 13 + origin.lon * 7)) % 1000, [origin]);

  if (!drone || !droneId) return null;
  return (
    <Canvas className="absolute inset-0" camera={{ position: [0, 300, 900], fov: 60, near: 0.5, far: 60000 }} gl={{ antialias: true, powerPreference: 'high-performance', logarithmicDepthBuffer: true }} dpr={[1, 1.5]} shadows={false}>
      <hemisphereLight args={[vision === 'standard' ? '#ffb27a' : '#666', vision === 'standard' ? '#2a1a12' : '#000', vision === 'lidar' ? 0.2 : 0.8]} />
      <directionalLight position={[800, 900, -400]} intensity={vision === 'standard' ? 1.4 : 0.6} color={vision === 'standard' ? '#ffb070' : '#ffffff'} />
      <TacticalWorld origin={origin} seed={seed} vision={vision}>
        <TacticalContent origin={origin} seed={seed} scenario={scenario} allFires={allFires} droneId={droneId} />
      </TacticalWorld>
      <Simulation />
    </Canvas>
  );
}
