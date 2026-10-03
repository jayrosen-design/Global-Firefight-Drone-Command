'use client';
import { useMemo, useRef, useState } from 'react';
import { Canvas, useFrame } from '@react-three/fiber';
import { OrbitControls } from '@react-three/drei';
import { Group, Vector3 } from 'three';
import { useGame, type LiveFeed } from '@/store/gameStore';
import { FLEETS } from '@/lib/config/fleets';
import { altitudeAt, apexForDistance } from '@/lib/geo/trajectory';
import { greatCirclePoint, haversineKm } from '@/lib/geo/wgs84';
import type { DroneUnit } from '@/lib/engine/DroneUnit';
import { DroneModel } from '@/components/models/DroneModel';
import { CarrierModel } from '@/components/models/CarrierModel';
import { TacticalWorld, useTacticalTerrain } from '@/components/tactical/TacticalWorld';
import { ProceduralGround } from '@/components/tactical/TacticalScene';
import { TacticalFires } from '@/components/tactical/TacticalFires';
import { Structures } from '@/components/tactical/Structures';
import { useLocalPlacements } from '@/components/tactical/useLocalPlacements';
import type { LocalFire } from '@/components/tactical/TacticalFires';

const ORBIT_RADIUS_M = 260;
const ORBIT_HEIGHT_M = 160;

interface Splash {
  id: number;
  pos: Vector3;
  vel: Vector3;
  colour: string;
  age: number;
  landed: boolean;
}

/** One drone as seen from the feed: true great-circle approach/return, local orbit on station. */
function FeedDrone({ drone, fires, rangeKm }: { drone: DroneUnit; fires: LocalFire[]; rangeKm: number }) {
  const terrain = useTacticalTerrain();
  const g = useRef<Group>(null);
  const prev = useRef(new Vector3());
  useFrame(({ clock }) => {
    const live = useGame.getState().drones.find((d) => d.id === drone.id);
    const node = g.current;
    if (!live || !node) return;
    const remainingKm = (1 - live.t) * live.distanceKm;
    node.visible = remainingKm < rangeKm;
    if (!node.visible) return;
    const target = fires.find((f) => f.fire.id === live.targetFireId);
    let p: Vector3;
    if ((live.state === 'onstation' || live.state === 'suppressing') && target) {
      const a = clock.elapsedTime * 0.45 + (live.swarmIndex / Math.max(1, live.swarmSize)) * Math.PI * 2;
      p = new Vector3(target.x + Math.cos(a) * ORBIT_RADIUS_M, target.y + ORBIT_HEIGHT_M + live.swarmIndex * 18, target.z + Math.sin(a) * ORBIT_RADIUS_M);
    } else {
      // In transit: place at its real lat/lon/altitude along the sortie arc.
      const ll = greatCirclePoint(live.origin, live.destination, live.t);
      const altM = Math.min(1500, altitudeAt(live.t, apexForDistance(live.distanceKm)) * 1000) + ORBIT_HEIGHT_M;
      p = terrain.project(ll, altM);
    }
    node.position.lerp(p, prev.current.lengthSq() === 0 ? 1 : 0.25);
    const dir = node.position.clone().sub(prev.current);
    if (dir.lengthSq() > 0.01) node.rotation.y = Math.atan2(dir.x, dir.z);
    prev.current.copy(node.position);
  });
  return (
    <group ref={g}>
      <group scale={9}>
        <DroneModel country={drone.country} />
      </group>
      <pointLight color={FLEETS[drone.country].drone.livery.accent} intensity={400} distance={160} />
    </group>
  );
}

/** Falling suppressant from every drop a drone in this feed makes (detected from the drone's drop counter). */
function DropEffects({ drones, fires }: { drones: DroneUnit[]; fires: LocalFire[] }) {
  const seen = useRef(new Map<string, number>());
  const seq = useRef(0);
  const [splashes, setSplashes] = useState<Splash[]>([]);
  useFrame((_, dtRaw) => {
    const dt = Math.min(dtRaw, 0.1);
    const live = useGame.getState().drones;
    const fresh: Splash[] = [];
    for (const d of drones) {
      const ld = live.find((x) => x.id === d.id);
      if (!ld) continue;
      const before = seen.current.get(d.id);
      seen.current.set(d.id, ld.drops);
      if (before === undefined || ld.drops <= before) continue;
      const target = fires.find((f) => f.fire.id === ld.targetFireId);
      if (!target) continue;
      const kind = FLEETS[ld.country].drone.suppressant;
      fresh.push({ id: seq.current++, pos: new Vector3(target.x + (Math.random() - 0.5) * 60, target.y + ORBIT_HEIGHT_M, target.z + (Math.random() - 0.5) * 60), vel: new Vector3(0, -20, 0), colour: kind === 'retardant' ? '#ff4d6d' : '#7fd8ff', age: 0, landed: false });
    }
    if (!fresh.length && !splashes.length) return;
    const next = [...splashes, ...fresh]
      .map((s) => {
        s.age += dt;
        if (!s.landed) {
          s.vel.y -= 9.81 * 2.5 * dt;
          s.pos.addScaledVector(s.vel, dt);
          const ground = fires.reduce((y, f) => (Math.hypot(f.x - s.pos.x, f.z - s.pos.z) < 200 ? f.y : y), s.pos.y - 1000);
          if (s.pos.y <= ground + 2) {
            s.pos.y = ground + 2;
            s.landed = true;
            s.age = 0;
          }
        }
        return s;
      })
      .filter((s) => !(s.landed && s.age > 1.6));
    setSplashes(next);
  });
  return (
    <>
      {splashes.map((s) =>
        s.landed ? (
          <mesh key={s.id} position={s.pos} rotation={[-Math.PI / 2, 0, 0]} scale={1 + s.age * 5}>
            <ringGeometry args={[18, 30, 32]} />
            <meshBasicMaterial color={s.colour} transparent opacity={Math.max(0, 1 - s.age / 1.6)} />
          </mesh>
        ) : (
          <mesh key={s.id} position={s.pos} scale={[1, 2.4, 1]}>
            <sphereGeometry args={[14, 10, 10]} />
            <meshStandardMaterial color={s.colour} transparent opacity={0.85} emissive={s.colour} emissiveIntensity={0.3} />
          </mesh>
        ),
      )}
    </>
  );
}

function FeedContent({ feed }: { feed: LiveFeed }) {
  const terrain = useTacticalTerrain();
  const scenario = useGame((s) => s.scenario);
  const allFires = useGame((s) => s.fires);
  const drones = useGame((s) => s.drones);
  const carriers = useGame((s) => s.carriers);
  const windDeg = useGame((s) => s.windDirectionDeg);
  const origin = useMemo(() => ({ lat: feed.lat, lon: feed.lon }), [feed.lat, feed.lon]);
  const seed = useMemo(() => Math.abs(Math.round(feed.lat * 13 + feed.lon * 7)) % 1000, [feed.lat, feed.lon]);
  const { fires, structures, rangeKm } = useLocalPlacements(origin, scenario, allFires);

  const fireIds = useMemo(() => new Set(fires.map((f) => f.fire.id)), [fires]);
  const localDrones = drones.filter((d) => fireIds.has(d.targetFireId));
  const localCarriers = carriers.filter((c) => haversineKm(c, origin) < rangeKm);

  const photoreal = terrain.real && terrain.ready;
  const bg = photoreal ? '#7a5a4a' : terrain.real ? '#0a0f14' : '#3a2a24';
  const groundY = terrain.height(0, 0);
  return (
    <>
      <color attach="background" args={[bg]} />
      <fog attach="fog" args={[photoreal ? '#8c6a58' : '#6b4a3a', terrain.real ? 1500 : 600, terrain.real ? 12000 : 4200]} />
      <ambientLight intensity={terrain.real ? 0.7 : 0.4} color="#ffd0a0" />
      {!terrain.real && <ProceduralGround seed={seed} vision="standard" />}
      <TacticalFires fires={fires} vision="standard" windDirectionDeg={windDeg} />
      <Structures structures={structures} vision="standard" />
      {localCarriers.map((c) => {
        const v = terrain.project(c);
        return (
          <group key={c.id} position={v} scale={9}>
            <CarrierModel country={c.country} />
          </group>
        );
      })}
      {localDrones.map((d) => (
        <FeedDrone key={d.id} drone={d} fires={fires} rangeKm={rangeKm} />
      ))}
      <DropEffects drones={localDrones} fires={fires} />
      <OrbitControls makeDefault target={[0, groundY + 40, 0]} autoRotate autoRotateSpeed={0.35} enableDamping minDistance={120} maxDistance={terrain.real ? 9000 : 4500} maxPolarAngle={Math.PI * 0.47} />
    </>
  );
}

/**
 * Live 3D feed of one point on the globe. Read-only spectator view of the
 * running simulation (the globe drives the clock); drag to orbit, scroll to zoom.
 */
export function FeedView({ feed }: { feed: LiveFeed }) {
  const origin = useMemo(() => ({ lat: feed.lat, lon: feed.lon }), [feed.lat, feed.lon]);
  const seed = useMemo(() => Math.abs(Math.round(feed.lat * 13 + feed.lon * 7)) % 1000, [feed.lat, feed.lon]);
  return (
    <Canvas camera={{ position: [420, 340, 620], fov: 50, near: 1, far: 60000 }} dpr={1} gl={{ antialias: true, powerPreference: 'high-performance', logarithmicDepthBuffer: true }}>
      <hemisphereLight args={['#ffb27a', '#2a1a12', 0.8]} />
      <directionalLight position={[800, 900, -400]} intensity={1.4} color="#ffb070" />
      <TacticalWorld origin={origin} seed={seed} vision="standard" errorTarget={20} reportStatus={false}>
        <FeedContent feed={feed} />
      </TacticalWorld>
    </Canvas>
  );
}
