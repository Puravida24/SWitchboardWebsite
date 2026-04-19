/*
 * The Switchboard tracker — pageview.js (T-2)
 *
 * Posts /api/tracking/pageview on each pageview with:
 *   - vid + sid (from identity.js)
 *   - path + referrer
 *   - UTM + click-ids (from attribution.js — first-touch inherited)
 *   - userAgent (so the server-side parser sees the same string the browser sent)
 *   - viewportW/H (innerWidth/innerHeight)
 *   - ts (client clock, server authoritative)
 *
 * Fires exactly once per page load. SPA navigations would re-invoke sw.pageview.record(),
 * but this site is SSR so one fire per document is correct.
 */
(function () {
  'use strict';

  function record() {
    if (!self.sw || !self.sw.transport) return;

    var attr = (self.sw.attribution && self.sw.attribution.current()) || {};
    var vid = self.sw.identity ? self.sw.identity.getVisitorId() : null;
    var sid = self.sw.identity ? self.sw.identity.getSessionId() : null;

    var payload = {
      vid: vid,
      sid: sid,
      path: location.pathname + (location.search || ''),
      referrer: document.referrer || null,
      userAgent: navigator.userAgent || null,
      utmSource: attr.utmSource || null,
      utmMedium: attr.utmMedium || null,
      utmCampaign: attr.utmCampaign || null,
      utmTerm: attr.utmTerm || null,
      utmContent: attr.utmContent || null,
      gclid: attr.gclid || null,
      fbclid: attr.fbclid || null,
      msclkid: attr.msclkid || null,
      viewportW: (window.innerWidth  | 0) || null,
      viewportH: (window.innerHeight | 0) || null,
      ts: new Date().toISOString(),
      consentState: 'none'
    };

    self.sw.transport.send('/api/tracking/pageview', payload);
  }

  (self.sw = self.sw || {}).pageview = { record: record };
})();
