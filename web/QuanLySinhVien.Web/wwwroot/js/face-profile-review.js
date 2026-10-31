(() => {
    'use strict';
    const root = document.getElementById('face-profile-workspace');
    if (!root) return;
    const $ = id => document.getElementById(id);
    let rows = [], review = null, busy = false;
    const pending = row => !row.daDuyet && !row.biTuChoi;
    const message = (text, error = false) => { $('attendance-status').textContent = text; $('attendance-status').style.color = error ? '#b42318' : '#087443'; };
    async function api(operation, body, code) {
        const response = await fetch(`${root.dataset.apiRoot}${operation}${code ? '?code=' + encodeURIComponent(code) : ''}`, {
            method: body === undefined ? 'GET' : 'POST', cache: 'no-store',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': root.querySelector('[name="__RequestVerificationToken"]').value },
            body: body === undefined ? undefined : JSON.stringify(body)
        });
        const text = await response.text();
        let value; try { value = text ? JSON.parse(text) : {}; } catch { throw new Error('Máy chủ trả về dữ liệu không hợp lệ.'); }
        if (!response.ok) { const error = new Error(value.message || `Yêu cầu thất bại (${response.status}).`); error.code = value.code; throw error; }
        return value;
    }
    async function guarded(action) {
        if (busy) return;
        busy = true; updateActions();
        try { await action(); }
        catch (e) { message(e.message, true); if (e.code === 'ReviewChanged') $('confirm-photo-comparison').checked = false; if ($('profile-review-dialog').open) $('review-status').textContent = e.message; }
        finally { busy = false; updateActions(); }
    }
    function hasEvidence() { return review?.anhThe && review.anhDangKy?.length === 3; }
    function enableZoom(image, label) {
        image.style.cursor = 'zoom-in'; image.tabIndex = 0; image.setAttribute('role', 'button'); image.setAttribute('aria-label', 'Xem lớn: ' + label);
        const show = () => { if (!image.src) return; $('photo-zoom').src = image.src; $('photo-zoom-label').textContent = label; $('photo-zoom-dialog').showModal(); };
        image.onclick = show; image.onkeydown = event => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); show(); } };
    }
    function updateActions() {
        $('approve-profile').disabled = busy || !review || !pending(review) || !hasEvidence() || !$('confirm-photo-comparison').checked;
        $('reject-profile').disabled = busy || !review || !pending(review) || !$('review-note').value.trim();
        $('save-identity-photo').disabled = busy || review?.daDuyet;
        $('close-review').disabled = busy;
        $('reload-review').disabled = busy;
    }
    async function loadProfiles() { rows = await api('profiles'); renderRows(); message(`Đã tải ${rows.length} hồ sơ sinh viên.`); }
    function renderRows() {
        const query = $('profile-search').value.trim().toLocaleLowerCase('vi-VN');
        const state = $('profile-filter').value;
        const visible = rows.filter(row => (state === 'all' || (state === 'pending' ? pending(row) : state === 'approved' ? row.daDuyet : row.biTuChoi))
            && `${row.maSinhVien} ${row.hoTen}`.toLocaleLowerCase('vi-VN').includes(query));
        $('profile-total').textContent = rows.length; $('profile-pending').textContent = rows.filter(pending).length;
        $('profile-approved').textContent = rows.filter(row => row.daDuyet).length; $('profile-rejected').textContent = rows.filter(row => row.biTuChoi).length;
        $('profiles').replaceChildren();
        if (!visible.length) { $('profiles').textContent = rows.length ? 'Không có hồ sơ phù hợp với bộ lọc.' : 'Chưa có hồ sơ đăng ký. Sinh viên cần hoàn tất đăng ký khuôn mặt trước.'; return; }
        const table = document.createElement('table'), head = document.createElement('thead'), header = document.createElement('tr'), body = document.createElement('tbody');
        for (const label of ['Mã sinh viên', 'Họ tên', 'Trạng thái', 'Ngày đăng ký', 'Thao tác']) { const th = document.createElement('th'); th.scope = 'col'; th.textContent = label; header.append(th); }
        head.append(header); table.append(head, body); $('profiles').append(table);
        for (const row of visible) {
            const tr = document.createElement('tr');
            for (const value of [row.maSinhVien, row.hoTen, row.daDuyet ? 'Đã duyệt' : row.biTuChoi ? 'Yêu cầu đăng ký lại' : 'Chờ duyệt', row.thoiGianCapNhat ? new Date(row.thoiGianCapNhat).toLocaleString('vi-VN') : '—']) { const td = document.createElement('td'); td.textContent = value; tr.append(td); }
            const actions = document.createElement('td'); const button = document.createElement('button'); button.type = 'button'; button.className = 'button secondary'; button.textContent = 'Đối chiếu ảnh'; button.onclick = () => guarded(() => openReview(row.maSinhVien)); actions.append(button);
            if (row.daDuyet) { const revoke = document.createElement('button'); revoke.type = 'button'; revoke.className = 'button secondary'; revoke.textContent = 'Hủy hiệu lực'; revoke.onclick = () => guarded(async () => { if (!window.confirm(`Hủy hồ sơ dùng chung của ${row.maSinhVien}? Sinh viên phải đăng ký lại cho việc điểm danh ở tất cả lớp.`)) return; await api('revoke', {}, row.maSinhVien); await loadProfiles(); }); actions.append(revoke); }
            tr.append(actions); body.append(tr);
        }
    }
    async function openReview(code) {
        review = await api('review', undefined, code);
        $('review-student').textContent = `${review.maSinhVien} · ${review.hoTen}`;
        $('review-metadata').textContent = `${review.lopHoc || ''} · ${review.daDuyet ? 'Đã duyệt bởi ' + review.nguoiDuyet : review.biTuChoi ? 'Đã yêu cầu đăng ký lại' : 'Chờ duyệt'}`;
        $('confirm-photo-comparison').checked = false; $('review-note').value = '';
        const identity = $('identity-photo'); identity.hidden = !review.anhThe;
        if (review.anhThe) identity.src = review.anhThe; else identity.removeAttribute('src');
        enableZoom(identity, 'Ảnh thẻ từ hồ sơ sinh viên');
        $('identity-photo-empty').hidden = !!review.anhThe;
        $('identity-photo-source').textContent = review.anhTheCapNhatBoi ? `Cập nhật từ hồ sơ gốc bởi ${review.anhTheCapNhatBoi} · ${new Date(review.anhTheCapNhatLucUtc).toLocaleString('vi-VN')}` : '';
        $('identity-photo-upload').hidden = review.daDuyet;
        $('identity-photo-file').value = '';
        $('registration-photos').replaceChildren();
        for (const [index, data] of (review.anhDangKy || []).entries()) {
            const figure = document.createElement('figure'), image = document.createElement('img'), caption = document.createElement('figcaption');
            figure.style.margin = '0'; image.src = data; image.alt = `Ảnh đăng ký bước ${index + 1}`; image.style.cssText = 'width:100%;height:240px;object-fit:contain;background:#f4f7fa;border-radius:10px';
            caption.textContent = ['Nhìn thẳng', 'Quay đầu', 'Nhìn thẳng lại'][index]; figure.append(image, caption); $('registration-photos').append(figure);
            enableZoom(image, caption.textContent);
        }
        $('review-decision-controls').hidden = !pending(review);
        $('review-status').textContent = review.biTuChoi ? `Lý do trả hồ sơ: ${review.lyDoTuChoi}` : hasEvidence() ? 'Đối chiếu hai nguồn ảnh trước khi quyết định.' : 'Thiếu ảnh thẻ hoặc ảnh đăng ký. Cập nhật ảnh thẻ từ hồ sơ gốc; nếu hồ sơ cũ thiếu ảnh chụp, yêu cầu sinh viên đăng ký lại.';
        if (!$('profile-review-dialog').open) $('profile-review-dialog').showModal();
        updateActions();
    }
    async function decide(approve) {
        if (!review || !pending(review)) return;
        const note = $('review-note').value.trim();
        if (approve && (!hasEvidence() || !$('confirm-photo-comparison').checked)) throw new Error('Cần đủ ảnh và xác nhận đã đối chiếu đúng sinh viên.');
        if (!approve && !note) throw new Error('Nhập lý do yêu cầu đăng ký lại.');
        const result = await api(approve ? 'approve' : 'reject', { profileVersion: review.profileVersion, identityPhotoVersion: review.identityPhotoVersion, confirmCompared: $('confirm-photo-comparison').checked, note }, review.maSinhVien);
        $('profile-review-dialog').close(); review = null; await loadProfiles(); message(result.message);
    }
    $('save-identity-photo').onclick = () => guarded(async () => {
        const file = $('identity-photo-file').files?.[0];
        if (!file || !['image/jpeg', 'image/png'].includes(file.type) || file.size > 1050000) throw new Error('Chọn ảnh thẻ JPEG/PNG tối đa 1 MB từ hồ sơ gốc.');
        const data = await new Promise((resolve, reject) => { const reader = new FileReader(); reader.onload = () => resolve(reader.result); reader.onerror = () => reject(new Error('Không đọc được ảnh thẻ.')); reader.readAsDataURL(file); });
        await api('identity-photo', { frame: data.split(',')[1] }, review.maSinhVien); await openReview(review.maSinhVien);
    });
    $('approve-profile').onclick = () => guarded(() => decide(true)); $('reject-profile').onclick = () => guarded(() => decide(false));
    $('reload-review').onclick = () => guarded(() => openReview(review.maSinhVien));
    $('confirm-photo-comparison').onchange = updateActions; $('review-note').oninput = updateActions;
    $('close-review').onclick = () => { if (!busy) { $('profile-review-dialog').close(); review = null; } };
    $('close-photo-zoom').onclick = () => { $('photo-zoom-dialog').close(); $('photo-zoom').removeAttribute('src'); };
    $('profile-review-dialog').addEventListener('cancel', event => { if (busy) event.preventDefault(); else review = null; });
    $('refresh-profiles').onclick = () => guarded(loadProfiles); $('profile-search').oninput = renderRows; $('profile-filter').onchange = renderRows;
    guarded(loadProfiles);
})();
