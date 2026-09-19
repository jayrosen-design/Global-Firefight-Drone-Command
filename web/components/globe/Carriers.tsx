'use client';
import { useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Html } from '@react-three/drei';
import { Group, Matrix4, Vector3 } from 'three';
import { latLonToVector3, surfaceFrame } from '@/lib/geo/wgs84';
import type { CarrierVehicle } from '@/lib/engine/CarrierVehicle';
import { FLEETS } from '@/lib/config/fleets';
import { useGame } from '@/store/gameStore';
import { CarrierModel } from '@/components/models/CarrierModel';

function Carrier({ carrier }: { carrier: CarrierVehicle }) {
  const group = useRef<Group>(null);
  const selected = useGame((s) => s.selection?.type === 'carrier' && s.selection.id === carrier.id);
  const select = useGame((s) => s.select);
  const fleet = FLEETS[carrier.country];
  const mat = useMemo(() => new Matrix4(), []);
  const camDir = useMemo(() => new Vector3(), []);
  const label = useRef<HTMLDivElement>(null);

  useFrame(({ camera }) => {
    const g = group.current;
    if (!g) return;
    latLonToVector3(carrier.lat, carrier.lon, 0, g.position);
    const { up, north, east } = surfaceFrame(carrier.lat, carrier.lon);
    mat.makeBasis(east, up, north.clone().negate());
    g.quaternion.setFromRotationMatrix(mat);
    // Constant-ish screen size: scale with camera distance to the globe.
    const d = camera.position.length() - 1;
    g.scale.setScalar(Math.max(0.0004, Math.min(0.02, d * 0.009)));
    camDir.copy(camera.position).normalize();
    const facing = camDir.dot(up);
    g.visible = facing > -0.05;
    if (label.current) label.current.style.opacity = facing > 0.08 ? '1' : '0';
  });

  return (
    <group ref={group}>
      <group onClick={(e) => { e.stopPropagation(); select({ type: 'carrier', id: carrier.id }); }} onPointerOver={() => (document.body.style.cursor = 'pointer')} onPointerOut={() => (document.body.style.cursor = 'default')}>
        <CarrierModel country={carrier.country} selected={selected} />
      </group>
      <Html ref={label} position={[0, 4.2, 0]} center zIndexRange={[30, 0]} style={{ pointerEvents: 'none' }}>
        <div className={`hud-label hud-label--carrier ${selected ? 'hud-label--selected' : ''}`}>
          <div className="hud-label__title">{fleet.flag} {fleet.carrier.model}</div>
          <div className="hud-label__meta">
            DRONES {carrier.dronesReady}/{fleet.carrier.droneCapacity}
            {carrier.rearmQueue.length ? ` · REARM ${carrier.rearmQueue.length}` : ''}
            {carrier.moveTarget ? ' · MOVING' : ''}
          </div>
        </div>
      </Html>
    </group>
  );
}

export function Carriers() {
  const carriers = useGame((s) => s.carriers);
  return (
    <>
      {carriers.map((c) => (
        <Carrier key={c.id} carrier={c} />
      ))}
    </>
  );
}
