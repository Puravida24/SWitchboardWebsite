/*
 * The Switchboard admin — realtime.js (T-8)
 *
 * Opens a SignalR connection to /hubs/realtime (admin-only hub). Every
 * pageview/click/form-event/ping from the site fans out here as an "activity"
 * message. We render a rolling event tape + update the live visitor counter.
 *
 * Admin-only bundle — never loaded on public pages.
 */
(function () {
  'use strict';

  if (typeof signalR === 'undefined') return;

  var tape = document.getElementById('sw-rt-tape');
  var counter = document.getElementById('sw-rt-visitor-count');
  var pingDot = document.getElementById('sw-rt-ping');
  var MAX_TAPE = 100;

  var connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/realtime')
    .withAutomaticReconnect([0, 2000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  // Live visitor counter — poll a JSON endpoint every 10s (cheap, ~50b response).
  async function refreshCounter() {
    try {
      var r = await fetch('/Admin/Reports/RealTime?handler=Stats', { credentials: 'same-origin' });
      if (!r.ok) return;
      var d = await r.json();
      if (counter) counter.textContent = d.active;
    } catch (e) { /* ignore */ }
  }

  function pulse() {
    if (!pingDot) return;
    pingDot.style.opacity = '1';
    setTimeout(function () { pingDot.style.opacity = '0.2'; }, 400);
  }

  function relTime(ts) {
    var t = new Date(ts).getTime();
    var s = Math.max(0, Math.round((Date.now() - t) / 1000));
    if (s < 60) return s + 's';
    if (s < 3600) return Math.round(s / 60) + 'm';
    return Math.round(s / 3600) + 'h';
  }

  function addRow(evt) {
    if (!tape) return;
    var row = document.createElement('div');
    row.style.cssText = 'display: flex; gap: 0.75rem; padding: 0.375rem 1rem; border-bottom: 1px solid rgba(255,255,255,0.04); font-size: 0.75rem; align-items: baseline;';
    var color = evt.kind === 'pageview' ? '#3b82f6'
              : evt.kind === 'signals'  ? '#64748b'
              : evt.kind === 'ping'     ? '#64748b'
              : '#10b981';
    row.innerHTML =
      '<span style="color: var(--slate-500); font-family: ui-monospace, monospace; min-width: 34px;">' + relTime(evt.ts) + '</span>' +
      '<span style="color: ' + color + '; min-width: 90px; font-weight: 600;">' + evt.kind + '</span>' +
      '<span style="color: var(--slate-300); font-family: ui-monospace, monospace;">' + (evt.path || '∅') + '</span>' +
      '<span style="color: var(--slate-500); margin-left: auto;">' +
        (evt.deviceType || '?') + ' · ' + (evt.browser || '?') +
        (evt.utmSource ? ' · ' + evt.utmSource : '') +
        (evt.isBot ? ' · <span style="color:#f59e0b;">bot</span>' : '') +
      '</span>';
    tape.insertBefore(row, tape.firstChild);
    while (tape.children.length > MAX_TAPE) tape.removeChild(tape.lastChild);
    pulse();
  }

  connection.on('activity', addRow);

  connection.start()
    .then(function () {
      var status = document.getElementById('sw-rt-status');
      if (status) {
        status.textContent = 'live';
        status.style.color = '#10b981';
      }
      refreshCounter();
      setInterval(refreshCounter, 10000);
    })
    .catch(function (err) {
      console.error('realtime connect failed', err);
      var status = document.getElementById('sw-rt-status');
      if (status) { status.textContent = 'disconnected'; status.style.color = '#ef4444'; }
    });
})();
