'use client';
import { useGame } from '@/store/gameStore';
import { FLEETS, COUNTRY_CODES, fleetUiColor } from '@/lib/config/fleets';
import { fmtUSD } from '@/lib/engine/economics';
import { intensity } from '@/lib/engine/fire';

export function BottomBar() {
  const selection = useGame((s) => s.selection);
  const carriers = useGame((s) => s.carriers);
  const fires = useGame((s) => s.fires);
  const drones = useGame((s) => s.drones);
  const placing = useGame((s) => s.placingCarrier);
  const moveArmed = useGame((s) => s.moveArmed);
  const scenario = useGame((s) => s.scenario);
  const showLive = useGame((s) => s.showLiveFeed);
  const st = useGame.getState;

  const carrier = selection?.type === 'carrier' ? carriers.find((c) => c.id === selection.id) : undefined;
  const fire = selection?.type === 'fire' ? fires.find((f) => f.id === selection.id) : undefined;
  const drone = selection?.type === 'drone' ? drones.find((d) => d.id === selection.id) : undefined;
  const airborne = drones.length > 0;

  return (
    <footer className="pointer-events-auto absolute inset-x-0 bottom-0 z-30 flex items-end gap-2 p-3">
      {/* Selected unit */}
      <div className="glass min-w-[340px] max-w-[460px] px-4 py-3">
        {carrier && (() => {
          const f = FLEETS[carrier.country];
          return (
            <div>
              <div className="panel__title">{f.flag} {f.carrier.model}</div>
              <div className="panel__sub">{f.carrier.description} · {carrier.label.split(' — ')[1] ?? ''}</div>
              <div className="mt-2 grid grid-cols-3 gap-2 text-xs">
                <Kv k="DRONES READY" v={`${carrier.dronesReady}/${f.carrier.droneCapacity}`} />
                <Kv k="AIRFRAME" v={f.drone.model} />
                <Kv k="PAYLOAD" v={`${f.drone.payloadLitres} L ${f.drone.suppressant}`} />
                <Kv k="SORTIE" v={f.drone.swarmSize > 1 ? `SWARM ×${f.drone.swarmSize}` : 'SINGLE'} />
                <Kv k="DEPLOY COST" v={fmtUSD(f.costs.deploymentUSD)} />
                <Kv k="POSITION" v={`${carrier.lat.toFixed(2)}°, ${carrier.lon.toFixed(2)}°`} />
              </div>
              <div className="mt-2 flex flex-wrap gap-1">
                {f.drone.abilities.map((a) => (
                  <span key={a.id} className="chip" title={a.description}>{a.name}</span>
                ))}
              </div>
              <div className="mt-3 flex gap-2">
                <button className={`btn ${moveArmed ? 'btn--active' : ''}`} onClick={() => st().setMoveArmed(!moveArmed)}>{moveArmed ? 'CLICK GLOBE TO MOVE…' : 'MOVE'}</button>
                <button className="btn btn--primary" disabled={carrier.dronesReady <= 0} title="Select a fire on the globe to dispatch" onClick={() => st().pushLog('Dispatch armed — click a fire to launch.', 'alert')}>
                  DISPATCH → CLICK FIRE
                </button>
              </div>
            </div>
          );
        })()}
        {fire && (
          <div>
            <div className="panel__title">🔥 {fire.label ?? 'ACTIVE FIRE'}</div>
            <div className="panel__sub">{fire.lat.toFixed(3)}°, {fire.lon.toFixed(3)}° · {fire.source === 'scenario' ? 'CAMPAIGN TARGET' : 'LIVE FIRMS HOTSPOT'}</div>
            <div className="mt-2 grid grid-cols-3 gap-2 text-xs">
              <Kv k="FRP" v={`${fire.frp.toFixed(0)} MW`} />
              <Kv k="INTENSITY" v={`${Math.round(intensity(fire) * 100)}%`} />
              <Kv k="STATUS" v={fire.extinguished ? 'OUT' : fire.contained ? 'CONTAINED' : 'SPREADING'} />
              <Kv k="PROPERTY AT RISK" v={fmtUSD(fire.propertyRemainingUSD)} />
              <Kv k="PEOPLE AT RISK" v={Math.round(fire.populationRemaining).toLocaleString()} />
              <Kv k="ON STATION" v={String(drones.filter((d) => d.targetFireId === fire.id && d.state !== 'enroute').length)} />
            </div>
            <div className="mt-3 flex gap-2">
              <button className="btn btn--primary" disabled={fire.extinguished} onClick={() => st().deploySuppressant(fire.id)}>DEPLOY SUPPRESSANT</button>
              <button className="btn" onClick={() => st().flyTo(fire.lat, fire.lon, 1.05)}>FOCUS</button>
              {carriers.length > 0 && !fire.extinguished && (
                <select className="btn" defaultValue="" onChange={(e) => { if (e.target.value) st().dispatch(e.target.value, fire.id); e.target.value = ''; }}>
                  <option value="">DISPATCH FROM…</option>
                  {carriers.map((c) => (<option key={c.id} value={c.id}>{FLEETS[c.country].flag} {FLEETS[c.country].carrier.model} ({c.dronesReady})</option>))}
                </select>
              )}
            </div>
          </div>
        )}
        {drone && (() => {
          const f = FLEETS[drone.country];
          const target = fires.find((x) => x.id === drone.targetFireId);
          return (
            <div>
              <div className="panel__title">{f.flag} {drone.callSign} · {f.drone.model}</div>
              <div className="panel__sub">{drone.state.toUpperCase()} → {target?.label ?? 'target'}</div>
              <div className="mt-2 grid grid-cols-3 gap-2 text-xs">
                <Kv k="PROGRESS" v={`${Math.round(drone.t * 100)}%`} />
                <Kv k="BATTERY" v={`${drone.battery.toFixed(0)}%`} />
                <Kv k="SUPPRESSANT" v={`${Math.round((drone.payloadLitres / drone.payloadMax) * 100)}%`} />
                <Kv k="RANGE" v={`${drone.distanceKm.toFixed(0)} km`} />
                <Kv k="DROPS" v={String(drone.drops)} />
                <Kv k="FLIGHT TIME" v={`${(drone.flightTimeSec / 60).toFixed(0)} min`} />
              </div>
              <div className="mt-3 flex gap-2">
                <button className="btn btn--primary" onClick={() => st().enterTactical(drone.id)}>TACTICAL DRONE VIEW</button>
                <button className="btn" onClick={() => target && st().flyTo(target.lat, target.lon, 1.05)}>FOCUS TARGET</button>
              </div>
            </div>
          );
        })()}
        {!carrier && !fire && !drone && (
          <div>
            <div className="panel__title">NO UNIT SELECTED</div>
            <div className="panel__sub">
              {placing ? `Click the globe to deploy the ${FLEETS[placing].carrier.model}.` : 'Select a carrier, then click a fire to dispatch. Click a live hotspot to engage it.'}
            </div>
            {scenario && (
              <ul className="mt-2 space-y-1 text-xs">
                {scenario.objectives.map((o) => (<li key={o.id} className="objective">{o.optional ? '◇' : '◆'} {o.title}</li>))}
              </ul>
            )}
          </div>
        )}
      </div>

      {/* Fleet / carrier deployment */}
      <div className="glass flex-1 px-4 py-3">
        <div className="flex items-center justify-between">
          <div className="panel__title">CARRIER DEPLOYMENT</div>
          <div className="flex gap-1">
            <button className={`btn btn--xs ${showLive ? 'btn--active' : ''}`} onClick={() => st().toggleLiveFeed()}>FIRMS LAYER</button>
            <button className="btn btn--xs" onClick={() => st().backToMenu()}>MENU</button>
          </div>
        </div>
        <div className="mt-2 grid grid-cols-3 gap-1 xl:grid-cols-6">
          {COUNTRY_CODES.map((c) => {
            const f = FLEETS[c];
            const owned = carriers.filter((x) => x.country === c);
            return (
              <button
                key={c}
                className={`fleet ${placing === c ? 'fleet--active' : ''}`}
                style={{ borderColor: fleetUiColor(c) }}
                onClick={() => (placing === c ? st().cancelPlacing() : st().beginPlacingCarrier(c))}
                title={`${f.carrier.model} — click, then click the globe to deploy`}
              >
                <div className="fleet__flag">{f.flag}</div>
                <div className="fleet__name">{f.drone.model}</div>
                <div className="fleet__meta">{f.agency} · {owned.length} deployed</div>
              </button>
            );
          })}
        </div>
      </div>

      {/* Camera toggle */}
      <div className="glass flex flex-col gap-2 px-4 py-3">
        <button className="btn btn--primary btn--tall" disabled={!airborne} onClick={() => st().enterTactical()} title={airborne ? 'Take manual control of an airborne drone' : 'Dispatch a drone first'}>
          TACTICAL<br />DRONE VIEW
        </button>
        <div className="stat__sub text-center">{drones.length} airborne</div>
      </div>
    </footer>
  );
}

function Kv({ k, v }: { k: string; v: string }) {
  return (
    <div>
      <div className="stat__label">{k}</div>
      <div className="font-mono text-[13px] text-white/90">{v}</div>
    </div>
  );
}
