'use client';
import { SCENARIOS } from '@/lib/config/scenarios';
import { FLEETS, fleetUiColor } from '@/lib/config/fleets';
import { useGame } from '@/store/gameStore';

export function ScenarioMenu() {
  const startScenario = useGame((s) => s.startScenario);
  const startFreePlay = useGame((s) => s.startFreePlay);
  const feedStatus = useGame((s) => s.feedStatus);
  const feed = useGame((s) => s.feed);
  return (
    <div className="pointer-events-auto absolute inset-0 z-40 flex items-center justify-center bg-gradient-to-b from-black/70 via-black/40 to-black/80 p-6">
      <div className="glass w-full max-w-6xl px-8 py-7">
        <div className="flex flex-wrap items-end justify-between gap-4">
          <div>
            <div className="brand__title text-3xl">GLOBAL FIREFIGHT</div>
            <div className="brand__sub text-base">DRONE COMMAND · WEB PROTOTYPE (GOD EYE FORK)</div>
            <p className="mt-2 max-w-2xl text-sm text-white/70">
              Dual-mode RTS and third-person tactical firefighting simulation driven by live NASA FIRMS and EONET data. Select a campaign to stage the national carrier, or enter free play against today&apos;s global fire picture.
            </p>
          </div>
          <div className="text-right text-xs text-white/60">
            <div>FEED: <span className="text-cyan-glow">{feedStatus.toUpperCase()}</span></div>
            {feed && <div>{feed.hotspots.length.toLocaleString()} hotspots · {feed.events.length} named events</div>}
            {feed?.message && <div className="max-w-[260px] text-white/40">{feed.message}</div>}
          </div>
        </div>

        <div className="mt-6 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          {SCENARIOS.map((s) => {
            const f = FLEETS[s.country];
            return (
              <button key={s.id} className="scenario" style={{ ['--accent' as string]: fleetUiColor(s.country) }} onClick={() => startScenario(s.id)}>
                <div className="scenario__head">
                  <span className="text-xl">{f.flag}</span>
                  <span className="scenario__year">{s.year}</span>
                </div>
                <div className="scenario__title">{s.title}</div>
                <div className="scenario__loc">{s.location}</div>
                <ul className="scenario__list">
                  {s.constraints.slice(0, 3).map((c) => (<li key={c}>▸ {c}</li>))}
                </ul>
                <div className="scenario__asset">{f.drone.model} · {f.carrier.model}</div>
              </button>
            );
          })}
          <button className="scenario scenario--free" style={{ ['--accent' as string]: '#5ef2ff' }} onClick={startFreePlay}>
            <div className="scenario__head"><span className="text-xl">🌍</span><span className="scenario__year">LIVE</span></div>
            <div className="scenario__title">Global Free Play</div>
            <div className="scenario__loc">All six national carriers · live FIRMS feed</div>
            <ul className="scenario__list">
              <li>▸ Engage any of today&apos;s hotspots</li>
              <li>▸ Deploy forward carriers anywhere</li>
              <li>▸ EONET named crises as beacons</li>
            </ul>
            <div className="scenario__asset">USA · CAN · BRA · CHN · DEU · AUS</div>
          </button>
        </div>
        <div className="mt-5 text-[11px] text-white/45">
          CONTROLS — Globe: drag to orbit, wheel to zoom. Select carrier → click fire to dispatch. Tactical: W/S throttle, A/D yaw, Q/E altitude, SHIFT boost, SPACE drop, R rescue (hold), V vision mode, ESC back.
        </div>
      </div>
    </div>
  );
}
