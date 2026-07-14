const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

test('requires a box scan for every package and keeps the scanner focused', async () => {
    const intervalCallbacks = [];
    const requests = [];
    let domReady;
    let scannerInput;
    let visibleOverlay = null;
    const alertDialog = {
        offsetParent: {},
        matches: () => true,
        focus() { document.activeElement = this; }
    };

    const document = {
        activeElement: null,
        body: {
            dataset: { boxPrefix: 'BOX-' },
            appendChild(element) {
                if (element.id === '__scannerInput') scannerInput = element;
                if (element.id === 'scanOverlay') visibleOverlay = element;
            }
        },
        contains: () => true,
        createElement(tagName) {
            const listeners = {};
            return {
                tagName,
                id: '',
                value: '',
                className: '',
                innerHTML: '',
                style: {},
                offsetParent: {},
                addEventListener(type, listener) { listeners[type] = listener; },
                setAttribute() {},
                querySelector(selector) {
                    return this.id === 'scanOverlay' && selector === '[role="alertdialog"]'
                        ? alertDialog
                        : null;
                },
                closest() { return null; },
                matches() { return false; },
                focus() { document.activeElement = this; },
                _listeners: listeners
            };
        },
        addEventListener(type, listener) {
            if (type === 'DOMContentLoaded') domReady = listener;
        },
        getElementById() { return null; },
        querySelector(selector) {
            if (selector.includes('.scanner-overlay.is-visible') &&
                visibleOverlay?.className.includes('is-visible')) return visibleOverlay;
            return null;
        }
    };

    const context = {
        window: { crypto: { randomUUID: () => 'request-id' } },
        document,
        FormData,
        fetch: async (url, options) => {
            requests.push({ url, data: Object.fromEntries(options.body.entries()) });
            return {
            json: async () => url === '/Box/AutoScanPackage'
                ? { success: false, noMatch: true }
                : { success: true, message: 'linked', currentQuantity: 1, expectedQuantity: 2 }
            };
        },
        setInterval(callback) { intervalCallbacks.push(callback); return intervalCallbacks.length; },
        clearInterval() {},
        setTimeout(callback, delay) { if (delay === 300) callback(); return 1; },
        clearTimeout() {},
        sessionStorage: { getItem: () => null, setItem() {}, removeItem() {} },
        localStorage: { getItem: () => 'off' },
        encodeURIComponent,
        console
    };
    context.window.window = context.window;
    vm.createContext(context);

    const scannerPath = path.resolve(__dirname, '../../MothersonBoxManagement/wwwroot/js/scanner.js');
    vm.runInContext(fs.readFileSync(scannerPath, 'utf8'), context);
    domReady();

    await context.window.simulateScan('PKG-001');
    assert.match(visibleOverlay.className, /is-visible/);
    assert.equal(document.activeElement, scannerInput);

    document.activeElement = visibleOverlay;
    intervalCallbacks[0]();

    assert.equal(document.activeElement, scannerInput);

    await context.window.simulateScan('BOX-001');
    await context.window.simulateScan('PKG-002');

    assert.equal(requests.filter(request => request.url === '/Box/AssociatePackage').length, 1);
    assert.equal(requests.filter(request => request.url === '/Box/AutoScanPackage').length, 2);
    assert.match(visibleOverlay.innerHTML, /Waiting for box scan/);
});
