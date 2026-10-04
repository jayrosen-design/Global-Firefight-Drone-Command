'use client';
import { useEffect } from 'react';
import type { Scenario } from '@/lib/config/scenarios';
import { FLEETS, fleetUiColor } from '@/lib/config/fleets';
import { CAMPAIGN_STORIES, NEWSPAPER_MASTHEAD, pressPhotoUrl } from '@/lib/config/campaignStories';
import { fmtUSD } from '@/lib/engine/economics';
import { Flag } from './Flag';

/**
 * Step 3 of the campaign flow: the historic fire's front page (in-game recreation) beside the
 * mission brief, with one obvious way forward — BEGIN MISSION — and one way back.
 */
export function MissionBriefing({ scenario, onBack, onBegin }: { scenario: Scenario; onBack: () => void; onBegin: () => void }) {
  const story = CAMPAIGN_STORIES[scenario.id];
  const fleet = FLEETS[scenario.country];
  const carriers = 1 + (scenario.forwardCarriers?.length ?? 0);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Enter') {
        e.preventDefault(); // one start, not keydown + the focused button's click
        onBegin();
      }
      else if (e.key === 'Escape') onBack();
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onBack, onBegin]);

  return (
    <div className="brief" style={{ ['--accent' as string]: fleetUiColor(scenario.country) }} role="dialog" aria-label={`Mission briefing: ${scenario.title}`}>
      {story && (
        <article className="paper" aria-label="Newspaper front page (in-game recreation)">
          <header className="paper__mast">
            <div className="paper__ears">
              <span>{story.edition}</span>
              <span>In-game recreation</span>
            </div>
            <div className="paper__name">{NEWSPAPER_MASTHEAD}</div>
            <div className="paper__dateline">
              <span>{story.date}</span>
              <span>{scenario.location}</span>
            </div>
          </header>
          <h2 className="paper__headline">{story.headline}</h2>
          <p className="paper__deck">{story.deck}</p>
          <figure className="paper__photo">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src={pressPhotoUrl(scenario.id)} alt={story.caption} />
            <figcaption>{story.caption}</figcaption>
          </figure>
          <div className="paper__cols">
            {story.article.map((p, i) => (
              <p key={i}>{p}</p>
            ))}
          </div>
          <div className="paper__note">Fictional masthead · events and figures summarised from public reports</div>
        </article>
      )}

      <section className="brief__panel glass">
        <div className="brief__eyebrow">
          <span className="step-pill">STEP 3 / 3</span> MISSION BRIEFING
        </div>
        <h1 className="brief__title">
          {scenario.title} <span className="brief__year">{scenario.year}</span>
        </h1>
        <div className="brief__where">
          <Flag code={scenario.country} h={16} /> {fleet.agency} · {scenario.location}
        </div>

        {story && <p className="brief__situation">{story.situation}</p>}

        {story && (
          <div className="brief__block">
            <div className="brief__label">WHAT HISTORY RECORDED</div>
            <div className="brief__record">
              {story.record.map((r) => (
                <div key={r.label} className="brief__stat">
                  <div className="brief__stat-v">{r.value}</div>
                  <div className="brief__stat-k">{r.label}</div>
                </div>
              ))}
            </div>
            <div className="brief__tag">This time, drone command is on scene from the first hour.</div>
          </div>
        )}

        <div className="brief__block">
          <div className="brief__label">OBJECTIVES</div>
          <ul className="brief__objectives">
            {scenario.objectives.map((o) => (
              <li key={o.id}>
                <span className="brief__bullet">{o.optional ? '◇' : '◆'}</span> {o.title}
                {o.optional && <span className="text-white/45"> (optional)</span>}
              </li>
            ))}
            <li className="brief__win">
              <span className="brief__bullet">★</span> Win: put out every fire. Lose: less than 25% of property left standing, or the budget runs dry.
            </li>
          </ul>
        </div>

        <div className="brief__grid">
          <div className="brief__block">
            <div className="brief__label">CONDITIONS</div>
            <div className="brief__chips">
              <span className="chip">
                Wind {scenario.wind.speedMph} mph · {scenario.wind.label}
              </span>
              {scenario.constraints.map((c) => (
                <span key={c} className="chip">
                  {c}
                </span>
              ))}
            </div>
          </div>
          <div className="brief__block">
            <div className="brief__label">YOUR FORCE</div>
            <div className="brief__force">
              <div>
                {carriers}× {fleet.carrier.model}
              </div>
              <div>
                {fleet.carrier.droneCapacity}× {fleet.drone.model} per carrier · {fleet.drone.payloadLitres} L {fleet.drone.suppressant}
              </div>
              <div>Budget {fmtUSD(scenario.budgetUSD)} · {scenario.fires.length} fires</div>
            </div>
          </div>
        </div>

        <div className="brief__actions">
          <button className="btn brief__back" onClick={onBack}>
            ◀ BACK
          </button>
          <button className="cta" onClick={onBegin} autoFocus>
            BEGIN MISSION ▶<span className="cta__hint">ENTER</span>
          </button>
        </div>
      </section>
    </div>
  );
}
