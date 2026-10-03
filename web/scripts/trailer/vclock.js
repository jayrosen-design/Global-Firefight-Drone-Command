// Deterministic frame stepping: once __startManual() is called, requestAnimationFrame callbacks
// are queued and only run on __step(ms), and performance.now() returns virtual time.
(() => {
  const realNow = performance.now.bind(performance);
  const realRaf = window.requestAnimationFrame.bind(window);
  let manual = false, vt = 0, q = [];
  performance.now = () => (manual ? vt : realNow());
  window.requestAnimationFrame = (cb) => { if (!manual) return realRaf(cb); q.push(cb); return q.length; };
  window.__startManual = () => { vt = realNow(); manual = true; };
  window.__step = (ms) => { vt += ms; const cbs = q; q = []; for (const cb of cbs) { try { cb(vt); } catch (e) { console.error(e); } } return cbs.length; };
})();
