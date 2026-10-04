'use client';
import dynamic from 'next/dynamic';
import { useCallback, useEffect, useState } from 'react';
import { COUNTRY_CODES, FLEETS, fleetUiColor, type CountryCode } from '@/lib/config/fleets';
import { SCENARIOS, SCENARIO_BY_ID } from '@/lib/config/scenarios';
import { pressPhotoUrl } from '@/lib/config/campaignStories';
import { fmtUSD } from '@/lib/engine/economics';
import { useGame } from '@/store/gameStore';
import { carrierArtUrl, droneArtUrl, insigniaUrl, portraitUrl } from '@/lib/config/teamArt';
import { TeamImage } from './TeamImage';
import { Flag } from './Flag';
import { MissionBriefing } from './MissionBriefing';

const ArmoryViewer = dynamic(() => import('./ArmoryViewer').then((m) => m.ArmoryViewer), { ssr: false });

/**
 * Country select: one nation showcased at a time using the pitch-deck artwork —
 * unit insignia, drone + carrier concept art (slides 17–22) and the six-person
 * crew line-up (slides 32–37) — plus specs and abilities. A rail of small flags
 * switches nation (click, ←/→, 1–6). The always-visible campaign bar is step 2;
 * picking a campaign (or Enter) opens its briefing (step 3), which starts the mission.
 */
export function CountrySelect() {
  const [index, setIndex] = useState(0);
  const [view, setView] = useState<'art' | '3d'>('art');
  const [crewFocus, setCrewFocus] = useState<number | null>(null);
  const [briefingId, setBriefingId] = useState<string | null>(null);
  const startScenario = useGame((s) => s.startScenario);
  const startFreePlay = useGame((s) => s.startFreePlay);
  const feedStatus = useGame((s) => s.feedStatus);
  const feed = useGame((s) => s.feed);
  const code: CountryCode = COUNTRY_CODES[index];
  const f = FLEETS[code];
  const accent = fleetUiColor(code);
  const campaigns = SCENARIOS.filter((s) => s.country === code);

  const step = useCallback((d: number) => {
    setCrewFocus(null);
    setIndex((i) => (i + d + COUNTRY_CODES.length) % COUNTRY_CODES.length);
  }, []);
  useEffect(() => {
    if (briefingId) return; // the briefing owns the keyboard while open
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'ArrowRight' || e.key === 'ArrowDown') step(1);
      else if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') step(-1);
      else if (/^[1-6]$/.test(e.key)) {
        setCrewFocus(null);
        setIndex(Number(e.key) - 1);
      } else if (e.key === 'Enter' && campaigns[0]) {
        // Without preventDefault the same keypress would also "click" the briefing's autofocused BEGIN button.
        e.preventDefault();
        setBriefingId(campaigns[0].id);
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [step, campaigns, briefingId]);
  const briefing = briefingId ? SCENARIO_BY_ID[briefingId] : null;
  const closeBriefing = useCallback(() => setBriefingId(null), []);
  const beginMission = useCallback(() => briefingId && startScenario(briefingId), [briefingId, startScenario]);

  const focused = crewFocus !== null ? f.crew[crewFocus] : null;

  return (
    <div className="cs pointer-events-auto absolute inset-0 z-40" style={{ ['--accent' as string]: accent }}>
      <div className="cs__header">
        <div>
          <div className="brand__title text-2xl">GLOBAL FIREFIGHT</div>
          <div className="brand__sub">DRONE COMMAND</div>
        </div>
        <ol className="steps" aria-label="Setup steps">
          <li className={briefing ? 'steps__done' : 'steps__now'}><span>1</span> NATION</li>
          <li className={briefing ? 'steps__done' : 'steps__now'}><span>2</span> CAMPAIGN</li>
          <li className={briefing ? 'steps__now' : ''}><span>3</span> BRIEFING</li>
          <li><span>▶</span> DEPLOY</li>
        </ol>
        <div className="text-right text-[11px] text-white/60">
          <div>FEED <span className="text-cyan-glow">{feedStatus.toUpperCase()}</span>{feed ? ` · ${feed.hotspots.length.toLocaleString()} hotspots · ${feed.events.length} named events` : ''}</div>
          <div className="text-white/40">← → switch nation · 1–6 jump · ENTER briefing</div>
        </div>
      </div>

      <nav className="cs__rail" aria-label="Nations">
        {COUNTRY_CODES.map((c, i) => (
          <button key={c} className={`cs__flag ${i === index ? 'cs__flag--active' : ''}`} style={{ ['--accent' as string]: fleetUiColor(c) }} onClick={() => { setCrewFocus(null); setIndex(i); }} title={FLEETS[c].country}>
            <Flag code={c} h={26} className="cs__flag-img" />
            <span className="cs__flag-code">{c}</span>
          </button>
        ))}
        <button className="cs__flag cs__flag--free" style={{ ['--accent' as string]: '#5ef2ff' }} onClick={startFreePlay} title="Global free play — all six carriers">
          <span className="cs__flag-emoji">🌍</span>
          <span className="cs__flag-code">ALL</span>
        </button>
      </nav>

      <section className="cs__stage glass" key={code}>
        <div className="cs__stage-head">
          <TeamImage src={insigniaUrl(code)} alt={`${f.agency} insignia`} className="cs__insignia" fallback={<Flag code={code} h={56} />} />
          <div>
            <div className="cs__country"><Flag code={code} h={30} className="cs__country-flag" /> {f.country}</div>
            <div className="cs__agency">{f.agency} · <span className="italic text-white/60">{f.motto}</span></div>
          </div>
          <div className="cs__nav">
            <button className="btn btn--xs" onClick={() => step(-1)} aria-label="Previous nation">◀</button>
            <span className="stat__sub">{index + 1} / {COUNTRY_CODES.length}</span>
            <button className="btn btn--xs" onClick={() => step(1)} aria-label="Next nation">▶</button>
          </div>
        </div>

        <div className="cs__body">
          <div className="cs__left">
            {/* Armory: deck concept art (default) or live 3D turntable */}
            <div className="cs__armory-panel">
              <div className="cs__toggle">
                <button className={`btn btn--xs ${view === 'art' ? 'btn--active' : ''}`} onClick={() => setView('art')}>CONCEPT ART</button>
                <button className={`btn btn--xs ${view === '3d' ? 'btn--active' : ''}`} onClick={() => setView('3d')}>3D MODEL</button>
              </div>
              {view === 'art' ? (
                <div className="cs__deck">
                  <figure className="cs__deck-item">
                    <TeamImage src={droneArtUrl(code)} alt={f.drone.model} className="cs__deck-img" fallback={<div className="cs__deck-missing">{f.drone.model}</div>} />
                    <figcaption><span className="stat__label">DRONE</span><span className="cs__model">{f.drone.model}</span><span className="stat__sub">{f.drone.livery.description}</span></figcaption>
                  </figure>
                  <figure className="cs__deck-item">
                    <TeamImage src={carrierArtUrl(code)} alt={f.carrier.model} className="cs__deck-img" fallback={<div className="cs__deck-missing">{f.carrier.model}</div>} />
                    <figcaption><span className="stat__label">MOBILE COMMAND CARRIER</span><span className="cs__model">{f.carrier.model}</span><span className="stat__sub">{f.carrier.description} · {f.carrier.droneCapacity} drones · rearm {f.carrier.rearmSeconds}s</span></figcaption>
                  </figure>
                </div>
              ) : (
                <div className="cs__viewer">
                  <ArmoryViewer country={code} />
                </div>
              )}
            </div>

            {/* Crew line-up from the team slide */}
            <div className="cs__lineup-panel">
              <div className="cs__lineup-head">
                <div className="panel__title">COMMAND CREW · {f.agency}</div>
                <div className="cs__lineup-focus">
                  {focused ? (<><b>{focused.name}</b> · {focused.role} · <span className="cs__crew-cs">{focused.callSign}</span></>) : <span className="text-white/40">Hover a crew member</span>}
                </div>
              </div>
              <div className="cs__lineup" onMouseLeave={() => setCrewFocus(null)}>
                {f.crew.map((m, i) => (
                  <button key={m.callSign} className={`cs__figure ${crewFocus === i ? 'cs__figure--focus' : ''}`} onMouseEnter={() => setCrewFocus(i)} onFocus={() => setCrewFocus(i)} title={`${m.name} — ${m.role}`}>
                    <TeamImage src={portraitUrl(code, i)} alt={`${m.name}, ${m.role}`} className="cs__figure-img" fallback={<span className="cs__crew-avatar">{m.name.split(' ').slice(-1)[0][0]}</span>} />
                    <span className="cs__figure-role">{m.role}</span>
                    <span className="cs__figure-cs">{m.callSign}</span>
                  </button>
                ))}
              </div>
            </div>
          </div>

          <div className="cs__info">
            <div className="panel__title">ARMORY SPECS</div>
            <div className="cs__specs">
              <Spec k="PAYLOAD" v={`${f.drone.payloadLitres} L ${f.drone.suppressant}`} />
              <Spec k="CRUISE" v={`${f.drone.cruiseKmh} km/h`} />
              <Spec k="ENDURANCE" v={`${f.drone.enduranceMin} min`} />
              <Spec k="SORTIE" v={f.drone.swarmSize > 1 ? `SWARM ×${f.drone.swarmSize}` : 'SINGLE'} />
              <Spec k="DEPLOY COST" v={fmtUSD(f.costs.deploymentUSD)} />
              <Spec k="FLIGHT / MIN" v={fmtUSD(f.costs.flightPerMinuteUSD)} />
            </div>

            <div className="panel__title mt-3">ABILITIES</div>
            <ul className="cs__abilities">
              {f.drone.abilities.map((a) => (
                <li key={a.id}><span className="cs__ability-name">{a.name}</span><span className="cs__ability-desc">{a.description}</span></li>
              ))}
            </ul>

          </div>
        </div>

        {/* Step 2: always-visible campaign bar — the way forward from this screen. */}
        <div className="cs__deploy">
          <div className="cs__deploy-head">
            <span className="step-pill">STEP 2 / 3</span>
            <span className="cs__deploy-title">CHOOSE A {f.country.toUpperCase()} CAMPAIGN</span>
            <span className="cs__deploy-sub">or pick another nation on the left</span>
          </div>
          <div className="cs__deploy-row">
            {campaigns.map((s) => (
              <button key={s.id} className="mission-card" onClick={() => setBriefingId(s.id)}>
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img className="mission-card__img" src={pressPhotoUrl(s.id)} alt="" />
                <div className="mission-card__body">
                  <div className="mission-card__year">{s.year}</div>
                  <div className="mission-card__title">{s.title}</div>
                  <div className="mission-card__loc">{s.location}</div>
                  <div className="mission-card__meta">{s.fires.length} fires · {s.objectives.length} objectives · budget {fmtUSD(s.budgetUSD)}</div>
                </div>
                <span className="mission-card__cta">VIEW BRIEFING ▶</span>
              </button>
            ))}
            <button className="mission-card mission-card--free" onClick={startFreePlay}>
              <div className="mission-card__body">
                <div className="mission-card__year">SANDBOX</div>
                <div className="mission-card__title">Global Free Play</div>
                <div className="mission-card__loc">All six carriers · live NASA FIRMS hotspots worldwide</div>
              </div>
              <span className="mission-card__cta mission-card__cta--ghost">PLAY ▶</span>
            </button>
          </div>
        </div>
      </section>

      {briefing && <MissionBriefing scenario={briefing} onBack={closeBriefing} onBegin={beginMission} />}
    </div>
  );
}

function Spec({ k, v }: { k: string; v: string }) {
  return (
    <div className="cs__spec">
      <div className="stat__label">{k}</div>
      <div className="cs__spec-v">{v}</div>
    </div>
  );
}
