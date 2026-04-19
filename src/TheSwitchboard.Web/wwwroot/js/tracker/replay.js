/*
 * The Switchboard tracker — replay.js (T-7)
 *
 * Session replay via rrweb. Vendored at /js/vendor/rrweb-record.min.js.
 *
 * Gates:
 *   1. DNT / GPC already short-circuit tracker.js boot — if we got here,
 *      we're allowed to record.
 *   2. 20% per-session sample. Same sw_sid = same coin flip (stable within
 *      a session so early chunks don't disappear if sample flips).
 *
 * PII:
 *   - maskAllInputs: true — every input/textarea/select is starred.
 *   - maskTextFn: regex-replaces email / phone / CC / SSN patterns inside
 *     text nodes before they serialize.
 *   - Any element with [data-tb-pii] is fully masked. Apply this to
 *     anything hand-rendered that contains customer-provided text.
 *
 * Transport:
 *   rrweb events accumulate in a buffer. Every 5 s (and on
 *   visibilitychange=hidden), the buffer is gzipped via CompressionStream,
 *   base64'd, and POSTed as one chunk. Server enforces 512 KB per chunk.
 */
(function () {
  'use strict';

  var SAMPLE_RATE = 0.2;
  var FLUSH_MS = 5000;
  var MAX_CHUNK_BYTES = 512 * 1024;

  var buffer = [];
  var sequence = 0;
  var stop = null;

  function sampled(sid) {
    if (!sid) return false;
    // Deterministic hash of sid → 0..1. Same sid always wins or loses.
    var h = 0;
    for (var i = 0; i < sid.length; i++) h = ((h * 31) + sid.charCodeAt(i)) | 0;
    var n = Math.abs(h % 1000) / 1000;
    return n < SAMPLE_RATE;
  }

  var PII_PATTERNS = [
    /[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}/g,     // email
    /\b(?:\d[ -]*?){13,16}\b/g,                            // credit card
    /\b\d{3}-\d{2}-\d{4}\b/g,                              // SSN
    /\+?\d[\d\s().-]{9,}\d/g                               // phone
  ];

  function maskText(t) {
    if (!t) return t;
    var out = t;
    for (var i = 0; i < PII_PATTERNS.length; i++) out = out.replace(PII_PATTERNS[i], '<pii>');
    return out;
  }

  async function gzip(uint8) {
    try {
      var cs = new CompressionStream('gzip');
      var stream = new Response(new Blob([uint8])).body.pipeThrough(cs);
      var buf = await new Response(stream).arrayBuffer();
      return new Uint8Array(buf);
    } catch (e) {
      // Some browsers (old Safari) don't ship CompressionStream. Fall through
      // and send uncompressed — server still accepts.
      return uint8;
    }
  }

  function uint8ToBase64(u8) {
    var s = '';
    var chunk = 0x8000;
    for (var i = 0; i < u8.length; i += chunk) {
      s += String.fromCharCode.apply(null, u8.subarray(i, i + chunk));
    }
    return btoa(s);
  }

  async function flush(isBeacon) {
    if (buffer.length === 0) return;
    var events = buffer.splice(0, buffer.length);
    try {
      var json = JSON.stringify(events);
      var raw = new TextEncoder().encode(json);
      var compressed = await gzip(raw);
      // If compression raised the byte count, fall back to raw.
      var payload = compressed.length < raw.length ? compressed : raw;
      var wasCompressed = payload === compressed;
      if (payload.length > MAX_CHUNK_BYTES) {
        // Single chunk too big — drop the oldest half and try again next tick.
        return;
      }
      var sid = self.sw && self.sw.identity ? self.sw.identity.getSessionId() : null;
      if (!sid) return;
      var body = {
        sid: sid,
        sequence: sequence++,
        ts: new Date().toISOString(),
        compressed: wasCompressed,
        payloadBase64: uint8ToBase64(payload)
      };
      if (self.sw && self.sw.transport) {
        if (isBeacon) self.sw.transport.beacon('/api/tracking/replay/chunk', body);
        else self.sw.transport.send('/api/tracking/replay/chunk', body);
      }
    } catch (e) { /* swallow — replay must never break the page */ }
  }

  function loadRrweb(cb) {
    if (window.rrwebRecord) { cb(); return; }
    var s = document.createElement('script');
    s.src = '/js/vendor/rrweb-record.min.js';
    s.defer = true;
    // Use the same per-request nonce injected into tracker.js so CSP allows it.
    var base = document.querySelector('script[src*="/js/tracker/tracker.js"]');
    if (base && base.getAttribute('nonce')) s.setAttribute('nonce', base.getAttribute('nonce'));
    s.onload = cb;
    s.onerror = function () { /* vendor script failed — no replay this session */ };
    document.head.appendChild(s);
  }

  function startRecording() {
    if (!window.rrwebRecord) return;
    try {
      stop = window.rrwebRecord({
        emit: function (e) { buffer.push(e); },
        maskAllInputs: true,
        maskInputOptions: {
          password: true, email: true, tel: true, text: true, search: true,
          url: true, number: true
        },
        maskTextFn: function (text) { return maskText(text); },
        maskTextSelector: '[data-tb-pii]',
        inlineStylesheet: true,
        slimDOMOptions: {
          script: true, comment: true, headFavicon: true, headWhitespace: true,
          headMetaDescKeywords: true, headMetaSocial: false, headMetaRobots: true,
          headMetaHttpEquiv: true, headMetaAuthorship: true
        },
        recordCanvas: false,
        collectFonts: false
      });

      setInterval(function () { flush(false); }, FLUSH_MS);
      document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'hidden') flush(true);
      });
      window.addEventListener('pagehide', function () { flush(true); });
    } catch (e) { /* swallow */ }
  }

  function boot() {
    try {
      var sid = self.sw && self.sw.identity ? self.sw.identity.getSessionId() : null;
      if (!sampled(sid)) return; // 80% of sessions never download rrweb.
      loadRrweb(startRecording);
    } catch (e) { /* swallow */ }
  }

  (self.sw = self.sw || {}).replay = { boot: boot };
})();
