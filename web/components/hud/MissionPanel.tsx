'use client';
import { useEffect } from 'react';
import { useShallow } from 'zustand/react/shallow';
import { useGame, DEFEAT_PROPERTY_FRAC } from '@/store/gameStore';
import { FLEETS } from '@/lib/config/fleets';
import { objectiveDone, objectiveHint } from '@/lib/engine/objectives';

type GameSnapshot = ReturnType<typeof useGame.getState>;

/** The single most useful thing to do right now, for players who don't know the game yet (plus the fire it refers to). */
function nextStep(s: GameSnapshot): { text: string; fireId?: string } {
  const text = nextStepText(s);
  return typeof text === 'string' ? { text } : text;
}

function nextStepText(s: GameSnapshot): string | { text: string; fireId: string } {
  if (s.placingCarrier) return `Click the globe to place the ${FLEETS[s.placingCarrier].carrier.model}.`;
  if (s.moveArmed) return 'Click the globe where the carrier should drive to.';
  const burning = s.fires.filter((f) => !f.extinguished && (!s.scenario || f.source === 'scenario'));
  if (!s.carriers.length) return 'Pick a fleet under Carrier Deployment, then click the globe to place it.';
  if (!burning.length) return s.scenario ? 'All fires out.' : 'Click any glowing FIRMS hotspot to engage it.';
  if (s.ledger.sorties === 0) return 'Click a burning fire (orange ring) to launch drones at it.';
  const ready = s.carriers.some((c) => c.dronesReady > 0);
  const unattended = burning
    .filter((f) => !s.drones.some((d) => d.targetFireId === f.id && d.state !== 'returning'))
    .sort((a, b) => b.frp - a.frp)[0];
  if (ready && unattended) return { text: `${unattended.label ?? 'A fire'} has no drones on it — click it to send some.`, fireId: unattended.id };
  if (ready) return 'Drones ready — click a fire again to send more until it goes out.';
  if (s.drones.some((d) => d.state === 'enroute') && s.timeScale < 180) return 'Drones in flight — press 2 or 3 to speed up time.';
  return 'All drones busy — they rearm on landing. Try TACTICAL DRONE VIEW to fly one yourself.';
}

/** Mission tracker: objectives with live status, progress, and a next-step coach line. Also owns the RTS hotkeys. */
export function MissionPanel() {
  const scenario = useGame((s) => s.scenario);
  const allFires = useGame((s) => s.fires);
  // In a campaign the tracker follows the campaign's fires (engaged live hotspots are a side bonus).
  const fires = scenario ? allFires.filter((f) => f.source === 'scenario') : allFires;
  const ledger = useGame((s) => s.ledger);
  const hint = useGame(useShallow(nextStep));

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const t = e.target as HTMLElement | null;
      if (t && (t.tagName === 'INPUT' || t.tagName === 'SELECT' || t.tagName === 'TEXTAREA')) return;
      const st = useGame.getState();
      if (st.mode !== 'rts') return;
      if (e.key === ' ') {
        e.preventDefault();
        st.togglePause();
      } else if (e.key === '1' || e.key === '2' || e.key === '3') st.setTimeScale([60, 180, 480][+e.key - 1]);
      else if (e.key === 'Escape') {
        st.cancelPlacing();
        st.select(null);
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, []);

  const out = fires.filter((f) => f.extinguished).length;
  const atRisk = fires.reduce((a, f) => a + f.propertyInitialUSD, 0);
  const standing = atRisk ? fires.reduce((a, f) => a + f.propertyRemainingUSD, 0) / atRisk : 1;

  return (
    <aside className="pointer-events-none absolute left-3 top-[92px] z-20 w-[300px] max-w-[38vw]">
      <div className="glass px-3 py-2">
        <div className="panel__title">{scenario ? `MISSION · ${scenario.title}` : 'FREE PLAY'}</div>
        {fires.length > 0 && (
          <div className="mt-2 space-y-1.5">
            <Meter label="FIRES OUT" value={`${out}/${fires.length}`} frac={out / fires.length} tone="good" />
            {scenario && (
              <Meter
                label="PROPERTY STANDING"
                value={`${Math.round(standing * 100)}%`}
                frac={standing}
                tone={standing > 0.5 ? 'good' : 'bad'}
                mark={DEFEAT_PROPERTY_FRAC}
                title={`Mission fails below ${DEFEAT_PROPERTY_FRAC * 100}%`}
              />
            )}
          </div>
        )}
        {scenario && (
          <ul className="mt-2 space-y-1 text-[11px] leading-snug">
            {scenario.objectives.map((o) => {
              const done = objectiveDone(o, fires, ledger);
              return (
                <li key={o.id} className={`objective ${done ? 'objective--done' : ''}`}>
                  {done ? '✔' : o.optional ? '◇' : '◆'} {o.title}
                  {o.optional && <span className="text-white/40"> (optional)</span>}
                  {!done && <div className="objective__hint">{objectiveHint(o, fires)}</div>}
                </li>
              );
            })}
          </ul>
        )}
        <div className="coach">
          <span className="coach__tag">NEXT</span> {hint.text}
          {hint.fireId && (
            <button
              className="btn btn--xs pointer-events-auto ml-1"
              onClick={() => {
                const st = useGame.getState();
                const f = st.fires.find((x) => x.id === hint.fireId);
                if (f) st.flyTo(f.lat, f.lon, 1.02);
              }}
            >
              SHOW
            </button>
          )}
        </div>
        <div className="mt-1 text-[10px] text-white/40">SPACE pause · 1 2 3 speed · ESC clear · drag to orbit</div>
      </div>
    </aside>
  );
}

function Meter({ label, value, frac, tone, mark, title }: { label: string; value: string; frac: number; tone: 'good' | 'bad'; mark?: number; title?: string }) {
  return (
    <div title={title}>
      <div className="flex justify-between text-[10px]">
        <span className="stat__label">{label}</span>
        <span className="font-mono text-white/80">{value}</span>
      </div>
      <div className="meter">
        <div className={`meter__fill meter__fill--${tone}`} style={{ width: `${Math.max(0, Math.min(1, frac)) * 100}%` }} />
        {mark !== undefined && <div className="meter__mark" style={{ left: `${mark * 100}%` }} />}
      </div>
    </div>
  );
}
