/*
 * The Switchboard tracker — attribution.js (T-2)
 *
 * Parses UTM + click-id parameters from the first URL we see in a session and
 * persists them to sessionStorage.swAttr so every subsequent pageview in the
 * same tab inherits the attribution. First-touch wins — a later internal click
 * that lacks utm_* does NOT overwrite the original source.
 *
 * Params captured (match the server PageView columns exactly):
 *   utm_source / utm_medium / utm_campaign / utm_term / utm_content
 *   gclid (Google Ads)
 *   fbclid (Meta)
 *   msclkid (Microsoft Ads)
 *
 * Exports on window.sw.attribution:
 *   current() → { utmSource, utmMedium, utmCampaign, utmTerm, utmContent, gclid, fbclid, msclkid }
 */
(function () {
  'use strict';

  var STORAGE_KEY = 'swAttr';

  function readQuery() {
    try {
      var params = new URLSearchParams(location.search);
      return {
        utmSource:   params.get('utm_source')   || null,
        utmMedium:   params.get('utm_medium')   || null,
        utmCampaign: params.get('utm_campaign') || null,
        utmTerm:     params.get('utm_term')     || null,
        utmContent:  params.get('utm_content')  || null,
        gclid:       params.get('gclid')        || null,
        fbclid:      params.get('fbclid')       || null,
        msclkid:     params.get('msclkid')      || null
      };
    } catch (e) {
      return {};
    }
  }

  function anyTruthy(obj) {
    for (var k in obj) if (obj[k]) return true;
    return false;
  }

  function readStored() {
    try {
      var raw = sessionStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      return JSON.parse(raw);
    } catch (e) { return null; }
  }

  function writeStored(attr) {
    try { sessionStorage.setItem(STORAGE_KEY, JSON.stringify(attr)); }
    catch (e) { /* storage full or disabled — live without persistence */ }
  }

  function current() {
    // First-touch model: if we already have a persisted attribution for the
    // session, return it even if the current URL is internal with no params.
    var stored = readStored();
    if (stored && anyTruthy(stored)) return stored;

    // First load of this session carrying any campaign params — persist + return.
    var fromQuery = readQuery();
    if (anyTruthy(fromQuery)) {
      writeStored(fromQuery);
      return fromQuery;
    }

    // Truly direct / organic — return an empty shape so pageview.js can just
    // spread it onto the payload.
    return {
      utmSource: null, utmMedium: null, utmCampaign: null,
      utmTerm: null,  utmContent: null,
      gclid: null,    fbclid: null,  msclkid: null
    };
  }

  (self.sw = self.sw || {}).attribution = { current: current };
})();
