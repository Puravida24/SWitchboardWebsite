/*
 * The Switchboard tracker — mousetrail.js (T-5)
 *
 * Samples mousemove coords every 200 ms into a buffer. Batches and flushes
 * every 5 s or on visibilitychange=hidden via sendBeacon. Caps 300 points
 * per session client-side so we never flood the endpoint — the server caps
 * too as belt-and-suspenders.
 *
 * Data is aggregated nightly by the T-10 roll-up job; the mouse heatmap page
 * renders the day's rolled-up density rather than raw trails.
 */
(function () {
  'use strict';

  var SAMPLE_MS = 200;
  var FLUSH_MS = 5000;
  var CAP = 300;

  var queue = [];
  var sent = 0;
  var lastSample = 0;
  var lastX = 0, lastY = 0;
  var hasMoved = false;

  function onMove(e) {
    if (sent >= CAP) return;
    var now = Date.now();
    if (now - lastSample < SAMPLE_MS) return;
    lastSample = now;
    lastX = e.pageX | 0;
    lastY = e.pageY | 0;
    hasMoved = true;
  }

  function snapshot() {
    if (!hasMoved || sent >= CAP) return;
    hasMoved = false;
    queue.push({
      sid: self.sw && self.sw.identity ? self.sw.identity.getSessionId() : null,
      vid: self.sw && self.sw.identity ? self.sw.identity.getVisitorId() : null,
      path: location.pathname + (location.search || ''),
      ts: new Date().toISOString(),
      x: lastX,
      y: lastY,
      viewportW: (window.innerWidth  | 0),
      viewportH: (window.innerHeight | 0)
    });
    sent++;
  }

  function flush() {
    if (queue.length === 0) return;
    var batch = queue.splice(0, queue.length);
    if (self.sw && self.sw.transport) self.sw.transport.send('/api/tracking/mouse-trail', { points: batch });
  }
  function flushBeacon() {
    if (queue.length === 0) return;
    var batch = queue.splice(0, queue.length);
    if (self.sw && self.sw.transport) self.sw.transport.beacon('/api/tracking/mouse-trail', { points: batch });
  }

  function boot() {
    try {
      document.addEventListener('mousemove', onMove, { passive: true });
      setInterval(snapshot, SAMPLE_MS);
      setInterval(flush, FLUSH_MS);
      document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'hidden') { snapshot(); flushBeacon(); }
      });
      window.addEventListener('pagehide', function () { snapshot(); flushBeacon(); });
    } catch (e) { /* swallow */ }
  }

  (self.sw = self.sw || {}).mousetrail = { boot: boot };
})();
