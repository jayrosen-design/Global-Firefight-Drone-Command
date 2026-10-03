'use client';
import dynamic from 'next/dynamic';
import { useEffect } from 'react';
import { useGame } from '@/store/gameStore';
import { useTelemetry } from '@/store/telemetryStore';
import { TopBar } from './hud/TopBar';
import { BottomBar } from './hud/BottomBar';
import { MissionLog } from './hud/MissionLog';
import { CountrySelect } from './hud/CountrySelect';
import { Debrief } from './hud/Debrief';
import { TacticalHUD } from './hud/TacticalHUD';
import { FeedPanel } from './feeds/FeedPanel';

const GlobeScene = dynamic(() => import('./globe/Scene').then((m) => m.GlobeScene), { ssr: false });
const TacticalScene = dynamic(() => import('./tactical/TacticalScene').then((m) => m.TacticalScene), { ssr: false });

export function CommandCenter() {
  const mode = useGame((s) => s.mode);
  const loadFeed = useGame((s) => s.loadFeed);
  const feedCount = useGame((s) => s.feeds.length);
  const split = (mode === 'rts' || mode === 'debrief') && feedCount > 0;
  useEffect(() => {
    loadFeed();
    // Debug/automation hook: window.__game exposes the store in the browser console.
    (window as unknown as { __game?: typeof useGame; __telemetry?: typeof useTelemetry }).__game = useGame;
    (window as unknown as { __telemetry?: typeof useTelemetry }).__telemetry = useTelemetry;
  }, [loadFeed]);

  return (
    <main className="relative h-screen w-screen overflow-hidden bg-[#020509] text-white">
      {mode === 'tactical' ? (
        <TacticalScene />
      ) : (
        /* Globe area: the always-live game view. Shrinks to the left when live feeds are open. */
        <div className={`globe-area ${split ? 'globe-area--split' : ''}`}>
          <GlobeScene />
          {(mode === 'rts' || mode === 'debrief') && (
            <>
              <MissionLog />
              <BottomBar compact={split} />
            </>
          )}
        </div>
      )}
      {(mode === 'rts' || mode === 'debrief') && (
        <>
          <TopBar />
          <FeedPanel />
        </>
      )}
      {mode === 'tactical' && (
        <>
          <TopBar />
          <TacticalHUD />
        </>
      )}
      {mode === 'menu' && <CountrySelect />}
      {mode === 'debrief' && <Debrief />}
    </main>
  );
}
