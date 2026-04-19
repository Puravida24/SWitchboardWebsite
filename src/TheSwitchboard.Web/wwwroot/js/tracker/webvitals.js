/*
 * The Switchboard tracker — webvitals.js (T-6)
 *
 * Collects Core Web Vitals via native PerformanceObserver + Navigation Timing
 * without importing a vendor library. Coverage:
 *   LCP  — largest-contentful-paint (PO)
 *   FCP  — paint entry "first-contentful-paint" (PO)
 *   CLS  — layout-shift entries (PO, session-max)
 *   TTFB — PerformanceNavigationTiming.responseStart
 *   INP  — first-input (approximation; full INP polyfill deferred)
 *
 * Buffers samples and flushes on visibilitychange=hidden + pagehide via Beacon.
 * Server rates the value against Google thresholds and stores rating alongside.
 */
(function () {
  'use strict';

  var buffer = [];
  var lcpValue = 0;
  var clsValue = 0;
  var clsEntries = [];
  var clsSessionValue = 0;
  var clsSessionStart = 0;
  var fcpValue = 0;
  var ttfbValue = 0;
  var fidValue = 0;
  var sent = { LCP: false, FCP: false, CLS: false, TTFB: false, INP: false };

  function id() {
    return {
      sid: self.sw && self.sw.identity ? self.sw.identity.getSessionId() : null,
      vid: self.sw && self.sw.identity ? self.sw.identity.getVisitorId() : null
    };
  }

  function enqueue(metric, value) {
    if (sent[metric]) return;
    var ident = id();
    if (!ident.sid) return;
    buffer.push({
      sid: ident.sid,
      vid: ident.vid,
      path: location.pathname + (location.search || ''),
      ts: new Date().toISOString(),
      metric: metric,
      value: value,
      navigationType: (performance.getEntriesByType('navigation')[0] || {}).type || 'navigate'
    });
  }

  function flush(beaconOnly) {
    if (buffer.length === 0) return;
    var batch = buffer.splice(0, buffer.length);
    // Mark fired so the unload flush doesn't double-send.
    for (var i = 0; i < batch.length; i++) sent[batch[i].metric] = true;
    var payload = { vitals: batch };
    if (self.sw && self.sw.transport) {
      if (beaconOnly) self.sw.transport.beacon('/api/tracking/vitals', payload);
      else self.sw.transport.send('/api/tracking/vitals', payload);
    }
  }

  function observeLCP() {
    try {
      var po = new PerformanceObserver(function (list) {
        var entries = list.getEntries();
        if (entries.length > 0) {
          var last = entries[entries.length - 1];
          lcpValue = last.renderTime || last.loadTime || last.startTime || lcpValue;
        }
      });
      po.observe({ type: 'largest-contentful-paint', buffered: true });
    } catch (e) { /* unsupported */ }
  }

  function observeFCP() {
    try {
      var po = new PerformanceObserver(function (list) {
        list.getEntries().forEach(function (entry) {
          if (entry.name === 'first-contentful-paint') fcpValue = entry.startTime;
        });
      });
      po.observe({ type: 'paint', buffered: true });
    } catch (e) { /* unsupported */ }
  }

  function observeCLS() {
    try {
      var po = new PerformanceObserver(function (list) {
        list.getEntries().forEach(function (entry) {
          if (entry.hadRecentInput) return;
          var now = entry.startTime;
          if (clsEntries.length === 0 ||
              now - clsEntries[clsEntries.length - 1].startTime > 1000 ||
              now - clsSessionStart > 5000) {
            clsEntries = [entry];
            clsSessionStart = now;
            clsSessionValue = entry.value;
          } else {
            clsEntries.push(entry);
            clsSessionValue += entry.value;
          }
          if (clsSessionValue > clsValue) clsValue = clsSessionValue;
        });
      });
      po.observe({ type: 'layout-shift', buffered: true });
    } catch (e) { /* unsupported */ }
  }

  function observeFID() {
    try {
      var po = new PerformanceObserver(function (list) {
        list.getEntries().forEach(function (entry) {
          if (!fidValue) fidValue = entry.processingStart - entry.startTime;
        });
      });
      po.observe({ type: 'first-input', buffered: true });
    } catch (e) { /* unsupported */ }
  }

  function captureTTFB() {
    try {
      var nav = performance.getEntriesByType('navigation')[0];
      if (nav) ttfbValue = nav.responseStart || 0;
    } catch (e) { /* unsupported */ }
  }

  function finalize(beaconOnly) {
    if (lcpValue > 0) enqueue('LCP', Math.round(lcpValue));
    if (fcpValue > 0) enqueue('FCP', Math.round(fcpValue));
    if (clsValue > 0) enqueue('CLS', Math.round(clsValue * 1000) / 1000);
    if (ttfbValue > 0) enqueue('TTFB', Math.round(ttfbValue));
    if (fidValue > 0) enqueue('INP', Math.round(fidValue));
    flush(beaconOnly);
  }

  function boot() {
    try {
      observeLCP();
      observeFCP();
      observeCLS();
      observeFID();
      if (document.readyState === 'complete') captureTTFB();
      else window.addEventListener('load', captureTTFB);

      document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'hidden') finalize(true);
      });
      window.addEventListener('pagehide', function () { finalize(true); });
    } catch (e) { /* swallow */ }
  }

  (self.sw = self.sw || {}).webvitals = { boot: boot };
})();
