// H-7b — per-user persistence of admin sidebar group open/closed state.
// The server emits each <details class="sb-group" data-group-id="X"> with the
// `open` attribute set for the group that contains the current page, so the
// admin always sees where they are. On top of that, this script persists any
// manual open/close the user does to localStorage so the preference sticks
// across navigations.
(function () {
  "use strict";
  var KEY_PREFIX = "sw.admin.sb.";

  function load(id) {
    try { return window.localStorage.getItem(KEY_PREFIX + id); }
    catch (_) { return null; }
  }
  function save(id, open) {
    try { window.localStorage.setItem(KEY_PREFIX + id, open ? "1" : "0"); }
    catch (_) { /* private-mode / quota — state degrades to per-nav */ }
  }

  function init() {
    var groups = document.querySelectorAll("details.sb-group[data-group-id]");
    for (var i = 0; i < groups.length; i++) {
      (function (g) {
        var id = g.getAttribute("data-group-id");
        var stored = load(id);

        // If user has a stored preference, honor it — BUT only override the
        // server default if the current page is NOT the active group. That
        // way an active group stays expanded when you land on it.
        if (stored !== null && !g.hasAttribute("data-active")) {
          g.open = (stored === "1");
        }

        g.addEventListener("toggle", function () { save(id, g.open); });
      })(groups[i]);
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
