/* eslint-disable */
// Procedural "press photo" illustrations for the campaign briefings' newspaper front pages.
// Each scene is drawn in greyscale on a canvas, then screened into newsprint halftone.
// Run via scripts/press/render.mjs (headless Chromium) → public/campaigns/<id>.png.

const W = 1200, H = 720;

function rng(seed) {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

const g = (v) => `rgb(${v},${v},${v})`;
const ga = (v, a) => `rgba(${v},${v},${v},${a})`;

function sky(c, stops) {
  const gr = c.createLinearGradient(0, 0, 0, H);
  stops.forEach(([o, v]) => gr.addColorStop(o, g(v)));
  c.fillStyle = gr;
  c.fillRect(0, 0, W, H);
}

/** Billowing smoke column: blurred puffs rising from (x, y), widening, drifting by `lean` px per px of rise. */
function plume(c, r, x, y, height, width, tone, lean = 0, puffs = 140) {
  c.save();
  c.filter = 'blur(10px)';
  for (let i = 0; i < puffs; i++) {
    const t = r();
    const py = y - t * height;
    const spread = width * (0.25 + t * 0.9);
    const px = x + lean * (y - py) + (r() - 0.5) * spread;
    const rad = 20 + t * width * 0.35 + r() * 30;
    const lit = (1 - t) * (1 - t) * 90; // firelight on the underside of the column
    const v = Math.max(0, Math.min(255, tone + lit + (r() - 0.5) * 70 - t * 30));
    c.fillStyle = ga(v, 0.3 + r() * 0.4);
    c.beginPath();
    c.arc(px, py, rad, 0, Math.PI * 2);
    c.fill();
  }
  c.restore();
}

function glow(c, x, y, rx, ry, strength = 1) {
  c.save();
  c.translate(x, y);
  c.scale(1, ry / rx);
  const gr = c.createRadialGradient(0, 0, 0, 0, 0, rx);
  gr.addColorStop(0, `rgba(255,255,255,${0.95 * strength})`);
  gr.addColorStop(0.35, `rgba(235,235,235,${0.6 * strength})`);
  gr.addColorStop(1, 'rgba(200,200,200,0)');
  c.fillStyle = gr;
  c.beginPath();
  c.arc(0, 0, rx, 0, Math.PI * 2);
  c.fill();
  c.restore();
}

/** A band of flame tongues along baseY between x0 and x1. */
function tongue(c, r, x, baseY, h, w, v, a) {
  const sway = (r() - 0.5) * w * 1.6;
  c.fillStyle = ga(v, a);
  c.beginPath();
  c.moveTo(x - w, baseY);
  c.bezierCurveTo(x - w * 1.1, baseY - h * 0.45, x + sway - w * 0.3, baseY - h * 0.7, x + sway, baseY - h);
  c.bezierCurveTo(x + sway + w * 0.2, baseY - h * 0.65, x + w * 1.1, baseY - h * 0.4, x + w, baseY);
  c.closePath();
  c.fill();
}

/** Fire front along baseY between x0 and x1 (baseY may be a function of x to follow terrain). */
function flames(c, r, x0, x1, baseY, height, density = 1) {
  const by = typeof baseY === 'function' ? baseY : () => baseY;
  c.save();
  c.filter = 'blur(6px)';
  for (let x = x0; x < x1; x += 40) glow(c, x, by(x) - height * 0.3, height * 1.4, height * 0.7, 0.35);
  c.filter = 'blur(1.5px)';
  const n = Math.round(((x1 - x0) / 14) * density);
  for (let pass = 0; pass < 3; pass++) {
    for (let i = 0; i < n; i++) {
      const x = x0 + r() * (x1 - x0);
      const h = height * (0.35 + r() * 0.9) * (1 - pass * 0.28);
      const w = (10 + r() * 22) * (1 - pass * 0.3);
      tongue(c, r, x, by(x) + 2, h, w, [190, 235, 255][pass], [0.75, 0.85, 0.95][pass]);
    }
  }
  c.restore();
}

function pine(c, x, baseY, h, v = 8) {
  c.fillStyle = g(v);
  c.fillRect(x - h * 0.02, baseY - h * 0.25, h * 0.04, h * 0.25);
  for (let k = 0; k < 4; k++) {
    const y = baseY - h * (0.15 + k * 0.22);
    const w = h * (0.28 - k * 0.055);
    c.beginPath();
    c.moveTo(x - w, y);
    c.lineTo(x, y - h * 0.38);
    c.lineTo(x + w, y);
    c.closePath();
    c.fill();
  }
}

function forest(c, r, x0, x1, baseY, hMin, hMax, v = 8, step = 14) {
  for (let x = x0; x < x1; x += step * (0.5 + r())) pine(c, x, baseY + r() * 6, hMin + r() * (hMax - hMin), v);
}

function house(c, x, baseY, w, h, v = 10, windows = 0) {
  c.fillStyle = g(v);
  c.fillRect(x, baseY - h, w, h);
  c.beginPath();
  c.moveTo(x - w * 0.08, baseY - h);
  c.lineTo(x + w / 2, baseY - h - w * 0.4);
  c.lineTo(x + w * 1.08, baseY - h);
  c.closePath();
  c.fill();
  for (let i = 0; i < windows; i++) {
    c.fillStyle = g(240);
    c.fillRect(x + w * (0.15 + i * 0.4), baseY - h * 0.7, w * 0.22, h * 0.3);
  }
}

function car(c, x, y, w, lights = 'tail', v = 6) {
  const h = w * 0.42;
  c.fillStyle = g(v);
  c.fillRect(x, y - h * 0.6, w, h * 0.6);
  c.fillRect(x + w * 0.2, y - h, w * 0.6, h * 0.45);
  c.save();
  c.filter = 'blur(3px)';
  c.fillStyle = g(lights === 'tail' ? 200 : 255);
  c.beginPath();
  c.arc(x + w * 0.1, y - h * 0.35, w * 0.08, 0, Math.PI * 2);
  c.arc(x + w * 0.9, y - h * 0.35, w * 0.08, 0, Math.PI * 2);
  c.fill();
  c.restore();
}

function palm(c, r, x, baseY, h, lean = 0.15, v = 6) {
  c.strokeStyle = g(v);
  c.lineWidth = h * 0.035;
  c.lineCap = 'round';
  const tx = x + lean * h, ty = baseY - h;
  c.beginPath();
  c.moveTo(x, baseY);
  c.quadraticCurveTo(x + lean * h * 0.2, baseY - h * 0.6, tx, ty);
  c.stroke();
  c.lineWidth = h * 0.02;
  for (let k = 0; k < 9; k++) {
    const a = -Math.PI + (k / 8) * Math.PI + (r() - 0.5) * 0.3 + lean * 2;
    const len = h * (0.3 + r() * 0.15);
    c.beginPath();
    c.moveTo(tx, ty);
    c.quadraticCurveTo(tx + Math.cos(a) * len * 0.6, ty + Math.sin(a) * len * 0.6 - len * 0.15, tx + Math.cos(a) * len, ty + Math.sin(a) * len + len * 0.35);
    c.stroke();
  }
}

function blobTree(c, r, x, baseY, h, v = 6) {
  c.fillStyle = g(v);
  c.fillRect(x - h * 0.025, baseY - h * 0.55, h * 0.05, h * 0.55);
  for (let k = 0; k < 9; k++) {
    c.beginPath();
    c.arc(x + (r() - 0.5) * h * 0.6, baseY - h * (0.55 + r() * 0.4), h * (0.1 + r() * 0.1), 0, Math.PI * 2);
    c.fill();
  }
}

function person(c, x, baseY, h, v = 5) {
  c.fillStyle = g(v);
  c.beginPath();
  c.arc(x, baseY - h * 0.88, h * 0.1, 0, Math.PI * 2);
  c.fill();
  c.beginPath();
  c.moveTo(x - h * 0.13, baseY - h * 0.75);
  c.lineTo(x + h * 0.13, baseY - h * 0.75);
  c.lineTo(x + h * 0.1, baseY - h * 0.35);
  c.lineTo(x + h * 0.08, baseY);
  c.lineTo(x - h * 0.08, baseY);
  c.lineTo(x - h * 0.1, baseY - h * 0.35);
  c.closePath();
  c.fill();
}

function ridge(c, r, y, amp, v, step = 30) {
  c.fillStyle = g(v);
  c.beginPath();
  c.moveTo(0, H);
  let yy = y;
  for (let x = 0; x <= W + step; x += step) {
    yy = y + Math.sin(x * 0.006 + r() * 0.2) * amp + (r() - 0.5) * amp * 0.4;
    c.lineTo(x, yy);
  }
  c.lineTo(W, H);
  c.closePath();
  c.fill();
}

function helicopter(c, x, y, s, v = 8) {
  c.fillStyle = g(v);
  c.strokeStyle = g(v);
  c.beginPath();
  c.ellipse(x, y, s * 0.5, s * 0.22, 0, 0, Math.PI * 2);
  c.fill();
  c.fillRect(x + s * 0.4, y - s * 0.06, s * 0.7, s * 0.08);
  c.fillRect(x + s * 1.05, y - s * 0.22, s * 0.06, s * 0.24);
  c.lineWidth = s * 0.03;
  c.beginPath();
  c.moveTo(x - s * 0.9, y - s * 0.32);
  c.lineTo(x + s * 0.9, y - s * 0.32);
  c.stroke();
  c.beginPath();
  c.moveTo(x, y - s * 0.32);
  c.lineTo(x, y - s * 0.2);
  c.stroke();
  // Bambi bucket on its long line
  c.lineWidth = s * 0.012;
  c.beginPath();
  c.moveTo(x, y + s * 0.2);
  c.lineTo(x - s * 0.05, y + s * 1.2);
  c.stroke();
  c.beginPath();
  c.moveTo(x - s * 0.15, y + s * 1.2);
  c.lineTo(x + s * 0.05, y + s * 1.2);
  c.lineTo(x + s * 0.0, y + s * 1.42);
  c.lineTo(x - s * 0.12, y + s * 1.42);
  c.closePath();
  c.fill();
}

const SCENES = {
  // Camp Fire, 2018: the plume over the ridge, cars fleeing on the Skyway through burning pines.
  paradise(c, r) {
    sky(c, [[0, 70], [0.45, 130], [0.62, 225], [0.7, 120], [1, 30]]);
    plume(c, r, 640, 470, 520, 900, 110, -0.25, 220);
    glow(c, 620, 470, 700, 120, 1);
    ridge(c, r, 470, 18, 28, 24);
    flames(c, r, 0, W, 478, 70, 1.2);
    forest(c, r, -20, W + 20, 520, 80, 170, 10, 22);
    flames(c, r, -20, 470, 525, 120, 1.2);
    flames(c, r, 760, W + 20, 525, 120, 1.2);
    // the Skyway: a road sweeping to the viewer with a line of cars
    const road = c.createLinearGradient(0, 520, 0, H); road.addColorStop(0, g(150)); road.addColorStop(1, g(55));
    c.fillStyle = road;
    c.beginPath();
    c.moveTo(520, 520); c.lineTo(680, 520); c.lineTo(1150, H); c.lineTo(250, H); c.closePath(); c.fill();
    c.strokeStyle = g(120); c.setLineDash([22, 26]); c.lineWidth = 4;
    c.beginPath(); c.moveTo(600, 522); c.lineTo(700, H); c.stroke(); c.setLineDash([]);
    [[560, 560, 34], [610, 600, 48], [470, 640, 66], [640, 680, 90], [380, 712, 110]].forEach(([x, y, w]) => car(c, x, y, w, 'tail'));
    forest(c, r, -40, 300, H + 10, 200, 320, 4, 46);
    forest(c, r, 1000, W + 60, H + 10, 200, 330, 4, 46);
  },
  // Lahaina, 2023: wind-driven fire along the Front Street waterfront, smoke streaming out to sea.
  lahaina(c, r) {
    sky(c, [[0, 60], [0.4, 120], [0.55, 210], [0.6, 150], [1, 50]]);
    for (let i = 0; i < 4; i++) plume(c, r, 300 + i * 220, 420, 300, 300, 110 - i * 6, 0.9, 90);
    glow(c, 600, 420, 650, 90, 1);
    // storefronts
    for (let x = 60; x < W; x += 95 + r() * 30) {
      const h = 70 + r() * 50;
      c.fillStyle = g(12);
      c.fillRect(x, 430 - h, 80 + r() * 20, h);
      c.fillStyle = g(235);
      for (let k = 0; k < 3; k++) c.fillRect(x + 8 + k * 24, 430 - h + 18, 14, 18);
      c.fillRect(x + 8, 430 - h * 0.45, 60, 14);
    }
    flames(c, r, 40, W - 40, 340, 110, 1.3);
    // the banyan's spreading canopy on the left
    c.fillStyle = g(8);
    for (let k = 0; k < 30; k++) { c.beginPath(); c.arc(40 + r() * 280, 300 + r() * 90, 30 + r() * 40, 0, Math.PI * 2); c.fill(); }
    for (let k = 0; k < 7; k++) c.fillRect(70 + k * 36, 360, 10, 80);
    // seawall and harbour
    c.fillStyle = g(20); c.fillRect(0, 430, W, 22);
    const sea = c.createLinearGradient(0, 452, 0, H); sea.addColorStop(0, g(170)); sea.addColorStop(1, g(40));
    c.fillStyle = sea; c.fillRect(0, 452, W, H - 452);
    c.save(); c.filter = 'blur(4px)';
    for (let k = 0; k < 120; k++) { c.fillStyle = ga(230, 0.5); c.fillRect(r() * W, 460 + r() * 200, 30 + r() * 80, 3); }
    c.restore();
    palm(c, r, 900, 452, 300, 0.35); palm(c, r, 1010, 452, 260, 0.4); palm(c, r, 760, 452, 220, 0.3);
    // boat silhouette
    c.fillStyle = g(6); c.beginPath(); c.moveTo(380, 600); c.lineTo(560, 600); c.lineTo(530, 630); c.lineTo(400, 630); c.closePath(); c.fill();
    c.fillRect(455, 520, 6, 80);
  },
  // Fort McMurray, 2016: the convoy south on Highway 63 under a pyrocumulus, spruce burning on both shoulders.
  fortmcmurray(c, r) {
    sky(c, [[0, 30], [0.35, 70], [0.55, 190], [0.62, 230], [0.7, 120], [1, 25]]);
    plume(c, r, 600, 450, 600, 1100, 105, 0.05, 260);
    glow(c, 600, 440, 800, 140, 1);
    flames(c, r, 0, 470, 470, 160, 1.5);
    flames(c, r, 730, W, 470, 160, 1.5);
    forest(c, r, -20, 470, 480, 110, 220, 8, 20);
    forest(c, r, 730, W + 20, 480, 110, 220, 8, 20);
    const hwy = c.createLinearGradient(0, 470, 0, H); hwy.addColorStop(0, g(170)); hwy.addColorStop(1, g(50));
    c.fillStyle = hwy;
    c.beginPath(); c.moveTo(480, 470); c.lineTo(720, 470); c.lineTo(1260, H); c.lineTo(-60, H); c.closePath(); c.fill();
    c.strokeStyle = g(150); c.setLineDash([26, 30]); c.lineWidth = 5;
    c.beginPath(); c.moveTo(600, 472); c.lineTo(600, H); c.stroke(); c.setLineDash([]);
    // two lanes of traffic heading away from camera (tail lights) — the evacuation south
    [[510, 490, 26], [530, 520, 34], [470, 560, 48], [420, 610, 64], [330, 680, 96], [640, 500, 30], [660, 540, 42], [700, 600, 60], [760, 680, 90]].forEach(([x, y, w]) => car(c, x, y, w, 'tail'));
  },
  // Pantanal, 2020: a fire front crossing open wetland, water channels catching the glow, a jaguar silhouette.
  pantanal(c, r) {
    sky(c, [[0, 120], [0.4, 150], [0.5, 200], [0.56, 120], [1, 70]]);
    for (let i = 0; i < 5; i++) plume(c, r, 120 + i * 250, 400, 260, 380, 120, 0.4, 80);
    glow(c, 600, 405, 900, 60, 0.9);
    flames(c, r, 0, W, 410, 60, 1.4);
    c.fillStyle = g(30); c.fillRect(0, 410, W, H - 410);
    c.save(); c.filter = 'blur(3px)';
    for (let k = 0; k < 9; k++) { // water channels
      const y = 440 + k * 30 + r() * 10;
      const gr = c.createLinearGradient(0, y, W, y); gr.addColorStop(0, ga(200, 0)); gr.addColorStop(0.5, ga(215, 0.8)); gr.addColorStop(1, ga(200, 0));
      c.fillStyle = gr; c.fillRect(-50 + r() * 300, y, 500 + r() * 700, 6 + k);
    }
    c.restore();
    [[150, 420, 160], [330, 418, 120], [880, 420, 190], [1080, 422, 140]].forEach(([x, y, h]) => palm(c, r, x, y, h, 0.02, 10));
    [[520, 420, 110], [700, 420, 90]].forEach(([x, y, h]) => blobTree(c, r, x, y, h, 10));
    // jaguar, walking left
    c.fillStyle = g(6);
    c.beginPath();
    c.ellipse(760, 600, 95, 34, 0, 0, Math.PI * 2); c.fill();
    c.beginPath(); c.ellipse(660, 585, 34, 26, -0.2, 0, Math.PI * 2); c.fill();
    c.beginPath(); c.arc(648, 562, 9, 0, Math.PI * 2); c.arc(672, 560, 9, 0, Math.PI * 2); c.fill();
    [[700, 30], [730, 28], [800, 28], [830, 30]].forEach(([x, l]) => c.fillRect(x, 610, 16, l + 25));
    c.lineWidth = 12; c.strokeStyle = g(6); c.lineCap = 'round';
    c.beginPath(); c.moveTo(850, 590); c.quadraticCurveTo(920, 600, 930, 650); c.stroke();
  },
  // Chongqing, 2022: the fire line climbing Jinyun Mountain at night above the city, volunteers' headlights on the slope.
  chongqing(c, r) {
    sky(c, [[0, 10], [0.5, 30], [1, 20]]);
    plume(c, r, 600, 250, 250, 900, 70, 0.1, 160);
    ridge(c, r, 250, 60, 22, 20);
    // the burning front snaking up the ridge
    const front = (x) => 300 - Math.sin(x * 0.006) * 70 + Math.sin(x * 0.05) * 8;
    glow(c, 600, 280, 600, 80, 0.5);
    flames(c, r, 120, 1080, front, 45, 0.9);
    // volunteers' motorbike headlights winding up the slope
    c.save(); c.filter = 'blur(2px)'; c.fillStyle = g(255);
    for (let k = 0; k < 60; k++) { const t = k / 60; c.beginPath(); c.arc(200 + t * 500 + Math.sin(t * 18) * 40, 520 - t * 160, 3, 0, Math.PI * 2); c.fill(); }
    c.restore();
    // city skyline foreground
    c.fillStyle = g(8);
    for (let bx = 0; bx < W; bx += 50 + r() * 40) {
      const bh = 90 + r() * 200, bw = 40 + r() * 40;
      c.fillStyle = g(8); c.fillRect(bx, H - bh, bw, bh);
      c.fillStyle = g(170);
      for (let wy = H - bh + 10; wy < H - 10; wy += 16) for (let wx = bx + 6; wx < bx + bw - 8; wx += 12) if (r() > 0.55) c.fillRect(wx, wy, 5, 7);
    }
  },
  // Bohemian–Saxon Switzerland, 2022: smoke pouring between sandstone towers, a helicopter bucket run.
  saxon(c, r) {
    sky(c, [[0, 150], [0.5, 175], [1, 110]]);
    plume(c, r, 650, 600, 650, 900, 120, -0.15, 220);
    glow(c, 640, 610, 500, 70, 0.7);
    const towers = [[90, 170, 420], [260, 120, 330], [420, 150, 470], [820, 140, 380], [990, 180, 450], [1140, 120, 300]];
    towers.forEach(([tx, tw, th]) => {
      c.fillStyle = g(30);
      c.beginPath();
      c.moveTo(tx - tw / 2, H);
      for (let k = 0; k <= 10; k++) { const y = H - (th * k) / 10; c.lineTo(tx - tw / 2 + (r() - 0.5) * 14 + (k / 10) * 10, y); }
      c.quadraticCurveTo(tx, H - th - 40, tx + tw / 2 - 10, H - th);
      for (let k = 10; k >= 0; k--) { const y = H - (th * k) / 10; c.lineTo(tx + tw / 2 + (r() - 0.5) * 14 - (k / 10) * 10, y); }
      c.closePath(); c.fill();
      c.strokeStyle = g(60); c.lineWidth = 2;
      for (let k = 1; k < 7; k++) { c.beginPath(); c.moveTo(tx - tw / 2, H - th * k / 7); c.lineTo(tx + tw / 2, H - th * k / 7 + (r() - 0.5) * 8); c.stroke(); }
      forest(c, r, tx - tw / 2, tx + tw / 2, H - th + 6, 20, 40, 14, 12);
    });
    forest(c, r, 480, 800, 680, 60, 120, 10, 16);
    flames(c, r, 470, 820, 690, 150, 1.6);
    helicopter(c, 760, 170, 70, 10);
  },
  // Black Summer, New Year's Eve 2019: Mallacoota residents on the beach under a black-red sky, boats offshore.
  blacksummer(c, r) {
    sky(c, [[0, 15], [0.3, 40], [0.55, 110], [0.62, 60], [1, 30]]);
    plume(c, r, 600, 380, 400, 1400, 60, 0.1, 200);
    glow(c, 820, 380, 500, 80, 0.9);
    // sun as a pale disc through the smoke
    c.fillStyle = ga(200, 0.8); c.beginPath(); c.arc(330, 170, 34, 0, Math.PI * 2); c.fill();
    flames(c, r, 500, W, 390, 60, 1.2);
    [[560, 395, 150], [700, 395, 190], [880, 395, 170], [1040, 395, 210], [1160, 395, 160]].forEach(([x, y, h]) => blobTree(c, r, x, y, h, 6));
    const sea = c.createLinearGradient(0, 395, 0, 520); sea.addColorStop(0, g(110)); sea.addColorStop(1, g(45));
    c.fillStyle = sea; c.fillRect(0, 395, W, 125);
    c.fillStyle = g(150); c.fillRect(0, 520, W, H - 520);
    c.fillStyle = g(120); for (let k = 0; k < 400; k++) c.fillRect(r() * W, 520 + r() * 200, 2, 2);
    // boats waiting offshore
    [[120, 450, 70], [280, 470, 50], [420, 445, 40]].forEach(([x, y, w]) => { c.fillStyle = g(10); c.fillRect(x, y, w, w * 0.18); c.fillRect(x + w * 0.3, y - w * 0.2, w * 0.3, w * 0.2); });
    // people on the sand
    for (let k = 0; k < 26; k++) person(c, 40 + r() * 1120, 540 + r() * 170, 60 + r() * 60, 6);
  },
};

/** Screen the greyscale scene into newsprint halftone (45° dot screen, ink on paper). */
function halftone(src, out, cell = 5) {
  const s = src.getContext('2d').getImageData(0, 0, W, H).data;
  const c = out.getContext('2d');
  c.fillStyle = '#ece4d2';
  c.fillRect(0, 0, W, H);
  c.fillStyle = '#1b1814';
  const a = Math.PI / 4, ca = Math.cos(a), sa = Math.sin(a);
  const R = Math.hypot(W, H);
  for (let u = -R; u < R; u += cell) {
    for (let v = -R; v < R; v += cell) {
      const x = u * ca - v * sa + W / 2, y = u * sa + v * ca + H / 2;
      if (x < -cell || y < -cell || x > W + cell || y > H + cell) continue;
      const xi = Math.min(W - 1, Math.max(0, x | 0)), yi = Math.min(H - 1, Math.max(0, y | 0));
      const i = (yi * W + xi) * 4;
      const lum = (s[i] * 0.3 + s[i + 1] * 0.59 + s[i + 2] * 0.11) / 255;
      const dark = Math.pow(1 - lum, 1.25);
      const rad = (cell / 2) * 1.25 * Math.sqrt(dark);
      if (rad < 0.25) continue;
      c.beginPath();
      c.arc(x, y, rad, 0, Math.PI * 2);
      c.fill();
    }
  }
}

window.renderPress = (id, seed = 7) => {
  const src = document.createElement('canvas');
  src.width = W; src.height = H;
  SCENES[id](src.getContext('2d'), rng(seed));
  const out = document.createElement('canvas');
  out.width = W; out.height = H;
  halftone(src, out);
  return { photo: out.toDataURL('image/png'), source: src.toDataURL('image/png') };
};
window.PRESS_SCENES = Object.keys(SCENES);
