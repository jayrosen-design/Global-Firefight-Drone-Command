'use client';
/**
 * Shared loader setup for 3d-tiles-renderer: DRACO decoding (Google tiles are
 * Draco-compressed) and three-mesh-bvh accelerated raycasting so terrain
 * height queries stay cheap.
 */
import { BufferGeometry, Mesh } from 'three';
import { DRACOLoader } from 'three/examples/jsm/loaders/DRACOLoader.js';
import { acceleratedRaycast, computeBoundsTree, disposeBoundsTree } from 'three-mesh-bvh';

let draco: DRACOLoader | null = null;
export function getDracoLoader() {
  if (!draco) {
    draco = new DRACOLoader();
    draco.setDecoderPath('/draco/');
  }
  return draco;
}

let patched = false;
export function enableBvhRaycast() {
  if (patched) return;
  patched = true;
  const proto = BufferGeometry.prototype as unknown as { computeBoundsTree?: unknown; disposeBoundsTree?: unknown };
  proto.computeBoundsTree = computeBoundsTree;
  proto.disposeBoundsTree = disposeBoundsTree;
  (Mesh.prototype as unknown as { raycast: unknown }).raycast = acceleratedRaycast;
}

/** Build BVHs for every mesh in a freshly loaded tile so raycasts are O(log n). */
export function buildTileBvh(scene: { traverse: (cb: (o: unknown) => void) => void }) {
  scene.traverse((o) => {
    const m = o as Mesh & { geometry: BufferGeometry & { boundsTree?: unknown; computeBoundsTree?: () => void } };
    if (m.isMesh && m.geometry && !m.geometry.boundsTree && m.geometry.computeBoundsTree) {
      try {
        m.geometry.computeBoundsTree();
      } catch {
        /* non-indexed or degenerate geometry — fall back to brute force */
      }
    }
  });
}
