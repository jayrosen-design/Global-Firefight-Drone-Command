'use client';
import { useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Html } from '@react-three/drei';
import { Group, Matrix4, Quaternion, Vector3 } from 'three';
import { latLonToVector3 } from '@/lib/geo/wgs84';
import { trajectoryManager, TrajectoryManager } from '@/lib/geo/trajectory';
import type { DroneUnit } from '@/lib/engine/DroneUnit';
import { FLEETS } from '@/lib/config/fleets';
import { useGame } from '@/store/gameStore';
import { DroneModel } from '@/components/models/DroneModel';

const ORBIT_ALT_KM = 3;
const ORBIT_RADIUS_KM = 9;

function Drone({ drone }: { drone: DroneUnit }) {
  const group = useRef<Group>(null);
  const selected = useGame((s) => s.selection?.type === 'drone' && s.selection.id === drone.id);
  const select = useGame((s) => s.select);
  const fleet = FLEETS[drone.country];
  const label = useRef<HTMLDivElement>(null);
  const tmp = useMemo(() => ({ pos: new Vector3(), fwd: new Vector3(), up: new Vector3(), right: new Vector3(), m: new Matrix4(), q: new Quaternion() }), []);

  useFrame(({ camera }) => {
    const g = group.current;
    if (!g) return;
    // Read the live unit from the store so the transform tracks the simulation without re-rendering.
    const live = useGame.getState().drones.find((d) => d.id === drone.id) ?? drone;
    const traj = trajectoryManager.build(live.id, live.origin, live.destination);
    if (live.state === 'enroute' || live.state === 'returning') {
      TrajectoryManager.sample(traj, live.t, tmp.pos, tmp.fwd);
      if (live.state === 'returning') tmp.fwd.negate();
      // Formation offset for swarms
      if (live.swarmSize > 1) {
        tmp.up.copy(tmp.pos).normalize();
        tmp.right.crossVectors(tmp.fwd, tmp.up).normalize();
        const off = (live.swarmIndex - (live.swarmSize - 1) / 2) * 0.004;
        tmp.pos.addScaledVector(tmp.right, off).addScaledVector(tmp.fwd, -Math.abs(off) * 0.8);
      }
    } else {
      // Orbit the target fire
      const center = latLonToVector3(live.destination.lat, live.destination.lon, ORBIT_ALT_KM, tmp.pos);
      tmp.up.copy(center).normalize();
      const east = new Vector3().crossVectors(new Vector3(0, 1, 0), tmp.up).normalize();
      const north = new Vector3().crossVectors(tmp.up, east).normalize();
      const r = ORBIT_RADIUS_KM / 6371;
      const a = live.orbit;
      const p = center.clone().addScaledVector(east, Math.cos(a) * r).addScaledVector(north, Math.sin(a) * r);
      tmp.fwd.copy(east).multiplyScalar(-Math.sin(a)).addScaledVector(north, Math.cos(a)).normalize();
      tmp.pos.copy(p);
    }
    g.position.copy(tmp.pos);
    tmp.up.copy(tmp.pos).normalize();
    tmp.right.crossVectors(tmp.fwd, tmp.up).normalize();
    tmp.up.crossVectors(tmp.right, tmp.fwd).normalize();
    tmp.m.makeBasis(tmp.right, tmp.up, tmp.fwd.clone().negate());
    g.quaternion.setFromRotationMatrix(tmp.m);
    const d = camera.position.length() - 1;
    g.scale.setScalar(Math.max(0.0003, Math.min(0.012, d * 0.0055)));
    const facing = tmp.pos.clone().normalize().dot(camera.position.clone().normalize());
    g.visible = facing > -0.05;
    if (label.current) label.current.style.opacity = facing > 0.08 ? '1' : '0';
  });

  return (
    <group ref={group}>
      <group onClick={(e) => { e.stopPropagation(); select({ type: 'drone', id: drone.id }); }} onPointerOver={() => (document.body.style.cursor = 'pointer')} onPointerOut={() => (document.body.style.cursor = 'default')}>
        <DroneModel country={drone.country} />
      </group>
      {selected && (
        <mesh rotation={[-Math.PI / 2, 0, 0]}>
          <ringGeometry args={[2.4, 2.7, 32]} />
          <meshBasicMaterial color="#5ef2ff" transparent opacity={0.9} toneMapped={false} />
        </mesh>
      )}
      {(selected || drone.swarmIndex === 0) && (
        <Html ref={label} position={[0, 3, 0]} center zIndexRange={[25, 0]} style={{ pointerEvents: 'none' }}>
          <div className={`hud-label hud-label--drone ${selected ? 'hud-label--selected' : ''}`}>
            <div className="hud-label__title">{drone.callSign}{drone.swarmSize > 1 ? ` ×${drone.swarmSize}` : ''}</div>
            <div className="hud-label__meta">{fleet.drone.model} · {drone.state.toUpperCase()}</div>
          </div>
        </Html>
      )}
    </group>
  );
}

export function Drones() {
  const drones = useGame((s) => s.drones);
  return (
    <>
      {drones.map((d) => (
        <Drone key={d.id} drone={d} />
      ))}
    </>
  );
}
