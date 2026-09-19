'use client';
import { useGame } from '@/store/gameStore';
import { finalScore, fmtUSD, livesSavedBonus, totalSuppressionCost, VALUE_OF_STATISTICAL_LIFE_USD, POPULATION_PROTECTED_USD } from '@/lib/engine/economics';

export function Debrief() {
  const ledger = useGame((s) => s.ledger);
  const scenario = useGame((s) => s.scenario);
  const fires = useGame((s) => s.fires);
  const simTime = useGame((s) => s.simTime);
  const backToMenu = useGame((s) => s.backToMenu);
  const startScenario = useGame((s) => s.startScenario);
  const startFreePlay = useGame((s) => s.startFreePlay);
  const score = finalScore(ledger);
  const bonus = livesSavedBonus(ledger);
  const cost = totalSuppressionCost(ledger);
  const propertyLost = fires.reduce((a, f) => a + (f.propertyInitialUSD - f.propertyRemainingUSD), 0);
  const out = fires.filter((f) => f.extinguished).length;
  const grade = score > 1e9 ? 'S' : score > 3e8 ? 'A' : score > 5e7 ? 'B' : score > 0 ? 'C' : 'D';

  return (
    <div className="pointer-events-auto absolute inset-0 z-40 flex items-center justify-center bg-black/75 p-6">
      <div className="glass w-full max-w-4xl px-8 py-7">
        <div className="flex items-start justify-between gap-6">
          <div>
            <div className="brand__sub">MISSION DEBRIEF · ECONOMIC IMPACT ANALYSIS</div>
            <div className="brand__title text-3xl">{scenario ? `${scenario.title} — ${scenario.location}` : 'Global Free Play'}</div>
            <div className="mt-1 text-xs text-white/60">Mission time {(simTime / 3600).toFixed(1)} h · {out}/{fires.length} fires extinguished · {ledger.sorties} sorties · {ledger.drops} drops · {Math.round(ledger.litresDropped).toLocaleString()} L</div>
          </div>
          <div className="text-center">
            <div className={`grade grade--${grade}`}>{grade}</div>
            <div className="stat__label">GRADE</div>
          </div>
        </div>

        <div className="mt-6 grid gap-6 md:grid-cols-[1fr_1fr]">
          <div>
            <div className="panel__title">LEDGER</div>
            <table className="ledger">
              <tbody>
                <tr className="ledger__pos"><td>Property Value Saved</td><td>{fmtUSD(ledger.propertySavedUSD, false)}</td></tr>
                <tr className="ledger__pos"><td>Lives Saved Bonus<br /><span className="ledger__note">{ledger.civiliansRescued} × {fmtUSD(VALUE_OF_STATISTICAL_LIFE_USD)} VSL + {ledger.populationProtected.toLocaleString()} × {fmtUSD(POPULATION_PROTECTED_USD)}</span></td><td>{fmtUSD(bonus, false)}</td></tr>
                <tr className="ledger__neg"><td>Drone Deployment Costs<br /><span className="ledger__note">{ledger.sorties} sorties</span></td><td>−{fmtUSD(ledger.deploymentCostUSD, false)}</td></tr>
                <tr className="ledger__neg"><td>Flight Time Costs</td><td>−{fmtUSD(ledger.flightTimeCostUSD, false)}</td></tr>
                <tr className="ledger__neg"><td>Payload Costs<br /><span className="ledger__note">{Math.round(ledger.litresDropped).toLocaleString()} litres</span></td><td>−{fmtUSD(ledger.payloadCostUSD, false)}</td></tr>
                <tr className="ledger__total"><td>FINAL SCORE = (Property + Lives) − Cost</td><td className={score >= 0 ? 'text-emerald-300' : 'text-rose-300'}>{fmtUSD(score, false)}</td></tr>
              </tbody>
            </table>
            <div className="mt-2 text-[11px] text-white/45">Total suppression cost {fmtUSD(cost, false)} · Property lost to fire {fmtUSD(propertyLost, false)}</div>
          </div>
          <div>
            <div className="panel__title">OBJECTIVES</div>
            <ul className="mt-2 space-y-2 text-sm">
              {(scenario?.objectives ?? []).map((o) => {
                let done = false;
                if (o.civilians) done = ledger.civiliansRescued >= o.civilians;
                else if (o.line) done = fires.some((f) => f.contained || f.extinguished);
                else if (o.protect) done = out >= Math.ceil(fires.length / 2);
                else done = out > 0;
                return (
                  <li key={o.id} className={`objective ${done ? 'objective--done' : 'objective--fail'}`}>
                    {done ? '✔' : '✖'} {o.title} {o.optional && <span className="text-white/40">(optional)</span>}
                  </li>
                );
              })}
              {!scenario && <li className="objective">Free play — no scripted objectives.</li>}
            </ul>
            <div className="mt-6 flex gap-2">
              <button className="btn btn--primary" onClick={() => (scenario ? startScenario(scenario.id) : startFreePlay())}>REPLAY</button>
              <button className="btn" onClick={backToMenu}>CAMPAIGN SELECT</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
