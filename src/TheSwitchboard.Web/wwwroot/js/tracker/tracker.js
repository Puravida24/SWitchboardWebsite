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

    // T-1 heartbeat. Small (vid/sid/path only) — kept alongside the enriched
    // pageview so the admin Health page's "pings last 60s" stays meaningful
    // even if the pageview endpoint regresses.
    self.sw.transport.send('/api/tracking/ping', {
      vid: vid,
      sid: sid,
      path: location.pathname,
      ts: new Date().toISOString(),
      consentState: 'none'
    });

    // T-2 enriched pageview — UTM, click-ids, UA, viewport. The server writes
    // one PageView row, flips LandingFlag, parses UA.
    if (self.sw.pageview && typeof self.sw.pageview.record === 'function') {
      self.sw.pageview.record();
    }

    // T-3 browser signals — once per session. Heavy-ish work deferred to idle.
    if (self.sw.signals && typeof self.sw.signals.record === 'function') {
      var fire = function () { self.sw.signals.record(); };
      if (typeof requestIdleCallback === 'function') {
        requestIdleCallback(fire, { timeout: 2000 });
      } else {
        setTimeout(fire, 250);
      }
    }

    // T-4 clickstream — capture every click, batch flush, rage + dead detection.
    if (self.sw.clickstream && typeof self.sw.clickstream.boot === 'function') {
      self.sw.clickstream.boot();
    }

    // T-5 scroll milestones + max-depth at unload.
    if (self.sw.scroll && typeof self.sw.scroll.boot === 'function') {
      self.sw.scroll.boot();
    }
    // T-5 mouse trail — sampled movement, 300-point cap.
    if (self.sw.mousetrail && typeof self.sw.mousetrail.boot === 'function') {
      self.sw.mousetrail.boot();
    }
    // T-5 form funnel — per-field focus/blur/paste/error/submit/abandon.
    if (self.sw.forms && typeof self.sw.forms.boot === 'function') {
      self.sw.forms.boot();
    }
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
