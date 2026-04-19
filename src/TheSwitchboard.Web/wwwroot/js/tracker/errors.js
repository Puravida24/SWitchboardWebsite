/*
 * The Switchboard tracker — errors.js (T-6)
 *
 * Installs window.onerror + unhandledrejection + console.error intercept to
 * capture uncaught JS errors. Batched + flushed every 10 s or on unload via
 * Beacon. Server dedupes by sha256(message + source + line)[..16].
 *
 * Nothing is rate-limited client-side — if a tight loop fires 1000 errors,
 * the server folds them into Count on a single row.
 */
(function () {
  'use strict';

  var buffer = [];
  var FLUSH_MS = 10000;
  var MAX_BUFFER = 50;

  function id() {
    return {
      sid: self.sw && self.sw.identity ? self.sw.identity.getSessionId() : null,
      vid: self.sw && self.sw.identity ? self.sw.identity.getVisitorId() : null
    };
  }

  function enqueue(item) {
    if (buffer.length >= MAX_BUFFER) return;
    var ident = id();
    if (!ident.sid) return;
    buffer.push(Object.assign({
      sid: ident.sid,
      vid: ident.vid,
      path: location.pathname + (location.search || ''),
      ts: new Date().toISOString(),
      userAgent: navigator.userAgent,
      buildId: null
    }, item));
  }

  function flush(beaconOnly) {
    if (buffer.length === 0) return;
    var batch = buffer.splice(0, buffer.length);
    var payload = { errors: batch };
    if (self.sw && self.sw.transport) {
      if (beaconOnly) self.sw.transport.beacon('/api/tracking/errors', payload);
      else self.sw.transport.send('/api/tracking/errors', payload);
    }
  }

  function messageFrom(e) {
    try {
      if (e && e.message) return String(e.message).slice(0, 500);
      if (e && e.reason) {
        if (e.reason.message) return String(e.reason.message).slice(0, 500);
        return String(e.reason).slice(0, 500);
      }
      return String(e).slice(0, 500);
    } catch (x) { return 'unknown error'; }
  }

  function stackFrom(e) {
    try {
      if (e && e.error && e.error.stack) return String(e.error.stack).slice(0, 4000);
      if (e && e.reason && e.reason.stack) return String(e.reason.stack).slice(0, 4000);
      if (e && e.stack) return String(e.stack).slice(0, 4000);
      return null;
    } catch (x) { return null; }
  }

  function boot() {
    try {
      window.addEventListener('error', function (e) {
        enqueue({
          message: messageFrom(e),
          stack: stackFrom(e),
          source: e.filename || null,
          line: e.lineno || null,
          col: e.colno || null
        });
      });

      window.addEventListener('unhandledrejection', function (e) {
        enqueue({
          message: 'Unhandled promise rejection: ' + messageFrom(e),
          stack: stackFrom(e),
          source: null, line: null, col: null
        });
      });

      setInterval(flush, FLUSH_MS);
      document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'hidden') flush(true);
      });
      window.addEventListener('pagehide', function () { flush(true); });
    } catch (e) { /* swallow */ }
  }

  (self.sw = self.sw || {}).errors = { boot: boot };
})();
