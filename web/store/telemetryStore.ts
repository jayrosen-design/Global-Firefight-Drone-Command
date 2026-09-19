'use client';
import { create } from 'zustand';

/** High-frequency tactical telemetry, kept separate from the game store to avoid per-frame re-renders of the whole HUD tree. */
interface Telemetry {
  altitude: number;
  airspeed: number;
  heading: number;
  targetFireId: string | null;
  targetDistance: number;
  reticleLocked: boolean;
  nearCivilian: boolean;
  civiliansRemaining: number;
  lastDropKnockdownMW: number;
  /** Local arena position (m) for the tactical minimap / debugging. */
  x: number;
  z: number;
  set: (t: Partial<Telemetry>) => void;
}

export const useTelemetry = create<Telemetry>((set) => ({
  altitude: 0,
  airspeed: 0,
  heading: 0,
  targetFireId: null,
  targetDistance: 0,
  reticleLocked: false,
  nearCivilian: false,
  civiliansRemaining: 0,
  lastDropKnockdownMW: 0,
  x: 0,
  z: 0,
  set: (t) => set(t),
}));
