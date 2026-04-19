/*
 * The Switchboard tracker — forms.js (T-5)
 *
 * Listens on every input/textarea/select carrying the data-tb-field="…" attribute
 * (the field name) inside a form element carrying data-tb-form-id="…" (the form
 * name, e.g. "contact"). Emits events:
 *
 *   focus / blur / input (debounced) / paste / error / submit / abandon
 *
 * Dwell time is the ms between focus and blur. Char count is the current
 * string length. Correction count approximates "how many times did they
 * backspace" (decrements in length since focus). Pasted flag is true if a
 * paste event fired during the focus window.
 *
 * The actual typed VALUE is never persisted — only counters and event kinds.
 * Email/phone/company/message content stays on the client.
 *
 * Abandon detection: on visibilitychange=hidden, any field with interaction
 * (focus fired) that hasn't blurred emits an abandon event via sendBeacon.
 */
(function () {
  'use strict';

  var queue = [];
  var FLUSH_MS = 5000;

  // Per-field state while user is interacting.
  var state = Object.create(null);

  function now() { return Date.now(); }
  function send() {
    if (queue.length === 0) return;
    var batch = queue.splice(0, queue.length);
    if (self.sw && self.sw.transport) self.sw.transport.send('/api/tracking/form-events', { events: batch });
  }
  function beacon() {
    if (queue.length === 0) return;
    var batch = queue.splice(0, queue.length);
    if (self.sw && self.sw.transport) self.sw.transport.beacon('/api/tracking/form-events', { events: batch });
  }

  function identity() {
    return {
      sid: self.sw && self.sw.identity ? self.sw.identity.getSessionId() : null,
      vid: self.sw && self.sw.identity ? self.sw.identity.getVisitorId() : null
    };
  }

  function fieldMeta(el) {
    var form = el.closest ? el.closest('[data-tb-form-id]') : null;
    return {
      formId: form ? form.getAttribute('data-tb-form-id') : 'unknown',
      fieldName: el.getAttribute('data-tb-field') || 'unknown'
    };
  }

  function emit(el, evt, extras) {
    var id = identity();
    var meta = fieldMeta(el);
    var base = {
      sid: id.sid, vid: id.vid,
      path: location.pathname + (location.search || ''),
      formId: meta.formId, fieldName: meta.fieldName,
      event: evt,
      occurredAt: new Date().toISOString()
    };
    for (var k in (extras || {})) base[k] = extras[k];
    queue.push(base);
  }

  function onFocus(e) {
    var el = e.target;
    if (!el || !el.matches || !el.matches('[data-tb-field]')) return;
    var key = fieldMeta(el).fieldName;
    state[key] = {
      focusedAt: now(),
      startLen: (el.value || '').length,
      lastLen: (el.value || '').length,
      corrections: 0,
      pasted: false
    };
    emit(el, 'focus');
  }

  function onBlur(e) {
    var el = e.target;
    if (!el || !el.matches || !el.matches('[data-tb-field]')) return;
    var key = fieldMeta(el).fieldName;
    var s = state[key];
    if (!s) { emit(el, 'blur'); return; }
    var dwellMs = now() - s.focusedAt;
    var charCount = (el.value || '').length;
    emit(el, 'blur', {
      dwellMs: dwellMs,
      charCount: charCount,
      correctionCount: s.corrections,
      pastedFlag: s.pasted
    });
    delete state[key];
  }

  function onInput(e) {
    var el = e.target;
    if (!el || !el.matches || !el.matches('[data-tb-field]')) return;
    var key = fieldMeta(el).fieldName;
    var s = state[key];
    if (!s) return;
    var newLen = (el.value || '').length;
    if (newLen < s.lastLen) s.corrections += 1;
    s.lastLen = newLen;
  }

  function onPaste(e) {
    var el = e.target;
    if (!el || !el.matches || !el.matches('[data-tb-field]')) return;
    var key = fieldMeta(el).fieldName;
    var s = state[key];
    if (s) s.pasted = true;
    emit(el, 'paste');
  }

  function onInvalid(e) {
    var el = e.target;
    if (!el || !el.matches || !el.matches('[data-tb-field]')) return;
    var code = (el.validity && (
      (el.validity.valueMissing && 'required') ||
      (el.validity.typeMismatch && 'type') ||
      (el.validity.patternMismatch && 'pattern') ||
      (el.validity.tooShort && 'short') ||
      (el.validity.tooLong && 'long') ||
      'invalid'
    )) || 'invalid';
    emit(el, 'error', { errorCode: code, errorMessage: el.validationMessage || null });
  }

  function onSubmit(e) {
    var form = e.target;
    if (!form || !form.matches || !form.matches('[data-tb-form-id]')) return;
    var id = identity();
    queue.push({
      sid: id.sid, vid: id.vid,
      path: location.pathname + (location.search || ''),
      formId: form.getAttribute('data-tb-form-id') || 'unknown',
      fieldName: '(form)',
      event: 'submit',
      occurredAt: new Date().toISOString()
    });
    send();
  }

  function abandonInFlight() {
    // Any field still in `state` at unload never blurred cleanly — emit abandon.
    var keys = Object.keys(state);
    if (keys.length === 0) return;
    for (var i = 0; i < keys.length; i++) {
      var key = keys[i];
      var s = state[key];
      queue.push({
        sid: (self.sw && self.sw.identity) ? self.sw.identity.getSessionId() : null,
        vid: (self.sw && self.sw.identity) ? self.sw.identity.getVisitorId() : null,
        path: location.pathname + (location.search || ''),
        formId: 'contact',
        fieldName: key,
        event: 'abandon',
        occurredAt: new Date().toISOString(),
        dwellMs: now() - s.focusedAt,
        charCount: s.lastLen,
        correctionCount: s.corrections,
        pastedFlag: s.pasted
      });
    }
  }

  function boot() {
    try {
      document.addEventListener('focusin',  onFocus,   true);
      document.addEventListener('focusout', onBlur,    true);
      document.addEventListener('input',    onInput,   true);
      document.addEventListener('paste',    onPaste,   true);
      document.addEventListener('invalid',  onInvalid, true);
      document.addEventListener('submit',   onSubmit,  true);

      setInterval(send, FLUSH_MS);

      document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'hidden') { abandonInFlight(); beacon(); }
      });
      window.addEventListener('pagehide', function () { abandonInFlight(); beacon(); });
    } catch (e) { /* swallow — tracker must never break the page */ }
  }

  (self.sw = self.sw || {}).forms = { boot: boot };
})();
