/**
 * Motherson Box Management - Global Barcode Scanner
 * Uses a hidden input field to reliably capture USB scanner input
 * (Zebra DS8178 and compatible HID keyboard wedge scanners).
 *
 * Instead of relying on keydown event timing, this approach:
 * 1. Creates a hidden <input> that captures all text input
 * 2. Uses the 'input' event + debounce to detect when a scan is complete
 * 3. Uses the 'keydown' Enter event to immediately process the barcode
 * 4. Periodically refocuses the hidden input to ensure it captures scans
 *
 * State machine:
 *   IDLE         → scan package (no prefix) → AWAITING_BOX
 *   IDLE         → scan package (prefix match) → auto-create → redirect
 *   IDLE         → scan BOX barcode → redirect to details
 *   AWAITING_BOX → scan BOX barcode → associate → IDLE
 *   AWAITING_BOX → timeout → IDLE
 */
(function () {
    'use strict';

    var STATE_IDLE = 'idle';
    var STATE_AWAITING_BOX = 'awaitingBox';
    var TIMEOUT_MS = 30000;
    var SCAN_DEBOUNCE_MS = 80;

    var _state = STATE_IDLE;
    var _pendingBarcode = null;
    var _timeoutId = null;
    var _tickId = null;
    var _debounceId = null;
    var _overlay = null;
    var _barEl = null;
    var _timerEl = null;
    var _dismissOverlayAction = null;
    var _overlayAllowsScanning = false;
    var _scanInput = null;
    var _scanQueue = Promise.resolve();
    var _previousFocus = null;

    // ── Hidden scan input ───────────────────────────────────────────

    function setScannerStatus(state, label) {
        var status = document.getElementById('scannerStatus');
        if (!status) return;

        status.dataset.state = state;
        var text = status.querySelector('.sidebar-scanner-label');
        if (text) text.textContent = label;
    }

    function ensureScanInput() {
        if (_scanInput) return _scanInput;

        _scanInput = document.createElement('input');
        _scanInput.type = 'text';
        _scanInput.id = '__scannerInput';
        _scanInput.autocomplete = 'off';
        _scanInput.setAttribute('aria-hidden', 'true');
        _scanInput.style.cssText =
            'position:fixed;top:-200px;left:-200px;width:1px;height:1px;' +
            'opacity:0;pointer-events:none;z-index:-1;border:none;padding:0;';

        document.body.appendChild(_scanInput);

        _scanInput.addEventListener('input', onScanInput);
        _scanInput.addEventListener('keydown', onScanKeydown);

        return _scanInput;
    }

    function focusScanInput() {
        var el = ensureScanInput();
        var active = document.activeElement;
        var scannerOverlay = document.querySelector('.scanner-overlay.is-visible');
        var isAwaitingBoxScan = _state === STATE_AWAITING_BOX && scannerOverlay;
        var scannerOverlayAllowsCapture = scannerOverlay &&
            (isAwaitingBoxScan || _overlayAllowsScanning);
        if (document.querySelector('.modal.show,[role="dialog"][aria-modal="true"]') ||
            (scannerOverlay && !scannerOverlayAllowsCapture)) {
            setScannerStatus('paused', 'Scanner paused');
            return;
        }
        // Never steal focus from a visible interactive/editable control.
        if (!scannerOverlayAllowsCapture && active && active !== el && active !== document.body &&
            active.matches('a[href],button,input,select,textarea,[contenteditable="true"],[tabindex]:not([tabindex="-1"])') &&
            active.offsetParent !== null) {
            setScannerStatus('paused', 'Scanner paused');
            return;
        }
        el.value = '';
        el.focus({ preventScroll: true });
        setScannerStatus('ready', 'Scanner ready');
    }

    function onScanInput(e) {
        var value = e.target.value;

        if (_debounceId) {
            clearTimeout(_debounceId);
            _debounceId = null;
        }

        _debounceId = setTimeout(function () {
            _debounceId = null;
            var barcode = value.trim();
            e.target.value = '';

            if (barcode.length >= 3) {
                enqueueBarcode(barcode);
            }
        }, SCAN_DEBOUNCE_MS);
    }

    function onScanKeydown(e) {
        if (e.key === 'Enter' || e.key === 'NumpadEnter' || e.keyCode === 13) {
            e.preventDefault();
            e.stopPropagation();

            if (_debounceId) {
                clearTimeout(_debounceId);
                _debounceId = null;
            }

            var barcode = e.target.value.trim();
            e.target.value = '';

            if (barcode.length >= 3) {
                enqueueBarcode(barcode);
            }
        }
    }

    // Keep the hidden input focused
    setInterval(focusScanInput, 1500);

    // ── CSRF token ──────────────────────────────────────────────────

    function csrfToken() {
        var meta = document.querySelector('meta[name="RequestVerificationToken"]');
        return meta ? meta.content : '';
    }

    // ── Overlay helpers ─────────────────────────────────────────────

    function ensureOverlay() {
        if (_overlay) return;
        _overlay = document.createElement('div');
        _overlay.id = 'scanOverlay';
        _overlay.className = 'scanner-overlay';
        _overlay.setAttribute('aria-hidden', 'true');
        _overlay.addEventListener('click', function (e) {
            if (e.target.closest('[data-scan-close]')) {
                hideOverlay(true);
                return;
            }
            if (e.target !== _overlay) return;
            if (_state === STATE_AWAITING_BOX) {
                _pendingBarcode = null;
                _state = STATE_IDLE;
            }
            hideOverlay(true);
        });
        document.body.appendChild(_overlay);
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && _overlay && _overlay.classList.contains('is-visible'))
                hideOverlay(true);
        });
    }

    function showOverlay(kind, innerHTML, onDismiss, allowScanning) {
        if (_tickId) { clearInterval(_tickId); _tickId = null; }
        ensureOverlay();
        _previousFocus = document.activeElement;
        _overlay.innerHTML = innerHTML;
        _overlay.className = 'scanner-overlay is-visible scanner-overlay--' + kind;
        _overlay.setAttribute('aria-hidden', 'false');
        _dismissOverlayAction = typeof onDismiss === 'function' ? onDismiss : null;
        _overlayAllowsScanning = allowScanning === true;
        var dialog = _overlay.querySelector('[role="alertdialog"]');
        if (dialog) dialog.focus({ preventScroll: true });
    }

    function hideOverlay(runDismissAction) {
        var dismissAction = _dismissOverlayAction;
        _dismissOverlayAction = null;
        _overlayAllowsScanning = false;
        if (_overlay) {
            _overlay.className = 'scanner-overlay';
            _overlay.setAttribute('aria-hidden', 'true');
            _overlay.innerHTML = '';
        }
        if (_tickId) { clearInterval(_tickId); _tickId = null; }
        if (_timeoutId) { clearTimeout(_timeoutId); _timeoutId = null; }
        _barEl = null; _timerEl = null;
        if (runDismissAction && dismissAction) dismissAction();
        if (_previousFocus && typeof _previousFocus.focus === 'function' && document.contains(_previousFocus))
            _previousFocus.focus({ preventScroll: true });
        _previousFocus = null;
    }

    function overlayCard(kind, label, title, contentHTML) {
        return '<div class="scanner-overlay__dialog" role="alertdialog" aria-live="assertive" aria-modal="true" aria-labelledby="scanOverlayTitle" tabindex="-1">' +
            '<div class="scanner-overlay__card scanner-overlay__card--' + kind + '">' +
                '<button type="button" class="scanner-overlay__close" data-scan-close aria-label="Close scan message">&times;</button>' +
                '<div class="scanner-overlay__header">' +
                    '<div class="scanner-overlay__icon scanner-overlay__icon--' + kind + '" aria-hidden="true">' + iconSvg(kind) + '</div>' +
                    '<div class="scanner-overlay__heading">' +
                        '<span class="scanner-overlay__badge scanner-overlay__badge--' + kind + '">' + esc(label) + '</span>' +
                        '<h2 id="scanOverlayTitle" class="scanner-overlay__title">' + esc(title) + '</h2>' +
                    '</div>' +
                '</div>' +
                '<div class="scanner-overlay__content">' + contentHTML + '</div>' +
            '</div></div>';
    }

    function iconSvg(kind) {
        if (kind === 'success') return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5"></path></svg>';
        if (kind === 'error') return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 6 6 18"></path><path d="m6 6 12 12"></path></svg>';
        if (kind === 'warning') return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 9v4"></path><path d="M12 17h.01"></path><path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path></svg>';
        if (kind === 'redirect') return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 7.5 12 3l9 4.5-9 4.5-9-4.5Z"></path><path d="M3 12l9 4.5 9-4.5"></path><path d="M3 16.5 12 21l9-4.5"></path></svg>';
        return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="7"></circle><path d="m21 21-4.35-4.35"></path></svg>';
    }

    // ── Audio ───────────────────────────────────────────────────────

    var AudioCtx = null;
    function getAudio() {
        if (!AudioCtx) { try { AudioCtx = new (window.AudioContext || window.webkitAudioContext)(); } catch (e) { AudioCtx = null; } }
        return AudioCtx;
    }
    function beep(freq, duration, type) {
        try { if (localStorage.getItem('mothersonScannerSound') === 'off') return; } catch (e) { }
        var ctx = getAudio(); if (!ctx) return;
        var osc = ctx.createOscillator(); var gain = ctx.createGain();
        osc.type = type || 'sine'; osc.frequency.setValueAtTime(freq, ctx.currentTime);
        gain.gain.setValueAtTime(0.12, ctx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + duration);
        osc.connect(gain); gain.connect(ctx.destination);
        osc.start(ctx.currentTime); osc.stop(ctx.currentTime + duration);
    }
    function sfxPieceDetected() { beep(900, 0.10, 'sine'); setTimeout(function () { beep(1100, 0.08, 'sine'); }, 60); }
    function sfxSuccess() { beep(700, 0.10, 'sine'); setTimeout(function () { beep(900, 0.10, 'sine'); }, 100); setTimeout(function () { beep(1100, 0.12, 'sine'); }, 200); }
    function sfxError() { beep(250, 0.25, 'square'); }
    function sfxTimeout() { beep(500, 0.15, 'sine'); setTimeout(function () { beep(350, 0.20, 'sine'); }, 120); setTimeout(function () { beep(200, 0.25, 'sine'); }, 280); }
    function sfxBoxComplete() { beep(523, 0.12, 'sine'); setTimeout(function () { beep(659, 0.12, 'sine'); }, 120); setTimeout(function () { beep(784, 0.12, 'sine'); }, 240); setTimeout(function () { beep(1047, 0.20, 'sine'); }, 380); }

    // ── Text helpers ────────────────────────────────────────────────

    function esc(value) { var s = document.createElement('span'); s.textContent = value; return s.innerHTML; }
    function barcodeBlock(barcode) { return '<div class="scanner-overlay__barcode-wrap"><code class="scanner-overlay__barcode">' + esc(barcode) + '</code></div>'; }
    function helperMessage(msg) { return '<p class="scanner-overlay__message">' + esc(msg) + '</p>'; }
    function callout(msg) { return '<div class="scanner-overlay__callout"><p class="scanner-overlay__callout-text">' + esc(msg) + '</p></div>'; }
    function metricBlock(cur, exp) { return '<div class="scanner-overlay__metrics"><span class="scanner-overlay__metric-value">' + cur + '</span><span class="scanner-overlay__metric-label">/ ' + exp + ' packages</span></div>'; }
    function progressBlock(pct) { return '<div class="scanner-overlay__progress" aria-hidden="true"><div class="scanner-overlay__progress-bar" style="width:' + pct + '%;"></div></div>'; }
    function countdownBlock() {
        return '<div class="scanner-overlay__timer"><div class="scanner-overlay__progress scanner-overlay__progress--countdown" aria-hidden="true"><div id="scanTimerBar" class="scanner-overlay__progress-bar scanner-overlay__progress-bar--countdown" style="width:100%;"></div></div><div class="scanner-overlay__timer-row"><span class="scanner-overlay__timer-label">Waiting for box scan</span><span id="scanTimerText" class="scanner-overlay__timer-value">30s</span></div></div>';
    }

    function startCountdown() {
        var start = Date.now();
        _tickId = setInterval(function () {
            var remaining = Math.max(0, TIMEOUT_MS - (Date.now() - start));
            var pct = Math.round((remaining / TIMEOUT_MS) * 100);
            if (_barEl) _barEl.style.width = pct + '%';
            if (_timerEl) _timerEl.textContent = Math.ceil(remaining / 1000) + 's';
            if (remaining <= 0 && _tickId) { clearInterval(_tickId); _tickId = null; }
        }, 200);
    }

    // ── Overlay display ─────────────────────────────────────────────

    function showPieceDetected(barcode) {
        showOverlay('info', overlayCard('info', 'Package detected', 'Scan the box to link this package',
            barcodeBlock(barcode) + callout('Scan the matching box barcode within 30 seconds to complete this link.') + countdownBlock()
        ), null, true);
        _barEl = document.getElementById('scanTimerBar');
        _timerEl = document.getElementById('scanTimerText');
        startCountdown();
        sfxPieceDetected();
        focusScanInput();
    }

    function showSuccess(message, current, expected) {
        var pct = expected > 0 ? Math.round((current / expected) * 100) : 0;
        showOverlay('success', overlayCard('success', 'Package linked', 'The package was added successfully',
            helperMessage(message) + metricBlock(current, expected) + progressBlock(pct)
        ), null, true);
        sfxSuccess();
        focusScanInput();
        setTimeout(hideOverlay, 2500);
    }

    function showError(message) {
        showOverlay('error', overlayCard('error', 'Scan rejected', 'This scan could not be completed', helperMessage(message)), clearAwaitingState);
        sfxError();
        // Errors remain visible until the operator acknowledges them.
    }

    function showTimeout() {
        showOverlay('warning', overlayCard('warning', 'Time expired', 'The pending package scan has expired',
            helperMessage('Rescan the package and then scan the box again to restart the association.')
        ), clearAwaitingState);
        sfxTimeout();
        // Timeouts require acknowledgement so the recovery instruction is not missed.
    }

    function showBoxRedirect(barcode) {
        showOverlay('redirect', overlayCard('redirect', 'Box detected', 'Redirecting to box details',
            barcodeBlock(barcode) + helperMessage('Opening the box details view...')
        ));
        beep(600, 0.10, 'sine');
        setTimeout(function () { window.location.href = '/Box/Details/' + encodeURIComponent(barcode); }, 600);
    }

    // ── State management ─────────────────────────────────────────────

    function isBoxBarcode(barcode) {
        var prefix = document.body.dataset.boxPrefix || 'BOX-';
        return barcode.toUpperCase().indexOf(prefix.toUpperCase()) === 0;
    }

    function clearAwaitingState() {
        _state = STATE_IDLE;
        _pendingBarcode = null;
        if (_timeoutId) { clearTimeout(_timeoutId); _timeoutId = null; }
        if (_tickId) { clearInterval(_tickId); _tickId = null; }
        _barEl = null; _timerEl = null;
    }

    // ── API calls ───────────────────────────────────────────────────

    function newRequestId() {
        return window.crypto && window.crypto.randomUUID
            ? window.crypto.randomUUID()
            : Date.now().toString(36) + Math.random().toString(36).slice(2);
    }

    async function postScan(url, formData) {
        try {
            return await fetch(url, { method: 'POST', headers: { 'X-CSRF-TOKEN': csrfToken() }, body: formData });
        } catch (firstError) {
            return await fetch(url, { method: 'POST', headers: { 'X-CSRF-TOKEN': csrfToken() }, body: formData });
        }
    }

    async function associatePackage(packageBarcode, boxBarcode) {
        var formData = new FormData();
        formData.append('packageBarcode', packageBarcode);
        formData.append('boxBarcode', boxBarcode);
        formData.append('requestId', newRequestId());
        if (window.MothersonStation) window.MothersonStation.appendToFormData(formData);
        var resp = await postScan('/Box/AssociatePackage', formData);
        return await resp.json();
    }

    // ── Main handler ─────────────────────────────────────────────────

    async function handleBarcode(barcode) {
        if (!barcode || barcode.length < 3) return;

        if (_state === STATE_IDLE) {
            if (isBoxBarcode(barcode)) { showBoxRedirect(barcode); return; }

            sfxPieceDetected();

            try {
                var autoFormData = new FormData();
                autoFormData.append('packageBarcode', barcode);
                autoFormData.append('requestId', newRequestId());
                if (window.MothersonStation) window.MothersonStation.appendToFormData(autoFormData);
                var autoResp = await postScan('/Box/AutoScanPackage', autoFormData);
                var autoData = await autoResp.json();

                if (autoData.success) {
                    sfxSuccess();

                    // Show brief success overlay, then redirect to PrintClient (auto-print)
                    showOverlay('success', overlayCard('success', 'Box auto-created', 'Package linked to new box ' + esc(autoData.boxNumber),
                        helperMessage(autoData.message) + callout('Redirecting to print label...')
                    ));

                    var detailsUrl = '/Box/Details/' + encodeURIComponent(autoData.boxNumber);
                    var printUrl = autoData.printQueued
                        ? detailsUrl
                        : '/Box/PrintClient/' + encodeURIComponent(autoData.boxNumber) +
                            '?autoPrint=true&returnUrl=' + encodeURIComponent(detailsUrl);

                    setTimeout(function () {
                        window.location.href = printUrl;
                    }, 1500);
                    return;
                }
                if (!autoData.noMatch) { showError(autoData.message); return; }
            } catch (e) { /* fall through */ }

            _pendingBarcode = barcode;
            _state = STATE_AWAITING_BOX;
            showPieceDetected(barcode);
            _timeoutId = setTimeout(function () { _state = STATE_IDLE; _pendingBarcode = null; _timeoutId = null; showTimeout(); }, TIMEOUT_MS);
            return;
        }

        if (_state === STATE_AWAITING_BOX) {
            if (!isBoxBarcode(barcode)) { showError('Scan a box barcode next, not another package barcode.'); return; }
            var pkg = _pendingBarcode;
            clearAwaitingState();
            try {
                var data = await associatePackage(pkg, barcode);
                if (data.success) {
                    showSuccess(data.message, data.currentQuantity || 0, data.expectedQuantity || 0);
                    if (data.status === 'Completed' || data.status === 'CompletedWithException') sfxBoxComplete();
                } else { showError(data.message); }
            } catch (e) { showError('A communication error occurred while contacting the server.'); }
        }
    }

    function enqueueBarcode(barcode) {
        _scanQueue = _scanQueue
            .then(function () { return handleBarcode(barcode); })
            .catch(function () { showError('A communication error occurred while processing the scan.'); });
        return _scanQueue;
    }

    // ── Expose for simulator ─────────────────────────────────────────
    window.simulateScan = enqueueBarcode;

    // ── Init ─────────────────────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', function () {
        ensureScanInput();
        setScannerStatus('ready', 'Scanner ready');
        setTimeout(focusScanInput, 300);

        // Clear values left by versions that persisted the previously scanned box.
        try { sessionStorage.removeItem('__scannerStickyBox'); } catch (e) { }
    });

})();
