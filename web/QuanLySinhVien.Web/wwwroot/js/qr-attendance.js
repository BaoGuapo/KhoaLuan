(() => {
    'use strict';
    const root = document.getElementById('qr-attendance');
    if (!root) return;
    const $ = id => document.getElementById(id);
    const student = root.dataset.student === 'true';
    let stream, scanning = false, challenge, mode, frames = [], sessionId, qrExpires, sessionExpires, busy = false;
    let poll, scanFrame;
    let liveRunning = false, captureRun = 0, guidanceTimer, poseGate;

    const message = (text, error = false) => { $('attendance-status').textContent = text; $('attendance-status').style.color = error ? '#b42318' : '#087443'; };

    // Vị trí chỉ là thông tin kèm theo: từ chối quyền hay định vị hỏng thì vẫn
    // điểm danh được, backend ghi lại là chưa xác minh.
    function layViTri() {
        if (!navigator.geolocation || !window.isSecureContext) return Promise.resolve(null);
        return new Promise(resolve => {
            navigator.geolocation.getCurrentPosition(
                p => resolve({ viDo: p.coords.latitude, kinhDo: p.coords.longitude, doChinhXacMet: p.coords.accuracy }),
                () => resolve(null),
                { enableHighAccuracy: true, timeout: 8000, maximumAge: 60000 });
        });
    }
    const ghiChuViTri = result => result.ketQuaViTri === 'NgoaiKhuonVien'
        ? ` Lưu ý: vị trí của bạn cách cơ sở khoảng ${result.khoangCachMet} m, giảng viên sẽ thấy ghi chú này.`
        : '';
    async function api(operation, body, params = {}) {
        const query = new URLSearchParams(params);
        const controller = new AbortController();
        const deadline = operation === 'guidance' ? setTimeout(() => controller.abort(), 10000) : null;
        let response;
        try { response = await fetch(`${root.dataset.apiRoot || '/diem-danh/api/'}${operation}?${query}`, {
            method: body === undefined ? 'GET' : 'POST', cache: 'no-store',
            signal: controller.signal,
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': root.querySelector('[name="__RequestVerificationToken"]').value },
            body: body === undefined ? undefined : JSON.stringify(body)
        }); } finally { clearTimeout(deadline); }
        const text = await response.text();
        let value; try { value = text ? JSON.parse(text) : {}; } catch { value = {}; }
        if (!response.ok) throw new Error(value.message || (response.status === 401 ? 'Phiên đăng nhập hết hạn. Hãy đăng nhập lại.' : `Yêu cầu thất bại (${response.status}).`));
        return value;
    }
    function stopLive() { liveRunning = false; captureRun++; clearTimeout(guidanceTimer); poseGate?.pause(); }
    function stopCamera() { stopLive(); scanning = false; cancelAnimationFrame(scanFrame); stream?.getTracks().forEach(x => x.stop()); stream = null; $('attendance-video').srcObject = null; }
    function cancelCapture() { stopCamera(); challenge = null; frames = []; $('captured-photos').replaceChildren(); $('pose-feedback').textContent = ''; $('pose-oval').hidden = true; $('capture-section').hidden = true; }
    async function camera(front) {
        stopCamera();
        if (!window.isSecureContext || !navigator.mediaDevices?.getUserMedia) throw new Error('Camera cần HTTPS hoặc localhost.');
        stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: { ideal: front ? 'user' : 'environment' }, width: { ideal: 960 }, height: { ideal: 720 } }, audio: false });
        $('capture-section').hidden = false;
        const video = $('attendance-video'); video.srcObject = stream; await video.play();
    }
    function instruction() {
        $('capture-instruction').textContent = frames.length === 3 ? 'Đã nhận đủ ba động tác. Kiểm tra ảnh trước khi gửi xác minh.' : [`Bước 1/3: Nhìn thẳng vào camera.`, `Bước 2/3: Quay nhẹ đầu sang ${challenge.direction === 'left' ? 'trái' : 'phải'} của bạn, giữ mặt trong khung.`, 'Bước 3/3: Nhìn thẳng trở lại.'][frames.length];
        $('capture-frame').textContent = frames.length === 3 ? 'Gửi xác minh' : liveRunning ? 'Tạm dừng' : 'Bắt đầu xác thực trực tiếp';
    }
    async function beginCapture(value, type) {
        challenge = value; mode = type; frames = [];
        await camera(true);
        poseGate = new FacePoseGuide.Gate(value.direction);
        $('captured-photos').replaceChildren(); $('pose-feedback').textContent = 'Chỉ nhận ảnh khi đúng động tác và giữ ổn định.'; $('pose-oval').hidden = false;
        $('capture-title').textContent = type === 'enroll' ? 'Đăng ký khuôn mặt' : '2. Xác minh khuôn mặt';
        $('verified-account').textContent = `${value.maSinhVien} · ${value.hoTen}${value.tenMonHoc ? ` · ${value.tenMonHoc} · ${value.maLopHocPhan}` : ''}`;
        $('capture-frame').hidden = false; $('capture-frame').disabled = false; instruction();
        $('capture-section').scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
    function cameraFrame() {
        const video = $('attendance-video');
        if (!video.videoWidth || video.readyState < 2) throw new Error('Camera chưa sẵn sàng.');
        const scale = Math.min(1, 960 / Math.max(video.videoWidth, video.videoHeight));
        const canvas = document.createElement('canvas'); canvas.width = Math.round(video.videoWidth * scale); canvas.height = Math.round(video.videoHeight * scale);
        canvas.getContext('2d').drawImage(video, 0, 0, canvas.width, canvas.height);
        return canvas.toDataURL('image/jpeg', 0.85).split(',')[1];
    }
    async function observe(run) {
        if (!liveRunning || run !== captureRun || !challenge || !stream) return;
        const id = challenge.challengeId;
        try {
            if (Date.now() >= Date.parse(challenge.expiresAtUtc)) throw new Error('Lượt xác minh hết hạn. Hãy bắt đầu lại.');
            const frame = cameraFrame();
            const observation = await api('guidance', { challengeId: id, frame });
            if (run !== captureRun || !liveRunning || id !== challenge?.challengeId) return;
            const result = poseGate.sample(observation, frame);
            $('pose-feedback').textContent = result.message;
            $('pose-feedback').style.color = result.stable || result.accepted ? '#087443' : '#975a16';
            $('pose-oval').style.borderColor = result.stable || result.accepted ? '#26a977' : '#d9a441';
            if (result.accepted) {
                frames = [...poseGate.frames];
                const image = document.createElement('img'); image.src = `data:image/jpeg;base64,${frame}`; image.alt = `Ảnh đã xác thực bước ${frames.length}`; image.style.cssText = 'width:90px;height:100px;object-fit:cover;border:2px solid #26a977;border-radius:12px';
                $('captured-photos').append(image);
                if (result.complete) stopLive();
                instruction();
            }
        } catch (e) {
            if (run !== captureRun) return;
            stopLive(); instruction(); $('pose-feedback').textContent = e.message;
        } finally {
            if (run === captureRun && liveRunning) guidanceTimer = setTimeout(() => observe(run), 650);
        }
    }
    async function capture() {
        if (busy || !challenge || !stream) return;
        if (frames.length < 3) {
            if (liveRunning) { stopLive(); instruction(); $('pose-feedback').textContent = 'Đã tạm dừng.'; return; }
            liveRunning = true; const run = ++captureRun; instruction(); await observe(run); return;
        }
        busy = true; $('capture-frame').disabled = true;
        try {
            if (Date.now() >= Date.parse(challenge.expiresAtUtc)) throw new Error('Lượt xác minh hết hạn. Hãy bắt đầu lại.');
            stopCamera(); message('Đang xác minh người thật và khuôn mặt…');
            const viTri = mode === 'confirm' ? await layViTri() : null;
            const result = await api(mode, { challengeId: challenge.challengeId, frames, consent: true, ...(viTri ? { viTri } : {}), ...(mode === 'enroll' ? { storeReviewPhotosConsent: true } : {}) });
            cancelCapture();
            message((result.message || `Đã ghi nhận có mặt lúc ${new Date(result.thoiGianCheckInUtc).toLocaleTimeString('vi-VN')}.`) + ghiChuViTri(result));
            if (mode === 'enroll') await loadProfile();
            if (mode === 'confirm') await loadHistory();
        } catch (e) { cancelCapture(); message(`${e.message} Hãy bắt đầu lại để lấy lượt xác minh mới.`, true); }
        finally { busy = false; $('capture-frame').disabled = false; }
    }
    async function scanQr() {
        if (busy) return;
        try {
            await camera(false); scanning = true; $('capture-frame').hidden = true;
            $('pose-oval').hidden = true; $('pose-feedback').textContent = ''; $('captured-photos').replaceChildren();
            $('capture-title').textContent = 'Quét QR trên màn hình giảng viên'; $('verified-account').textContent = '';
            $('capture-instruction').textContent = 'Đưa toàn bộ QR vào khung hình.';
            const canvas = document.createElement('canvas'); const context = canvas.getContext('2d', { willReadFrequently: true });
            let last = 0;
            const decode = async now => {
                if (!scanning) return;
                const video = $('attendance-video');
                if (video.videoWidth && now - last > 200) {
                    last = now; const scale = Math.min(1, 800 / video.videoWidth);
                    canvas.width = Math.round(video.videoWidth * scale); canvas.height = Math.round(video.videoHeight * scale);
                    context.drawImage(video, 0, 0, canvas.width, canvas.height);
                    const data = context.getImageData(0, 0, canvas.width, canvas.height);
                    const code = jsQR(data.data, data.width, data.height);
                    if (code) {
                        stopCamera(); busy = true; message('Đang kiểm tra tài khoản và lớp học…');
                        try { const value = await api('scan', { qrToken: code.data }); await beginCapture(value, 'confirm'); message('Tài khoản thuộc lớp. Hãy thực hiện ba bước xác minh.'); }
                        catch (e) { cancelCapture(); message(e.message, true); } finally { busy = false; }
                        return;
                    }
                }
                scanFrame = requestAnimationFrame(decode);
            };
            scanFrame = requestAnimationFrame(decode);
        } catch (e) { cancelCapture(); message(e.message, true); }
    }
    async function loadProfile() {
        const profile = await api('profile');
        $('profile-status').textContent = profile.daDuyet ? 'Hồ sơ đã được duyệt, dùng cho tất cả lớp. Không cần đăng ký hoặc duyệt lại theo môn.' : profile.daDangKy ? 'Đã đăng ký, đang chờ CBQL hoặc Admin xác minh và duyệt một lần. Không cần đăng ký lại.' : 'Bạn chưa đăng ký khuôn mặt. Đăng ký một lần để dùng cho tất cả lớp.';
        $('start-enrollment').disabled = profile.daDangKy;
        $('face-consent').disabled = profile.daDangKy;
    }
    async function loadHistory() {
        const rows = await api('my-results'); $('my-results').replaceChildren();
        for (const row of rows) {
            const p = document.createElement('p'); p.textContent = `${new Date(row.ngayHoc).toLocaleDateString('vi-VN')} · ${row.maLopHocPhan} · ${row.tenMonHoc} · ${row.trangThai}${row.thoiGianCheckInUtc ? ` · ${new Date(row.thoiGianCheckInUtc).toLocaleTimeString('vi-VN')}` : ''}`;
            $('my-results').append(p);
        }
        if (!rows.length) $('my-results').textContent = 'Chưa có kết quả điểm danh.';
    }
    async function loadLessons() {
        const rows = await api('lessons'); $('lesson').replaceChildren();
        for (const row of rows) {
            const option = new Option(`${new Date(row.ngayHoc).toLocaleDateString('vi-VN')} · ${row.maLopHocPhan} · ${row.tenMonHoc}${row.daChot ? ' · Đã chốt' : ''}`, row.maBuoiHoc);
            option.dataset.sessionId = row.sessionId || ''; option.disabled = row.daChot;
            if (row.maBuoiHoc === root.dataset.lesson) option.selected = true;
            $('lesson').append(option);
        }
        if (!rows.length) message('Chưa có buổi học trong khoảng thời gian này. Tạo lịch học trước khi mở phiên.');
    }
    async function updateSession() {
        if (!sessionId) return;
        const value = await api('qr', undefined, { id: sessionId });
        qrExpires = Date.parse(value.qrExpiresAtUtc); sessionExpires = Date.parse(value.sessionExpiresAtUtc);
        $('qr-area').hidden = false; $('qr-image').replaceChildren();
        new QRCode($('qr-image'), { text: value.qrToken, width: 300, height: 300, correctLevel: QRCode.CorrectLevel.M });
        $('session-title').textContent = `${value.maLopHocPhan} · ${value.tenMonHoc} · ${value.maBuoiHoc}`;
        $('close-session').disabled = false;
        const rows = await api('results', undefined, { id: sessionId }); $('attendance-results').replaceChildren();
        for (const row of rows) {
            const tr = document.createElement('tr');
            for (const value of [row.maSinhVien, row.hoTen, row.trangThai, row.thoiGianCheckIn ? new Date(row.thoiGianCheckIn).toLocaleTimeString('vi-VN') : '—']) {
                const td = document.createElement('td'); td.textContent = value; tr.append(td);
            }
            $('attendance-results').append(tr);
        }
    }
    async function resume(id) {
        clearInterval(poll); sessionId = id;
        await updateSession();
        poll = setInterval(() => updateSession().catch(e => { clearInterval(poll); $('qr-area').hidden = true; message(e.message, true); }), 15000);
    }
    async function guarded(action) { if (busy) return; busy = true; try { await action(); } catch (e) { message(e.message, true); } finally { busy = false; } }
    $('capture-frame').onclick = capture; $('cancel-capture').onclick = cancelCapture;
    if (student) {
        if (root.dataset.enrollment === 'true') {
            loadProfile().catch(e => message(e.message, true));
            $('start-enrollment').onclick = () => guarded(async () => {
                if (!$('face-consent').checked) throw new Error('Cần đồng ý xử lý dữ liệu khuôn mặt.');
                await beginCapture(await api('enrollment-challenge', {}), 'enroll');
            });
        } else {
            $('scan-qr').onclick = scanQr;
            $('refresh-history').onclick = () => guarded(loadHistory);
            loadHistory().catch(e => message(e.message, true));
        }
    } else {
        loadLessons().catch(e => message(e.message, true));
        $('open-session').onclick = () => guarded(async () => {
            const minutes = Number($('duration').value); if (!Number.isInteger(minutes) || minutes < 1 || minutes > 30) throw new Error('Thời gian từ 1 đến 30 phút.');
            const value = await api('open', { minutes }, { code: $('lesson').value }); await resume(value.sessionId); await loadLessons(); message('Đã mở phiên điểm danh.');
        });
        $('resume-session').onclick = () => guarded(async () => { const id = $('lesson').selectedOptions[0]?.dataset.sessionId; if (!id) throw new Error('Buổi học chưa có phiên đang mở.'); await resume(id); });
        $('close-session').onclick = () => guarded(async () => { await api('close', {}, { id: sessionId }); clearInterval(poll); sessionId = null; $('qr-area').hidden = true; $('close-session').disabled = true; await loadLessons(); message('Đã đóng phiên.'); });
        setInterval(() => { if (sessionId && qrExpires) $('qr-countdown').textContent = `QR còn ${Math.max(0, Math.ceil((qrExpires - Date.now()) / 1000))} giây · Phiên còn ${Math.max(0, Math.ceil((sessionExpires - Date.now()) / 60000))} phút`; }, 1000);
    }
    window.addEventListener('pagehide', () => { stopCamera(); clearInterval(poll); frames = []; });
    document.addEventListener('visibilitychange', () => { if (document.hidden) cancelCapture(); });
})();



