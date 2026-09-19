'use client';
import { useGame } from '@/store/gameStore';
import { finalScore, fmtUSD, livesSavedBonus, totalSuppressionCost } from '@/lib/engine/economics';

function clock(sim: number) {
  const h = Math.floor(sim / 3600), m = Math.floor((sim % 3600) / 60), s = Math.floor(sim % 60);
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
}

export function TopBar() {
  const ledger = useGame((s) => s.ledger);
  const budget = useGame((s) => s.budgetUSD);
  const simTime = useGame((s) => s.simTime);
  const scenario = useGame((s) => s.scenario);
  const paused = useGame((s) => s.paused);
  const timeScale = useGame((s) => s.timeScale);
  const mode = useGame((s) => s.mode);
  const feedStatus = useGame((s) => s.feedStatus);
  const togglePause = useGame((s) => s.togglePause);
  const setTimeScale = useGame((s) => s.setTimeScale);
  const endMission = useGame((s) => s.endMission);
  const score = finalScore(ledger);
  const cost = totalSuppressionCost(ledger);
  const remaining = budget - cost;

  return (
    <header className="pointer-events-auto absolute inset-x-0 top-0 z-30 flex items-stretch gap-2 p-3">
      <div className="glass flex items-center gap-3 px-4 py-2">
        <div className="brand">
          <div className="brand__title">GLOBAL FIREFIGHT</div>
          <div className="brand__sub">DRONE COMMAND · {mode === 'tactical' ? 'TACTICAL' : 'GLOBAL RTS'}</div>
        </div>
      </div>
      <div className="glass flex flex-1 items-center gap-6 px-5 py-2 overflow-x-auto">
        <Stat label="NET SCORE" value={fmtUSD(score)} tone={score >= 0 ? 'good' : 'bad'} big />
        <Stat label="PROPERTY SAVED" value={fmtUSD(ledger.propertySavedUSD)} tone="good" />
        <Stat label="LIVES SAVED BONUS" value={fmtUSD(livesSavedBonus(ledger))} tone="good" sub={`${ledger.civiliansRescued} rescued · ${ledger.populationProtected.toLocaleString()} protected`} />
        <Stat label="SUPPRESSION COST" value={fmtUSD(cost)} tone="bad" />
        <Stat label="BUDGET REMAINING" value={fmtUSD(remaining)} tone={remaining > budget * 0.25 ? 'neutral' : 'bad'} sub={`of ${fmtUSD(budget)}`} />
      </div>
      <div className="glass flex items-center gap-3 px-4 py-2">
        <div className="text-right">
          <div className="stat__label">MISSION CLOCK</div>
          <div className="font-mono text-lg tabular-nums text-cyan-glow">{clock(simTime)}</div>
          <div className="stat__sub">{scenario ? `${scenario.title} · ${scenario.year}` : 'FREE PLAY'} · FEED {feedStatus.toUpperCase()}</div>
        </div>
        {mode === 'rts' && (
          <div className="flex flex-col gap-1">
            <div className="flex gap-1">
              <button className={`btn btn--xs ${paused ? 'btn--active' : ''}`} onClick={togglePause}>{paused ? '▶' : '❚❚'}</button>
              {[60, 180, 480].map((s) => (
                <button key={s} className={`btn btn--xs ${timeScale === s ? 'btn--active' : ''}`} onClick={() => setTimeScale(s)}>{s / 60}×</button>
              ))}
            </div>
            <button className="btn btn--xs btn--danger" onClick={endMission}>END MISSION</button>
          </div>
        )}
      </div>
    </header>
  );
}

function Stat({ label, value, sub, tone, big }: { label: string; value: string; sub?: string; tone: 'good' | 'bad' | 'neutral'; big?: boolean }) {
  return (
    <div className="stat shrink-0">
      <div className="stat__label">{label}</div>
      <div className={`stat__value ${big ? 'stat__value--big' : ''} stat__value--${tone}`}>{value}</div>
      {sub && <div className="stat__sub">{sub}</div>}
    </div>
  );
}
