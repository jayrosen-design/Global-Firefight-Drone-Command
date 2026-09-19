'use client';
import { useGame } from '@/store/gameStore';
import { MAP_ROUTE, MAP_ROUTE_LABEL } from '@/lib/config/tiles';

export function MissionLog() {
  const log = useGame((s) => s.log);
  const feed = useGame((s) => s.feed);
  return (
    <aside className="pointer-events-none absolute right-3 top-[92px] z-20 w-[360px] max-w-[40vw]">
      <div className="glass px-3 py-2">
        <div className="panel__title">COMMS LOG</div>
        <ul className="mt-1 max-h-[34vh] space-y-1 overflow-hidden text-[11px] leading-snug">
          {log.slice(0, 12).map((l, i) => (
            <li key={i} className={`log log--${l.kind}`}>
              <span className="log__t">T+{Math.floor(l.t / 60)}m</span> {l.text}
            </li>
          ))}
        </ul>
        {feed && (
          <div className="mt-2 border-t border-white/10 pt-1 text-[10px] text-white/50">
            MAP {MAP_ROUTE_LABEL[MAP_ROUTE.kind]} · FIRMS {feed.source === 'live' ? 'LIVE' : 'FALLBACK'} · {feed.hotspots.length.toLocaleString()} hotspots · {feed.events.length} EONET events
            {feed.message ? ` · ${feed.message}` : ''}
          </div>
        )}
      </div>
    </aside>
  );
}
