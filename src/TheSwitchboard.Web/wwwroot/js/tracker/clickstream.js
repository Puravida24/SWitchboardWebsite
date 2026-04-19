/*
 * The Switchboard tracker — clickstream.js (T-4)
 *
 * Captures every click with:
 *   - CSS selector path (max 6 levels, nth-of-type fallback)
 *   - text sample (64 chars)
 *   - coordinates + viewport + page size
 *   - mouse button
 *
 * Dead-click detection is client-side: for 1 second after each click, a
 * MutationObserver watches the document for any subtree mutation. If none
 * is observed AND no navigation happened, the click is tagged isDead=true.
 *
 * Rage-click detection is SERVER-side at ingest — we just deliver clicks
 * reliably. Client batches clicks and flushes every 5s, or on
 * visibilitychange=hidden via sendBeacon for zero-loss on tab close.
 *
 * Cap: 500 clicks per session on the server side — client keeps sending,
 * server drops overflow silently.
 */
(function () {
  'use strict';

  var QUEUE = [];
  var FLUSH_INTERVAL_MS = 5000;
  var DEAD_OBSERVE_MS = 1000;

  function buildSelector(el) {
    if (!el || !el.tagName) return '';
    var parts = [];
    var node = el;
    var depth = 0;
    while (node && node.tagName && depth < 6) {
      var piece = node.tagName.toLowerCase();
      if (node.id) {
        piece = '#' + node.id;
        parts.unshift(piece);
        break; // IDs are anchors — stop walking up.
      }
      if (node.classList && node.classList.length) {
        // Cap classes to keep selector short.
        var cls = Array.prototype.slice.call(node.classList, 0, 2).join('.');
        if (cls) piece += '.' + cls;
      }
      // nth-of-type fallback for siblings without classes.
      var parent = node.parentElement;
      if (parent && !node.id) {
        var sameTag = Array.prototype.filter.call(
          parent.children,
          function (c) { return c.tagName === node.tagName; }
        );
        if (sameTag.length > 1) {
          var idx = sameTag.indexOf(node) + 1;
          if (idx > 0) piece += ':nth-of-type(' + idx + ')';
        }
      }
      parts.unshift(piece);
      node = parent;
      depth++;
    }
    return parts.join(' > ');
  }

  function textSample(el) {
    try {
      var t = (el.textContent || '').replace(/\s+/g, ' ').trim();
      return t.slice(0, 64);
    } catch (e) { return null; }
  }

  function enqueue(click) {
    QUEUE.push(click);
    if (QUEUE.length >= 20) flush(); // size-based flush for high-velocity sessions
  }

  function flush() {
    if (QUEUE.length === 0) return;
    var batch = QUEUE.splice(0, QUEUE.length);
    if (!self.sw || !self.sw.transport) return;
    self.sw.transport.send('/api/tracking/clicks', { clicks: batch });
  }

  function flushBeacon() {
    if (QUEUE.length === 0) return;
    var batch = QUEUE.splice(0, QUEUE.length);
    if (!self.sw || !self.sw.transport) return;
    self.sw.transport.beacon('/api/tracking/clicks', { clicks: batch });
  }

  function observeForMutation(click) {
    // Arm a MutationObserver for DEAD_OBSERVE_MS. If anything mutates in the DOM
    // or the location changes, the click isn't dead. Otherwise flip isDead=true
    // before the batch flushes.
    var observed = false;
    var mo;
    try {
      mo = new MutationObserver(function () { observed = true; });
      mo.observe(document.body, { childList: true, subtree: true, attributes: true, characterData: true });
    } catch (e) { /* no DOM → cannot observe */ }

    var startUrl = location.href;
    setTimeout(function () {
      try { if (mo) mo.disconnect(); } catch (e) { /* swallow */ }
      var navigated = (location.href !== startUrl);
      click.isDead = !observed && !navigated;
    }, DEAD_OBSERVE_MS);
  }

  function onClick(e) {
    try {
      if (!e || !e.target) return;
      var el = e.target;
      if (!(el instanceof Element)) return;
      var vid = self.sw && self.sw.identity ? self.sw.identity.getVisitorId() : null;
      var sid = self.sw && self.sw.identity ? self.sw.identity.getSessionId() : null;
      if (!sid) return;

      var click = {
        sid: sid,
        vid: vid,
        path: location.pathname + (location.search || ''),
        ts: new Date().toISOString(),
        x: e.pageX | 0,
        y: e.pageY | 0,
        viewportW: (window.innerWidth  | 0),
        viewportH: (window.innerHeight | 0),
        pageW: (document.documentElement.scrollWidth  | 0),
        pageH: (document.documentElement.scrollHeight | 0),
        selector: buildSelector(el),
        tagName: (el.tagName || '').toLowerCase(),
        elementText: textSample(el),
        elementHref: (el.closest && el.closest('a')) ? (el.closest('a').getAttribute('href') || null) : null,
        mouseButton: (e.button | 0),
        isDead: false
      };
      enqueue(click);
      observeForMutation(click);
    } catch (err) { /* tracker must never break the page */ }
  }

  function boot() {
    try {
      document.addEventListener('click', onClick, true);
    } catch (e) { return; }

    // Timed flush.
    setInterval(flush, FLUSH_INTERVAL_MS);

    // Final flush on tab close / visibility change.
    document.addEventListener('visibilitychange', function () {
      if (document.visibilityState === 'hidden') flushBeacon();
    });
    window.addEventListener('pagehide', flushBeacon);
    window.addEventListener('beforeunload', flushBeacon);
  }

  (self.sw = self.sw || {}).clickstream = { boot: boot, flush: flush };
})();
