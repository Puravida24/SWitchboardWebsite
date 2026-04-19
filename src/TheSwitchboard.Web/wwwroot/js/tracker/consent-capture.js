/*
 * The Switchboard tracker — consent-capture.js (T-7B)
 *
 * On submit of any [data-tb-form-id] form, snapshots the consent disclosure
 * (the element carrying [data-tb-consent-disclosure]) — its exact textContent,
 * computed font size / color / background / visibility, WCAG contrast, and
 * click coords + behavioral signals accumulated during the session.
 *
 * Email + phone values are SHA-256 hashed client-side via crypto.subtle so
 * the server never sees the raw PII on the consent path.
 *
 * The returned certificateId is stamped onto a hidden input so the form
 * submission links back to the proof record.
 */
(function () {
  'use strict';

  var pageLoadedAt = new Date();
  var keystrokes = 0;
  var fieldsInteracted = new Set();
  var mouseDistance = 0;
  var lastMouse = null;
  var maxScrollPct = 0;

  function installAccumulators() {
    document.addEventListener('keydown', function () { keystrokes++; });
    document.addEventListener('focusin', function (e) {
      if (e.target && e.target.getAttribute && e.target.getAttribute('data-tb-field')) {
        fieldsInteracted.add(e.target.getAttribute('data-tb-field'));
      }
    }, true);
    document.addEventListener('mousemove', function (e) {
      if (lastMouse) {
        var dx = e.pageX - lastMouse.x;
        var dy = e.pageY - lastMouse.y;
        mouseDistance += Math.sqrt(dx * dx + dy * dy) | 0;
      }
      lastMouse = { x: e.pageX, y: e.pageY };
    }, { passive: true });
    document.addEventListener('scroll', function () {
      var vh = window.innerHeight || 0;
      var dh = document.documentElement.scrollHeight || 1;
      var pct = Math.min(100, Math.round(((window.scrollY || 0) + vh) / dh * 100));
      if (pct > maxScrollPct) maxScrollPct = pct;
    }, { passive: true });
  }

  // Parse CSS color (hex / rgb / rgba) into {r,g,b} sRGB 0..255.
  function parseColor(css) {
    if (!css) return null;
    css = css.trim();
    if (css[0] === '#') {
      var h = css.slice(1);
      if (h.length === 3) h = h.split('').map(function (c) { return c + c; }).join('');
      return { r: parseInt(h.slice(0, 2), 16), g: parseInt(h.slice(2, 4), 16), b: parseInt(h.slice(4, 6), 16) };
    }
    var m = css.match(/rgba?\s*\(\s*(\d+)[,\s]+(\d+)[,\s]+(\d+)/i);
    if (m) return { r: +m[1], g: +m[2], b: +m[3] };
    return null;
  }

  // WCAG 2.1 relative luminance + contrast ratio.
  function relLum(c) {
    function ch(v) { v = v / 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); }
    return 0.2126 * ch(c.r) + 0.7152 * ch(c.g) + 0.0722 * ch(c.b);
  }
  function contrast(fg, bg) {
    if (!fg || !bg) return null;
    var L1 = relLum(fg), L2 = relLum(bg);
    var a = Math.max(L1, L2), b = Math.min(L1, L2);
    return (a + 0.05) / (b + 0.05);
  }

  function visibleInViewport(el) {
    if (!el || !el.getBoundingClientRect) return false;
    var r = el.getBoundingClientRect();
    var vh = window.innerHeight || document.documentElement.clientHeight;
    var vw = window.innerWidth || document.documentElement.clientWidth;
    return r.bottom >= 0 && r.top <= vh && r.right >= 0 && r.left <= vw;
  }

  async function sha256Hex(text) {
    try {
      if (!text) return null;
      var enc = new TextEncoder().encode(text.trim().toLowerCase());
      var buf = await crypto.subtle.digest('SHA-256', enc);
      var bytes = new Uint8Array(buf);
      var hex = '';
      for (var i = 0; i < bytes.length; i++) hex += bytes[i].toString(16).padStart(2, '0');
      return hex;
    } catch (e) { return null; }
  }

  function findDisclosure(form) {
    // Prefer scoped to the form, fall back to page-global.
    return (form.querySelector && form.querySelector('[data-tb-consent-disclosure]'))
        || document.querySelector('[data-tb-consent-disclosure]');
  }

  function snapshotDisclosure(el) {
    if (!el) return null;
    var styles = getComputedStyle(el);
    var fg = parseColor(styles.color);
    var bg = parseColor(styles.backgroundColor);
    // Walk up until we find a non-transparent background — modern pages
    // stack transparent panels on colored body.
    var bgEl = el;
    while (bg && (bg.r | bg.g | bg.b) === 0 && styles.backgroundColor === 'rgba(0, 0, 0, 0)') {
      bgEl = bgEl.parentElement;
      if (!bgEl) break;
      styles = getComputedStyle(bgEl);
      bg = parseColor(styles.backgroundColor);
    }
    var finalStyles = getComputedStyle(el);
    return {
      text: (el.textContent || '').replace(/\s+/g, ' ').trim(),
      fontSize: finalStyles.fontSize,
      color: finalStyles.color,
      backgroundColor: bg ? `rgb(${bg.r}, ${bg.g}, ${bg.b})` : finalStyles.backgroundColor,
      contrast: contrast(fg, bg),
      visible: visibleInViewport(el)
    };
  }

  async function captureAndStamp(form, event) {
    try {
      var disclosureEl = findDisclosure(form);
      var snapshot = snapshotDisclosure(disclosureEl);
      if (!snapshot || !snapshot.text) return; // nothing to sign

      var email = (form.querySelector('[data-tb-field="email"]') || {}).value || null;
      var phone = (form.querySelector('[data-tb-field="phone"]') || {}).value || null;
      var emailHash = email ? await sha256Hex(email) : null;
      var phoneHash = phone ? await sha256Hex(phone) : null;

      var payload = {
        sid: (self.sw && self.sw.identity) ? self.sw.identity.getSessionId() : null,
        vid: (self.sw && self.sw.identity) ? self.sw.identity.getVisitorId() : null,
        consentTimestamp: new Date().toISOString(),
        consentMethod: event && event.submitter ? 'submit-button' : 'form-submit',
        consentElementSelector: event && event.submitter
          ? (event.submitter.id ? '#' + event.submitter.id : event.submitter.tagName.toLowerCase())
          : 'form',
        clickX: event && event.submitter ? (event.submitter.getBoundingClientRect().left + window.scrollX) | 0 : null,
        clickY: event && event.submitter ? (event.submitter.getBoundingClientRect().top  + window.scrollY) | 0 : null,
        pageLoadedAt: pageLoadedAt.toISOString(),
        timeOnPageSeconds: Math.round((Date.now() - pageLoadedAt.getTime()) / 1000),
        disclosureText: snapshot.text,
        disclosureFontSize: snapshot.fontSize,
        disclosureColor: snapshot.color,
        disclosureBackgroundColor: snapshot.backgroundColor,
        disclosureContrastRatio: snapshot.contrast,
        disclosureIsVisible: snapshot.visible,
        userAgent: navigator.userAgent,
        browserName: (navigator.userAgentData && navigator.userAgentData.brands
          && navigator.userAgentData.brands[0] && navigator.userAgentData.brands[0].brand) || null,
        osName: (navigator.userAgentData && navigator.userAgentData.platform) || null,
        screenResolution: (screen.width | 0) + 'x' + (screen.height | 0),
        viewportW: window.innerWidth | 0,
        viewportH: window.innerHeight | 0,
        pageUrl: location.href,
        keystrokesPerMinute: Math.round(keystrokes / Math.max(1, (Date.now() - pageLoadedAt.getTime()) / 60000)),
        formFieldsInteracted: fieldsInteracted.size,
        mouseDistancePx: mouseDistance,
        scrollDepthPercent: maxScrollPct,
        emailHashHex: emailHash,
        phoneHashHex: phoneHash
      };

      var resp = await fetch('/api/tracking/consent', {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
        keepalive: true
      });
      if (!resp.ok) return;
      var body = await resp.json();
      if (!body || !body.certificateId) return;

      // Stamp hidden input + data attr on the form for any downstream handler.
      var hidden = form.querySelector('input[name="ConsentCertificateId"]');
      if (hidden) hidden.value = body.certificateId;
      form.setAttribute('data-tb-cert-id', body.certificateId);
    } catch (e) { /* never block form submission */ }
  }

  function boot() {
    try {
      installAccumulators();
      // Intercept submits in capture phase so we can fire the consent POST
      // before the form's own submit handler runs. We do NOT preventDefault
      // — the form posts normally; the cert id just rides along.
      document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form || !form.matches || !form.matches('[data-tb-form-id]')) return;
        // Fire-and-forget — don't await on the submit path to avoid blocking.
        captureAndStamp(form, e);
      }, true);
    } catch (e) { /* swallow */ }
  }

  (self.sw = self.sw || {}).consent = { boot: boot };
})();
