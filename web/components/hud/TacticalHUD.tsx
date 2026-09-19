'use client';
import { useGame, type VisionMode } from '@/store/gameStore';
import { useTelemetry } from '@/store/telemetryStore';
import { FLEETS } from '@/lib/config/fleets';

const MODES: { id: VisionMode; label: string }[] = [
  { id: 'standard', label: 'STANDARD' },
  { id: 'ir', label: 'IR WHITE-HOT' },
  { id: 'lidar', label: 'LIDAR CLOUD' },
];

export function TacticalHUD() {
  const droneId = useGame((s) => s.tacticalDroneId);
  const drone = useGame((s) => s.drones.find((d) => d.id === droneId));
  const vision = useGame((s) => s.visionMode);
  const setVision = useGame((s) => s.setVisionMode);
  const exit = useGame((s) => s.exitTactical);
  const fires = useGame((s) => s.fires);
  const t = useTelemetry();
  if (!drone) return null;
  const fleet = FLEETS[drone.country];
  const target = fires.find((f) => f.id === t.targetFireId);
  const payloadPct = Math.round((drone.payloadLitres / drone.payloadMax) * 100);
  const cls = vision === 'ir' ? 'thud thud--ir' : vision === 'lidar' ? 'thud thud--lidar' : 'thud';

  return (
    <div className={`${cls} pointer-events-none absolute inset-0 z-30`}>
      {/* Vision overlay tint */}
      <div className="thud__tint" />
      {/* Reticle */}
      <div className={`reticle ${t.reticleLocked ? 'reticle--locked' : ''}`}>
        <div className="reticle__ring" />
        <div className="reticle__cross" />
        <div className="reticle__label">{t.reticleLocked ? `DROP WINDOW · ${target?.label ?? 'TARGET'} · ${target ? target.frp.toFixed(0) : '—'} MW` : 'NO TARGET IN DROP WINDOW'}</div>
        {t.lastDropKnockdownMW > 0 && <div className="reticle__hit">−{t.lastDropKnockdownMW.toFixed(0)} MW</div>}
      </div>
      {/* Left telemetry */}
      <div className="glass thud__panel thud__panel--left pointer-events-auto">
        <div className="panel__title">{fleet.flag} {drone.callSign} · {fleet.drone.model}</div>
        <div className="panel__sub">{fleet.drone.livery.description}</div>
        <Gauge label="ALTITUDE" value={`${t.altitude.toFixed(0)} m AGL`} pct={Math.min(100, t.altitude / 6)} />
        <Gauge label="AIRSPEED" value={`${t.airspeed.toFixed(0)} km/h`} pct={Math.min(100, (t.airspeed / (fleet.drone.tactical.maxSpeed * 3.6 * 1.8)) * 100)} />
        <Gauge label="BATTERY" value={`${drone.battery.toFixed(0)}%`} pct={drone.battery} warn={drone.battery < 25} />
        <Gauge label={`SUPPRESSANT · ${fleet.drone.suppressant.toUpperCase()}`} value={`${payloadPct}% · ${Math.round(drone.payloadLitres)} L`} pct={payloadPct} warn={payloadPct < 20} />
        <div className="mt-2 text-[11px] text-white/60">HDG {t.heading.toFixed(0).padStart(3, '0')}° · TARGET {Number.isFinite(t.targetDistance) ? `${(t.targetDistance / 1000).toFixed(2)} km` : '—'}</div>
      </div>
      {/* Right: vision + abilities */}
      <div className="glass thud__panel thud__panel--right pointer-events-auto">
        <div className="panel__title">VISION MODE <span className="text-white/40">(V)</span></div>
        <div className="mt-1 flex flex-col gap-1">
          {MODES.map((m) => (
            <button key={m.id} className={`btn btn--xs ${vision === m.id ? 'btn--active' : ''}`} onClick={() => setVision(m.id)}>{m.label}</button>
          ))}
        </div>
        <div className="panel__title mt-3">ABILITIES</div>
        <ul className="mt-1 space-y-1 text-[11px]">
          {fleet.drone.abilities.map((a) => (<li key={a.id} className="chip block text-left" title={a.description}>{a.name}</li>))}
        </ul>
        <button className="btn mt-3 w-full" onClick={exit}>GLOBAL RTS VIEW (ESC)</button>
      </div>
      {/* Bottom prompt */}
      <div className="thud__prompt">
        {t.nearCivilian ? <span className="thud__alert">▣ PERSONNEL IN RANGE — HOLD R TO EXTRACT</span> : <span>W/S THROTTLE · A/D YAW · Q/E ALT · SHIFT BOOST · SPACE DROP · R EXTRACT · V VISION</span>}
        <span className="ml-4 text-white/50">SURVIVORS REMAINING {t.civiliansRemaining}</span>
      </div>
      {vision === 'ir' && <div className="thud__watermark">IR · WHITE HOT · {fleet.drone.abilities.some((a) => a.id === 'uxo') ? 'UXO SWEEP ACTIVE' : 'THERMAL IMAGING'}</div>}
      {vision === 'lidar' && <div className="thud__watermark">LIDAR · POINT CLOUD · CANOPY PATHWAYS</div>}
    </div>
  );
}

function Gauge({ label, value, pct, warn }: { label: string; value: string; pct: number; warn?: boolean }) {
  return (
    <div className="mt-2">
      <div className="flex justify-between text-[10px] tracking-widest text-white/60"><span>{label}</span><span className={warn ? 'text-rose-300' : 'text-white/90'}>{value}</span></div>
      <div className="gauge"><div className={`gauge__fill ${warn ? 'gauge__fill--warn' : ''}`} style={{ width: `${Math.max(0, Math.min(100, pct))}%` }} /></div>
    </div>
  );
}
