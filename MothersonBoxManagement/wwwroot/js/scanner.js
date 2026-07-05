/**
 * Motherson Box Management - Barcode Scanner & AJAX Scan Handler
 */

let buffer = "";
let lastKeyTime = 0;

document.addEventListener('DOMContentLoaded', () => {
    // 1. Hook the manual scan form submit to intercept and perform AJAX scan
    const scanForm = document.getElementById('scanForm');
    if (scanForm) {
        scanForm.addEventListener('submit', function (e) {
            e.preventDefault();
            const input = document.getElementById('scanInput');
            if (input) {
                submitScan(input.value.trim());
            }
        });
    }

    // 2. Keystroke detection buffer for rapid keyboard/wedge scanner input
    document.addEventListener('keydown', function (e) {
        // Ignore helper keys (Shift, Control, Alt, etc.)
        if (e.key.length !== 1 && e.key !== 'Enter') {
            return;
        }

        const currentTime = Date.now();

        if (e.key === 'Enter') {
            const timeDiff = currentTime - lastKeyTime;
            // Check if buffer contains a barcode and characters were entered rapidly (wedge scanner style)
            if (buffer.length >= 3 && timeDiff <= 50) {
                e.preventDefault();
                submitScan(buffer.trim());
                buffer = "";
            } else {
                buffer = "";
            }
        } else {
            // Reset buffer if delay between characters exceeds 100ms (user is typing manually)
            if (buffer.length > 0 && (currentTime - lastKeyTime > 100)) {
                buffer = "";
            }
            buffer += e.key;
            lastKeyTime = currentTime;
        }
    });
});

/**
 * Submits the barcode via AJAX to the ScanAjax action
 */
async function submitScan(barcode) {
    if (!barcode) return;

    const csrfTokenMeta = document.querySelector('meta[name="RequestVerificationToken"]');
    const csrfToken = csrfTokenMeta ? csrfTokenMeta.content : "";

    const boxIdInput = document.getElementById('scanBoxId');
    const boxBarcodeInput = document.getElementById('scanBoxBarcode');

    if (!boxIdInput || !boxBarcodeInput) {
        console.error("Required hidden fields boxId or boxBarcode not found.");
        return;
    }

    const boxId = boxIdInput.value;
    const boxBarcode = boxBarcodeInput.value;

    const formData = new FormData();
    formData.append('boxId', boxId);
    formData.append('boxBarcode', boxBarcode);
    formData.append('barcode', barcode);

    // Clear and re-focus manual input field immediately
    const input = document.getElementById('scanInput');
    if (input) {
        input.value = "";
        input.focus();
    }

    try {
        const response = await fetch('/Box/ScanAjax', {
            method: 'POST',
            headers: {
                'X-CSRF-TOKEN': csrfToken
            },
            body: formData
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();

        if (data.success) {
            addPackageRow(data.package);
            updateProgressBar(data.currentQuantity, data.expectedQuantity);
            showFeedback(true, data.message);
            playBeep(true);
        } else {
            showFeedback(false, data.message);
            playBeep(false);
        }

        if (data.status === "Completed") {
            disableScanInput();
        }
    } catch (error) {
        console.error("Error during scan submission:", error);
        showFeedback(false, "An error occurred during communication with the server.");
        playBeep(false);
    }
}

/**
 * Prepends a new package row to the scan list table
 */
function addPackageRow(pkg) {
    if (!pkg) return;

    const placeholder = document.getElementById('noPackagesPlaceholder');
    if (placeholder) {
        placeholder.style.display = 'none';
    }

    const packageTable = document.getElementById('packageTable');
    if (packageTable) {
        packageTable.style.display = 'table';
    }

    const tbody = document.getElementById('packageTableBody');
    if (tbody) {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td><code>${escapeHtml(pkg.barcode)}</code></td>
            <td>${escapeHtml(pkg.scannedBy || '')}</td>
            <td>${escapeHtml(pkg.scannedAt || '')}</td>
        `;
        tbody.insertBefore(tr, tbody.firstChild);
    }

    // Update count in header
    const rowCount = tbody ? tbody.children.length : 0;
    const headerCount = document.getElementById('packageCountHeader');
    if (headerCount) {
        headerCount.innerHTML = `Packages (<span id="packageCount">${rowCount}</span>)`;
    }
    const pkgCount = document.getElementById('packageCount');
    if (pkgCount) {
        pkgCount.textContent = `${rowCount} scanned`;
    }
}

/**
 * Animates and updates the progress bar fill
 */
function updateProgressBar(current, expected) {
    const percent = expected > 0 ? Math.round((current / expected) * 100) : 0;
    const fill = document.getElementById('progressBarFill');
    if (fill) {
        fill.style.width = percent + '%';
        fill.textContent = `${current} / ${expected}`;

        if (percent >= 100) {
            fill.classList.remove('bg-primary');
            fill.classList.add('bg-success');
        } else {
            fill.classList.remove('bg-success');
            fill.classList.add('bg-primary');
        }

        const progressContainer = fill.parentElement;
        if (progressContainer) {
            progressContainer.classList.add('pulse-success');
            setTimeout(() => {
                progressContainer.classList.remove('pulse-success');
            }, 1500);
        }
    }

    // Update the "X / Y Packages" label above the progress bar
    const fillingLabel = document.getElementById('fillingProgressLabel');
    if (fillingLabel) {
        fillingLabel.textContent = `${current} / ${expected} Packages`;
    }

    // Update the "N package(s) remaining" badge in the scan card
    const remainingBadge = document.getElementById('remainingBadge');
    if (remainingBadge) {
        const remaining = expected - current;
        if (remaining > 0) {
            remainingBadge.textContent = `${remaining} package(s) remaining`;
        } else {
            remainingBadge.style.display = 'none';
        }
    }
}

/**
 * Displays user feedback as an alert and triggers a temporary container flash
 */
function showFeedback(isSuccess, message) {
    const feedbackDiv = document.getElementById('scanFeedback');
    if (!feedbackDiv) return;

    const alertClass = isSuccess ? 'alert-success' : 'alert-danger';

    feedbackDiv.innerHTML = `
        <div class="alert ${alertClass} alert-dismissible fade show" role="alert">
            ${escapeHtml(message)}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;

    // Trigger visual flash animation on the card
    const input = document.getElementById('scanInput');
    if (input) {
        const cardBody = input.closest('.card-body');
        if (cardBody) {
            const flashClass = isSuccess ? 'scan-flash-success' : 'scan-flash-error';
            cardBody.classList.remove('scan-flash-success', 'scan-flash-error');
            // Trigger reflow to restart CSS animation
            void cardBody.offsetWidth;
            cardBody.classList.add(flashClass);
            setTimeout(() => {
                cardBody.classList.remove(flashClass);
            }, 500);
        }
    }

    // Auto-dismiss alert after 5 seconds
    if (feedbackDiv._timeoutId) {
        clearTimeout(feedbackDiv._timeoutId);
    }
    feedbackDiv._timeoutId = setTimeout(() => {
        feedbackDiv.innerHTML = '';
    }, 5000);
}

/**
 * Emits audio feedback using the Web Audio API
 */
function playBeep(isSuccess) {
    try {
        const AudioContextClass = window.AudioContext || window.webkitAudioContext;
        if (!AudioContextClass) return;

        const ctx = new AudioContextClass();
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();

        osc.connect(gain);
        gain.connect(ctx.destination);

        if (isSuccess) {
            osc.frequency.value = 800;
            gain.gain.setValueAtTime(0.08, ctx.currentTime);
            osc.start();
            gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.15);
            osc.stop(ctx.currentTime + 0.15);
        } else {
            osc.frequency.value = 300;
            gain.gain.setValueAtTime(0.12, ctx.currentTime);
            osc.start();
            gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.3);
            osc.stop(ctx.currentTime + 0.3);
        }
    } catch (e) {
        console.warn("Web Audio API not supported or blocked by browser policy:", e);
    }
}

/**
 * Disables the barcode input elements when box is completed
 */
function disableScanInput() {
    const input = document.getElementById('scanInput');
    const btn = document.getElementById('scanBtn');
    if (input) {
        input.disabled = true;
        input.placeholder = "Box completed";
        input.value = "";
    }
    if (btn) {
        btn.disabled = true;
    }
}

/**
 * Basic HTML escaping utility
 */
function escapeHtml(str) {
    if (typeof str !== 'string') return str;
    return str
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}
