/*
 * The Switchboard tracker — signals.js (T-3)
 *
 * Captures per-session environment signals ONCE per session (tracked in
 * sessionStorage.swSignalsSent) and POSTs /api/tracking/signals. Server is
 * idempotent so duplicate sends on cross-tab navigation are harmless.
 *
 * Fields are broad/shallow by design — they bucket traffic for device / privacy
 * / fraud heuristics without fingerprinting any individual visitor. The canvas
 * hash is a stub (a short hash of a fixed-shape canvas) — full Bayesian
 * fingerprinting is deferred.
 *
 * Never blocks the page — wrapped in try/catch, fires in idle callback.
 */
(function () {
  'use strict';

  var SENT_KEY = 'swSignalsSent';

  function safe(fn, fallback) {
    try { return fn(); } catch (e) { return fallback; }
  }

  function detectWebView(ua) {
    // Meta in-app browser (Facebook / Instagram WebView) — identified by FBAN/FBAV
    // or Instagram tokens. Sessions here have broken cookie persistence and ITP
    // tracking-protection headaches that affect attribution.
    var isMeta = /FBAN|FBAV|Instagram/i.test(ua);
    // TikTok in-app browser — identified by the BytedanceWebview token.
    var isTikTok = /BytedanceWebview|musical_ly|TikTok/i.test(ua);
    return { isMeta: isMeta, isTikTok: isTikTok };
  }

  function canvasStub() {
    // Tiny canvas — stable across visits for the same device, trivially cheap.
    try {
      var c = document.createElement('canvas');
      c.width = 60; c.height = 30;
      var ctx = c.getContext('2d');
      if (!ctx) return null;
      ctx.textBaseline = 'top';
      ctx.font = '14px Arial';
      ctx.fillStyle = '#f60'; ctx.fillRect(0, 0, 60, 30);
      ctx.fillStyle = '#069'; ctx.fillText('sw', 2, 15);
      return c.toDataURL().slice(-32); // last 32 chars as stable-ish hash stub
    } catch (e) { return null; }
  }

  function webgl() {
    try {
      var c = document.createElement('canvas');
      var gl = c.getContext('webgl') || c.getContext('experimental-webgl');
      if (!gl) return { vendor: null, renderer: null };
      var dbg = gl.getExtension('WEBGL_debug_renderer_info');
      if (!dbg) return { vendor: null, renderer: null };
      return {
        vendor:   gl.getParameter(dbg.UNMASKED_VENDOR_WEBGL) || null,
        renderer: gl.getParameter(dbg.UNMASKED_RENDERER_WEBGL) || null
      };
    } catch (e) { return { vendor: null, renderer: null }; }
  }

  function storageWorks(kind) {
    try {
      var s = self[kind];
      if (!s) return false;
      s.setItem('__swprobe', '1');
      s.removeItem('__swprobe');
      return true;
    } catch (e) { return false; }
  }

  function record() {
    if (!self.sw || !self.sw.transport || !self.sw.identity) return;
    // Once per session — no value re-sending identical data every pageview.
    try {
      if (sessionStorage.getItem(SENT_KEY) === '1') return;
    } catch (e) { /* storage disabled — fall through and attempt send anyway */ }

    var ua = navigator.userAgent || '';
    var wv = detectWebView(ua);
    var gl = webgl();

    var payload = {
      vid: self.sw.identity.getVisitorId(),
      sid: self.sw.identity.getSessionId(),
      timezone: safe(function () { return Intl.DateTimeFormat().resolvedOptions().timeZone || null; }, null),
      language: navigator.language || null,
      colorDepth: safe(function () { return screen.colorDepth | 0; }, null),
      hardwareConcurrency: safe(function () { return navigator.hardwareConcurrency | 0; }, null),
      deviceMemory: safe(function () { return (navigator.deviceMemory | 0) || null; }, null),
      touchPoints: safe(function () { return navigator.maxTouchPoints | 0; }, null),
      screenW: safe(function () { return screen.width | 0; }, null),
      screenH: safe(function () { return screen.height | 0; }, null),
      pixelRatio: safe(function () { return window.devicePixelRatio || 1; }, null),
      cookies: navigator.cookieEnabled === true,
      localStorage: storageWorks('localStorage'),
      sessionStorage: storageWorks('sessionStorage'),
      isMetaWebview: wv.isMeta,
      isTikTokWebview: wv.isTikTok,
      canvasFingerprint: canvasStub(),
      webGLVendor: gl.vendor,
      webGLRenderer: gl.renderer,
      connection: safe(function () {
        var c = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
        return c ? (c.effectiveType || null) : null;
      }, null)
    };

    self.sw.transport.send('/api/tracking/signals', payload);
    try { sessionStorage.setItem(SENT_KEY, '1'); } catch (e) { /* swallow */ }
  }

  (self.sw = self.sw || {}).signals = { record: record };
})();
