'use client';
import { createContext, useContext } from 'react';
import type { TilesRenderer } from '3d-tiles-renderer/three';

/** The globe-view TilesRenderer, when photorealistic tiles are active. */
export const GlobeTilesContext = createContext<TilesRenderer | null>(null);
export const useGlobeTiles = () => useContext(GlobeTilesContext);
