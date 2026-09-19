'use client';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useFrame, useThree } from '@react-three/fiber';
import { Group, Vector3 } from 'three';
import { FLEETS } from '@/lib/config/fleets';
import type { DroneUnit } from '@/lib/engine/DroneUnit';
import { useTacticalTerrain } from './TacticalWorld';
import { useGame, type VisionMode } from '@/store/gameStore';
import { useTelemetry } from '@/store/telemetryStore';
import { DroneModel } from '@/components/models/DroneModel';
import type { LocalFire } from './TacticalFires';
import type { Civilian } from './Civilians';

interface Drop {
  id: number;
  pos: Vector3;
  vel: Vector3;
  litres: number;
  age: number;
  splash: number; // >0 while splashing
}

const KEYS = new Set(['w', 'a', 's', 'd', 'q', 'e', 'shift', ' ', 'r', 'arrowup', 'arrowdown', 'arrowleft', 'arrowright']);

/**
 * Player-controlled drone: WASD/arrows to fly, Q/E altitude, Shift boost,
 * SPACE drops suppressant (falls under gravity, splashes onto the nearest fire
 * inside the drop radius), R hovers to extract a civilian, V cycles vision.
 * Also drives the chase camera and publishes telemetry for the HUD.
 */
export function DroneController({
  drone,
  fires,
  civilians,
  setCivilians,
  vision,
  onDrop,
}: {
  drone: DroneUnit;
  fires: LocalFire[];
  civilians: Civilian[];
  setCivilians: (fn: (c: Civilian[]) => Civilian[]) => void;
  vision: VisionMode;
  onDrop: (fireId: string, litres: number) => void;
}) {
  const group = useRef<Group>(null);
  const { camera } = useThree();
  const terrain = useTacticalTerrain();
  const fleet = FLEETS[drone.country];
  const keys = useRef<Set<string>>(new Set());
  const state = useRef({ pos: new Vector3(0, 220, 700), yaw: Math.PI, pitch: 0, speed: 0, rescueHold: 0, dropCooldown: 0 });
  const [drops, setDrops] = useState<Drop[]>([]);
  const dropsRef = useRef<Drop[]>([]);
  const dropSeq = useRef(0);
  const tmp = useMemo(() => ({ fwd: new Vector3(), camTarget: new Vector3(), look: new Vector3() }), []);

  useEffect(() => {
    // Spawn above the nearest fire with a run-in
    const first = fires[0];
    if (first) state.current.pos.set(first.x, first.y + 150, first.z + (terrain.real ? 1100 : 650));
    const down = (e: KeyboardEvent) => {
      const k = e.key.toLowerCase();
      if (KEYS.has(k)) {
        e.preventDefault();
        keys.current.add(k);
      }
      if (k === 'v') {
        const modes: VisionMode[] = ['standard', 'ir', 'lidar'];
        const cur = useGame.getState().visionMode;
        useGame.getState().setVisionMode(modes[(modes.indexOf(cur) + 1) % modes.length]);
      }
      if (k === 'escape' || k === 'tab') {
        e.preventDefault();
        useGame.getState().exitTactical();
      }
    };
    const up = (e: KeyboardEvent) => keys.current.delete(e.key.toLowerCase());
    window.addEventListener('keydown', down);
    window.addEventListener('keyup', up);
    return () => {
      window.removeEventListener('keydown', down);
      window.removeEventListener('keyup', up);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fires]);

  useFrame((_, dtRaw) => {
    const dt = Math.min(dtRaw, 0.1);
    const s = state.current;
    const k = keys.current;
    const spec = fleet.drone.tactical;
    const live = useGame.getState().drones.find((d) => d.id === drone.id);

    // Controls
    const boost = k.has('shift') ? 1.8 : 1;
    const throttle = (k.has('w') || k.has('arrowup') ? 1 : 0) - (k.has('s') || k.has('arrowdown') ? 0.6 : 0);
    s.speed += (throttle * spec.maxSpeed * terrain.speedScale * boost - s.speed) * Math.min(1, dt * 1.6);
    const yawIn = (k.has('a') || k.has('arrowleft') ? 1 : 0) - (k.has('d') || k.has('arrowright') ? 1 : 0);
    s.yaw += yawIn * spec.turnRate * dt;
    const climb = (k.has('e') ? 1 : 0) - (k.has('q') ? 1 : 0);
    tmp.fwd.set(Math.sin(s.yaw), 0, Math.cos(s.yaw));
    s.pos.addScaledVector(tmp.fwd, s.speed * dt);
    s.pos.y += climb * 28 * terrain.speedScale * dt;
    const ground = terrain.height(s.pos.x, s.pos.z);
    s.pos.y = Math.max(ground + 6, Math.min(ground + 600, s.pos.y));
    s.pitch += ((throttle * 0.18 - s.pitch) * dt) * 4;

    const g = group.current;
    if (g) {
      g.position.copy(s.pos);
      g.rotation.set(s.pitch, s.yaw, -yawIn * 0.35);
    }

    // Chase camera
    const camBack = terrain.real ? 60 : 42;
    tmp.camTarget.copy(s.pos).addScaledVector(tmp.fwd, -camBack).setY(s.pos.y + camBack * 0.4);
    camera.position.lerp(tmp.camTarget, Math.min(1, dt * 4));
    tmp.look.copy(s.pos).addScaledVector(tmp.fwd, 60);
    camera.lookAt(tmp.look);

    // Predicted impact point (ballistic, same gravity as the drop bodies) drives the reticle.
    const agl = Math.max(1, s.pos.y - ground);
    const fallT = Math.sqrt((2 * agl) / (9.81 * 2.2));
    const impX = s.pos.x + tmp.fwd.x * s.speed * 0.8 * fallT;
    const impZ = s.pos.z + tmp.fwd.z * s.speed * 0.8 * fallT;
    let nearest: LocalFire | null = null;
    let nd = Infinity;
    const liveFires = useGame.getState().fires;
    for (const lf of fires) {
      const f = liveFires.find((x) => x.id === lf.fire.id);
      if (!f || f.extinguished) continue;
      const d = Math.hypot(lf.x - impX, lf.z - impZ);
      if (d < nd) {
        nd = d;
        nearest = lf;
      }
    }
    const dropWindow = (spec.dropRadius * 6 + 40) * (terrain.real ? 1.6 : 1);
    const reticleLocked = !!nearest && nd < dropWindow;

    // Drops
    s.dropCooldown -= dt;
    if (k.has(' ') && s.dropCooldown <= 0 && live && live.payloadLitres > 0) {
      s.dropCooldown = 0.6;
      const litres = Math.min(live.payloadLitres, Math.max(80, live.payloadMax / 6));
      dropsRef.current.push({ id: dropSeq.current++, pos: s.pos.clone(), vel: tmp.fwd.clone().multiplyScalar(s.speed * 0.8), litres, age: 0, splash: 0 });
      // Debit payload at release so the HUD reflects it immediately.
      useGame.setState((st) => ({ drones: st.drones.map((d) => (d.id === drone.id ? { ...d, payloadLitres: d.payloadLitres - litres } : d)) }));
    }
    let changed = false;
    for (const d of dropsRef.current) {
      d.age += dt;
      if (d.splash > 0) {
        d.splash -= dt;
        continue;
      }
      d.vel.y -= 9.81 * dt * 2.2;
      d.pos.addScaledVector(d.vel, dt);
      const gy = terrain.height(d.pos.x, d.pos.z);
      if (d.pos.y <= gy + 1) {
        d.pos.y = gy + 1;
        d.splash = 0.9;
        changed = true;
        // Hit test against fires
        let best: LocalFire | null = null;
        let bd = Infinity;
        for (const lf of fires) {
          const dist = Math.hypot(lf.x - d.pos.x, lf.z - d.pos.z);
          if (dist < bd) {
            bd = dist;
            best = lf;
          }
        }
        // Payload was debited at release; a miss simply wastes it.
        if (best && bd < dropWindow) {
          const inten = Math.max(0.35, 1 - bd / dropWindow);
          onDrop(best.fire.id, d.litres * inten);
        } else {
          useTelemetry.getState().set({ lastDropKnockdownMW: 0 });
        }
      }
    }
    const before = dropsRef.current.length;
    dropsRef.current = dropsRef.current.filter((d) => !(d.splash < 0 && d.age > 1));
    if (changed || before !== dropsRef.current.length || dropsRef.current.length) setDrops([...dropsRef.current]);

    // Rescue
    let nearCiv = false;
    if (s.pos.y - ground < 30) {
      for (const c of civilians) {
        if (c.rescued) continue;
        const d = Math.hypot(c.x - s.pos.x, c.z - s.pos.z);
        if (d < 40) {
          nearCiv = true;
          if (k.has('r')) {
            s.rescueHold += dt;
            if (s.rescueHold > 1.5) {
              s.rescueHold = 0;
              setCivilians((cs) => cs.map((x) => (x.id === c.id ? { ...x, rescued: true } : x)));
              useGame.getState().rescueCivilian();
            }
          }
          break;
        }
      }
    }
    if (!nearCiv || !k.has('r')) s.rescueHold = 0;

    useTelemetry.getState().set({
      altitude: s.pos.y - ground,
      airspeed: s.speed * 3.6,
      heading: ((s.yaw * 180) / Math.PI + 360) % 360,
      targetFireId: nearest?.fire.id ?? null,
      targetDistance: nd,
      reticleLocked,
      nearCivilian: nearCiv,
      civiliansRemaining: civilians.filter((c) => !c.rescued).length,
      x: s.pos.x,
      z: s.pos.z,
    });
  });

  return (
    <>
      <group ref={group}>
        <group scale={4} rotation={[0, Math.PI, 0]}>
          <DroneModel country={drone.country} vision={vision} />
        </group>
        {/* Targeting laser (USA PinPoint / DEU precision) */}
        <mesh position={[0, -8, 0]}>
          <cylinderGeometry args={[0.12, 0.12, 16, 6]} />
          <meshBasicMaterial color={vision === 'ir' ? '#ffffff' : '#5ef2ff'} transparent opacity={0.35} />
        </mesh>
        <pointLight color={vision === 'ir' ? '#ffffff' : fleet.drone.livery.accent} intensity={200} distance={120} />
      </group>
      {drops.map((d) => (
        <group key={d.id} position={d.pos.toArray()}>
          {d.splash > 0 ? (
            <mesh rotation={[-Math.PI / 2, 0, 0]} scale={1 + (0.9 - d.splash) * 3}>
              <ringGeometry args={[6, 12, 24]} />
              <meshBasicMaterial color={fleet.drone.suppressant === 'retardant' ? '#ff4d6d' : vision === 'ir' ? '#222' : '#7fd8ff'} transparent opacity={d.splash} />
            </mesh>
          ) : (
            <mesh>
              <sphereGeometry args={[2.2 + d.litres / 300, 8, 8]} />
              <meshStandardMaterial color={fleet.drone.suppressant === 'retardant' ? '#ff4d6d' : '#7fd8ff'} transparent opacity={0.85} />
            </mesh>
          )}
        </group>
      ))}
    </>
  );
}
