(function () {
  'use strict';

  // ── Constants ─────────────────────────────────────────────────────────────
  var SOURCE_ACTIONS = {
    ReturnDevices: ['status-update', 'location-update', 'return-mark'],
    LoanerDevices: ['status-update', 'location-update', 'assignment-update', 'return-mark'],
    WicStock:      ['status-update', 'location-update'],
  };

  var STATUS_OPTIONS = {
    ReturnDevices: ['PendingPickup', 'PickedUp'],
    LoanerDevices: ['Available', 'NotAvailable'],
    WicStock:      ['Available', 'NotAvailable'],
  };

  var ACTION_LABELS = {
    'status-update':     window.i18n.actionStatusUpdate,
    'location-update':   window.i18n.actionLocationUpdate,
    'assignment-update': window.i18n.actionAssignmentUpdate,
    'return-mark':       window.i18n.actionReturnMark,
  };

  var SOURCE_LABELS = {
    ReturnDevices: 'Return',
    LoanerDevices: 'Loaner',
    WicStock:      'WIC Stock',
  };

  // Maps proposedChanges keys (PascalCase from C# dict) to normalized device keys
  var PROPOSED_KEY_MAP = {
    Status:         'status',
    DeviceLocation: 'deviceLocation',
    WorkOrder:      'workOrder',
    PickupStatus:   'pickupStatus',
    Location:       'location',
    KidHandedTo:    'kidHandedTo',
    KID:            'kid',
    DateHandedOver: 'dateHandedOver',
    SwapRITM:       'swapRitm',
  };

  // ── State ─────────────────────────────────────────────────────────────────
  // global: idle | searching | results_ready | no_results | error
  // drawer: closed | open_details | open_step1 | preparing | prepared_ok |
  //         prepared_blocked | prepared_not_supported | prepared_error |
  //         confirming | confirmed_ok | confirmed_error
  var g = {
    global:      'idle',
    drawer:      'closed',
    results:     [],
    selected:    null,
    drawerMode:  null,
    requestId:   null,
    prepareData: null,
  };

  // ── Normalizers ───────────────────────────────────────────────────────────
  // Handle both camelCase (GET/prepare via Ok()) and PascalCase (call via Content())

  function rk(obj, camel, pascal) {
    return obj[camel] !== undefined ? obj[camel] : (obj[pascal] !== undefined ? obj[pascal] : null);
  }

  function normalizeDevice(d) {
    if (!d) return null;
    return {
      source:             rk(d, 'source',           'Source')           || '',
      id:                 rk(d, 'id',               'Id')               || 0,
      serialNumber:       rk(d, 'serialNumber',     'SerialNumber')     || '',
      deviceType:         rk(d, 'deviceType',       'DeviceType')       || '',
      deviceStateType:    rk(d, 'deviceStateType',  'DeviceStateType')  || '',
      ritm:               rk(d, 'rITM',             'RITM')             || '',
      date:               rk(d, 'date',             'Date'),
      deviceLocation:     rk(d, 'deviceLocation',   'DeviceLocation')   || '',
      status:             rk(d, 'status',           'Status')           || '',
      isDeleted:          rk(d, 'isDeleted',        'IsDeleted')        || false,
      workOrder:          rk(d, 'workOrder',        'WorkOrder'),
      pickupStatus:       rk(d, 'pickupStatus',     'PickupStatus'),
      location:           rk(d, 'location',         'Location'),
      chargerReturned:    rk(d, 'chargerReturned',  'ChargerReturned'),
      powerCableReturned: rk(d, 'powerCableReturned', 'PowerCableReturned'),
      kidHandedTo:        rk(d, 'kidHandedTo',      'KidHandedTo'),
      dateHandedOver:     rk(d, 'dateHandedOver',   'DateHandedOver'),
      wic:                rk(d, 'wIC',              'WIC'),
      kid:                rk(d, 'kID',              'KID'),
      swapRitm:           rk(d, 'swapRITM',         'SwapRITM'),
    };
  }

  function normalizePrepare(d) {
    if (!d) return null;
    return {
      action:          rk(d, 'action',          'Action')          || '',
      currentState:    normalizeDevice(rk(d, 'currentState', 'CurrentState')),
      proposedChanges: rk(d, 'proposedChanges', 'ProposedChanges') || {},
      warning:         rk(d, 'warning',         'Warning'),
    };
  }

  function normalizeEnvelope(json) {
    var data = json.data;
    var err  = json.error || {};
    var r = {
      ok:      json.ok === true,
      code:    err.code    || null,
      message: err.message || null,
      items:   [],
      count:   0,
      device:  null,
      prepare: null,
    };
    if (!data) return r;
    if (Array.isArray(data)) {
      r.items = data.map(normalizeDevice);
      r.count = r.items.length;
    } else if (rk(data, 'action', 'Action') !== null) {
      r.prepare = normalizePrepare(data);
    } else {
      r.device = normalizeDevice(data);
    }
    return r;
  }

  // ── Fetch helpers ─────────────────────────────────────────────────────────

  function apiFetch(url) {
    return fetch(url)
      .then(function (r) { return r.json(); })
      .then(normalizeEnvelope);
  }

  function apiPost(url, body, idempKey) {
    var headers = { 'Content-Type': 'application/json' };
    if (idempKey) headers['Idempotency-Key'] = idempKey;
    return fetch(url, { method: 'POST', headers: headers, body: JSON.stringify(body) })
      .then(function (r) { return r.json(); })
      .then(normalizeEnvelope);
  }

  // ── UUID ──────────────────────────────────────────────────────────────────

  function generateUuid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
      var r = Math.random() * 16 | 0;
      return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
  }

  // ── DOM cache ─────────────────────────────────────────────────────────────

  var el = {};

  function cacheEls() {
    el.identifierInput        = document.getElementById('identifierInput');
    el.sourceSelect           = document.getElementById('sourceSelect');
    el.btnFind                = document.getElementById('btnFind');
    el.btnAvailable           = document.getElementById('btnAvailable');
    el.btnReset               = document.getElementById('btnReset');
    el.searchStatus           = document.getElementById('searchStatus');
    el.resultsPanel           = document.getElementById('resultsPanel');
    el.resultsBody            = document.getElementById('resultsBody');
    el.drawerOverlay          = document.getElementById('drawerOverlay');
    el.drawer                 = document.getElementById('drawer');
    el.drawerSteps            = document.getElementById('drawerSteps');
    el.step1Label             = document.getElementById('step1Label');
    el.step2Label             = document.getElementById('step2Label');
    el.dSource                = document.getElementById('dSource');
    el.dId                    = document.getElementById('dId');
    el.dSerial                = document.getElementById('dSerial');
    el.dType                  = document.getElementById('dType');
    el.dStatus                = document.getElementById('dStatus');
    el.dDeviceLocation        = document.getElementById('dDeviceLocation');
    el.actionForm             = document.getElementById('actionForm');
    el.actionSelect           = document.getElementById('actionSelect');
    el.fieldsStatusUpdate     = document.getElementById('fieldsStatusUpdate');
    el.fieldsLocationUpdate   = document.getElementById('fieldsLocationUpdate');
    el.fieldsAssignmentUpdate = document.getElementById('fieldsAssignmentUpdate');
    el.newStatus              = document.getElementById('newStatus');
    el.newLocation            = document.getElementById('newLocation');
    el.assignKidHandedTo      = document.getElementById('assignKidHandedTo');
    el.assignKid              = document.getElementById('assignKid');
    el.assignDateHandedOver   = document.getElementById('assignDateHandedOver');
    el.proposedSection        = document.getElementById('proposedSection');
    el.proposedContent        = document.getElementById('proposedContent');
    el.validationArea         = document.getElementById('validationArea');
    el.btnCancel              = document.getElementById('btnCancel');
    el.btnPrepare             = document.getElementById('btnPrepare');
    el.btnConfirm             = document.getElementById('btnConfirm');
  }

  // ── HTML escaping ─────────────────────────────────────────────────────────

  function esc(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  }

  function srcClass(source) {
    var map = { ReturnDevices: 'return', LoanerDevices: 'loaner', WicStock: 'wic' };
    return map[source] || 'unknown';
  }

  // ── Search status area ────────────────────────────────────────────────────

  function setStatus(text, cls) {
    el.searchStatus.textContent = text;
    el.searchStatus.className   = 'ops-status-area' + (cls ? ' ops-status-' + cls : '');
  }

  function setSearchBusy(busy) {
    el.btnFind.disabled      = busy;
    el.btnAvailable.disabled = busy;
    el.btnReset.disabled     = busy;
  }

  // ── Table rendering ───────────────────────────────────────────────────────

  function renderTable(items) {
    if (!items || !items.length) { el.resultsPanel.style.display = 'none'; return; }
    el.resultsBody.innerHTML = '';
    items.forEach(function (d) {
      var tr = document.createElement('tr');
      tr.innerHTML =
        '<td><span class="ops-badge ops-src-' + srcClass(d.source) + '">' +
          esc(SOURCE_LABELS[d.source] || d.source) + '</span></td>' +
        '<td class="ops-muted">' + esc(d.id) + '</td>' +
        '<td>' + esc(d.serialNumber) + '</td>' +
        '<td class="ops-muted">' + esc(d.deviceType) + '</td>' +
        '<td><span class="ops-badge ops-status-badge">' + esc(d.status) + '</span></td>' +
        '<td class="ops-muted">' + esc(d.deviceLocation) + '</td>' +
        '<td class="ops-row-actions">' +
          '<button class="ops-btn-row" data-role="details">' + window.i18n.btnDetails + '</button>' +
          '<button class="ops-btn-row ops-btn-row-primary" data-role="prepare">' + window.i18n.btnPrepareChange + '</button>' +
        '</td>';
      (function (device) {
        tr.querySelector('[data-role="details"]').addEventListener('click', function () { openDetails(device); });
        tr.querySelector('[data-role="prepare"]').addEventListener('click', function () { openAction(device); });
      }(d));
      el.resultsBody.appendChild(tr);
    });
    el.resultsPanel.style.display = '';
  }

  // ── Drawer open/close ─────────────────────────────────────────────────────

  function openDrawerPanel() {
    el.drawer.classList.add('ops-drawer-open');
    el.drawerOverlay.classList.add('ops-overlay-visible');
  }

  function closeDrawer() {
    el.drawer.classList.remove('ops-drawer-open');
    el.drawerOverlay.classList.remove('ops-overlay-visible');
    g.drawer      = 'closed';
    g.selected    = null;
    g.requestId   = null;
    g.prepareData = null;
    hideProposed();
    hideValidation();
    resetActionForm();
  }

  function resetActionForm() {
    el.actionSelect.innerHTML     = '<option value="">' + window.i18n.actionSelectPlaceholder + '</option>';
    el.newLocation.value          = '';
    el.assignKidHandedTo.value    = '';
    el.assignKid.value            = '';
    el.assignDateHandedOver.value = '';
    hideAllActionFields();
  }

  function hideAllActionFields() {
    el.fieldsStatusUpdate.style.display     = 'none';
    el.fieldsLocationUpdate.style.display   = 'none';
    el.fieldsAssignmentUpdate.style.display = 'none';
  }

  // ── Drawer modes ──────────────────────────────────────────────────────────

  function populateDeviceSummary(d) {
    el.dSource.textContent        = SOURCE_LABELS[d.source] || d.source;
    el.dId.textContent            = String(d.id);
    el.dSerial.textContent        = d.serialNumber;
    el.dType.textContent          = d.deviceType;
    el.dStatus.textContent        = d.status;
    el.dDeviceLocation.textContent = d.deviceLocation;
  }

  function populateActionDropdown(source) {
    var actions = SOURCE_ACTIONS[source] || [];
    el.actionSelect.innerHTML = '<option value="">' + window.i18n.actionSelectPlaceholder + '</option>';
    actions.forEach(function (a) {
      var opt = document.createElement('option');
      opt.value = a;
      opt.textContent = ACTION_LABELS[a] || a;
      el.actionSelect.appendChild(opt);
    });
  }

  function populateStatusDropdown(source) {
    var opts = STATUS_OPTIONS[source] || [];
    el.newStatus.innerHTML = '';
    opts.forEach(function (s) {
      var opt = document.createElement('option');
      opt.value = s;
      opt.textContent = s;
      el.newStatus.appendChild(opt);
    });
  }

  function openDetails(device) {
    g.selected    = device;
    g.drawerMode  = 'details';
    g.drawer      = 'open_details';
    g.requestId   = null;
    g.prepareData = null;
    populateDeviceSummary(device);
    el.drawerSteps.style.display = 'none';
    el.actionForm.style.display  = 'none';
    el.btnPrepare.style.display  = 'none';
    el.btnConfirm.style.display  = 'none';
    hideProposed();
    hideValidation();
    resetActionForm();
    openDrawerPanel();
  }

  function openAction(device) {
    g.selected    = device;
    g.drawerMode  = 'action';
    g.drawer      = 'open_step1';
    g.requestId   = null;
    g.prepareData = null;
    populateDeviceSummary(device);
    populateActionDropdown(device.source);
    populateStatusDropdown(device.source);
    el.drawerSteps.style.display = '';
    el.step1Label.classList.add('ops-step-active');
    el.step2Label.classList.remove('ops-step-active');
    el.actionForm.style.display  = '';
    el.btnPrepare.style.display  = '';
    el.btnPrepare.disabled       = false;
    el.btnConfirm.style.display  = '';
    el.btnConfirm.disabled       = true;
    hideProposed();
    hideValidation();
    resetActionForm();
    populateActionDropdown(device.source);
    populateStatusDropdown(device.source);
    openDrawerPanel();
  }

  // ── Action dropdown change ────────────────────────────────────────────────

  function onActionChange() {
    var action = el.actionSelect.value;
    hideAllActionFields();
    hideProposed();
    hideValidation();
    el.btnConfirm.disabled = true;
    g.drawer      = 'open_step1';
    g.requestId   = null;
    g.prepareData = null;
    if (action === 'status-update')     el.fieldsStatusUpdate.style.display     = '';
    if (action === 'location-update')   el.fieldsLocationUpdate.style.display   = '';
    if (action === 'assignment-update') el.fieldsAssignmentUpdate.style.display = '';
  }

  // ── Proposed changes display ──────────────────────────────────────────────

  function showProposed(prepareResult) {
    el.proposedSection.style.display = '';
    var changes = prepareResult ? prepareResult.proposedChanges : {};
    var keys    = Object.keys(changes || {});
    if (!keys.length) {
      el.proposedContent.innerHTML = '<span class="ops-muted">' + window.i18n.noChanges + '</span>';
      return;
    }
    var current = prepareResult.currentState || {};
    var html = '';
    keys.forEach(function (k) {
      var normalKey = PROPOSED_KEY_MAP[k] || k.charAt(0).toLowerCase() + k.slice(1);
      var before    = current[normalKey];
      if (before == null) before = '—';
      var after = changes[k] == null ? '—' : changes[k];
      html +=
        '<div class="ops-proposed-row">' +
          '<span class="ops-proposed-key">' + esc(k) + '</span>' +
          '<span class="ops-proposed-before">' + esc(String(before)) + '</span>' +
          '<span class="ops-arrow">&#8594;</span>' +
          '<span class="ops-proposed-after">' + esc(String(after)) + '</span>' +
        '</div>';
    });
    el.proposedContent.innerHTML = html;
  }

  function hideProposed() {
    el.proposedSection.style.display = 'none';
    el.proposedContent.innerHTML     = '';
  }

  // ── Validation area ───────────────────────────────────────────────────────

  function showValidation(variant, code, message) {
    el.validationArea.style.display = '';
    el.validationArea.className     = 'ops-validation ops-val-' + variant;
    el.validationArea.innerHTML     =
      (code    ? '<span class="ops-val-code">' + esc(code) + '</span> ' : '') +
      (message ? '<span class="ops-val-msg">'  + esc(message) + '</span>' : '');
  }

  function hideValidation() {
    el.validationArea.style.display = 'none';
    el.validationArea.innerHTML     = '';
    el.validationArea.className     = 'ops-validation';
  }

  // ── Build request bodies ──────────────────────────────────────────────────

  function buildBody(device, action) {
    var base = { Source: device.source, Id: device.id };
    if (action === 'status-update') {
      var s = el.newStatus.value;
      if (!s) return null;
      return Object.assign({}, base, { Status: s });
    }
    if (action === 'location-update') {
      var loc = el.newLocation.value.trim();
      if (!loc) return null;
      return Object.assign({}, base, { DeviceLocation: loc });
    }
    if (action === 'assignment-update') {
      return Object.assign({}, base, {
        KidHandedTo:    el.assignKidHandedTo.value.trim()    || null,
        KID:            el.assignKid.value.trim()            || null,
        DateHandedOver: el.assignDateHandedOver.value        || null,
      });
    }
    if (action === 'return-mark') return base;
    return null;
  }

  // ── Prepare ───────────────────────────────────────────────────────────────

  function doPrepare() {
    if (!g.selected) return;
    var action = el.actionSelect.value;
    if (!action) {
      showValidation('error', 'INVALID_INPUT', window.i18n.validationSelectAction);
      return;
    }
    var body = buildBody(g.selected, action);
    if (!body) {
      showValidation('error', 'INVALID_INPUT', window.i18n.validationFillFields);
      return;
    }

    g.drawer = 'preparing';
    el.btnPrepare.disabled = true;
    el.btnConfirm.disabled = true;
    hideProposed();
    showValidation('info', null, window.i18n.preparing);

    apiPost('/api/agent/v1/actions/prepare/' + action, body)
      .then(function (r) {
        if (r.ok) {
          g.prepareData = r.prepare;
          g.requestId   = generateUuid();
          g.drawer      = 'prepared_ok';
          el.step1Label.classList.remove('ops-step-active');
          el.step2Label.classList.add('ops-step-active');
          showProposed(r.prepare);
          if (r.prepare && r.prepare.warning) {
            showValidation('warning', 'WARNING', r.prepare.warning);
          } else {
            showValidation('ok', null, window.i18n.prepareOk);
          }
          el.btnPrepare.disabled = false;
          el.btnConfirm.disabled = false;
        } else {
          onPrepareFailure(r.code, r.message);
        }
      })
      .catch(function () {
        onPrepareFailure('NETWORK_ERROR', window.i18n.networkErrorRetry);
      });
  }

  function onPrepareFailure(code, message) {
    var variant = (code === 'BLOCKED' || code === 'NOT_SUPPORTED') ? 'blocked' : 'error';
    g.drawer = code === 'BLOCKED' ? 'prepared_blocked'
             : code === 'NOT_SUPPORTED' ? 'prepared_not_supported'
             : 'prepared_error';
    showValidation(variant, code, message);
    el.btnPrepare.disabled = false;
    el.btnConfirm.disabled = true;
  }

  // ── Confirm ───────────────────────────────────────────────────────────────

  function doConfirm() {
    if (g.drawer !== 'prepared_ok' || !g.selected || !g.requestId) return;
    var action     = el.actionSelect.value;
    var callAction = action === 'return-mark' ? 'return' : action;
    var body       = buildBody(g.selected, action);

    g.drawer = 'confirming';
    el.btnConfirm.disabled = true;
    el.btnPrepare.disabled = true;
    showValidation('info', null, window.i18n.executing);

    apiPost('/api/agent/v1/actions/call/' + callAction, body, g.requestId)
      .then(function (r) {
        if (r.ok) {
          g.drawer = 'confirmed_ok';
          var devInfo = r.device
            ? window.i18n.devInfoSerial + r.device.serialNumber + window.i18n.devInfoStatus + r.device.status
            : '';
          showValidation('ok', null, window.i18n.confirmedOk + devInfo);
          if (r.device) updateResultRow(r.device);
          el.btnPrepare.disabled = true;
          el.btnConfirm.disabled = true;
        } else {
          g.drawer = 'confirmed_error';
          showValidation('error', r.code, r.message);
          el.btnPrepare.disabled = false;
          el.btnConfirm.disabled = true;
        }
      })
      .catch(function () {
        g.drawer = 'confirmed_error';
        showValidation('error', 'NETWORK_ERROR', window.i18n.networkError);
        el.btnPrepare.disabled = false;
        el.btnConfirm.disabled = true;
      });
  }

  function updateResultRow(updatedDevice) {
    for (var i = 0; i < g.results.length; i++) {
      if (g.results[i].source === updatedDevice.source && g.results[i].id === updatedDevice.id) {
        g.results[i] = updatedDevice;
        renderTable(g.results);
        return;
      }
    }
  }

  // ── Search handlers ───────────────────────────────────────────────────────

  function doSearch() {
    var identifier = el.identifierInput.value.trim();
    if (!identifier) {
      setStatus(window.i18n.enterSerial, 'idle');
      return;
    }
    g.global = 'searching';
    setStatus(window.i18n.searching, 'searching');
    setSearchBusy(true);

    apiFetch('/api/agent/v1/devices/lookup?identifier=' + encodeURIComponent(identifier))
      .then(function (r) {
        setSearchBusy(false);
        if (!r.ok) {
          g.global = 'error';
          setStatus(window.i18n.errorPrefix + (r.message || r.code || window.i18n.unknownError), 'error');
          el.resultsPanel.style.display = 'none';
          return;
        }
        g.results = r.items;
        if (!r.count) {
          g.global = 'no_results';
          setStatus(window.i18n.noResults, 'empty');
          el.resultsPanel.style.display = 'none';
        } else {
          g.global  = 'results_ready';
          var msg   = window.i18n.foundPrefix + r.count;
          if (r.count > 1) msg += window.i18n.multipleHits;
          setStatus(msg, 'found');
          renderTable(r.items);
        }
      })
      .catch(function () {
        setSearchBusy(false);
        g.global = 'error';
        setStatus(window.i18n.errorPrefix + window.i18n.networkError, 'error');
        el.resultsPanel.style.display = 'none';
      });
  }

  function doShowAvailable() {
    var source = el.sourceSelect.value;
    g.global = 'searching';
    setStatus(window.i18n.searching, 'searching');
    setSearchBusy(true);

    var url = '/api/agent/v1/devices?status=Available';
    if (source) url += '&source=' + encodeURIComponent(source);

    apiFetch(url)
      .then(function (r) {
        setSearchBusy(false);
        if (!r.ok) {
          g.global = 'error';
          setStatus(window.i18n.errorPrefix + (r.message || r.code || window.i18n.unknownError), 'error');
          el.resultsPanel.style.display = 'none';
          return;
        }
        g.results = r.items;
        if (!r.count) {
          g.global = 'no_results';
          setStatus(window.i18n.noAvailable, 'empty');
          el.resultsPanel.style.display = 'none';
        } else {
          g.global = 'results_ready';
          setStatus(window.i18n.availablePrefix + r.count, 'found');
          renderTable(r.items);
        }
      })
      .catch(function () {
        setSearchBusy(false);
        g.global = 'error';
        setStatus(window.i18n.errorPrefix + window.i18n.networkError, 'error');
        el.resultsPanel.style.display = 'none';
      });
  }

  function doReset() {
    g.global  = 'idle';
    g.results = [];
    el.identifierInput.value      = '';
    el.sourceSelect.value         = '';
    el.resultsPanel.style.display = 'none';
    setStatus(window.i18n.enterSerial, 'idle');
    if (g.drawer !== 'closed') closeDrawer();
  }

  // ── Init ──────────────────────────────────────────────────────────────────

  document.addEventListener('DOMContentLoaded', function () {
    cacheEls();
    el.identifierInput.addEventListener('keydown', function (e) {
      if (e.key === 'Enter') doSearch();
    });
    el.actionSelect.addEventListener('change', onActionChange);
    setStatus(window.i18n.enterSerial, 'idle');
  });

  // Expose for inline onclick handlers in the view
  window.doSearch        = doSearch;
  window.doShowAvailable = doShowAvailable;
  window.doReset         = doReset;
  window.closeDrawer     = closeDrawer;
  window.doPrepare       = doPrepare;
  window.doConfirm       = doConfirm;
  window.onActionChange  = onActionChange;

}());
