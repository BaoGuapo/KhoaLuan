(function (root) {
    'use strict';
    class Gate {
        constructor(direction) { this.direction = direction; this.frames = []; this.baseline = null; this.last = null; this.stable = 0; }
        pause() { this.stable = 0; this.last = null; }
        sample(observation, frame) {
            const fail = message => { this.pause(); return { accepted: false, message, stable: 0 }; };
            if (this.frames.length === 3) return fail('Đã nhận đủ ba động tác.');
            if (this.frames.includes(frame)) return fail('Camera trả lại ảnh cũ. Cử động nhẹ và kiểm tra camera trực tiếp.');
            const pose = observation.pose;
            if (!observation.usable || !Number.isFinite(pose)) return fail(observation.message || 'Đưa mặt vào khung oval.');
            const step = this.frames.length;
            if (step === 0 && Math.abs(pose) > .15) return fail('Nhìn thẳng vào camera, chưa quay đầu.');
            if (step === 1) {
                const turn = (pose - this.baseline) * (this.direction === 'left' ? 1 : -1);
                const side = this.direction === 'left' ? 'trái' : 'phải';
                if (turn < -.04) return fail(`Bạn đang quay ngược hướng. Quay sang ${side} của bạn.`);
                if (turn < observation.minimumTurn + .025) return fail(`Quay thêm một chút sang ${side} của bạn.`);
            }
            if (step === 2 && (Math.abs(pose - this.baseline) > .10 || Math.abs(pose) > .25)) return fail('Quay mặt về chính diện như ảnh đầu.');
            this.stable = this.last !== null && Math.abs(pose - this.last) < .045 ? this.stable + 1 : 1;
            this.last = pose;
            if (this.stable < 3) return { accepted: false, stable: this.stable, message: 'Đúng động tác. Giữ yên một chút…' };
            if (step === 0) this.baseline = pose;
            this.frames.push(frame); this.pause();
            return { accepted: true, stable: 0, complete: this.frames.length === 3, message: this.frames.length === 3 ? 'Đã nhận đủ ba động tác. Xem lại ảnh trước khi gửi.' : 'Đã nhận đúng động tác. Làm theo hướng dẫn tiếp theo.' };
        }
    }
    if (typeof module === 'object' && module.exports) module.exports = { Gate };
    else root.FacePoseGuide = { Gate };
})(typeof globalThis === 'object' ? globalThis : this);
