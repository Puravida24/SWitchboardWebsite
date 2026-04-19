/*
 * The Switchboard tracker — scroll.js (T-5)
 *
 * Fires scroll-depth milestones at 25 / 50 / 75 / 100 % once per page load.
 * Also sends a final max-depth sample on visibilitychange=hidden via sendBeacon
 * for zero-loss on tab close.
 *
 * Milestone dedup is enforced BOTH client-side (via a seen-set) and server-side
 * (unique index on (sid, path, depth)).
 */
(function () {
  'use strict';

  var MILESTONES = [25, 50, 75, 100];
  var seen = Object.create(null);
  var maxDepth = 0;
  var loadStart = Date.now();

  function depthPct() {
    var doc = document.documentElement;
    var vh = window.innerHeight || 0;
    var dh = doc.scrollHeight || 0;
    if (dh <= vh) return 100; // page fits in viewport — treat as fully seen
    var scrolled = (window.scrollY || doc.scrollTop || 0) + vh;
    return Math.max(0, Math.min(100, Math.round((scrolled / dh) * 100)));
  }

  function send(depth, isMax) {
    if (!self.sw || !self.sw.transport || !self.sw.identity) return;
    var sample = {
      sid: self.sw.identity.getSessionId(),
      vid: self.sw.identity.getVisitorId(),
      path: location.pathname + (location.search || ''),
      ts: new Date().toISOString(),
      depth: depth,
      maxDepth: Math.max(maxDepth, depth),
      viewportH: (window.innerHeight | 0),
      documentH: (document.documentElement.scrollHeight | 0),
      timeSinceLoadMs: Date.now() - loadStart
    };
    var payload = { samples: [sample] };
    if (isMax) self.sw.transport.beacon('/api/tracking/scroll', payload);
    else      self.sw.transport.send('/api/tracking/scroll', payload);
  }

  function onScroll() {
    var pct = depthPct();
    if (pct > maxDepth) maxDepth = pct;
    for (var i = 0; i < MILESTONES.length; i++) {
      var m = MILESTONES[i];
      if (pct >= m && !seen[m]) {
        seen[m] = true;
        send(m, false);
      }
    }
  }

  function boot() {
    try {
      // Initial check — the page might already be fully in view (short pages).
      onScroll();
      window.addEventListener('scroll', throttle(onScroll, 150), { passive: true });
      // Unload — beacon the max depth as a distinct sample.
      document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'hidden' && maxDepth > 0) {
          send(maxDepth, true);
        }
      });
      window.addEventListener('pagehide', function () { if (maxDepth > 0) send(maxDepth, true); });
    } catch (e) { /* swallow — tracker must never break the page */ }
  }

  function throttle(fn, wait) {
    var last = 0, timeout = null;
    return function () {
      var now = Date.now();
      var remaining = wait - (now - last);
      if (remaining <= 0) {
        if (timeout) { clearTimeout(timeout); timeout = null; }
        last = now; fn();
      } else if (!timeout) {
        timeout = setTimeout(function () { last = Date.now(); timeout = null; fn(); }, remaining);
      }
    };
  }

  (self.sw = self.sw || {}).scroll = { boot: boot };
})();
