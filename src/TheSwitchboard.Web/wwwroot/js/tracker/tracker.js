/*
 * The Switchboard tracker — tracker.js (T-1 entry)
 *
 * Responsibilities at T-1:
 *   1. Honor DNT / Sec-GPC / globalPrivacyControl — early-exit before any network call.
 *   2. Load identity.js + transport.js (they attach to window.sw when evaluated).
 *   3. Fire a heartbeat POST /api/tracking/ping carrying { vid, sid, consentState }.
 *
 * Later slices plug in pageview, attribution, clickstream, scroll, forms, webvitals,
 * errors, signals, replay. Each module is loaded independently and booted from here.
 *
 * This file MUST be served from the same origin. The homepage injects it with the
 * per-request CSP nonce via PublicPageModel's {{NONCE}} + {{BUILDID}} substitution.
 */
(function () {
  'use strict';

  // ── T-1.A Privacy gate ────────────────────────────────────────────────
  // Match server-side AnalyticsMiddleware: honor DNT = 1 and Sec-GPC / globalPrivacyControl.
  function privacySignal() {
    try {
      if (navigator && navigator.doNotTrack === '1') return 'dnt';
      // Firefox uses window.doNotTrack; some older IE uses navigator.msDoNotTrack.
      if (typeof window !== 'undefined' && window.doNotTrack === '1') return 'dnt';
      if (navigator && navigator.msDoNotTrack === '1') return 'dnt';
      if (navigator && navigator.globalPrivacyControl === true) return 'gpc';
    } catch (e) { /* defensive — don't block page if navigator is quirky */ }
    return 'none';
  }

  var consent = privacySignal();
  if (consent !== 'none') {
    // Do not boot. Do not set cookies. Do not touch network.
    (self.sw = self.sw || {}).consentState = consent;
    (self.sw).disabled = true;
    return;
  }

  function boot() {
    // identity.js + transport.js must have loaded (we import them via <script> tags
    // ahead of this one). If not yet evaluated, bail quietly — will retry on next
    // page load.
    if (!self.sw || !self.sw.identity || !self.sw.transport) return;

    var vid = self.sw.identity.getVisitorId();
    var sid = self.sw.identity.getSessionId();

    self.sw.vid = vid;
    self.sw.sid = sid;
    self.sw.consentState = 'none';

    // T-1 heartbeat. No real data yet — just proves the pipe is alive so the
    // admin Health page can show "pings in last 60s."
    self.sw.transport.send('/api/tracking/ping', {
      vid: vid,
      sid: sid,
      path: location.pathname,
      ts: new Date().toISOString(),
      consentState: 'none'
    });
  }

  // Defer so identity.js + transport.js — loaded with matching `defer` attributes
  // — have a chance to evaluate first. They attach synchronously, so by the time
  // DOMContentLoaded fires, they're ready.
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot, { once: true });
  } else {
    boot();
  }
})();
