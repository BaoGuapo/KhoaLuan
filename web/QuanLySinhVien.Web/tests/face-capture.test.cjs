const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const source = fs.readFileSync(path.join(__dirname, '..', 'wwwroot', 'js', 'face-capture.js'), 'utf8');

function startPage(autoCapture, cameraError) {
    const listeners = {};
    let cameraRequests = 0;
    const form = {
        dataset: { autoStart: 'true' },
        hasAttribute: () => autoCapture,
        requestSubmit: () => {}
    };
    const video = { play: async () => {}, videoWidth: 720, videoHeight: 540 };
    const startButton = { disabled: false, hidden: autoCapture, addEventListener: (_, handler) => { listeners.start = handler; } };
    const captureButton = autoCapture ? null : { disabled: true, addEventListener: () => {} };
    const message = { dataset: {}, textContent: '' };
    const elements = {
        '[data-face-form]': form,
        '[data-face-video]': video,
        '[data-camera-start]': startButton,
        '[data-face-capture]': captureButton,
        '[data-face-descriptors]': { value: '' },
        '[data-face-message]': message
    };
    const faceapi = {
        TinyFaceDetectorOptions: class {},
        nets: {
            tinyFaceDetector: { loadFromUri: async () => {} },
            faceLandmark68TinyNet: { loadFromUri: async () => {} }
        },
        detectAllFaces: () => ({ withFaceLandmarks: () => new Promise(() => {}) })
    };
    const ort = {
        env: { wasm: {} },
        InferenceSession: { create: async () => ({}) }
    };
    vm.runInNewContext(source, {
        document: { querySelector: selector => elements[selector] },
        window: { faceapi, ort, setTimeout, addEventListener: () => {} },
        navigator: {
            mediaDevices: {
                getUserMedia: async () => {
                    cameraRequests += 1;
                    if (cameraError) throw cameraError;
                    return { getTracks: () => [] };
                }
            }
        },
        faceapi,
        ort
    });
    return { listeners, message, startButton, cameraRequests: () => cameraRequests };
}

test('bước xác thực mặt tự mở camera khi tải trang', async () => {
    const page = startPage(true);
    await new Promise(resolve => setImmediate(resolve));
    assert.equal(page.cameraRequests(), 1);
    assert.match(page.message.textContent, /Đang lấy mẫu/);
    assert.equal(page.startButton.hidden, true);
});

test('trang đăng ký mặt vẫn chờ người dùng bật camera', async () => {
    const page = startPage(false);
    await new Promise(resolve => setImmediate(resolve));
    assert.equal(page.cameraRequests(), 0);
    await page.listeners.start();
    assert.equal(page.cameraRequests(), 1);
});

test('khi bị từ chối camera, trang hướng dẫn cấp quyền và cho thử lại', async () => {
    const page = startPage(true, { name: 'NotAllowedError' });
    await new Promise(resolve => setImmediate(resolve));
    assert.match(page.message.textContent, /cấp quyền camera/);
    assert.equal(page.message.dataset.state, 'error');
    assert.equal(page.startButton.hidden, false);
    assert.equal(page.startButton.disabled, false);
});
