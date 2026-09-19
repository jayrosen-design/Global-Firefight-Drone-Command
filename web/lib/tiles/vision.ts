'use client';
/**
 * Vision-mode material overrides for streamed tile meshes.
 *  - standard: original materials
 *  - ir:       monochrome white-hot shader (terrain reads dark, emitters stay white)
 *  - lidar:    meshes hidden, geometry re-rendered as an elevation-coloured point cloud
 */
import { Color, Material, Mesh, Object3D, Points, ShaderMaterial, Texture } from 'three';
import type { VisionMode } from '@/store/gameStore';

const IR_VERT = /* glsl */ `
varying vec2 vUv;
varying vec3 vNormalW;
void main() {
  vUv = uv;
  vNormalW = normalize(mat3(modelMatrix) * normal);
  gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
}`;
const IR_FRAG = /* glsl */ `
uniform sampler2D map;
uniform float hasMap;
varying vec2 vUv;
varying vec3 vNormalW;
void main() {
  vec3 c = hasMap > 0.5 ? texture2D(map, vUv).rgb : vec3(0.35);
  float lum = dot(c, vec3(0.299, 0.587, 0.114));
  // cool ground: compress to a dark band, slight lift on slopes facing up
  float up = clamp(vNormalW.y * 0.5 + 0.5, 0.0, 1.0);
  float v = 0.06 + lum * 0.22 + up * 0.05;
  gl_FragColor = vec4(vec3(v), 1.0);
}`;
const LIDAR_VERT = /* glsl */ `
uniform float uMinY;
uniform float uRange;
varying float vT;
void main() {
  vec4 world = modelMatrix * vec4(position, 1.0);
  vT = clamp((world.y - uMinY) / uRange, 0.0, 1.0);
  vec4 mv = viewMatrix * world;
  gl_PointSize = clamp(900.0 / -mv.z, 1.0, 6.0);
  gl_Position = projectionMatrix * mv;
}`;
const LIDAR_FRAG = /* glsl */ `
uniform vec3 uLow;
uniform vec3 uHigh;
varying float vT;
void main() {
  float d = length(gl_PointCoord - 0.5) * 2.0;
  if (d > 1.0) discard;
  gl_FragColor = vec4(mix(uLow, uHigh, vT), 0.9);
}`;

const lidarMaterial = new ShaderMaterial({
  vertexShader: LIDAR_VERT,
  fragmentShader: LIDAR_FRAG,
  uniforms: { uMinY: { value: -50 }, uRange: { value: 600 }, uLow: { value: new Color('#0aa6c9') }, uHigh: { value: new Color('#7dffb3') } },
  transparent: true,
  depthWrite: false,
});

export function setLidarElevationRange(minY: number, range: number) {
  lidarMaterial.uniforms.uMinY.value = minY;
  lidarMaterial.uniforms.uRange.value = range;
}

interface Tagged extends Mesh {
  userData: { gfOriginal?: Material | Material[]; gfIr?: ShaderMaterial; gfPoints?: Points };
}

function irMaterialFor(mesh: Tagged): ShaderMaterial {
  if (mesh.userData.gfIr) return mesh.userData.gfIr;
  const orig = (Array.isArray(mesh.material) ? mesh.material[0] : mesh.material) as Material & { map?: Texture | null };
  const map = orig?.map ?? null;
  const m = new ShaderMaterial({ vertexShader: IR_VERT, fragmentShader: IR_FRAG, uniforms: { map: { value: map }, hasMap: { value: map ? 1 : 0 } } });
  mesh.userData.gfIr = m;
  return m;
}

/** Apply a vision mode to every mesh under `root` (a loaded tile scene). */
export function applyVisionToTile(root: Object3D, mode: VisionMode) {
  root.traverse((o) => {
    const mesh = o as Tagged;
    if (!mesh.isMesh || (mesh as unknown as Points).isPoints) return;
    if (!mesh.userData.gfOriginal) mesh.userData.gfOriginal = mesh.material;
    // reset
    mesh.material = mesh.userData.gfOriginal;
    mesh.visible = true;
    if (mesh.userData.gfPoints) mesh.userData.gfPoints.visible = false;

    if (mode === 'ir') {
      mesh.material = irMaterialFor(mesh);
    } else if (mode === 'lidar') {
      if (!mesh.userData.gfPoints) {
        const pts = new Points(mesh.geometry, lidarMaterial);
        pts.matrixAutoUpdate = false;
        pts.matrix.copy(mesh.matrix);
        pts.frustumCulled = false;
        mesh.parent?.add(pts);
        mesh.userData.gfPoints = pts;
      }
      mesh.userData.gfPoints.matrix.copy(mesh.matrix);
      mesh.userData.gfPoints.visible = true;
      mesh.visible = false;
    }
  });
}

/** Release derived materials/points when a tile is disposed. */
export function disposeVisionForTile(root: Object3D) {
  root.traverse((o) => {
    const mesh = o as Tagged;
    if (!mesh.isMesh) return;
    mesh.userData.gfIr?.dispose();
    if (mesh.userData.gfPoints) mesh.userData.gfPoints.parent?.remove(mesh.userData.gfPoints);
  });
}
