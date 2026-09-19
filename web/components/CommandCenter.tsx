'use client';
import dynamic from 'next/dynamic';
import { useEffect } from 'react';
import { useGame } from '@/store/gameStore';
import { useTelemetry } from '@/store/telemetryStore';
import { TopBar } from './hud/TopBar';
import { BottomBar } from './hud/BottomBar';
import { MissionLog } from './hud/MissionLog';
import { ScenarioMenu } from './hud/ScenarioMenu';
import { Debrief } from './hud/Debrief';
import { TacticalHUD } from './hud/TacticalHUD';

const GlobeScene = dynamic(() => import('./globe/Scene').then((m) => m.GlobeScene), { ssr: false });
const TacticalScene = dynamic(() => import('./tactical/TacticalScene').then((m) => m.TacticalScene), { ssr: false });

export function CommandCenter() {
  const mode = useGame((s) => s.mode);
  const loadFeed = useGame((s) => s.loadFeed);
  useEffect(() => {
    loadFeed();
    // Debug/automation hook: window.__game exposes the store in the browser console.
    (window as unknown as { __game?: typeof useGame; __telemetry?: typeof useTelemetry }).__game = useGame;
    (window as unknown as { __telemetry?: typeof useTelemetry }).__telemetry = useTelemetry;
  }, [loadFeed]);

  return (
    <main className="relative h-screen w-screen overflow-hidden bg-[#020509] text-white">
      {mode === 'tactical' ? <TacticalScene /> : <GlobeScene />}
      {(mode === 'rts' || mode === 'debrief') && (
        <>
          <TopBar />
          <MissionLog />
          <BottomBar />
        </>
      )}
      {mode === 'tactical' && (
        <>
          <TopBar />
          <TacticalHUD />
        </>
      )}
      {mode === 'menu' && <ScenarioMenu />}
      {mode === 'debrief' && <Debrief />}
    </main>
  );
}
