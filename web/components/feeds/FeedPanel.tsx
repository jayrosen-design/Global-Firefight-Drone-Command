'use client';
import dynamic from 'next/dynamic';
import { useMemo } from 'react';
import { useGame, MAX_FEEDS, type LiveFeed } from '@/store/gameStore';
import { FLEETS } from '@/lib/config/fleets';
import { haversineKm } from '@/lib/geo/wgs84';
import { fmtUSD } from '@/lib/engine/economics';

const FeedView = dynamic(() => import('./FeedView').then((m) => m.FeedView), { ssr: false });

const FEED_RANGE_KM = 30;

/** Chrome + live stats around one feed's 3D view. */
function FeedWindow({ feed, slot }: { feed: LiveFeed; slot: number }) {
  const fires = useGame((s) => s.fires);
  const drones = useGame((s) => s.drones);
  const active = useGame((s) => s.activeFeedId === feed.id);
  const closeFeed = useGame((s) => s.closeFeed);
  const flyTo = useGame((s) => s.flyTo);
  const enterTactical = useGame((s) => s.enterTactical);

  const stats = useMemo(() => {
    const local = fires.filter((f) => haversineKm(f, feed) < FEED_RANGE_KM);
    const ids = new Set(local.map((f) => f.id));
    const localDrones = drones.filter((d) => ids.has(d.targetFireId));
    return {
      burning: local.filter((f) => !f.extinguished).length,
      out: local.filter((f) => f.extinguished).length,
      frp: local.reduce((a, f) => a + f.frp, 0),
      atRisk: local.reduce((a, f) => a + (f.extinguished ? 0 : f.propertyRemainingUSD), 0),
      drones: localDrones,
      onStation: localDrones.filter((d) => d.state === 'onstation' || d.state === 'suppressing'),
    };
  }, [fires, drones, feed]);

  const pilot = stats.onStation[0] ?? stats.drones[0];
  return (
    <section className={`feed glass ${active ? 'feed--active' : ''}`} aria-label={`Live feed ${slot}: ${feed.label}`}>
      <header className="feed__head">
        <span className="feed__slot">FEED {slot}</span>
        <span className="feed__title" title={feed.label}>{feed.label}</span>
        <span className="feed__live">● LIVE</span>
        <button className="feed__btn" onClick={() => flyTo(feed.lat, feed.lon, 1.05)} title="Centre the globe on this feed">◎</button>
        <button className="feed__btn" onClick={() => closeFeed(feed.id)} title="Close feed" aria-label="Close feed">✕</button>
      </header>
      <div className="feed__view">
        <FeedView feed={feed} />
        <div className="feed__stats">
          <span>{stats.burning} burning{stats.out ? ` · ${stats.out} out` : ''}</span>
          <span>{stats.frp.toFixed(0)} MW</span>
          <span>{fmtUSD(stats.atRisk)} at risk</span>
          <span>{stats.onStation.length}/{stats.drones.length} drones on station</span>
        </div>
        {pilot && (
          <button className="feed__pilot" onClick={() => enterTactical(pilot.id)} title="Take manual control of this drone">
            {FLEETS[pilot.country].flag} PILOT {pilot.callSign}
          </button>
        )}
      </div>
    </section>
  );
}

/** Right-hand column of up to MAX_FEEDS live windows. The globe stays the primary, always-live view. */
export function FeedPanel() {
  const feeds = useGame((s) => s.feeds);
  const closeAll = useGame((s) => s.closeAllFeeds);
  if (!feeds.length) return null;
  return (
    <aside className="feeds" data-count={feeds.length}>
      <div className="feeds__bar">
        <span className="panel__title">LIVE FEEDS {feeds.length}/{MAX_FEEDS}</span>
        <span className="stat__sub">Click the globe to open · oldest closes at {MAX_FEEDS + 1}</span>
        <button className="btn btn--xs" onClick={closeAll}>CLOSE ALL</button>
      </div>
      <div className="feeds__list">
        {feeds.map((f, i) => (
          <FeedWindow key={f.id} feed={f} slot={i + 1} />
        ))}
      </div>
    </aside>
  );
}
