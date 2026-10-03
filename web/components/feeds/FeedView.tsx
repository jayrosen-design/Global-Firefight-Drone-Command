'use client';
import { createContext, useContext, useEffect, useMemo, useRef, useState } from 'react';
import { Canvas, useFrame, useThree } from '@react-three/fiber';
import { OrbitControls } from '@react-three/drei';
import { Group, Vector3 } from 'three';
import type { OrbitControls as OrbitControlsImpl } from 'three-stdlib';
import { useGame, type LiveFeed, type VisionMode } from '@/store/gameStore';
import { FLEETS } from '@/lib/config/fleets';
import { altitudeAt, apexForDistance } from '@/lib/geo/trajectory';
import { greatCirclePoint, haversineKm } from '@/lib/geo/wgs84';
import type { DroneUnit } from '@/lib/engine/DroneUnit';
import type { CarrierVehicle } from '@/lib/engine/CarrierVehicle';
import { DroneModel } from '@/components/models/DroneModel';
import { CarrierModel } from '@/components/models/CarrierModel';
import { TacticalWorld, useTacticalTerrain } from '@/components/tactical/TacticalWorld';
import { ProceduralGround } from '@/components/tactical/TacticalScene';
import { TacticalFires, type LocalFire } from '@/components/tactical/TacticalFires';
import { Structures } from '@/components/tactical/Structures';
import { useLocalPlacements } from '@/components/tactical/useLocalPlacements';

const ORBIT_RADIUS_M = 260;
const ORBIT_HEIGHT_M = 160;
const CARRIER_SCALE = 9; // CarrierModel units → metres
const ROOF_Y = 1.62 * CARRIER_SCALE;
/** Distance over which a launching drone climbs from the carrier roof onto its sortie arc. */
const LAUNCH_CLIMB_KM = 1.2;

/** World positions of drones rendered in this feed, for the drone chase camera. */
const DronePositions = createContext<Map<string, Vector3>>(new Map());

interface Splash {
  id: number;
  pos: Vector3;
  vel: Vector3;
  colour: string;
  age: number;
  landed: boolean;
}

function carrierPos(terrain: ReturnType<typeof useTacticalTerrain>, c: CarrierVehicle) {
  return terrain.project(c);
}

/** One drone as seen from the feed: launch climb off the carrier, great-circle transit, orbit on station. */
function FeedDrone({ drone, fires, rangeKm, vision }: { drone: DroneUnit; fires: LocalFire[]; rangeKm: number; vision: VisionMode }) {
  const terrain = useTacticalTerrain();
  const positions = useContext(DronePositions);
  const g = useRef<Group>(null);
  const body = useRef<Group>(null);
  const prev = useRef(new Vector3());
  useEffect(() => () => void positions.delete(drone.id), [positions, drone.id]);
  useFrame(({ clock, camera }) => {
    const st = useGame.getState();
    const live = st.drones.find((d) => d.id === drone.id);
    const node = g.current;
    if (!live || !node) return;
    const remainingKm = (1 - live.t) * live.distanceKm;
    const travelledKm = live.t * live.distanceKm;
    const nearCarrier = travelledKm < rangeKm;
    node.visible = remainingKm < rangeKm || nearCarrier;
    if (!node.visible) return positions.delete(drone.id);
    const target = fires.find((f) => f.fire.id === live.targetFireId);
    let p: Vector3;
    if ((live.state === 'onstation' || live.state === 'suppressing') && target) {
      const a = clock.elapsedTime * 0.45 + (live.swarmIndex / Math.max(1, live.swarmSize)) * Math.PI * 2;
      p = new Vector3(target.x + Math.cos(a) * ORBIT_RADIUS_M, target.y + ORBIT_HEIGHT_M + live.swarmIndex * 18, target.z + Math.sin(a) * ORBIT_RADIUS_M);
    } else {
      const ll = greatCirclePoint(live.origin, live.destination, live.t);
      const altM = Math.min(1500, altitudeAt(live.t, apexForDistance(live.distanceKm)) * 1000) + ORBIT_HEIGHT_M;
      p = terrain.project(ll, altM);
      const carrier = st.carriers.find((c) => c.id === live.carrierId);
      if (carrier && live.state === 'enroute' && travelledKm < LAUNCH_CLIMB_KM) {
        // Lift-off: rise vertically off the roof, then pitch onto the sortie arc.
        const deck = carrierPos(terrain, carrier).add(new Vector3((live.swarmIndex - 1) * 9, ROOF_Y + 2, -4));
        const k = travelledKm / LAUNCH_CLIMB_KM;
        // Phase 1 (k<0.6): vertical lift to ~45 m with a slight fan-out; phase 2: transition onto the sortie arc.
        const hover = deck.clone().add(new Vector3((live.swarmIndex - 1) * 10, 8 + 38 * Math.min(1, k / 0.6), 6));
        p = k < 0.6 ? deck.lerp(hover, k / 0.6) : hover.lerp(p, (k - 0.6) / 0.4);
      }
    }
    node.position.lerp(p, prev.current.lengthSq() === 0 ? 1 : 0.25);
    const dir = node.position.clone().sub(prev.current);
    if (dir.lengthSq() > 0.01) node.rotation.y = Math.atan2(dir.x, dir.z);
    prev.current.copy(node.position);
    positions.set(drone.id, node.position);
    // Perceptual sizing: true scale up close, enlarged when far so it stays readable.
    if (body.current) body.current.scale.setScalar(Math.min(9, Math.max(3.6, camera.position.distanceTo(node.position) * 0.012)));
  });
  return (
    <group ref={g}>
      <group ref={body} scale={9}>
        <DroneModel country={drone.country} vision={vision} />
      </group>
      {vision !== 'lidar' && <pointLight color={vision === 'ir' ? '#ffffff' : FLEETS[drone.country].drone.livery.accent} intensity={400} distance={160} />}
    </group>
  );
}

/** A carrier with its ready drones parked on the roof drone bay. */
function FeedCarrier({ carrier, vision }: { carrier: CarrierVehicle; vision: VisionMode }) {
  const terrain = useTacticalTerrain();
  const ready = useGame((s) => s.carriers.find((c) => c.id === carrier.id)?.dronesReady ?? 0);
  const v = carrierPos(terrain, carrier);
  const shown = Math.min(ready, 4);
  return (
    <group position={v}>
      <group scale={CARRIER_SCALE}>
        <CarrierModel country={carrier.country} selected />
      </group>
      {Array.from({ length: shown }, (_, i) => (
        <group key={i} position={[(i % 2 ? 1 : -1) * 3.6, ROOF_Y + 2.2, -4 - Math.floor(i / 2) * 8]} scale={3.2}>
          <DroneModel country={carrier.country} vision={vision} spin={false} />
        </group>
      ))}
      {vision !== 'lidar' && <pointLight position={[0, 30, 0]} color={vision === 'ir' ? '#ffffff' : FLEETS[carrier.country].carrier.livery.accent} intensity={2500} distance={140} />}
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

/** Camera behaviour per feed focus. Area/carrier use orbit controls; drone focus is a chase cam over the fire. */
function FeedCamera({ feed, fires, carriers, drones, groundY }: { feed: LiveFeed; fires: LocalFire[]; carriers: CarrierVehicle[]; drones: DroneUnit[]; groundY: number }) {
  const terrain = useTacticalTerrain();
  const positions = useContext(DronePositions);
  const controls = useRef<OrbitControlsImpl>(null);
  const { camera } = useThree();
  const lookAt = useMemo(() => new Vector3(), []);
  const want = useMemo(() => new Vector3(), []);
  const aim = useMemo(() => new Vector3(), []);

  const carrier = feed.focus === 'carrier' ? carriers.find((c) => c.id === feed.carrierId) ?? carriers[0] : undefined;
  const chaseId = feed.focus === 'drone' ? (drones.find((d) => d.id === feed.droneId) ?? drones.find((d) => d.state === 'onstation' || d.state === 'suppressing') ?? drones[0])?.id : undefined;

  // Re-frame when the focus changes.
  useEffect(() => {
    if (carrier) {
      const c = carrierPos(terrain, carrier);
      controls.current?.target.set(c.x, c.y + 22, c.z);
      camera.position.set(c.x + 62, c.y + 40, c.z + 78);
    } else if (feed.focus === 'area') {
      controls.current?.target.set(0, groundY + 40, 0);
      camera.position.set(420, groundY + 340, 620);
    }
    controls.current?.update();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [feed.focus, carrier?.id]);

  useFrame(() => {
    if (feed.focus !== 'drone' || !chaseId) return;
    const pos = positions.get(chaseId);
    if (!pos) return;
    const live = useGame.getState().drones.find((d) => d.id === chaseId);
    const fire = fires.find((f) => f.fire.id === live?.targetFireId);
    if (fire) lookAt.set(fire.x, fire.y + 10, fire.z);
    else lookAt.copy(pos).add(new Vector3(0, -80, 0));
    // Wingman framing: off the drone's flank, slightly above, looking down past it into the fire.
    const away = pos.clone().sub(lookAt).setY(0).normalize();
    const side = new Vector3(0, 1, 0).cross(away).normalize();
    want.copy(pos).addScaledVector(away, 34).addScaledVector(side, 26).add(new Vector3(0, 12, 0));
    // Rigid mount (no easing) so the drone stays framed at any frame rate.
    camera.position.copy(want);
    aim.copy(pos).lerp(lookAt, 0.3);
    camera.lookAt(aim);
  });

  return (
    <OrbitControls
      ref={controls}
      makeDefault
      enabled={feed.focus !== 'drone'}
      target={[0, groundY + 40, 0]}
      autoRotate={feed.focus !== 'drone'}
      autoRotateSpeed={feed.focus === 'carrier' ? 0.8 : 0.35}
      enableDamping
      minDistance={feed.focus === 'carrier' ? 25 : 120}
      maxDistance={terrain.real ? 9000 : 4500}
      maxPolarAngle={Math.PI * 0.47}
    />
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
  const vision = feed.vision;

  const fireIds = useMemo(() => new Set(fires.map((f) => f.fire.id)), [fires]);
  const localCarriers = carriers.filter((c) => haversineKm(c, origin) < rangeKm);
  const localCarrierIds = new Set(localCarriers.map((c) => c.id));
  // Drones working fires here, plus drones launching from carriers here.
  const localDrones = drones.filter((d) => fireIds.has(d.targetFireId) || localCarrierIds.has(d.carrierId));

  const photoreal = terrain.real && terrain.ready;
  const bg = vision === 'standard' ? (photoreal ? '#7a5a4a' : terrain.real ? '#0a0f14' : '#3a2a24') : '#000000';
  const fogColor = vision === 'standard' ? (photoreal ? '#8c6a58' : '#6b4a3a') : vision === 'ir' ? '#050505' : '#00161c';
  const groundY = terrain.height(0, 0);
  const positions = useMemo(() => new Map<string, Vector3>(), []);
  return (
    <DronePositions.Provider value={positions}>
      <color attach="background" args={[bg]} />
      <fog attach="fog" args={[fogColor, terrain.real ? 1500 : 600, vision === 'standard' ? (terrain.real ? 12000 : 4200) : 9000]} />
      <ambientLight intensity={vision === 'ir' ? 0.9 : terrain.real ? 0.7 : 0.4} color={vision === 'ir' ? '#ffffff' : '#ffd0a0'} />
      {!terrain.real && <ProceduralGround seed={seed} vision={vision} />}
      <TacticalFires fires={fires} vision={vision} windDirectionDeg={windDeg} />
      <Structures structures={structures} vision={vision} />
      {localCarriers.map((c) => (
        <FeedCarrier key={c.id} carrier={c} vision={vision} />
      ))}
      {localDrones.map((d) => (
        <FeedDrone key={d.id} drone={d} fires={fires} rangeKm={rangeKm} vision={vision} />
      ))}
      <DropEffects drones={localDrones} fires={fires} />
      <FeedCamera feed={feed} fires={fires} carriers={localCarriers} drones={localDrones} groundY={groundY} />
    </DronePositions.Provider>
  );
}

/**
 * Live 3D feed of one point on the globe. Read-only spectator view of the
 * running simulation (the globe drives the clock); drag to orbit, scroll to zoom.
 */
export function FeedView({ feed }: { feed: LiveFeed }) {
  const origin = useMemo(() => ({ lat: feed.lat, lon: feed.lon }), [feed.lat, feed.lon]);
  const seed = useMemo(() => Math.abs(Math.round(feed.lat * 13 + feed.lon * 7)) % 1000, [feed.lat, feed.lon]);
  const std = feed.vision === 'standard';
  return (
    <Canvas camera={{ position: [420, 340, 620], fov: 50, near: 1, far: 60000 }} dpr={1} gl={{ antialias: true, powerPreference: 'high-performance', logarithmicDepthBuffer: true }}>
      <hemisphereLight args={[std ? '#ffb27a' : '#666', std ? '#2a1a12' : '#000', feed.vision === 'lidar' ? 0.2 : 0.8]} />
      <directionalLight position={[800, 900, -400]} intensity={std ? 1.4 : 0.6} color={std ? '#ffb070' : '#ffffff'} />
      <TacticalWorld origin={origin} seed={seed} vision={feed.vision} errorTarget={20} reportStatus={false}>
        <FeedContent feed={feed} />
      </TacticalWorld>
    </Canvas>
  );
}
