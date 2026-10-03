import { chromium } from 'playwright-core';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// Usage: next build && next start -p 3002, then: node scripts/trailer/capture.mjs [baseUrl]
// Requires playwright-core and a Chromium (CHROMIUM_PATH). Frames land in scripts/trailer/frames;
// encode with: ffmpeg -framerate 24 -i frames/f%05d.jpg -c:v libx264 -crf 20 -pix_fmt yuv420p trailer.mp4
const HERE = path.dirname(fileURLToPath(import.meta.url));
const BASE = process.argv[2] ?? 'http://localhost:3002/play';

const FPS = 24, W = 1280, H = 720;
const OUT = path.join(HERE, 'frames');
fs.rmSync(OUT, { recursive: true, force: true }); fs.mkdirSync(OUT, { recursive: true });

const browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH ?? '/opt/pw-browsers/chromium', args: ['--use-gl=swiftshader', '--enable-webgl', '--ignore-gpu-blocklist'] });
const page = await browser.newPage({ viewport: { width: W, height: H } });
const errors = [];
page.on('pageerror', (e) => errors.push('PAGEERROR ' + e.message));
await page.addInitScript({ content: fs.readFileSync(path.join(HERE, 'vclock.js'), 'utf8') });
await page.goto(BASE, { waitUntil: 'networkidle', timeout: 120000 });
await page.waitForTimeout(4000);

// Trailer overlay (captions, title cards) + freeze CSS animations so every frame is deterministic.
await page.addStyleTag({ content: `
  *, *::before, *::after { animation: none !important; transition: none !important; }
  body.tr-title .cs { display: none !important; }
  #tr-cap { position: fixed; left: 0; right: 0; bottom: 0; height: 104px; z-index: 9999; pointer-events: none;
    display: flex; align-items: flex-end; justify-content: center; padding-bottom: 26px;
    background: linear-gradient(transparent, rgba(0,0,0,0.85) 55%); }
  #tr-cap span { font: 600 25px/1.2 'JetBrains Mono', ui-monospace, monospace; letter-spacing: 0.04em; color: #fff;
    text-shadow: 0 0 18px rgba(94,242,255,0.55), 0 2px 4px #000; max-width: 92%; text-align: center; }
  #tr-cap b { color: #5ef2ff; font-weight: 800; }
  #tr-card { position: fixed; inset: 0; z-index: 9998; pointer-events: none; display: flex; flex-direction: column;
    align-items: center; justify-content: center; text-align: center; background: radial-gradient(ellipse at center, rgba(2,5,9,0.15), rgba(2,5,9,0.88) 75%); }
  #tr-card.solid { background: radial-gradient(ellipse at center, rgba(2,5,9,0.86), rgba(2,5,9,0.97) 70%); }
  #tr-card .t1 { font: 900 78px/1 'JetBrains Mono', monospace; letter-spacing: 0.16em; color: #fff; text-shadow: 0 0 34px rgba(94,242,255,0.6); }
  #tr-card .t2 { font: 700 26px/1 'JetBrains Mono', monospace; letter-spacing: 0.5em; color: #5ef2ff; margin-top: 18px; }
  #tr-card .t3 { font: 500 18px/1.5 'JetBrains Mono', monospace; letter-spacing: 0.06em; color: rgba(255,255,255,0.82); margin-top: 34px; }
  #tr-card .t4 { font: 600 14px/1 'JetBrains Mono', monospace; letter-spacing: 0.3em; color: #ff8a3d; margin-top: 22px; }
` });
await page.evaluate(() => {
  const cap = document.createElement('div'); cap.id = 'tr-cap'; cap.innerHTML = '<span></span>'; document.body.appendChild(cap);
  const card = document.createElement('div'); card.id = 'tr-card'; card.style.opacity = '0'; document.body.appendChild(card);
});

const G = (fn, a) => page.evaluate(fn, a);
const ease = (x) => Math.max(0, Math.min(1, x));

// ---- Storyboard (seconds) ----
const scenes = [
  { id: 'title', dur: 5, cap: '',
    enter: async () => { await G(() => document.body.classList.add('tr-title')); },
    card: { t1: 'GLOBAL FIREFIGHT', t2: 'DRONE COMMAND', t3: 'Every active fire on Earth — live from NASA satellites.' },
    frame: async (t) => G((t) => window.__game.getState().flyTo(28, -40 - t * 14, 2.9), t) },
  { id: 'select', dur: 6.5, cap: 'Command <b>six real fire services</b> — their crews, drones and mobile carriers.',
    enter: async () => { await G(() => document.body.classList.remove('tr-title')); await page.waitForTimeout(2500); },
    frame: async (t, f) => { const order = ['1','2','3','4','5','6','2']; const i = Math.min(order.length - 1, Math.floor(t / 0.92)); if (f === 0 || Math.floor((t - 1 / FPS) / 0.92) !== i) await page.keyboard.press(order[i]); } },
  { id: 'flyin', dur: 4.5, cap: '<b>Fort McMurray, 2016.</b> 88,000 residents in the fire’s path.',
    enter: async () => { await G(() => { const s = window.__game.getState(); s.startScenario('fortmcmurray'); s.flyTo(57, -110, 2.2); s.setTimeScale(1); }); await page.waitForTimeout(1500); },
    frame: async (t) => { if (t > 0.6) await G((k) => window.__game.getState().flyTo(55.2, -112.6, 2.2 - 0.82 * k), ease((t - 0.6) / 3.2)); } },
  { id: 'dispatch', dur: 7, cap: 'Deploy carriers anywhere — <b>drone swarms launch</b> on great-circle arcs.',
    enter: async () => { await G(() => { const s = window.__game.getState(); s.setTimeScale(30); s.select({ type: 'carrier', id: s.carriers[0].id }); }); },
    frame: async (t, f) => {
      if (f === 18) await G((remainingSec) => {
        const s = window.__game.getState(); s.dispatch(s.carriers[0].id, s.fires[0].id);
        // Pace the sortie so the swarm is ~0.5 km out when the split-view scene starts.
        const d = window.__game.getState().drones[0];
        const simNeeded = Math.max(0, d.distanceKm - 0.5) / (320 / 3600);
        s.setTimeScale(simNeeded / remainingSec);
      }, (7 * FPS - 18) / FPS);
      if (f === 54) await G(() => { const s = window.__game.getState(); s.beginPlacingCarrier('USA'); });
      if (f === 66) await G(() => { const s = window.__game.getState(); s.placeCarrierAt({ lat: 51.05, lon: -114.07 }); });
      if (f === 80) await G(() => { const s = window.__game.getState(); const us = s.carriers.find((c) => c.country === 'USA'); s.dispatch(us.id, s.fires[2].id); });
    } },
  { id: 'split', dur: 8.5, cap: '<b>Split view:</b> open live feeds anywhere — the globe never stops.',
    enter: async () => {
      await G(() => { const s = window.__game.getState(); const c = s.carriers[0]; const f0 = s.fires[0];
        s.setTimeScale(2); s.select(null);
        s.openFeed({ lat: c.lat, lon: c.lon }, { carrierId: c.id, focus: 'carrier', label: 'FIRE INCENDIE Carrier — swarm launch' });
        s.openFeed({ lat: f0.lat, lon: f0.lon }, { fireId: f0.id, vision: 'ir', focus: 'drone' });
        const lead = window.__game.getState().drones.find((d) => d.targetFireId === f0.id && d.swarmIndex === 0);
        const ir = window.__game.getState().feeds[1]; if (lead) window.__game.getState().setFeedFocus(ir.id, 'drone', lead.id);
        s.flyTo(55.0, -112.6, 1.36); });
      await page.waitForTimeout(5000);
    },
    frame: async (t, f) => {
      if (f === 10) await G(() => { const s = window.__game.getState(); s.dispatch(s.carriers[0].id, s.fires[3].id); });
      if (t > 4.2) await G(() => { const el = document.querySelector('#tr-cap span'); if (el) el.innerHTML = 'Carrier close-ups, <b>IR drone cams</b>, LIDAR — up to four feeds at once.'; });
    } },
  { id: 'pilot', dur: 9, cap: '<b>Take the stick.</b> Fly the drone, line up the drop.',
    enter: async () => {
      await G(() => { const s = window.__game.getState(); const d = s.drones.find((x) => x.state === 'onstation' || x.state === 'suppressing') ?? s.drones[0]; s.enterTactical(d.id); });
      await page.waitForTimeout(5000);
      await page.keyboard.down('w'); await page.keyboard.down('Shift');
    },
    frame: async (t, f) => {
      if (f % 22 === 8 && t > 2) await page.keyboard.down(' ');
      if (f % 22 === 12 && t > 2) await page.keyboard.up(' ');
      if (f === Math.round(3.4 * FPS)) await G(() => { window.__game.getState().setVisionMode('ir'); document.querySelector('#tr-cap span').innerHTML = 'See through the smoke in <b>infrared white-hot</b>.'; });
      if (f === Math.round(6.4 * FPS)) await G(() => { window.__game.getState().setVisionMode('lidar'); document.querySelector('#tr-cap span').innerHTML = 'Map canopy and terrain with <b>LIDAR</b>.'; });
    },
    exit: async () => { await page.keyboard.up(' '); await page.keyboard.up('w'); await page.keyboard.up('Shift'); } },
  { id: 'payoff', dur: 6, cap: 'Every litre counts — <b>property saved, lives protected</b>, costs tracked live.',
    enter: async () => { await G(() => { const s = window.__game.getState(); s.exitTactical(); s.setTimeScale(240); s.flyTo(55.0, -112.6, 1.36); }); await page.waitForTimeout(3000); } },
  { id: 'debrief', dur: 4.5, cap: 'The <b>debrief</b>: the economics of every decision.',
    enter: async () => { await G(() => window.__game.getState().endMission()); await page.waitForTimeout(1500); } },
  { id: 'end', dur: 4.5, cap: '',
    card: { t1: 'GLOBAL FIREFIGHT', t2: 'DRONE COMMAND', t3: 'Live NASA FIRMS & EONET data · 6 nations · 7 historic campaigns<br/>Real-world 3D globe · split-view live feeds · IR & LIDAR', t4: 'PLAY IN YOUR BROWSER' } },
];

await G(() => window.__startManual());
let frame = 0;
const t0 = Date.now();
for (const sc of scenes) {
  if (sc.enter) await sc.enter();
  await G((sc) => {
    const span = document.querySelector('#tr-cap span'); span.innerHTML = sc.cap || '';
    document.getElementById('tr-cap').style.display = sc.cap ? 'flex' : 'none';
    const card = document.getElementById('tr-card');
    card.innerHTML = sc.card ? `<div class="t1">${sc.card.t1}</div><div class="t2">${sc.card.t2}</div><div class="t3">${sc.card.t3}</div>${sc.card.t4 ? `<div class="t4">${sc.card.t4}</div>` : ''}` : '';
    card.style.opacity = sc.card ? '1' : '0';
    card.classList.toggle('solid', sc.id === 'end');
  }, { cap: sc.cap, card: sc.card, id: sc.id });
  const n = Math.round(sc.dur * FPS);
  for (let f = 0; f < n; f++) {
    const t = f / FPS;
    if (sc.frame) await sc.frame(t, f);
    // fade captions/cards in and out over ~8 frames
    const a = Math.min(1, f / 8, (n - 1 - f) / 8);
    await G((a) => { document.getElementById('tr-cap').style.opacity = String(a); const c = document.getElementById('tr-card'); if (c.innerHTML) c.style.opacity = String(a); }, sc.id === 'end' || sc.id === 'title' ? Math.min(1, f / 10, sc.id === 'end' ? 1 : (n - 1 - f) / 10) : a);
    await G(() => window.__step(1000 / 24));
    await page.screenshot({ path: `${OUT}/f${String(frame).padStart(5, '0')}.jpg`, type: 'jpeg', quality: 90 });
    frame++;
  }
  if (sc.exit) await sc.exit();
  const st = await G(() => { const s = window.__game.getState(); return `mode=${s.mode} sim=${s.simTime.toFixed(0)} score=${(s.ledger.propertySavedUSD/1e9).toFixed(2)}B out=${s.fires.filter(f=>f.extinguished).length}/${s.fires.length} drones=${s.drones.map(d=>d.state[0]).join('')}`; });
  console.log(`[${sc.id}] frames=${frame} ${((Date.now() - t0) / 1000).toFixed(0)}s | ${st}`);
}
console.log('ERRORS:', errors.length ? '\n' + [...new Set(errors)].join('\n') : 'none');
await browser.close();
