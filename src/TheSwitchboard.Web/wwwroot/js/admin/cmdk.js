/*
 * Admin Cmd-K palette. Cmd+K / Ctrl+K opens a floating dialog with fuzzy
 * search over every admin route. Arrow keys navigate, Enter opens.
 */
(function () {
  'use strict';

  const ROUTES = [
    ['Dashboard',               '/Admin/Dashboard'],
    ['Real-time',               '/Admin/Reports/RealTime'],
    ['Health',                  '/Admin/Reports/Health'],
    ['Overview',                '/Admin/Reports/Overview'],
    ['Trends',                  '/Admin/Reports/Trends'],
    ['Visitors',                '/Admin/Reports/Visitors'],
    ['Cohorts',                 '/Admin/Reports/Cohorts'],
    ['Engagement',              '/Admin/Reports/Engagement'],
    ['Attribution',             '/Admin/Reports/Attribution'],
    ['Sessions',                '/Admin/Reports/Sessions'],
    ['Frustration',             '/Admin/Reports/Frustration'],
    ['Click heatmap',           '/Admin/Reports/Heatmaps/Click'],
    ['Scroll heatmap',          '/Admin/Reports/Heatmaps/Scroll'],
    ['Performance',             '/Admin/Reports/Performance'],
    ['JS errors',               '/Admin/Reports/Errors'],
    ['Error impact',            '/Admin/Reports/ErrorImpact'],
    ['Form funnel',             '/Admin/Reports/Forms/Funnel'],
    ['Abandonment',             '/Admin/Reports/Abandonment'],
    ['Goals',                   '/Admin/Reports/Goals'],
    ['Insights',                '/Admin/Reports/Insights'],
    ['Alerts',                  '/Admin/Reports/Alerts'],
    ['Segments',                '/Admin/Reports/Segments'],
    ['Compliance · TCPA',       '/Admin/Reports/Compliance'],
    ['Certificates',            '/Admin/Reports/Certificates'],
    ['Disclosures',             '/Admin/Reports/Disclosures'],
    ['Submissions',             '/Admin/Submissions'],
    ['Exports',                 '/Admin/Reports/Exports'],
    ['Changes log',             '/Admin/Reports/ChangesLog'],
    ['Deploys',                 '/Admin/Reports/Deploys'],
    ['DSR · erase data',        '/Admin/Reports/DSR'],
    ['Partners & Logos',        '/Admin/Partners'],
    ['Site Settings',           '/Admin/Settings']
  ];

  function fuzzy(q, s) {
    if (!q) return 0;
    q = q.toLowerCase(); s = s.toLowerCase();
    let si = 0, score = 0, run = 0;
    for (let i = 0; i < q.length; i++) {
      const idx = s.indexOf(q[i], si);
      if (idx < 0) return -1;
      score += (idx === si ? 2 : 1);
      run = idx === si ? run + 1 : 0;
      score += run;
      si = idx + 1;
    }
    return score;
  }

  let root, input, list, open = false, index = 0, results = [];

  function ensureDom() {
    if (root) return;
    root = document.createElement('div');
    root.id = 'sw-cmdk';
    root.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,0.65);backdrop-filter:blur(6px);display:none;align-items:flex-start;justify-content:center;padding-top:12vh;z-index:99999;';
    root.innerHTML = `
      <div style="width:min(560px,90vw);background:#111827;border:1px solid rgba(255,255,255,0.08);border-radius:12px;overflow:hidden;box-shadow:0 20px 50px rgba(0,0,0,0.6);">
        <input id="sw-cmdk-input" type="text" placeholder="Jump to... (↑↓ navigate, Enter open, Esc close)"
          style="width:100%;padding:14px 18px;background:#0a0a0a;border:0;border-bottom:1px solid rgba(255,255,255,0.06);color:#fff;font-size:15px;outline:none;box-sizing:border-box;" />
        <div id="sw-cmdk-list" style="max-height:50vh;overflow-y:auto;padding:6px 0;"></div>
      </div>`;
    document.body.appendChild(root);
    input = root.querySelector('#sw-cmdk-input');
    list = root.querySelector('#sw-cmdk-list');
    input.addEventListener('input', render);
    input.addEventListener('keydown', onKey);
    root.addEventListener('click', function (e) { if (e.target === root) close(); });
  }

  function render() {
    const q = input.value.trim();
    results = ROUTES
      .map(([name, href]) => ({ name, href, score: q ? fuzzy(q, name) : 0 }))
      .filter(r => r.score >= 0)
      .sort((a, b) => b.score - a.score);
    if (!q) results = ROUTES.map(([name, href]) => ({ name, href, score: 0 }));
    index = 0;
    list.innerHTML = results.map((r, i) =>
      `<button data-idx="${i}" data-href="${r.href}" style="display:block;width:100%;text-align:left;padding:9px 18px;background:${i===0?'rgba(59,130,246,0.18)':'transparent'};border:0;color:${i===0?'#fff':'#cbd5e1'};font-size:14px;cursor:pointer;font-family:inherit;">
         <span>${r.name}</span>
         <span style="float:right;color:#64748b;font-family:ui-monospace,monospace;font-size:11px;">${r.href}</span>
       </button>`
    ).join('');
    Array.from(list.querySelectorAll('button')).forEach(b => {
      b.addEventListener('mouseenter', function () { index = +b.dataset.idx; paint(); });
      b.addEventListener('click', function () { location.href = b.dataset.href; });
    });
  }

  function paint() {
    Array.from(list.querySelectorAll('button')).forEach((b, i) => {
      b.style.background = i === index ? 'rgba(59,130,246,0.18)' : 'transparent';
      b.style.color = i === index ? '#fff' : '#cbd5e1';
    });
  }

  function onKey(e) {
    if (e.key === 'Escape') { e.preventDefault(); close(); return; }
    if (e.key === 'ArrowDown') { e.preventDefault(); index = Math.min(results.length - 1, index + 1); paint(); return; }
    if (e.key === 'ArrowUp')   { e.preventDefault(); index = Math.max(0, index - 1); paint(); return; }
    if (e.key === 'Enter') {
      e.preventDefault();
      if (results[index]) location.href = results[index].href;
    }
  }

  function openPalette() {
    ensureDom();
    open = true;
    root.style.display = 'flex';
    input.value = '';
    render();
    setTimeout(() => input.focus(), 0);
  }
  function close() {
    open = false;
    if (root) root.style.display = 'none';
  }

  window.addEventListener('keydown', function (e) {
    if ((e.metaKey || e.ctrlKey) && (e.key === 'k' || e.key === 'K')) {
      e.preventDefault();
      if (open) close(); else openPalette();
    }
  });
})();
