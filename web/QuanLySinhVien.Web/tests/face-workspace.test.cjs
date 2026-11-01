const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');

async function workspace({ missingPhoto = false } = {}) {
    class Element {
        constructor() { this.children = []; this.style = {}; this.dataset = {}; this.value = ''; this.textContent = ''; this.checked = false; this.open = false; }
        set disabled(value) { this._disabled = !!value; } get disabled() { return this._disabled; }
        append(...children) { this.children.push(...children); }
        replaceChildren(...children) { this.children = children; this.textContent = ''; }
        removeAttribute(name) { delete this[name]; }
        setAttribute(name, value) { this[name] = value; }
        addEventListener() {}
        showModal() { this.open = true; } close() { this.open = false; }
    }
    const ids = ['face-profile-workspace','attendance-status','refresh-profiles','profile-search','profile-filter','profiles','profile-total','profile-pending','profile-approved','profile-rejected','profile-review-dialog','review-student','review-metadata','review-status','close-review','identity-photo','identity-photo-empty','identity-photo-source','identity-photo-upload','identity-photo-file','save-identity-photo','registration-photos','review-decision-controls','confirm-photo-comparison','review-note','approve-profile','reject-profile','reload-review','photo-zoom-dialog','photo-zoom','photo-zoom-label','close-photo-zoom'];
    const elements = Object.fromEntries(ids.map(id => [id, new Element()]));
    elements['face-profile-workspace'].dataset = { apiRoot: '/quan-ly/ho-so-khuon-mat/api/' };
    elements['face-profile-workspace'].querySelector = () => ({ value: 'csrf-fixture' });
    elements['profile-filter'].value = 'pending';
    const calls = [];
    const entries = [
        { maSinhVien: 'SV1', hoTen: 'Nguyễn An', daDuyet: false },
        { maSinhVien: 'SV2', hoTen: 'Trần Bình', daDuyet: true },
        { maSinhVien: 'SV3', hoTen: 'Lê Chi', daDuyet: false },
        { maSinhVien: 'SV4', hoTen: 'Lê Dũng', daDuyet: false, biTuChoi: true },
    ];
    const photo = 'data:image/jpeg;base64,fixture';
    const context = {
        document: { getElementById: id => elements[id] ?? null, createElement: () => new Element() },
        window: { confirm: () => true }, URLSearchParams,
        navigator: { mediaDevices: { getUserMedia: () => { throw new Error('Staff photo review must never request a camera'); } } },
        fetch: async (url, options) => {
            const body = options.body ? JSON.parse(options.body) : undefined;
            calls.push({ url, body }); assert.equal(options.headers.RequestVerificationToken, 'csrf-fixture');
            let result;
            if (url.endsWith('/profiles')) result = entries;
            else if (url.includes('/review?')) result = { maSinhVien: 'SV1', hoTen: 'Nguyễn An', daDuyet: false, biTuChoi: false, profileVersion: 'p1', identityPhotoVersion: missingPhoto ? null : 'i1', anhThe: missingPhoto ? null : photo, anhDangKy: [photo, photo, photo] };
            else if (url.includes('/approve?')) { entries[0].daDuyet = true; result = { message: 'Đã duyệt' }; }
            else if (url.includes('/reject?')) { entries[0].biTuChoi = true; result = { message: 'Đã trả hồ sơ' }; }
            else throw new Error('Unexpected endpoint: ' + url);
            return { ok: true, text: async () => JSON.stringify(result) };
        },
    };
    vm.runInNewContext(fs.readFileSync(require.resolve('../wwwroot/js/face-profile-review.js'), 'utf8'), context);
    await new Promise(resolve => setImmediate(resolve));
    const rows = () => elements.profiles.children[0].children[1].children;
    return { elements, calls, rows };
}
test('officer workspace auto-loads and filters stored profiles, with no QR or camera setup', async () => {
    const { elements, calls, rows } = await workspace();
    assert.equal(calls.length, 1); assert.match(calls[0].url, /ho-so-khuon-mat\/api\/profiles$/);
    assert.equal(elements['profile-total'].textContent, 4); assert.equal(elements['profile-pending'].textContent, 2);
    assert.equal(rows().length, 2);
    elements['profile-filter'].value = 'approved'; elements['profile-filter'].onchange();
    assert.equal(rows().length, 1); assert.equal(rows()[0].children[0].textContent, 'SV2');
    elements['profile-filter'].value = 'all'; elements['profile-search'].value = 'lê chi'; elements['profile-search'].oninput();
    assert.equal(rows().length, 1); assert.equal(rows()[0].children[0].textContent, 'SV3');
});
test('review renders existing photos and approves only after staff confirms comparison, without recapture', async () => {
    const { elements, calls, rows } = await workspace();
    await rows()[0].children[4].children[0].onclick();
    assert.equal(elements['profile-review-dialog'].open, true);
    assert.equal(elements['registration-photos'].children.length, 3);
    assert.match(elements['identity-photo'].src, /^data:image\/jpeg;base64,/);
    assert.equal(elements['approve-profile'].disabled, true);
    elements['confirm-photo-comparison'].checked = true; elements['confirm-photo-comparison'].onchange();
    assert.equal(elements['approve-profile'].disabled, false);
    await elements['approve-profile'].onclick();
    const decision = calls.find(call => call.url.includes('/approve?'));
    assert.deepEqual(decision.body, { profileVersion: 'p1', identityPhotoVersion: 'i1', confirmCompared: true, note: '' });
    assert.equal(elements['profile-review-dialog'].open, false);
    assert.equal(calls.some(call => /challenge|guidance/.test(call.url)), false);
});
test('missing official photo blocks approval; staff can return with a reason instead', async () => {
    const { elements, calls, rows } = await workspace({ missingPhoto: true });
    await rows()[0].children[4].children[0].onclick();
    assert.equal(elements['approve-profile'].disabled, true);
    elements['confirm-photo-comparison'].checked = true;
    await elements['approve-profile'].onclick();
    assert.equal(calls.some(call => call.url.includes('/approve?')), false);
    await elements['reject-profile'].onclick();
    assert.equal(calls.some(call => call.url.includes('/reject?')), false);
    elements['review-note'].value = 'Ảnh không khớp hồ sơ'; elements['review-note'].oninput();
    await elements['reject-profile'].onclick();
    assert.equal(calls.find(call => call.url.includes('/reject?')).body.note, 'Ảnh không khớp hồ sơ');
});

