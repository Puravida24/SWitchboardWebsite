/*
 * The Switchboard tracker — transport.js (T-1)
 * Fire-and-forget POST with keepalive + sendBeacon fallback. Every tracking module
 * funnels through here so retry / batching / beacon live in one place.
 *
 * Exports (attached to window.sw):
 *   send(path, payload)       → returns Promise<boolean>, fast-path for low-volume events
 *   beacon(path, payload)     → synchronous-ish, used on unload
 */
(function () {
  'use strict';

  function toBody(payload) {
    try {
      return JSON.stringify(payload || {});
    } catch (e) {
      return '{}';
    }
  }

  function send(path, payload) {
    var body = toBody(payload);
    if (typeof fetch === 'function') {
      return fetch(path, {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: body,
        keepalive: true
      }).then(function (r) { return r.ok; }).catch(function () { return false; });
    }
    // Pre-fetch fallback (very old browsers): sendBeacon.
    return Promise.resolve(beacon(path, payload));
  }

  function beacon(path, payload) {
    try {
      var body = toBody(payload);
      if (navigator && typeof navigator.sendBeacon === 'function') {
        var blob = new Blob([body], { type: 'application/json' });
        return navigator.sendBeacon(path, blob);
      }
    } catch (e) { /* swallow — tracker must never break the page */ }
    return false;
  }

  (self.sw = self.sw || {}).transport = { send: send, beacon: beacon };
})();
