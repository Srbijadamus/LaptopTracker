/**
 * topscroll.js — synced top + bottom horizontal scrollbars for wide tables.
 *
 * Handles three HTML patterns:
 *   1. .table-scroll > table          (ReturnDevices, Handovers)
 *   2. .ops-table-scroll > table      (Operations)
 *   3. .panel > table  (direct child) (Archive, Search, WicStock, LoanerDevices)
 *      → auto-wrapped in .table-scroll before processing
 *
 * For pattern 3 the wrapper is injected so the existing bottom scroll
 * (which was missing) is also created.  A ResizeObserver keeps the
 * inner-width in sync even for containers that start hidden (Operations).
 */
(function () {
  'use strict';

  var SCROLL_CLS    = 'table-scroll';
  var TOP_BAR_CLS   = 'top-scroll-bar';
  var TOP_INNER_CLS = 'top-scroll-inner';
  var DONE_ATTR     = 'data-ts-init';

  /* ── wrap .panel > table (direct child, no existing scroll wrapper) ── */
  function wrapBareTablePanels() {
    document.querySelectorAll('.panel > table').forEach(function (tbl) {
      var wrap = document.createElement('div');
      wrap.className = SCROLL_CLS;
      tbl.parentNode.insertBefore(wrap, tbl);
      wrap.appendChild(tbl);
    });
  }

  /* ── attach top scrollbar to one scroll-container element ──────────── */
  function attachTopBar(ts) {
    if (ts.getAttribute(DONE_ATTR)) return;
    ts.setAttribute(DONE_ATTR, '1');

    var bar   = document.createElement('div');
    bar.className = TOP_BAR_CLS;
    var inner = document.createElement('div');
    inner.className = TOP_INNER_CLS;
    bar.appendChild(inner);
    ts.parentNode.insertBefore(bar, ts);

    function updateWidth() {
      var w = ts.scrollWidth;
      if (w > 0) inner.style.width = w + 'px';
    }
    updateWidth();

    /* ResizeObserver keeps width current when container becomes visible
       (e.g. Operations results panel starts display:none).             */
    if (typeof ResizeObserver !== 'undefined') {
      new ResizeObserver(updateWidth).observe(ts);
    }
    window.addEventListener('resize', updateWidth);

    /* Two-way scroll sync — guard flag prevents recursion */
    var lock = false;
    bar.addEventListener('scroll', function () {
      if (lock) return;
      lock = true;
      ts.scrollLeft = bar.scrollLeft;
      lock = false;
    });
    ts.addEventListener('scroll', function () {
      if (lock) return;
      lock = true;
      bar.scrollLeft = ts.scrollLeft;
      lock = false;
    });
  }

  /* ── process all scroll containers on the page ─────────────────────── */
  function init() {
    wrapBareTablePanels();
    document.querySelectorAll('.' + SCROLL_CLS + ', .ops-table-scroll')
      .forEach(attachTopBar);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  /* Expose for pages that inject content dynamically */
  window.initTopScrollbars = init;
}());
