/*
 * The Switchboard tracker — identity.js (T-1)
 * Manages the sw_vid (1yr) + sw_sid (30min sliding) cookies. Triple-writes to
 * localStorage + sessionStorage so Safari ITP / Meta in-app browsers don't silently
 * wipe the visitor ID on reload.
 *
 * Exports (attached to window.sw by tracker.js):
 *   getVisitorId() → string
 *   getSessionId() → string
 *   refreshSession()
 */
(function () {
  'use strict';

  var VID_KEY = 'sw_vid';
  var SID_KEY = 'sw_sid';
  var VID_MAX_AGE = 60 * 60 * 24 * 365; // 1 year
  var SID_MAX_AGE = 60 * 30;            // 30 minutes sliding

  function randomId(len) {
    var bytes = new Uint8Array(len);
    (self.crypto || self.msCrypto).getRandomValues(bytes);
    var out = '';
    for (var i = 0; i < bytes.length; i++) {
      // base36-ish: 0-9 + a-z, url-safe
      out += (bytes[i] % 36).toString(36);
    }
    return out;
  }

  function readCookie(name) {
    var match = document.cookie.match(new RegExp('(^|; )' + name + '=([^;]+)'));
    return match ? decodeURIComponent(match[2]) : null;
  }

  function writeCookie(name, value, maxAge) {
    var parts = [name + '=' + encodeURIComponent(value),
                 'Max-Age=' + maxAge,
                 'Path=/',
                 'SameSite=Lax'];
    if (location.protocol === 'https:') parts.push('Secure');
    document.cookie = parts.join('; ');
  }

  function readTripleWrite(key) {
    try {
      return readCookie(key) || localStorage.getItem(key) || sessionStorage.getItem(key);
    } catch (e) {
      // Private mode can throw on localStorage — fall back to cookie only.
      return readCookie(key);
    }
  }

  function writeTripleWrite(key, value, maxAge) {
    writeCookie(key, value, maxAge);
    try { localStorage.setItem(key, value); } catch (e) { /* private mode */ }
    try { sessionStorage.setItem(key, value); } catch (e) { /* private mode */ }
  }

  function ensureVisitorId() {
    var vid = readTripleWrite(VID_KEY);
    if (!vid) {
      vid = randomId(24);
      writeTripleWrite(VID_KEY, vid, VID_MAX_AGE);
    } else {
      // Refresh the cookie expiry on every visit.
      writeCookie(VID_KEY, vid, VID_MAX_AGE);
    }
    return vid;
  }

  function ensureSessionId() {
    var sid = readTripleWrite(SID_KEY);
    if (!sid) {
      sid = randomId(20);
    }
    // Sliding: always refresh expiry on every touch.
    writeTripleWrite(SID_KEY, sid, SID_MAX_AGE);
    return sid;
  }

  function refreshSession() {
    var sid = readTripleWrite(SID_KEY);
    if (sid) writeTripleWrite(SID_KEY, sid, SID_MAX_AGE);
    return sid;
  }

  // H-8: sw.setUser({email, role}) — hashes email client-side (never leaves
  // the browser in plaintext) and posts to /api/tracking/identify so the
  // current Session row gets IdentifiedEmailHash + IdentifiedRole.
  async function sha256Hex(text) {
    try {
      if (!text) return null;
      var enc = new TextEncoder().encode(String(text).trim().toLowerCase());
      var buf = await crypto.subtle.digest('SHA-256', enc);
      var bytes = new Uint8Array(buf);
      var hex = '';
      for (var i = 0; i < bytes.length; i++) hex += bytes[i].toString(16).padStart(2, '0');
      return hex;
    } catch (e) { return null; }
  }

  async function setUser(opts) {
    try {
      var email = opts && opts.email ? String(opts.email) : null;
      var role  = opts && opts.role  ? String(opts.role).slice(0, 64) : null;
      var hash  = email ? await sha256Hex(email) : null;
      var body  = {
        sid: ensureSessionId(),
        vid: ensureVisitorId(),
        emailHashHex: hash,
        role: role
      };
      if (self.sw && self.sw.transport && self.sw.transport.send) {
        await self.sw.transport.send('/api/tracking/identify', body);
      }
    } catch (e) { /* swallow */ }
  }

  var api = {
    getVisitorId: ensureVisitorId,
    getSessionId: ensureSessionId,
    refreshSession: refreshSession,
    setUser: setUser
  };

  (self.sw = self.sw || {}).identity = api;
  // Public convenience — lets page code call sw.setUser(...) without going
  // through sw.identity.
  (self.sw).setUser = setUser;
})();
