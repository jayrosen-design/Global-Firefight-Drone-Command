'use client';
import { useEffect, useMemo, useState } from 'react';
import { Canvas, useFrame } from '@react-three/fiber';
import { useGame } from '@/store/gameStore';
import { useTelemetry } from '@/store/telemetryStore';
import { haversineKm } from '@/lib/geo/wgs84';
import { terrainHeight, toLocal } from '@/lib/tactical/local';
import { Terrain, Trees, useTerrainGeometry } from './Terrain';
import { TacticalFires, type LocalFire } from './TacticalFires';
import { Structures, type LocalStructure } from './Structures';
import { Civilians, type Civilian } from './Civilians';
import { DroneController } from './DroneController';

function Simulation() {
  const tick = useGame((s) => s.tick);
  useFrame((_, dt) => tick(dt));
  return null;
}

/**
 * Third-person tactical view centred on the controlled drone's target fire.
 * Nearby scenario fires, protected structures and civilians are projected into
 * a local metre-scale arena. Vision modes restyle every layer.
 */
export function TacticalScene() {
  const droneId = useGame((s) => s.tacticalDroneId);
  const drone = useGame((s) => s.drones.find((d) => d.id === droneId));
  const scenario = useGame((s) => s.scenario);
  const vision = useGame((s) => s.visionMode);
  const windDeg = useGame((s) => s.windDirectionDeg);
  const tacticalDrop = useGame((s) => s.tacticalDrop);
  const allFires = useGame((s) => s.fires);

  const origin = useMemo(() => (drone ? drone.destination : { lat: 0, lon: 0 }), [drone]);
  const seed = useMemo(() => Math.abs(Math.round(origin.lat * 13 + origin.lon * 7)) % 1000, [origin]);
  const geometry = useTerrainGeometry(seed);

  const fires = useMemo<LocalFire[]>(() => {
    return allFires
      .filter((f) => haversineKm(origin, { lat: f.lat, lon: f.lon }) < 30)
      .map((f) => {
        const [x, z] = toLocal(origin, { lat: f.lat, lon: f.lon });
        return { fire: f, x, y: terrainHeight(x, z, seed), z };
      })
      .sort((a, b) => haversineKm(origin, { lat: a.fire.lat, lon: a.fire.lon }) - haversineKm(origin, { lat: b.fire.lat, lon: b.fire.lon }));
    // Only recompute when the set of fires changes, not on every FRP tick.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [origin, seed, allFires.length, allFires.map((f) => f.extinguished).join()]);

  const structures = useMemo<LocalStructure[]>(() => {
    if (!scenario) return [];
    return scenario.objectives
      .filter((o) => o.protect)
      .map((o) => {
        const [x, z] = toLocal(origin, { lat: o.protect!.lat, lon: o.protect!.lon });
        return { id: o.id, label: o.protect!.label, x, z, valueUSD: o.protect!.valueUSD, critical: true };
      });
  }, [scenario, origin]);

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
  const bg = vision === 'standard' ? '#3a2a24' : '#000000';
  return (
    <Canvas className="absolute inset-0" camera={{ position: [0, 300, 900], fov: 60, near: 0.5, far: 12000 }} gl={{ antialias: true, powerPreference: 'high-performance' }} dpr={[1, 1.5]} shadows={false}>
      <color attach="background" args={[bg]} />
      <fog attach="fog" args={[vision === 'standard' ? '#6b4a3a' : vision === 'ir' ? '#050505' : '#00161c', 400, vision === 'standard' ? 3800 : 5200]} />
      <ambientLight intensity={vision === 'ir' ? 0.9 : 0.35} color={vision === 'ir' ? '#ffffff' : '#ffd0a0'} />
      <hemisphereLight args={[vision === 'standard' ? '#ffb27a' : '#666', vision === 'standard' ? '#2a1a12' : '#000', vision === 'lidar' ? 0.2 : 0.8]} />
      <directionalLight position={[800, 900, -400]} intensity={vision === 'standard' ? 1.4 : 0.6} color={vision === 'standard' ? '#ffb070' : '#ffffff'} />
      <Terrain geometry={geometry} vision={vision} />
      <Trees seed={seed} vision={vision} />
      <TacticalFires fires={fires} vision={vision} windDirectionDeg={windDeg} />
      <Structures structures={structures} seed={seed} vision={vision} />
      <Civilians civilians={civilians} seed={seed} vision={vision} />
      <DroneController drone={drone} fires={fires} civilians={civilians} setCivilians={setCivilians} seed={seed} vision={vision} onDrop={onDrop} />
      <Simulation />
    </Canvas>
  );
}
