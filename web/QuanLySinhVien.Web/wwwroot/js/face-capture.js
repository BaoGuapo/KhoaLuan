(() => {
    const form = document.querySelector('[data-face-form]');
    const video = document.querySelector('[data-face-video]');
    const startButton = document.querySelector('[data-camera-start]');
    const captureButton = document.querySelector('[data-face-capture]');
    const descriptorInput = document.querySelector('[data-face-descriptors]');
    const message = document.querySelector('[data-face-message]');
    const frame = video?.closest?.('[data-camera-frame]');
    const placeholder = frame?.querySelector('[data-camera-placeholder]');
    const autoCapture = form?.hasAttribute('data-auto-face-capture') ?? false;
    const autoStart = autoCapture && form.dataset.autoStart !== 'false';
    if (!form || !video || !startButton || (!autoCapture && !captureButton) || !descriptorInput || !message) return;

    const modelUrl = '/models/face';
    const mobileNetUrl = '/models/face/mobilenetv2_mcp.onnx';
    let stream;
    let modelsLoaded = false;
    let recognitionSession;
    let scanning = false;

    const pause = milliseconds => new Promise(resolve => window.setTimeout(resolve, milliseconds));
    const retryableError = text => Object.assign(new Error(text), { retryable: true });

    const setMessage = (text, state = '') => {
        message.textContent = text;
        message.dataset.state = state;
        if (state === 'error' && !stream && frame) frame.dataset.state = 'error';
    };

    const setCameraState = (state, caption) => {
        if (frame) frame.dataset.state = state;
        if (placeholder) {
            placeholder.hidden = state === 'ready';
            const label = placeholder.querySelector('span:last-child');
            if (label && caption) label.textContent = caption;
        }
    };

    const readableError = error => {
        if (error?.name === 'NotAllowedError') return 'Camera bị từ chối. Hãy cấp quyền camera trong trình duyệt rồi thử lại.';
        if (error?.name === 'NotFoundError') return 'Không tìm thấy camera. Hãy kiểm tra thiết bị rồi thử lại.';
        if (error?.name === 'NotReadableError') return 'Camera đang được ứng dụng khác sử dụng. Hãy đóng ứng dụng đó rồi thử lại.';
        return error?.message || 'Không thể mở camera. Hãy thử lại.';
    };

    const stopCamera = () => {
        stream?.getTracks().forEach(track => track.stop());
        stream = undefined;
        video.srcObject = null;
        setCameraState('idle', 'Camera chưa bật');
    };

    const waitForLibraries = async () => {
        for (let attempt = 0; attempt < 50; attempt += 1) {
            if (window.faceapi && window.ort) return;
            await pause(100);
        }
        throw new Error('Không tải được thư viện nhận diện khuôn mặt. Hãy tải lại trang.');
    };

    const loadModels = async () => {
        if (modelsLoaded) return;
        setMessage('Đang tải mô hình nhận diện…');
        setCameraState('loading', 'Đang tải mô hình nhận diện');
        await waitForLibraries();
        await Promise.all([
            faceapi.nets.tinyFaceDetector.loadFromUri(modelUrl),
            faceapi.nets.faceLandmark68TinyNet.loadFromUri(modelUrl)
        ]);
        ort.env.wasm.wasmPaths = '/vendor/onnxruntime/';
        ort.env.wasm.numThreads = 1;
        recognitionSession = await ort.InferenceSession.create(mobileNetUrl, {
            executionProviders: ['wasm']
        });
        modelsLoaded = true;
    };

    const averagePoint = points => ({
        x: points.reduce((sum, point) => sum + point.x, 0) / points.length,
        y: points.reduce((sum, point) => sum + point.y, 0) / points.length
    });

    const alignedFace = landmarks => {
        const positions = landmarks.positions;
        const source = [
            averagePoint(positions.slice(36, 42)),
            averagePoint(positions.slice(42, 48)),
            positions[30],
            positions[48],
            positions[54]
        ];
        const target = [
            { x: 38.2946, y: 51.6963 },
            { x: 73.5318, y: 51.5014 },
            { x: 56.0252, y: 71.7366 },
            { x: 41.5493, y: 92.3655 },
            { x: 70.7299, y: 92.2041 }
        ];

        const sourceCenter = averagePoint(source);
        const targetCenter = averagePoint(target);
        let denominator = 0;
        let real = 0;
        let imaginary = 0;
        for (let index = 0; index < source.length; index += 1) {
            const x = source[index].x - sourceCenter.x;
            const y = source[index].y - sourceCenter.y;
            const u = target[index].x - targetCenter.x;
            const v = target[index].y - targetCenter.y;
            denominator += x * x + y * y;
            real += x * u + y * v;
            imaginary += x * v - y * u;
        }
        if (denominator < 1) throw new Error('Không thể căn chỉnh khuôn mặt. Hãy thử lại.');

        const a = real / denominator;
        const b = imaginary / denominator;
        const translateX = targetCenter.x - a * sourceCenter.x + b * sourceCenter.y;
        const translateY = targetCenter.y - b * sourceCenter.x - a * sourceCenter.y;
        const canvas = document.createElement('canvas');
        canvas.width = 112;
        canvas.height = 112;
        const context = canvas.getContext('2d', { willReadFrequently: true });
        context.setTransform(a, b, -b, a, translateX, translateY);
        context.drawImage(video, 0, 0, video.videoWidth, video.videoHeight);
        return context.getImageData(0, 0, 112, 112).data;
    };

    const extractDescriptor = async imageData => {
        const channelSize = 112 * 112;
        const input = new Float32Array(channelSize * 3);
        for (let pixel = 0; pixel < channelSize; pixel += 1) {
            const rgbaIndex = pixel * 4;
            for (let channel = 0; channel < 3; channel += 1) {
                input[channel * channelSize + pixel] =
                    (imageData[rgbaIndex + channel] - 127.5) / 127.5;
            }
        }

        const tensor = new ort.Tensor('float32', input, [1, 3, 112, 112]);
        const result = await recognitionSession.run({ input: tensor });
        const raw = Array.from(result.output.data);
        if (raw.length !== 512) throw new Error('Mô hình MobileNetV2 trả về vector không hợp lệ.');
        const norm = Math.hypot(...raw);
        if (!Number.isFinite(norm) || norm < 0.001) {
            throw new Error('Không thể chuẩn hóa kết quả nhận diện.');
        }
        return raw.map(value => value / norm);
    };

    const captureSamples = async () => {
        const samples = [];
        for (let index = 0; index < 3; index += 1) {
            setMessage(`Đang lấy mẫu ${index + 1}/3. Nhìn thẳng vào camera…`);
            const detections = await faceapi
                .detectAllFaces(video, new faceapi.TinyFaceDetectorOptions({ inputSize: 320, scoreThreshold: 0.7 }))
                .withFaceLandmarks(true);

            if (detections.length !== 1) {
                throw retryableError(detections.length === 0
                    ? 'Chưa thấy khuôn mặt. Hãy nhìn thẳng và tăng ánh sáng.'
                    : 'Chỉ để một khuôn mặt trong khung hình.');
            }
            if (detections[0].detection.box.width < 110) {
                throw retryableError('Khuôn mặt còn nhỏ. Hãy tiến gần camera hơn.');
            }
            samples.push(await extractDescriptor(alignedFace(detections[0].landmarks)));
            if (index < 2) await pause(650);
        }
        return samples;
    };

    const submitSamples = samples => {
        descriptorInput.value = JSON.stringify(samples);
        setMessage(autoCapture
            ? 'Đã quét xong. Đang xác thực và đăng nhập…'
            : 'Đã chuẩn hóa ba mẫu. Đang gửi kết quả an toàn đến hệ thống…', 'ready');
        stopCamera();
        form.requestSubmit();
    };

    const scanAutomatically = async () => {
        if (scanning) return;
        scanning = true;
        const deadline = Date.now() + 4 * 60 * 1000;
        try {
            while (stream && Date.now() < deadline) {
                try {
                    submitSamples(await captureSamples());
                    return;
                } catch (error) {
                    if (!stream) return;
                    if (!error?.retryable) throw error;
                    setMessage(error.message, 'hint');
                    await pause(900);
                }
            }
            stopCamera();
            setMessage('Phiên quét đã hết thời gian. Vui lòng đăng nhập lại.', 'error');
            startButton.hidden = true;
        } catch (error) {
            stopCamera();
            startButton.hidden = false;
            startButton.disabled = false;
            setMessage(readableError(error), 'error');
        } finally {
            scanning = false;
        }
    };

    const startCamera = async () => {
        if (scanning) return;
        startButton.disabled = true;
        setCameraState('loading', 'Đang chuẩn bị camera');
        try {
            await loadModels();
            if (!navigator.mediaDevices?.getUserMedia) {
                throw new Error('Trình duyệt không hỗ trợ camera trên trang này. Hãy dùng HTTPS và cấp quyền camera.');
            }
            stopCamera();
            stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: 'user', width: { ideal: 720 }, height: { ideal: 540 } },
                audio: false
            });
            video.srcObject = stream;
            await video.play();
            setCameraState('ready');
            setMessage(autoCapture
                ? 'Camera đã sẵn sàng. Hệ thống đang tự quét khuôn mặt…'
                : 'Camera đã sẵn sàng. Giữ khuôn mặt trong khung.', 'ready');
            if (autoCapture) {
                startButton.hidden = true;
                void scanAutomatically();
            } else {
                captureButton.disabled = false;
            }
        } catch (error) {
            stopCamera();
            startButton.hidden = false;
            startButton.disabled = false;
            setMessage(readableError(error), 'error');
        }
    };

    startButton.addEventListener('click', startCamera);
    captureButton?.addEventListener('click', async () => {
        captureButton.disabled = true;
        startButton.disabled = true;
        try {
            submitSamples(await captureSamples());
        } catch (error) {
            captureButton.disabled = !stream;
            startButton.disabled = false;
            setMessage(readableError(error), 'error');
        }
    });

    window.addEventListener('pagehide', stopCamera);
    if (autoStart) void startCamera();
})();
