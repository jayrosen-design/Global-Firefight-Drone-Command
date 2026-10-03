import type { Metadata } from 'next';
import Link from 'next/link';
import { COUNTRY_CODES, FLEETS, fleetUiColor } from '@/lib/config/fleets';
import { SCENARIOS } from '@/lib/config/scenarios';
import { droneArtUrl, flagUrl, insigniaUrl } from '@/lib/config/teamArt';
import { PLAY_PATH, REPO_URL, STEAM_PAGE_LIVE, STEAM_URL } from '@/lib/config/site';
import './landing.css';

export const metadata: Metadata = {
  title: 'Global Firefight: Drone Command — Command the world’s firefighting drone fleets',
  description:
    'A dual-mode RTS and drone-piloting firefighting game on a live 3D globe of real NASA fire data. Six nations, seven historic campaigns, split-view live feeds, infrared and LIDAR. Play the demo in your browser.',
  openGraph: { images: ['/media/09-split-view-feeds.webp'] },
};

/** Muted, looping gameplay clip (no audio in any clip). */
function Clip({ name, label }: { name: string; label: string }) {
  return (
    <figure className="lp-clip">
      <video poster={`/media/${name}.jpg`} autoPlay muted loop playsInline preload="metadata" aria-label={label}>
        <source src={`/media/${name}.webm`} type="video/webm" />
        <source src={`/media/${name}.mp4`} type="video/mp4" />
      </video>
      <figcaption>{label}</figcaption>
    </figure>
  );
}

function Buttons({ size = 'lg' }: { size?: 'lg' | 'md' }) {
  return (
    <div className={`lp-cta lp-cta--${size}`}>
      <Link href={PLAY_PATH} className="lp-btn lp-btn--play">
        <span aria-hidden>▶</span> PLAY DEMO
      </Link>
      <a href={STEAM_URL} target="_blank" rel="noopener noreferrer" className="lp-btn lp-btn--steam">
        <SteamMark /> WISHLIST ON STEAM
      </a>
    </div>
  );
}

function SteamMark() {
  return (
    <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden fill="currentColor">
      <path d="M12 2a10 10 0 0 0-9.95 9.05l5.35 2.21a2.83 2.83 0 0 1 1.6-.5h.16l2.38-3.45v-.05a3.77 3.77 0 1 1 3.77 3.77h-.09l-3.4 2.42v.13a2.83 2.83 0 0 1-5.6.56L2.4 14.6A10 10 0 1 0 12 2Zm-4.3 15.2-1.23-.5a2.12 2.12 0 1 0 1.16-2.9l1.27.53a1.56 1.56 0 1 1-1.2 2.87Zm9.23-7.08a2.51 2.51 0 1 0-2.51 2.51 2.51 2.51 0 0 0 2.51-2.51Zm-4.39 0a1.89 1.89 0 1 1 1.88 1.89 1.88 1.88 0 0 1-1.88-1.89Z" />
    </svg>
  );
}

const CHAPTERS = [
  {
    n: '01',
    title: 'Choose your nation',
    clip: 'clip-select',
    clipLabel: 'Country select — crew line-up, drone & carrier art, abilities, campaigns',
    body: (
      <>
        <p>Six real-world fire services stand ready. Each brings its own crew, drone airframe, Mobile Command Carrier, liveries, abilities and costs — from the USFS Guardian Mk IV laying Phos-Chek lines to Canada’s FireSwarm Thunder Wasps that fly in synchronized three-drone swarms.</p>
        <ul>
          <li><b>←/→</b> or the flag rail switches nation; <b>1–6</b> jumps; <b>Enter</b> launches the first campaign.</li>
          <li>Toggle <b>Concept Art / 3D Model</b> to inspect the armory; hover crew to see roles and call signs.</li>
        </ul>
      </>
    ),
  },
  {
    n: '02',
    title: 'Read the globe',
    img: '02-global-rts-freeplay.webp',
    imgLabel: 'Global RTS view — live FIRMS hotspots, EONET named events, six staged carriers',
    body: (
      <>
        <p>The Global Command View is a WGS84 Earth streaming real fire data. Every glowing point is a <b>NASA FIRMS</b> satellite hotspot sized and coloured by Fire Radiative Power; pulsing beacons mark named crises from <b>NASA EONET</b>. With a Google or Cesium ion key the globe streams Google Photorealistic 3D Tiles — the same map stack as God’s Eye View.</p>
        <ul>
          <li>Drag to orbit, scroll to zoom. Click a hotspot to engage it as a mission.</li>
          <li>The top bar tracks <b>Net Score</b>, property and lives saved, suppression cost and remaining budget in real time.</li>
        </ul>
      </>
    ),
  },
  {
    n: '03',
    title: 'Deploy carriers, dispatch swarms',
    clip: 'clip-dispatch',
    clipLabel: 'A US carrier deployed at Calgary sends a drone on a long great-circle arc to Fort McMurray',
    body: (
      <>
        <p>Select a carrier, then click a fire: drones launch along great-circle arcs with real flight times, orbit on arrival and make coordinated drops until the fire is out — then return to rearm. Deploy forward carriers anywhere from the fleet panel, or order a carrier to <b>MOVE</b>.</p>
        <ul>
          <li>Short hops are fast; intercontinental ferries cost flight time and battery.</li>
          <li>Retardant contains a fire’s spread; foam and water knock down its intensity.</li>
        </ul>
      </>
    ),
  },
  {
    n: '04',
    title: 'Split view: watch every front',
    clip: 'clip-split',
    clipLabel: 'Live feeds — carrier launch close-up and an IR drone cam over the fire, beside the live globe',
    body: (
      <>
        <p>Click anywhere on the globe to open a <b>live feed</b> beside it — up to four at once — while the globe keeps running the game. Each feed renders that location in 3D with its fires, structures, carriers and drones.</p>
        <ul>
          <li>Per-feed sensor: <b>STD · IR white-hot · LIDAR</b>. Per-feed camera: <b>Area · Drone Cam · Carrier</b>.</li>
          <li><b>PILOT</b> jumps into any drone working that area; numbered markers on the globe match each window.</li>
        </ul>
      </>
    ),
  },
  {
    n: '05',
    title: 'Take the stick',
    clip: 'clip-pilot',
    clipLabel: 'Tactical drone view — manual flight and drops in standard, IR white-hot and LIDAR',
    body: (
      <>
        <p>Drop into the third-person <b>Tactical Drone View</b> and fly the run yourself. The reticle predicts where your payload will land; it turns gold when the drop window is on a fire. Switch to <b>IR white-hot</b> to find personnel through smoke, or <b>LIDAR</b> to read canopy and terrain.</p>
        <ul>
          <li>Hover low over survivors and hold <b>R</b> to extract them — each one is worth $10M in the Lives Saved bonus.</li>
        </ul>
      </>
    ),
  },
  {
    n: '06',
    title: 'Win the debrief',
    clip: 'clip-debrief',
    clipLabel: 'Mission debrief — itemised economic ledger, objectives and grade',
    body: (
      <>
        <p>Every mission is scored on real economics:</p>
        <p className="lp-formula">Final Score = (Property Value Saved + Lives Saved Bonus) − Total Suppression Cost</p>
        <p>Property is credited for what is still standing when a fire goes out; deployment, flight time and every litre of payload cost money. Hit the campaign objectives and spend wisely for an <b>S</b> grade.</p>
      </>
    ),
  },
] as const;

const CONTROLS: [string, string, string][] = [
  ['Globe', 'Drag · Scroll', 'Orbit · zoom the globe'],
  ['Globe', 'Click carrier → click fire', 'Dispatch a sortie (swarm for CAN / CHN)'],
  ['Globe', 'Click anywhere', 'Open a live feed (max 4)'],
  ['Globe', 'MOVE, then click', 'Relocate the selected carrier'],
  ['Globe', 'Fleet panel → click globe', 'Deploy a forward carrier'],
  ['Drone', 'W / S · A / D', 'Throttle · yaw'],
  ['Drone', 'Q / E · Shift', 'Altitude · boost'],
  ['Drone', 'Space', 'Drop suppressant'],
  ['Drone', 'Hold R (low, near survivor)', 'Extract a civilian'],
  ['Drone', 'V · Esc', 'Cycle STD / IR / LIDAR · back to globe'],
];

export default function LandingPage() {
  return (
    <div className="lp">
      <header className="lp-nav">
        <a href="#top" className="lp-brand">
          <span className="lp-brand__t">GLOBAL FIREFIGHT</span>
          <span className="lp-brand__s">DRONE COMMAND</span>
        </a>
        <nav className="lp-nav__links" aria-label="Sections">
          <a href="#manual">Manual</a>
          <a href="#nations">Nations</a>
          <a href="#campaigns">Campaigns</a>
          <a href="#controls">Controls</a>
          <Link href={PLAY_PATH} className="lp-nav__play">PLAY DEMO</Link>
        </nav>
      </header>

      {/* Above the fold */}
      <section className="lp-hero" id="top">
        <div className="lp-hero__copy">
          <div className="lp-kicker">INCIDENT COMMAND · REAL NASA FIRE DATA · BROWSER DEMO</div>
          <h1>Command the world’s firefighting drone fleets.</h1>
          <p className="lp-lede">
            A real-time strategy and drone-piloting game played on a live 3D globe of every active fire on Earth. Deploy national carriers, launch drone swarms, watch every front in split-view feeds — then take the stick and fly the drop yourself.
          </p>
          <Buttons />
          <ul className="lp-stats" aria-label="At a glance">
            <li><b>6</b> nations</li>
            <li><b>7</b> historic campaigns</li>
            <li><b>4</b> live feeds</li>
            <li><b>3</b> sensor modes</li>
          </ul>
        </div>
        <figure className="lp-hero__video">
          <div className="lp-frame">
            <video poster="/media/trailer-poster.jpg" autoPlay muted loop playsInline preload="auto" controls aria-label="Gameplay trailer">
              <source src="/media/trailer.webm" type="video/webm" />
              <source src="/media/trailer.mp4" type="video/mp4" />
            </video>
            <span className="lp-frame__tag">● GAMEPLAY TRAILER · 0:55</span>
          </div>
          <figcaption>Captured from a play session of the Fort McMurray campaign. No sound.</figcaption>
        </figure>
      </section>

      {/* Briefing */}
      <section className="lp-brief">
        <div className="lp-brief__item"><span>01</span><h3>Real fires</h3><p>Live hotspots from NASA FIRMS satellites and named crises from NASA EONET populate the globe.</p></div>
        <div className="lp-brief__item"><span>02</span><h3>Two scales of play</h3><p>Manage fleets across continents in the RTS view, then pilot a single drone over a single ridge.</p></div>
        <div className="lp-brief__item"><span>03</span><h3>Real stakes</h3><p>Scored on property and lives saved against what every sortie, flight minute and litre costs.</p></div>
      </section>

      {/* Manual */}
      <section className="lp-section" id="manual">
        <div className="lp-section__head">
          <div className="lp-kicker">FIELD MANUAL</div>
          <h2>How to play</h2>
        </div>
        {CHAPTERS.map((c, i) => (
          <article key={c.n} className={`lp-chapter ${i % 2 ? 'lp-chapter--flip' : ''}`}>
            <div className="lp-chapter__media">
              {'clip' in c ? <Clip name={c.clip} label={c.clipLabel} /> : (
                <figure className="lp-clip">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img src={`/media/${c.img}`} alt={c.imgLabel} loading="lazy" />
                  <figcaption>{c.imgLabel}</figcaption>
                </figure>
              )}
            </div>
            <div className="lp-chapter__text">
              <div className="lp-chapter__n">CHAPTER {c.n}</div>
              <h3>{c.title}</h3>
              {c.body}
            </div>
          </article>
        ))}
        <div className="lp-gallery">
          {[
            ['09-split-view-feeds.webp', 'Split view: carrier launch + IR drone cam'],
            ['06-tactical-ir-white-hot.webp', 'IR white-hot: personnel detected through smoke'],
            ['07-tactical-lidar.webp', 'LIDAR point cloud: canopy pathways'],
            ['08-mission-debrief.webp', 'Debrief ledger and grade'],
          ].map(([src, alt]) => (
            <figure key={src}>
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={`/media/${src}`} alt={alt} loading="lazy" />
              <figcaption>{alt}</figcaption>
            </figure>
          ))}
        </div>
      </section>

      {/* Nations */}
      <section className="lp-section" id="nations">
        <div className="lp-section__head">
          <div className="lp-kicker">ORDER OF BATTLE</div>
          <h2>Six nations, six fleets</h2>
        </div>
        <div className="lp-nations">
          {COUNTRY_CODES.map((code) => {
            const f = FLEETS[code];
            return (
              <article key={code} className="lp-nation" style={{ ['--accent' as string]: fleetUiColor(code) }}>
                <div className="lp-nation__head">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img className="lp-nation__badge" src={insigniaUrl(code)} alt={`${f.agency} insignia`} loading="lazy" />
                  <div>
                    <h3>
                      {/* eslint-disable-next-line @next/next/no-img-element */}
                      <img className="lp-nation__flag" src={flagUrl(code)} alt="" /> {f.country}
                    </h3>
                    <div className="lp-nation__agency">{f.agency}</div>
                  </div>
                </div>
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img className="lp-nation__drone" src={droneArtUrl(code)} alt={f.drone.model} loading="lazy" />
                <div className="lp-nation__model">{f.drone.model}</div>
                <div className="lp-nation__meta">{f.drone.payloadLitres} L {f.drone.suppressant} · {f.drone.cruiseKmh} km/h · {f.drone.swarmSize > 1 ? `swarm ×${f.drone.swarmSize}` : 'single sortie'}</div>
                <ul className="lp-nation__abilities">
                  {f.drone.abilities.map((a) => (<li key={a.id}>{a.name}</li>))}
                </ul>
              </article>
            );
          })}
        </div>
      </section>

      {/* Campaigns */}
      <section className="lp-section" id="campaigns">
        <div className="lp-section__head">
          <div className="lp-kicker">CAMPAIGNS</div>
          <h2>Seven historic fires</h2>
        </div>
        <div className="lp-campaigns">
          {SCENARIOS.map((s) => (
            <article key={s.id} className="lp-campaign" style={{ ['--accent' as string]: fleetUiColor(s.country) }}>
              <div className="lp-campaign__top">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={flagUrl(s.country)} alt={FLEETS[s.country].country} className="lp-nation__flag" />
                <span className="lp-campaign__year">{s.year}</span>
              </div>
              <h3>{s.title}</h3>
              <div className="lp-campaign__loc">{s.location}</div>
              <ul>
                {s.constraints.map((c) => (<li key={c}>{c}</li>))}
              </ul>
              <div className="lp-campaign__obj">{s.objectives.filter((o) => !o.optional).map((o) => o.title).join(' · ')}</div>
            </article>
          ))}
        </div>
      </section>

      {/* Controls */}
      <section className="lp-section" id="controls">
        <div className="lp-section__head">
          <div className="lp-kicker">QUICK REFERENCE</div>
          <h2>Controls</h2>
        </div>
        <table className="lp-controls">
          <thead><tr><th>Mode</th><th>Input</th><th>Action</th></tr></thead>
          <tbody>
            {CONTROLS.map(([m, k, a]) => (<tr key={k}><td>{m}</td><td><kbd>{k}</kbd></td><td>{a}</td></tr>))}
          </tbody>
        </table>
      </section>

      {/* Final CTA */}
      <section className="lp-final">
        <h2>The fire season doesn’t wait.</h2>
        <p>Play the browser demo now — no install. {STEAM_PAGE_LIVE ? 'Wishlist on Steam to follow the full release.' : 'Steam page coming soon.'}</p>
        <Buttons size="md" />
      </section>

      <footer className="lp-footer">
        <span>Global Firefight: Drone Command · NDIA Hackathon project</span>
        <span>Fire data: NASA FIRMS &amp; EONET · Globe: 3D Tiles via 3d-tiles-renderer · Art from the project pitch deck</span>
        <a href={REPO_URL} target="_blank" rel="noopener noreferrer">GitHub</a>
      </footer>
    </div>
  );
}
