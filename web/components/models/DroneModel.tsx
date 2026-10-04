'use client';
import { useEffect, useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Color, Group, MeshStandardMaterial } from 'three';
import type { CountryCode } from '@/lib/config/fleets';
import type { VisionMode } from '@/store/gameStore';

/**
 * Procedural drone bodies modelled on the pitch-deck concept art (public/teams/<CODE>/drone.webp):
 * heavy-lift multirotors on skids with an underslung suppressant tank and gimbal camera.
 *  USA Guardian Mk IV   — white hex, red stripe, star roundel, tail boom + fin, orange prop tips
 *  CAN FireSwarm Wasp   — red/white octagonal hex, white GPS dome, grey ribbed tank
 *  BRA Arara            — green hex, yellow-banded arms, yellow saddle tank with green band
 *  CHN EHang 216F       — faceted red hex, white tank, large black gimbal
 *  DEU GF-CA            — red box hex, yellow Feuerwehr stripe, red tank, red camera ball
 *  AUS Kookaburra       — olive quad, olive tank with yellow end caps
 * +Z is the nose, +Y up. `vision` swaps materials for IR white-hot / LIDAR wireframe.
 * `detail="low"` keeps only hull, arms, rotors and tank (~5× fewer draw calls) for the globe, where drones are a few pixels.
 */

type Hull = 'capsule' | 'octagon' | 'box';

interface DroneSpec {
  arms: 4 | 6;
  armLen: number;
  armRadius: number;
  armColor: string;
  /** Coloured bands along the arms (BRA yellow, CAN red sleeves). */
  armBand?: string;
  motor: string;
  motorBand: string;
  propTip?: string;
  hull: Hull;
  hullSize: [number, number, number];
  hullColor: string;
  hullTop?: string;
  stripe?: string;
  /** Thin pinstripe instead of a full side panel (octagon hulls). */
  pinstripe?: boolean;
  tank: {
    shape: 'box' | 'capsule';
    size: [number, number, number];
    color: string;
    band?: string;
    caps?: string;
    ribs?: boolean;
  };
  gimbal: { color: string; r: number };
  skid: string;
  dome?: string;
  patch?: [string, string];
}

const SPECS: Record<CountryCode, DroneSpec> = {
  USA: {
    arms: 6,
    armLen: 1.25,
    armRadius: 0.05,
    armColor: '#2b2f33',
    motor: '#1d2024',
    motorBand: '#ff5a1a',
    propTip: '#ff6a1a',
    hull: 'capsule',
    hullSize: [0.36, 0.38, 1.0],
    hullColor: '#eef0f2',
    stripe: '#d8262b',
    tank: { shape: 'capsule', size: [0.24, 0.24, 0.75], color: '#d9dcdf' },
    gimbal: { color: '#e6e8ea', r: 0.13 },
    skid: '#1b1d20',
    dome: '#f4f4f4',
    patch: ['#1d2b55', '#ffffff'],
  },
  CAN: {
    arms: 6,
    armLen: 1.2,
    armRadius: 0.05,
    armColor: '#25282b',
    armBand: '#c8321e',
    motor: '#c8321e',
    motorBand: '#1f2124',
    hull: 'octagon',
    hullSize: [0.46, 0.42, 0.46],
    hullColor: '#c8321e',
    hullTop: '#ececea',
    stripe: '#f2f2ef',
    tank: {
      shape: 'box',
      size: [0.62, 0.3, 0.5],
      color: '#c9c9c4',
      band: '#a8a8a3',
      ribs: true,
    },
    gimbal: { color: '#2a2c2f', r: 0.1 },
    skid: '#3a3d40',
    dome: '#f2f2ee',
  },
  BRA: {
    arms: 6,
    armLen: 1.3,
    armRadius: 0.055,
    armColor: '#1f8a3a',
    armBand: '#f2cf1d',
    motor: '#1f8a3a',
    motorBand: '#161819',
    hull: 'octagon',
    hullSize: [0.46, 0.4, 0.5],
    hullColor: '#1f8a3a',
    stripe: '#f2cf1d',
    pinstripe: true,
    tank: {
      shape: 'capsule',
      size: [0.3, 0.3, 0.6],
      color: '#f0c419',
      band: '#1f8a3a',
    },
    gimbal: { color: '#1c1e20', r: 0.11 },
    skid: '#4a4d4f',
    patch: ['#1f8a3a', '#f2cf1d'],
  },
  CHN: {
    arms: 6,
    armLen: 1.3,
    armRadius: 0.055,
    armColor: '#1e1f22',
    motor: '#c4161c',
    motorBand: '#c9a14a',
    hull: 'box',
    hullSize: [0.8, 0.42, 1.0],
    hullColor: '#cc1d22',
    hullTop: '#b8151a',
    tank: { shape: 'box', size: [0.55, 0.32, 0.6], color: '#ecebe6' },
    gimbal: { color: '#202225', r: 0.18 },
    skid: '#1b1c1e',
    patch: ['#de2910', '#ffde00'],
  },
  DEU: {
    arms: 6,
    armLen: 1.3,
    armRadius: 0.065,
    armColor: '#1b1c1e',
    motor: '#1b1c1e',
    motorBand: '#2a2b2d',
    hull: 'box',
    hullSize: [0.82, 0.46, 1.0],
    hullColor: '#d3121b',
    stripe: '#f5e31a',
    tank: {
      shape: 'box',
      size: [0.68, 0.42, 0.62],
      color: '#d3121b',
      band: '#f5e31a',
    },
    gimbal: { color: '#d3121b', r: 0.14 },
    skid: '#1b1c1e',
    patch: ['#111111', '#ffce00'],
  },
  AUS: {
    arms: 4,
    armLen: 1.35,
    armRadius: 0.07,
    armColor: '#4b5a3c',
    motor: '#4b5a3c',
    motorBand: '#ff8a2a',
    hull: 'octagon',
    hullSize: [0.52, 0.48, 0.58],
    hullColor: '#4f5e3f',
    hullTop: '#56664a',
    tank: {
      shape: 'box',
      size: [0.72, 0.42, 0.56],
      color: '#4b5a3c',
      caps: '#d9b02c',
    },
    gimbal: { color: '#1e2022', r: 0.12 },
    skid: '#46543a',
    patch: ['#012169', '#ffffff'],
  },
};

const PROP_LEN = 0.95;

export function DroneModel({
  country,
  vision = 'standard',
  spin = true,
  detail = 'full',
}: {
  country: CountryCode;
  vision?: VisionMode;
  spin?: boolean;
  detail?: 'full' | 'low';
}) {
  const full = detail === 'full';
  const spec = SPECS[country] ?? SPECS.USA;
  const props = useRef<(Group | null)[]>([]);

  // One material per (colour, hot) pair, rebuilt when the vision mode changes.
  const paint = useMemo(() => {
    const cache = new Map<string, MeshStandardMaterial>();
    const get = (c: string, hot = false) => {
      const key = `${c}|${hot}`;
      let m = cache.get(key);
      if (m) return m;
      if (vision === 'ir') {
        // White-hot: motors and glowing parts burn white, airframe reads as warm grey by luminance.
        const lum = new Color(c).getHSL({ h: 0, s: 0, l: 0 }).l;
        const g = hot ? 1 : 0.55 + lum * 0.3;
        m = new MeshStandardMaterial({
          color: new Color(g, g, g),
          emissive: new Color(g, g, g),
          emissiveIntensity: hot ? 0.9 : 0.25,
          roughness: 0.8,
        });
      } else if (vision === 'lidar') {
        m = new MeshStandardMaterial({
          color: hot ? '#7dffb3' : '#5ef2ff',
          emissive: hot ? '#7dffb3' : '#5ef2ff',
          emissiveIntensity: 0.9,
          wireframe: true,
        });
      } else {
        m = new MeshStandardMaterial({
          color: c,
          metalness: 0.3,
          roughness: 0.5,
          emissive: hot ? c : '#000000',
          emissiveIntensity: hot ? 0.35 : 0,
        });
      }
      cache.set(key, m);
      return m;
    };
    return { get, cache };
  }, [vision]);
  useEffect(() => () => paint.cache.forEach((m) => m.dispose()), [paint]);

  useFrame((_, dt) => {
    if (!spin) return;
    props.current.forEach((p, i) => {
      if (p) p.rotation.y += dt * (i % 2 ? -42 : 42);
    });
  });

  const P = paint.get;
  const [hw, hh, hl] = spec.hullSize;
  const armOffset = spec.arms === 6 ? Math.PI / 6 : Math.PI / 4;
  const armY = 0.06;
  const bodyR = spec.hull === 'box' ? Math.min(hw, hl) * 0.45 : hw * 0.8;
  const tankY = -hh / 2 - spec.tank.size[1] / 2 - 0.08;
  const skidY = tankY - spec.tank.size[1] / 2 - 0.32;
  const skidX = 0.62;

  return (
    <group>
      {/* Hull */}
      <Hull spec={spec} P={P} full={full} />

      {/* Arms, motors and props */}
      {Array.from({ length: spec.arms }).map((_, i) => {
        const a = armOffset + (i / spec.arms) * Math.PI * 2;
        const dx = Math.sin(a);
        const dz = Math.cos(a);
        const len = spec.armLen - bodyR;
        const mid = bodyR + len / 2;
        return (
          <group key={i}>
            <mesh position={[dx * mid, armY, dz * mid]} rotation={[0, a - Math.PI / 2, Math.PI / 2]} material={P(spec.armColor)}>
              <cylinderGeometry args={[spec.armRadius, spec.armRadius, len, 8]} />
            </mesh>
            {full &&
              spec.armBand &&
              [0.3, 0.72].map((t) => (
                <mesh
                  key={t}
                  position={[dx * (bodyR + len * t), armY, dz * (bodyR + len * t)]}
                  rotation={[0, a - Math.PI / 2, Math.PI / 2]}
                  material={P(spec.armBand!)}
                >
                  <cylinderGeometry args={[spec.armRadius * 1.15, spec.armRadius * 1.15, len * 0.12, 8]} />
                </mesh>
              ))}
            <group position={[dx * spec.armLen, armY, dz * spec.armLen]}>
              <mesh position={[0, 0.06, 0]} material={P(spec.motor)}>
                <cylinderGeometry args={[0.13, 0.12, 0.26, 14]} />
              </mesh>
              {full && (
                <mesh position={[0, -0.02, 0]} material={P(spec.motorBand, true)}>
                  <cylinderGeometry args={[0.135, 0.135, 0.06, 14]} />
                </mesh>
              )}
              {full && (
                <mesh position={[0, 0.2, 0]} material={P('#1a1b1d')}>
                  <cylinderGeometry args={[0.06, 0.08, 0.06, 10]} />
                </mesh>
              )}
              <group
                position={[0, 0.25, 0]}
                rotation={[0, i * 0.7, 0]}
                ref={(g) => {
                  props.current[i] = g;
                }}
              >
                <mesh material={P('#24272b')}>
                  <boxGeometry args={[PROP_LEN, 0.015, 0.085]} />
                </mesh>
                {full &&
                  spec.propTip &&
                  [-1, 1].map((s) => (
                    <mesh key={s} position={[s * (PROP_LEN / 2 - 0.06), 0, 0]} material={P(spec.propTip!, true)}>
                      <boxGeometry args={[0.12, 0.02, 0.09]} />
                    </mesh>
                  ))}
              </group>
            </group>
          </group>
        );
      })}

      {/* Underslung suppressant tank */}
      <Tank spec={spec} y={tankY} P={P} full={full} />

      {full && (
        <>
          {/* Gimbal camera, nose-low */}
          <group position={[0, tankY + 0.02, Math.max(spec.tank.size[2], hl) / 2 + 0.02]}>
            <mesh position={[0, 0.12, 0]} material={P('#202225')}>
              <cylinderGeometry args={[0.04, 0.04, 0.18, 8]} />
            </mesh>
            <mesh material={P(spec.gimbal.color)}>
              <sphereGeometry args={[spec.gimbal.r, 16, 12]} />
            </mesh>
            <mesh position={[0, 0, spec.gimbal.r * 0.85]} rotation={[Math.PI / 2, 0, 0]} material={P('#0b0c0e', true)}>
              <cylinderGeometry args={[spec.gimbal.r * 0.5, spec.gimbal.r * 0.55, spec.gimbal.r * 0.4, 14]} />
            </mesh>
          </group>

          {/* Skid landing gear: four struts down to two rails */}
          {[-1, 1].map((sx) => (
            <group key={sx}>
              {[-1, 1].map((sz) => (
                <mesh key={sz} position={[sx * (skidX * 0.62), (skidY + armY - 0.1) / 2, sz * 0.36]} rotation={[0, 0, sx * 0.32]} material={P(spec.skid)}>
                  <cylinderGeometry args={[0.035, 0.035, Math.abs(skidY - armY) + 0.05, 8]} />
                </mesh>
              ))}
              <mesh position={[sx * skidX, skidY, 0]} rotation={[Math.PI / 2, 0, 0]} material={P(spec.skid)}>
                <cylinderGeometry args={[0.04, 0.04, 1.3, 8]} />
              </mesh>
              {[-1, 1].map((sz) => (
                <mesh key={sz} position={[sx * skidX, skidY + 0.05, sz * 0.68]} rotation={[sz * -0.9, 0, 0]} material={P(spec.skid)}>
                  <cylinderGeometry args={[0.04, 0.04, 0.16, 8]} />
                </mesh>
              ))}
            </group>
          ))}

          {/* GPS dome on a mast */}
          {spec.dome && (
            <group position={[0, hh / 2 + (spec.hull === 'capsule' ? 0.12 : 0.02), spec.hull === 'capsule' ? -0.05 : 0]}>
              {spec.hull === 'capsule' && (
                <mesh position={[0, -0.05, 0]} material={P('#202225')}>
                  <cylinderGeometry args={[0.02, 0.02, 0.14, 6]} />
                </mesh>
              )}
              <mesh position={[0, 0.06, 0]} material={P(spec.dome)}>
                <sphereGeometry args={[spec.hull === 'capsule' ? 0.09 : 0.2, 16, 10, 0, Math.PI * 2, 0, spec.hull === 'capsule' ? Math.PI : Math.PI / 2]} />
              </mesh>
            </group>
          )}

          {/* USA: tail boom with vertical fin (GF-01) */}
          {country === 'USA' && (
            <group>
              <mesh position={[0, 0.02, -1.15]} rotation={[Math.PI / 2, 0, 0]} material={P('#c9cdd1')}>
                <cylinderGeometry args={[0.07, 0.11, 1.0, 10]} />
              </mesh>
              <mesh position={[0, 0.3, -1.58]} rotation={[0.35, 0, 0]} material={P('#eef0f2')}>
                <boxGeometry args={[0.04, 0.55, 0.3]} />
              </mesh>
              <mesh position={[0, -0.06, -1.58]} material={P('#eef0f2')}>
                <boxGeometry args={[0.04, 0.2, 0.2]} />
              </mesh>
            </group>
          )}

          {/* BRA: side foam canister */}
          {country === 'BRA' && (
            <mesh position={[0.42, tankY + 0.08, -0.05]} rotation={[Math.PI / 2, 0, 0]} material={P('#1f8a3a')}>
              <capsuleGeometry args={[0.12, 0.35, 6, 12]} />
            </mesh>
          )}

          {/* CHN: forward antenna */}
          {country === 'CHN' && (
            <mesh position={[-0.28, tankY + 0.15, hl / 2 + 0.2]} rotation={[Math.PI / 2 - 0.25, 0, 0]} material={P('#1e1f22')}>
              <cylinderGeometry args={[0.015, 0.015, 0.6, 6]} />
            </mesh>
          )}
        </>
      )}
    </group>
  );
}

type Paint = (c: string, hot?: boolean) => MeshStandardMaterial;

function Hull({ spec, P, full }: { spec: DroneSpec; P: Paint; full: boolean }) {
  const [w, h, l] = spec.hullSize;
  if (spec.hull === 'capsule') {
    // USA: long white fuselage, red side stripe, navy star roundel on the spine.
    return (
      <group>
        <mesh rotation={[Math.PI / 2, 0, 0]} scale={[1, 1, h / w]} material={P(spec.hullColor)}>
          <capsuleGeometry args={[w, l, 8, 16]} />
        </mesh>
        {full && spec.stripe && (
          <mesh position={[0, -0.04, 0.05]} rotation={[Math.PI / 2, 0, 0]} scale={[1, 1, h / w]} material={P(spec.stripe)}>
            <cylinderGeometry args={[w + 0.008, w + 0.008, l * 0.9, 16, 1, true]} />
          </mesh>
        )}
        {full && spec.stripe && (
          <mesh position={[0, -0.04, 0.05]} rotation={[Math.PI / 2, 0, 0]} scale={[1, 1, h / w]} material={P(spec.hullColor)}>
            <cylinderGeometry args={[w + 0.012, w + 0.012, l * 0.9, 16, 1, true, -Math.PI * 0.42, Math.PI * 0.84]} />
          </mesh>
        )}
        {full && spec.patch && (
          <group position={[0, h + 0.005, 0.3]}>
            <mesh rotation={[-Math.PI / 2, 0, 0]} material={P(spec.patch[0])}>
              <circleGeometry args={[0.15, 20]} />
            </mesh>
            <mesh position={[0, 0.003, 0]} rotation={[-Math.PI / 2, 0, Math.PI / 2]} material={P(spec.patch[1], true)}>
              <circleGeometry args={[0.09, 5]} />
            </mesh>
          </group>
        )}
      </group>
    );
  }
  if (spec.hull === 'octagon') {
    // CAN / BRA / AUS: chamfered octagonal hull, lighter crown.
    return (
      <group>
        <mesh rotation={[0, Math.PI / 8, 0]} scale={[1, 1, l / w]} material={P(spec.hullColor)}>
          <cylinderGeometry args={[w * 0.82, w, h, 8]} />
        </mesh>
        <mesh position={[0, h / 2 + 0.04, 0]} rotation={[0, Math.PI / 8, 0]} scale={[1, 1, l / w]} material={P(spec.hullTop ?? spec.hullColor)}>
          <cylinderGeometry args={[w * 0.6, w * 0.82, 0.08, 8]} />
        </mesh>
        {full &&
          spec.stripe &&
          // CAN white "CANADA" side panels / BRA yellow pinstripe
          [-1, 1].map((s) => (
            <mesh key={s} position={[s * w * 0.93, -0.02, 0.02]} rotation={[0, 0, s * -0.12]} material={P(spec.stripe!)}>
              <boxGeometry args={[0.02, spec.pinstripe ? 0.05 : h * 0.7, l * 0.9]} />
            </mesh>
          ))}
        {full && spec.patch && <Patch colors={spec.patch} y={h / 2 + 0.085} z={l * 0.3} P={P} />}
      </group>
    );
  }
  // CHN / DEU: faceted box hull with a bevelled lid, optional wrap-around stripe.
  return (
    <group>
      <mesh material={P(spec.hullColor)}>
        <boxGeometry args={[w, h, l]} />
      </mesh>
      <mesh position={[0, h / 2 + 0.05, -0.03]} material={P(spec.hullTop ?? spec.hullColor)}>
        <boxGeometry args={[w * 0.8, 0.1, l * 0.82]} />
      </mesh>
      <mesh position={[0, -h * 0.05, l / 2 + 0.06]} rotation={[0.5, 0, 0]} material={P(spec.hullColor)}>
        <boxGeometry args={[w * 0.92, h * 0.6, 0.16]} />
      </mesh>
      <mesh position={[0, -h * 0.1, l / 2 + 0.12]} material={P('#121315')}>
        <boxGeometry args={[w * 0.4, h * 0.18, 0.04]} />
      </mesh>
      {full && spec.stripe && (
        <mesh position={[0, -h * 0.12, 0]} material={P(spec.stripe, true)}>
          <boxGeometry args={[w + 0.01, h * 0.12, l + 0.01]} />
        </mesh>
      )}
      {full && spec.patch && <Patch colors={spec.patch} y={h / 2 + 0.105} z={l * 0.22} x={-w * 0.22} P={P} />}
    </group>
  );
}

function Patch({ colors, x = 0, y, z, P }: { colors: [string, string]; x?: number; y: number; z: number; P: Paint }) {
  return (
    <group position={[x, y, z]}>
      <mesh rotation={[-Math.PI / 2, 0, 0]} material={P(colors[0])}>
        <planeGeometry args={[0.24, 0.16]} />
      </mesh>
      <mesh position={[0, 0.002, 0]} rotation={[-Math.PI / 2, 0, 0]} material={P(colors[1], true)}>
        <planeGeometry args={[0.24, 0.05]} />
      </mesh>
    </group>
  );
}

function Tank({ spec, y, P, full }: { spec: DroneSpec; y: number; P: Paint; full: boolean }) {
  const { shape, size, color, band, caps } = spec.tank;
  const [w, h, l] = size;
  if (shape === 'capsule') {
    // USA lengthwise tank; BRA transverse saddle tank with a green band.
    const transverse = !!band;
    return (
      <group position={[0, y, 0]} rotation={transverse ? [0, Math.PI / 2, 0] : [0, 0, 0]}>
        <mesh rotation={[Math.PI / 2, 0, 0]} material={P(color)}>
          <capsuleGeometry args={[w, l, 6, 14]} />
        </mesh>
        {band && (
          <mesh rotation={[Math.PI / 2, 0, 0]} material={P(band)}>
            <cylinderGeometry args={[w + 0.01, w + 0.01, 0.07, 14, 1, true]} />
          </mesh>
        )}
        {!band && (
          <mesh position={[0, h * 0.5, -l * 0.15]} material={P('#22262a')}>
            <boxGeometry args={[w * 1.5, h * 0.7, l * 0.6]} />
          </mesh>
        )}
      </group>
    );
  }
  return (
    <group position={[0, y, 0]}>
      <mesh material={P(color)}>
        <boxGeometry args={[w, h, l]} />
      </mesh>
      {full &&
        band &&
        (spec.tank.ribs ? (
          // CAN ribbed tank
          [-0.3, 0, 0.3].map((t) => (
            <mesh key={t} position={[w * t, 0, 0]} material={P(band)}>
              <boxGeometry args={[0.02, h + 0.01, l + 0.01]} />
            </mesh>
          ))
        ) : (
          // DEU yellow wrap stripe
          <mesh position={[0, -h * 0.1, 0]} material={P(band, true)}>
            <boxGeometry args={[w + 0.01, h * 0.16, l + 0.01]} />
          </mesh>
        ))}
      {full &&
        caps &&
        [-1, 1].map((s) => (
          <mesh key={s} position={[s * (w / 2 + 0.03), 0, 0]} material={P(caps)}>
            <boxGeometry args={[0.08, h * 0.92, l * 0.92]} />
          </mesh>
        ))}
      {full && (
        <mesh position={[0, h / 2 + 0.04, 0]} material={P('#202225')}>
          <cylinderGeometry args={[w * 0.32, w * 0.36, 0.08, 12]} />
        </mesh>
      )}
    </group>
  );
}
