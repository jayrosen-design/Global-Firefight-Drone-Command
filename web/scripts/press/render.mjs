// Renders the campaign briefing press photos (scenes.js) to public/campaigns/<id>.png.
// Usage: node scripts/press/render.mjs [outDir] [--source]   (needs playwright-core + a Chromium)
// Halftone is ink-on-paper, so each PNG is reduced to a 4-colour palette (~70 KB) when python3 + Pillow are available.
import { chromium } from 'playwright-core';
import { execFileSync } from 'node:child_process';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const outDir = process.argv[2] && !process.argv[2].startsWith('--') ? process.argv[2] : join(here, '../../public/campaigns');
const withSource = process.argv.includes('--source');
mkdirSync(outDir, { recursive: true });

const browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH || '/opt/pw-browsers/chromium' });
const page = await browser.newPage();
await page.setContent('<html><body></body></html>');
await page.addScriptTag({ content: readFileSync(join(here, 'scenes.js'), 'utf8') });
const ids = await page.evaluate(() => window.PRESS_SCENES);
for (const id of ids) {
  const { photo, source } = await page.evaluate((id) => window.renderPress(id), id);
  const file = join(outDir, `${id}.png`);
  writeFileSync(file, Buffer.from(photo.split(',')[1], 'base64'));
  try {
    execFileSync('python3', ['-c', 'import sys; from PIL import Image; f = sys.argv[1]; Image.open(f).convert("RGB").quantize(colors=4).save(f, optimize=True)', file]);
  } catch {
    console.warn('  (python3/Pillow not available — left as full-colour PNG)');
  }
  if (withSource) writeFileSync(join(outDir, `${id}.source.png`), Buffer.from(source.split(',')[1], 'base64'));
  console.log('wrote', id);
}
await browser.close();
