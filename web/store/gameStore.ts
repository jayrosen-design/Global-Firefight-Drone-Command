'use client';
import { create } from 'zustand';
import { FLEETS, SUPPRESSANT_EFFECTIVENESS, type CountryCode, COUNTRY_CODES } from '@/lib/config/fleets';
import { SCENARIO_BY_ID, type Scenario } from '@/lib/config/scenarios';
import type { EonetEvent, FireFeed, FirmsHotspot } from '@/lib/data/types';
import { fetchFirmsHotspots } from '@/lib/data/nasa-firms';
import { fetchEonetWildfires } from '@/lib/data/nasa-eonet';
import { buildFallbackFeed } from '@/lib/data/fallback-fires';
import { haversineKm, type LatLon } from '@/lib/geo/wgs84';
import { applyDrop, BALANCE, createScenarioFires, fireFromHotspot, stepFire, type Fire } from '@/lib/engine/fire';
import { createCarrier, stepCarrier, type CarrierVehicle } from '@/lib/engine/CarrierVehicle';
import { batteryDrainPerSec, createDrone, droneRangeKm, progressRate, type DroneUnit } from '@/lib/engine/DroneUnit';
import { emptyLedger, totalSuppressionCost, type Ledger } from '@/lib/engine/economics';

export type GameMode = 'menu' | 'rts' | 'tactical' | 'debrief';
export type VisionMode = 'standard' | 'ir' | 'lidar';
export type Selection = { type: 'carrier' | 'fire' | 'drone'; id: string } | null;
/** Campaign result: every fire out (victory), or the town lost / budget spent (defeat). Null while playing or after END MISSION. */
export type MissionOutcome = { result: 'victory' | 'defeat'; reason: string } | null;

/** A campaign is lost when less than this share of the property at risk is still standing. */
export const DEFEAT_PROPERTY_FRAC = 0.25;

export interface CameraRequest {
  lat: number;
  lon: number;
  /** Camera distance from globe centre in globe radii */
  distance: number;
  nonce: number;
}

/** A live side-window view of a point on the globe (max MAX_FEEDS open). */
export interface LiveFeed {
  id: string;
  lat: number;
  lon: number;
  label: string;
  /** Fire the feed was opened on, if any. */
  fireId?: string;
  openedAt: number;
  /** Sensor mode for this feed's camera. */
  vision: VisionMode;
  /** area: orbit the location · carrier: close-up of a carrier deploying drones · drone: chase cam on a drone over its fire. */
  focus: FeedFocus;
  carrierId?: string;
  droneId?: string;
}

export type FeedFocus = 'area' | 'carrier' | 'drone';

export const MAX_FEEDS = 4;
/** Clicking within this distance of an open feed re-uses it instead of opening another. */
const FEED_DEDUPE_KM = 12;
let feedSeq = 0;

export interface LogEntry {
  t: number;
  text: string;
  kind: 'info' | 'dispatch' | 'drop' | 'alert' | 'success';
}

interface GameState {
  mode: GameMode;
  feed: FireFeed | null;
  feedStatus: 'idle' | 'loading' | 'live' | 'fallback';
  scenario: Scenario | null;
  fires: Fire[];
  carriers: CarrierVehicle[];
  drones: DroneUnit[];
  selection: Selection;
  hoverFireId: string | null;
  placingCarrier: CountryCode | null;
  ledger: Ledger;
  budgetUSD: number;
  /** Simulated seconds since mission start. */
  simTime: number;
  /** Real→sim multiplier (RTS). */
  timeScale: number;
  paused: boolean;
  visionMode: VisionMode;
  tacticalDroneId: string | null;
  cameraRequest: CameraRequest | null;
  log: LogEntry[];
  windMph: number;
  windDirectionDeg: number;
  showLiveFeed: boolean;
  /** MOVE order armed: next globe click relocates the selected carrier. */
  moveArmed: boolean;
  feeds: LiveFeed[];
  /** Feed most recently opened or re-focused (briefly highlighted). */
  activeFeedId: string | null;
  outcome: MissionOutcome;

  openFeed: (ll: LatLon, opts?: { fireId?: string; label?: string; carrierId?: string; focus?: FeedFocus; vision?: VisionMode }) => void;
  setFeedVision: (id: string, vision: VisionMode) => void;
  setFeedFocus: (id: string, focus: FeedFocus, targetId?: string) => void;
  closeFeed: (id: string) => void;
  closeAllFeeds: () => void;
  setMoveArmed: (v: boolean) => void;
  loadFeed: () => Promise<void>;
  startScenario: (id: string) => void;
  startFreePlay: () => void;
  backToMenu: () => void;
  select: (s: Selection) => void;
  setHoverFire: (id: string | null) => void;
  beginPlacingCarrier: (c: CountryCode) => void;
  cancelPlacing: () => void;
  placeCarrierAt: (ll: LatLon) => void;
  moveCarrier: (carrierId: string, ll: LatLon) => void;
  engageHotspot: (h: FirmsHotspot) => Fire;
  dispatch: (carrierId: string, fireId: string) => boolean;
  /** One-click dispatch: the selected carrier if it can reach the fire, else the nearest carrier that can. Logs why when none can. */
  dispatchTo: (fireId: string) => boolean;
  deploySuppressant: (fireId: string) => void;
  enterTactical: (droneId?: string) => void;
  exitTactical: () => void;
  setVisionMode: (m: VisionMode) => void;
  tacticalDrop: (fireId: string, litres: number, debitPayload?: boolean) => number;
  rescueCivilian: () => void;
  tick: (dtReal: number) => void;
  setTimeScale: (s: number) => void;
  togglePause: () => void;
  endMission: () => void;
  flyTo: (lat: number, lon: number, distance?: number) => void;
  toggleLiveFeed: () => void;
  pushLog: (text: string, kind?: LogEntry['kind']) => void;
}

const MAX_LOG = 60;

export const useGame = create<GameState>((set, get) => ({
  mode: 'menu',
  feed: null,
  feedStatus: 'idle',
  scenario: null,
  fires: [],
  carriers: [],
  drones: [],
  selection: null,
  hoverFireId: null,
  placingCarrier: null,
  ledger: emptyLedger(),
  budgetUSD: 0,
  simTime: 0,
  timeScale: 60,
  paused: false,
  visionMode: 'standard',
  tacticalDroneId: null,
  cameraRequest: null,
  log: [],
  windMph: 15,
  windDirectionDeg: 270,
  showLiveFeed: true,
  moveArmed: false,
  feeds: [],
  activeFeedId: null,
  outcome: null,

  openFeed: (ll, opts = {}) => {
    const s = get();
    const near = s.feeds.find((f) => haversineKm(f, ll) < FEED_DEDUPE_KM);
    if (near) {
      set((st) => ({
        activeFeedId: near.id,
        feeds: opts.focus || opts.carrierId
          ? st.feeds.map((f) => (f.id === near.id ? { ...f, focus: opts.focus ?? f.focus, carrierId: opts.carrierId ?? f.carrierId } : f))
          : st.feeds,
      }));
      return;
    }
    const feed: LiveFeed = {
      id: `feed-${++feedSeq}`,
      lat: ll.lat,
      lon: ll.lon,
      label: opts.label ?? feedLabel(s, ll, opts.fireId),
      fireId: opts.fireId,
      openedAt: s.simTime,
      vision: opts.vision ?? 'standard',
      focus: opts.focus ?? (opts.carrierId ? 'carrier' : 'area'),
      carrierId: opts.carrierId,
    };
    let feeds = [...s.feeds, feed];
    if (feeds.length > MAX_FEEDS) {
      const [dropped, ...rest] = feeds;
      feeds = rest;
      get().pushLog(`Feed "${dropped.label}" closed — ${MAX_FEEDS} live feeds max.`, 'info');
    }
    set({ feeds, activeFeedId: feed.id });
  },
  closeFeed: (id) => set((s) => ({ feeds: s.feeds.filter((f) => f.id !== id), activeFeedId: s.activeFeedId === id ? null : s.activeFeedId })),
  closeAllFeeds: () => set({ feeds: [], activeFeedId: null }),
  setFeedVision: (id, vision) => set((s) => ({ feeds: s.feeds.map((f) => (f.id === id ? { ...f, vision } : f)) })),
  setFeedFocus: (id, focus, targetId) =>
    set((s) => ({
      feeds: s.feeds.map((f) =>
        f.id === id
          ? { ...f, focus, droneId: focus === 'drone' ? targetId ?? f.droneId : f.droneId, carrierId: focus === 'carrier' ? targetId ?? f.carrierId : f.carrierId }
          : f,
      ),
    })),

  setMoveArmed: (moveArmed) => set({ moveArmed }),

  pushLog: (text, kind = 'info') =>
    // Repeated clicks re-trigger the same notice; show it once.
    set((s) => (s.log[0]?.text === text ? {} : { log: [{ t: s.simTime, text, kind }, ...s.log].slice(0, MAX_LOG) })),

  loadFeed: async () => {
    if (get().feedStatus === 'loading') return;
    set({ feedStatus: 'loading' });
    let hotspots: FirmsHotspot[] = [];
    let events: EonetEvent[] = [];
    let live = true;
    let message: string | undefined;
    try {
      hotspots = await fetchFirmsHotspots();
    } catch (e) {
      live = false;
      message = (e as Error).message;
    }
    try {
      events = await fetchEonetWildfires();
    } catch (e) {
      if (!events.length) message = message ?? (e as Error).message;
    }
    if (!live || hotspots.length === 0) {
      const fb = buildFallbackFeed();
      hotspots = fb.hotspots;
      if (!events.length) events = fb.events;
      set({
        feed: { source: 'fallback', fetchedAt: Date.now(), hotspots, events, message },
        feedStatus: 'fallback',
      });
      return;
    }
    set({ feed: { source: 'live', fetchedAt: Date.now(), hotspots, events }, feedStatus: 'live' });
  },

  startScenario: (id) => {
    const s = SCENARIO_BY_ID[id];
    if (!s) return;
    const carrier = createCarrier(s.country, s.carrier.lat, s.carrier.lon, `${FLEETS[s.country].carrier.model} — ${s.carrier.label}`);
    const forward = (s.forwardCarriers ?? []).map((c) => createCarrier(s.country, c.lat, c.lon, `${FLEETS[s.country].carrier.model} — ${c.label}`));
    set({
      mode: 'rts',
      outcome: null,
      // Campaign fires are the goal; live FIRMS hotspots stay one toggle away instead of competing for clicks.
      showLiveFeed: false,
      feeds: [],
      activeFeedId: null,
      scenario: s,
      fires: createScenarioFires(s),
      carriers: [carrier, ...forward],
      drones: [],
      selection: { type: 'carrier', id: carrier.id },
      ledger: emptyLedger(),
      budgetUSD: s.budgetUSD,
      simTime: 0,
      paused: false,
      timeScale: 60,
      visionMode: 'standard',
      tacticalDroneId: null,
      windMph: s.wind.speedMph,
      windDirectionDeg: s.wind.directionDeg,
      log: [],
      cameraRequest: { lat: s.center.lat, lon: s.center.lon, distance: framingDistance(s), nonce: Date.now() },
    });
    get().pushLog(`[${s.country}] ${s.title} — ${s.location} (${s.year}). Carrier staged at ${s.carrier.label}.`, 'info');
    get().pushLog(`Wind ${s.wind.speedMph} mph ${s.wind.label}. Click a burning fire to launch drones — keep sending them until it's out.`, 'alert');
  },

  startFreePlay: () => {
    const carriers = COUNTRY_CODES.map((c) => createCarrier(c, FLEETS[c].base.lat, FLEETS[c].base.lon, `${FLEETS[c].carrier.model} — ${FLEETS[c].base.label}`));
    set({
      mode: 'rts',
      outcome: null,
      showLiveFeed: true,
      feeds: [],
      activeFeedId: null,
      scenario: null,
      fires: [],
      carriers,
      drones: [],
      selection: null,
      ledger: emptyLedger(),
      budgetUSD: 25_000_000,
      simTime: 0,
      paused: false,
      timeScale: 60,
      visionMode: 'standard',
      tacticalDroneId: null,
      windMph: 15,
      windDirectionDeg: 270,
      log: [],
      cameraRequest: { lat: 20, lon: -30, distance: 3.2, nonce: Date.now() },
    });
    get().pushLog('Global free play. Six national carriers are staged at their home bases. Click any live hotspot to engage it.', 'info');
  },

  backToMenu: () => set({ mode: 'menu', outcome: null, selection: null, placingCarrier: null, tacticalDroneId: null, feeds: [], activeFeedId: null }),

  select: (selection) => set({ selection, placingCarrier: null, moveArmed: false }),
  setHoverFire: (hoverFireId) => set({ hoverFireId }),

  beginPlacingCarrier: (c) => set({ placingCarrier: c, selection: null }),
  cancelPlacing: () => set({ placingCarrier: null }),
  placeCarrierAt: (ll) => {
    const c = get().placingCarrier;
    if (!c) return;
    const carrier = createCarrier(c, ll.lat, ll.lon, `${FLEETS[c].carrier.model} — Forward Base`);
    set((s) => ({ carriers: [...s.carriers, carrier], placingCarrier: null, selection: { type: 'carrier', id: carrier.id } }));
    get().pushLog(`[${c}] ${FLEETS[c].carrier.model} deployed at ${ll.lat.toFixed(2)}°, ${ll.lon.toFixed(2)}°.`, 'info');
  },

  moveCarrier: (carrierId, ll) => {
    set((s) => ({ carriers: s.carriers.map((c) => (c.id === carrierId ? { ...c, moveTarget: ll } : c)) }));
    get().pushLog(`Carrier relocating to ${ll.lat.toFixed(2)}°, ${ll.lon.toFixed(2)}°.`, 'info');
  },

  engageHotspot: (h) => {
    const existing = get().fires.find((f) => f.id === `live-${h.id}`);
    if (existing) return existing;
    const fire = fireFromHotspot(h);
    set((s) => ({ fires: [...s.fires, fire] }));
    return fire;
  },

  dispatch: (carrierId, fireId) => {
    const s = get();
    const carrier = s.carriers.find((c) => c.id === carrierId);
    const fire = s.fires.find((f) => f.id === fireId);
    if (!carrier || !fire || fire.extinguished) return false;
    const fleet = FLEETS[carrier.country];
    const swarm = Math.min(fleet.drone.swarmSize, carrier.dronesReady);
    if (swarm <= 0) {
      get().pushLog(`${carrier.label}: no drones ready (rearming).`, 'alert');
      return false;
    }
    const origin = { lat: carrier.lat, lon: carrier.lon };
    const dest = { lat: fire.lat, lon: fire.lon };
    const distanceKm = haversineKm(origin, dest);
    const rangeKm = droneRangeKm(carrier.country);
    if (distanceKm > rangeKm) {
      get().pushLog(`${fire.label ?? 'Target'} is ${distanceKm.toFixed(0)} km from ${carrier.label} — beyond ${fleet.drone.model} range (${rangeKm.toFixed(0)} km). MOVE the carrier closer.`, 'alert');
      return false;
    }
    if (s.budgetUSD - totalSuppressionCost(s.ledger) < fleet.costs.deploymentUSD * swarm) {
      get().pushLog('Budget exhausted — no funds for another sortie.', 'alert');
      return false;
    }
    const newDrones: DroneUnit[] = [];
    for (let i = 0; i < swarm; i++) newDrones.push(createDrone(carrier.country, carrier.id, origin, fireId, dest, distanceKm, i, swarm));
    const deployment = fleet.costs.deploymentUSD * swarm;
    set((st) => ({
      drones: [...st.drones, ...newDrones],
      carriers: st.carriers.map((c) => (c.id === carrierId ? { ...c, dronesReady: c.dronesReady - swarm } : c)),
      ledger: { ...st.ledger, deploymentCostUSD: st.ledger.deploymentCostUSD + deployment, sorties: st.ledger.sorties + swarm },
    }));
    get().pushLog(
      `[${carrier.country}] ${swarm > 1 ? `${swarm}× ` : ''}${fleet.drone.model} dispatched → ${fire.label ?? 'target'} (${distanceKm.toFixed(0)} km).`,
      'dispatch',
    );
    return true;
  },

  dispatchTo: (fireId) => {
    const s = get();
    const fire = s.fires.find((f) => f.id === fireId);
    if (!fire || fire.extinguished) return false;
    const km = (c: CarrierVehicle) => haversineKm(c, fire);
    const ready = s.carriers.filter((c) => c.dronesReady > 0);
    const inRange = ready.filter((c) => km(c) <= droneRangeKm(c.country)).sort((a, b) => km(a) - km(b));
    const selected = s.selection?.type === 'carrier' ? inRange.find((c) => c.id === s.selection!.id) : undefined;
    const pick = selected ?? inRange[0];
    if (pick) return get().dispatch(pick.id, fire.id);
    if (!s.carriers.length) {
      get().pushLog('No carriers deployed — pick a fleet under Carrier Deployment, then click the globe to place it.', 'alert');
    } else if (!ready.length) {
      const next = Math.min(...s.carriers.flatMap((c) => c.rearmQueue));
      get().pushLog(
        Number.isFinite(next)
          ? `All drones are airborne or rearming — next drone ready in ${Math.ceil(next / s.timeScale)} s.`
          : 'All drones are airborne — they rearm as soon as they land.',
        'alert',
      );
    } else {
      const nearest = [...s.carriers].sort((a, b) => km(a) - km(b))[0];
      get().pushLog(
        `${fire.label ?? 'This fire'} is ${km(nearest).toFixed(0)} km from the nearest carrier — beyond drone range (${droneRangeKm(nearest.country).toFixed(0)} km). Select a carrier and MOVE it closer.`,
        'alert',
      );
    }
    return false;
  },

  deploySuppressant: (fireId) => {
    // Orders every on-station drone at this fire to drop immediately.
    set((s) => ({
      drones: s.drones.map((d) => (d.targetFireId === fireId && (d.state === 'onstation' || d.state === 'suppressing') ? { ...d, dropCooldown: 0 } : d)),
    }));
  },

  enterTactical: (droneId) => {
    const s = get();
    const id = droneId ?? s.drones.find((d) => d.state === 'onstation' || d.state === 'suppressing')?.id ?? s.drones.find((d) => d.state === 'enroute')?.id ?? null;
    if (!id) {
      get().pushLog('No drone airborne — dispatch a drone before switching to tactical view.', 'alert');
      return;
    }
    set({ mode: 'tactical', tacticalDroneId: id, timeScale: 6, selection: { type: 'drone', id } });
  },

  exitTactical: () => set({ mode: 'rts', timeScale: 60, visionMode: 'standard' }),
  setVisionMode: (visionMode) => set({ visionMode }),

  tacticalDrop: (fireId, litres, debitPayload = true) => {
    const s = get();
    const drone = s.drones.find((d) => d.id === s.tacticalDroneId);
    const fire = s.fires.find((f) => f.id === fireId);
    if (!drone || !fire) return 0;
    const fleet = FLEETS[drone.country];
    const l = debitPayload ? Math.min(litres, drone.payloadLitres) : litres;
    if (l <= 0) return 0;
    const eff = SUPPRESSANT_EFFECTIVENESS[fleet.drone.suppressant];
    const fires = s.fires.map((f) => ({ ...f }));
    const target = fires.find((f) => f.id === fireId)!;
    const wasOut = target.extinguished;
    const knocked = applyDrop(target, l, eff, fleet.drone.suppressant === 'retardant');
    const ledger = { ...s.ledger };
    ledger.payloadCostUSD += l * fleet.costs.payloadPerLitreUSD;
    ledger.litresDropped += l;
    ledger.drops += 1;
    if (!wasOut && target.extinguished) creditExtinguish(target, ledger, get().pushLog);
    set({
      fires,
      ledger,
      drones: s.drones.map((d) => (d.id === drone.id ? { ...d, payloadLitres: debitPayload ? d.payloadLitres - l : d.payloadLitres, drops: d.drops + 1 } : d)),
    });
    return knocked;
  },

  rescueCivilian: () => {
    set((s) => ({ ledger: { ...s.ledger, civiliansRescued: s.ledger.civiliansRescued + 1 } }));
    get().pushLog('Civilian extracted — Lives Saved bonus applied.', 'success');
  },

  setTimeScale: (timeScale) => set({ timeScale }),
  togglePause: () => set((s) => ({ paused: !s.paused })),

  flyTo: (lat, lon, distance = 1.07) => set({ cameraRequest: { lat, lon, distance, nonce: Date.now() } }),
  toggleLiveFeed: () => set((s) => ({ showLiveFeed: !s.showLiveFeed })),

  endMission: () => set({ mode: 'debrief', paused: true }),

  tick: (dtReal) => {
    const s = get();
    if (s.paused || (s.mode !== 'rts' && s.mode !== 'tactical')) return;
    const dt = Math.min(dtReal, 0.1) * s.timeScale;
    const fires = s.fires.map((f) => ({ ...f }));
    const carriers = s.carriers.map((c) => ({ ...c, rearmQueue: [...c.rearmQueue] }));
    const ledger = { ...s.ledger };
    const logs: [string, LogEntry['kind']][] = [];
    const fireById = new Map(fires.map((f) => [f.id, f]));
    const carrierById = new Map(carriers.map((c) => [c.id, c]));

    for (const f of fires) stepFire(f, dt, s.windMph);
    for (const c of carriers) stepCarrier(c, dt);

    const drones: DroneUnit[] = [];
    for (const d0 of s.drones) {
      const d = { ...d0 };
      const fleet = FLEETS[d.country];
      const fire = fireById.get(d.targetFireId);
      d.flightTimeSec += dt;
      d.battery = Math.max(0, d.battery - batteryDrainPerSec(d) * dt);
      ledger.flightTimeCostUSD += (fleet.costs.flightPerMinuteUSD / 60) * dt;

      switch (d.state) {
        case 'enroute': {
          d.t = Math.min(1, d.t + progressRate(d) * dt);
          if (d.t >= 1) {
            d.state = 'onstation';
            if (d.swarmIndex === 0) logs.push([`${d.callSign} on station over ${fire?.label ?? 'target'} — commencing orbit.`, 'info']);
          }
          break;
        }
        case 'onstation':
        case 'suppressing': {
          d.orbit += dt * 0.35;
          if (!fire || fire.extinguished) {
            d.state = 'returning';
            d.t = 1;
            break;
          }
          d.dropCooldown -= dt;
          if (d.dropCooldown <= 0 && d.payloadLitres > 0 && s.mode === 'rts') {
            // Coordinated drop: swarms release together; each drop is a fraction of payload.
            const litres = Math.min(d.payloadLitres, d.payloadMax / 3);
            const eff = SUPPRESSANT_EFFECTIVENESS[fleet.drone.suppressant] * (d.swarmSize > 1 ? 1.15 : 1);
            const wasOut = fire.extinguished;
            applyDrop(fire, litres, eff, fleet.drone.suppressant === 'retardant');
            d.payloadLitres -= litres;
            d.drops++;
            d.state = 'suppressing';
            d.dropCooldown = 12;
            ledger.payloadCostUSD += litres * fleet.costs.payloadPerLitreUSD;
            ledger.litresDropped += litres;
            ledger.drops++;
            if (d.swarmIndex === 0) logs.push([`${d.callSign} ${fleet.drone.suppressant} drop on ${fire.label ?? 'target'} — ${fire.frp.toFixed(0)} MW remaining.`, 'drop']);
            if (!wasOut && fire.extinguished) creditExtinguish(fire, ledger, (t, k) => logs.push([t, k ?? 'success']));
          }
          if (d.payloadLitres <= 0 || d.battery < 20) {
            d.state = 'returning';
            d.t = 1;
            logs.push([`${d.callSign} RTB — ${d.payloadLitres <= 0 ? 'payload expended' : 'low battery'}.`, 'info']);
          }
          break;
        }
        case 'returning': {
          const carrier = carrierById.get(d.carrierId);
          if (carrier) d.origin = { lat: carrier.lat, lon: carrier.lon };
          d.t = Math.max(0, d.t - progressRate(d) * dt * 1.15);
          if (d.t <= 0) {
            d.state = 'landed';
            if (carrier) carrier.rearmQueue.push(FLEETS[carrier.country].carrier.rearmSeconds * BALANCE.rearmSimScale);
          }
          break;
        }
        case 'landed':
          continue; // removed from the active list
      }
      drones.push(d);
    }

    let selection = s.selection;
    let tacticalDroneId = s.tacticalDroneId;
    if (selection?.type === 'drone' && !drones.some((d) => d.id === selection!.id)) selection = null;
    let mode: GameMode = s.mode;
    if (mode === 'tactical' && tacticalDroneId && !drones.some((d) => d.id === tacticalDroneId)) {
      mode = 'rts';
      tacticalDroneId = null;
      logs.push(['Tactical drone recovered — returning to Global RTS view.', 'info']);
    }

    // Campaign end: every fire out, the town lost, or no money left for another sortie.
    let outcome: MissionOutcome = null;
    // Only the campaign's own fires count; live hotspots engaged on the side are a bonus.
    const campaignFires = fires.filter((f) => f.source === 'scenario');
    if (s.scenario && campaignFires.length) {
      const atRisk = campaignFires.reduce((a, f) => a + f.propertyInitialUSD, 0);
      const standing = campaignFires.reduce((a, f) => a + f.propertyRemainingUSD, 0);
      const cheapestSortie = Math.min(...carriers.map((c) => FLEETS[c.country].costs.deploymentUSD));
      if (campaignFires.every((f) => f.extinguished)) outcome = { result: 'victory', reason: 'Every fire is out.' };
      else if (standing < atRisk * DEFEAT_PROPERTY_FRAC)
        outcome = { result: 'defeat', reason: `The fire front overran ${s.scenario.location} — less than ${DEFEAT_PROPERTY_FRAC * 100}% of property still standing.` };
      else if (!drones.length && s.budgetUSD - totalSuppressionCost(ledger) < cheapestSortie)
        outcome = { result: 'defeat', reason: 'Budget exhausted with fires still burning.' };
    }
    if (outcome) {
      mode = 'debrief';
      logs.push([outcome.result === 'victory' ? `MISSION ACCOMPLISHED — ${outcome.reason}` : `MISSION FAILED — ${outcome.reason}`, outcome.result === 'victory' ? 'success' : 'alert']);
    }

    const log = logs.length ? [...logs.map(([text, kind]) => ({ t: s.simTime + dt, text, kind })).reverse(), ...s.log].slice(0, MAX_LOG) : s.log;
    set({
      fires,
      carriers,
      drones,
      ledger,
      simTime: s.simTime + dt,
      selection,
      mode,
      tacticalDroneId,
      log,
      timeScale: mode !== s.mode ? 60 : s.timeScale,
      ...(outcome ? { outcome, paused: true, visionMode: 'standard' as VisionMode } : {}),
    });
  },
}));

function creditExtinguish(fire: Fire, ledger: Ledger, log: (text: string, kind?: LogEntry['kind']) => void) {
  ledger.propertySavedUSD += fire.propertyRemainingUSD;
  ledger.populationProtected += Math.round(fire.populationRemaining);
  ledger.firesExtinguished += 1;
  log(`${fire.label ?? 'Fire'} extinguished. Property saved ${(fire.propertyRemainingUSD / 1e6).toFixed(1)}M, ${Math.round(fire.populationRemaining).toLocaleString()} residents protected.`, 'success');
}

export const selectFleet = (c: CountryCode) => FLEETS[c];

/**
 * Opening camera distance (globe radii) that spreads the main carrier's reachable fires across the
 * view, so each fire is its own click target. Far clusters (Chongqing → Liangshan) are left to zoom-out.
 */
function framingDistance(s: Scenario): number {
  const range = droneRangeKm(s.country);
  const reach = s.fires.filter((f) => haversineKm(f, s.carrier) <= range);
  const radiusKm = Math.max(8, haversineKm(s.carrier, s.center), ...reach.map((f) => haversineKm(f, s.center)));
  // Cluster radius fills ~55% of the half-height of the 42° field of view.
  const altitude = radiusKm / 6371 / (0.55 * Math.tan((21 * Math.PI) / 180));
  return 1 + Math.min(0.6, Math.max(0.008, altitude));
}

/** Human label for a feed: the fire it was opened on, a nearby named fire / EONET event / campaign, else coordinates. */
function feedLabel(s: GameState, ll: LatLon, fireId?: string): string {
  const fire = fireId ? s.fires.find((f) => f.id === fireId) : undefined;
  if (fire?.label) return fire.label;
  const named = s.fires
    .filter((f) => f.label && haversineKm(f, ll) < 20)
    .sort((a, b) => haversineKm(a, ll) - haversineKm(b, ll))[0];
  if (named?.label) {
    const km = haversineKm(named, ll);
    return km < 1 ? named.label : `${fire ? 'Fire' : 'Sector'} ${km.toFixed(0)} km from ${named.label}`;
  }
  const ev = s.feed?.events.find((e) => haversineKm({ lat: e.latitude, lon: e.longitude }, ll) < 60);
  if (ev) return ev.title;
  if (s.scenario && haversineKm(s.scenario.center, ll) < 60) return `${s.scenario.location}`;
  const ns = ll.lat >= 0 ? 'N' : 'S';
  const ew = ll.lon >= 0 ? 'E' : 'W';
  return `${Math.abs(ll.lat).toFixed(2)}°${ns} ${Math.abs(ll.lon).toFixed(2)}°${ew}`;
}
