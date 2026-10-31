const test = require('node:test');
const assert = require('node:assert/strict');
const { Gate } = require('../wwwroot/js/face-pose-guide.js');
const observation = pose => ({ usable: true, pose, minimumTurn: .12 });
test('wrong/insufficient turn cannot advance, exact validated frames are retained', () => {
    const gate = new Gate('right');
    for (let i = 0; i < 3; i++) gate.sample(observation(0), `front-${i}`);
    assert.equal(gate.frames.length, 1);
    for (let i = 0; i < 8; i++) assert.match(gate.sample(observation(.20), `wrong-${i}`).message, /ngược hướng/);
    assert.equal(gate.frames.length, 1);
    for (let i = 0; i < 3; i++) gate.sample(observation(-.18), `turn-${i}`);
    assert.equal(gate.frames.length, 2);
    assert.match(gate.sample(observation(-.18), 'not-returned').message, /chính diện/);
    for (let i = 0; i < 3; i++) gate.sample(observation(.01), `back-${i}`);
    assert.deepEqual(gate.frames, ['front-2', 'turn-2', 'back-2']);
});
test('missing face, unstable movement and pause break the stable sequence', () => {
    const gate = new Gate('left');
    gate.sample(observation(0), 'a'); gate.sample(observation(.10), 'b');
    assert.equal(gate.stable, 1);
    gate.sample({ usable: false, message: 'No face' }, 'c');
    gate.sample(observation(0), 'd'); gate.sample(observation(0), 'e');
    assert.equal(gate.frames.length, 0); gate.pause();
    assert.equal(gate.stable, 0);
});
test('duplicate accepted frame cannot advance even with valid reported pose', () => {
    const gate = new Gate('left');
    for (let i = 0; i < 3; i++) gate.sample(observation(0), 'same');
    assert.equal(gate.frames.length, 1);
    for (let i = 0; i < 5; i++) assert.match(gate.sample(observation(.20), 'same').message, /ảnh cũ/);
    assert.equal(gate.frames.length, 1);
});
