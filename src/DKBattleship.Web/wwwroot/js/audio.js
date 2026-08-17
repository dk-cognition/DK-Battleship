// Shared Web Audio plumbing: a single AudioContext, one master gain and the
// helpers every synthesized voice needs. No audio assets are used anywhere.

let ctx = null;
let master = null;
let noise = null;

/** Creates the context on first use and resumes it when the browser suspended it. */
export function ensureContext() {
    if (!ctx) {
        const Ctor = window.AudioContext || window.webkitAudioContext;
        if (!Ctor) {
            return null;
        }

        ctx = new Ctor();
        master = ctx.createGain();
        master.gain.value = 0.5;
        master.connect(ctx.destination);
    }

    if (ctx.state === "suspended") {
        ctx.resume().catch(() => { });
    }

    return ctx;
}

export function context() {
    return ctx;
}

export function masterGain() {
    return master;
}

/** White noise buffer, built once and shared by every noise-based voice. */
export function noiseBuffer() {
    if (!noise) {
        const length = Math.floor(ctx.sampleRate * 1.2);
        noise = ctx.createBuffer(1, length, ctx.sampleRate);
        const data = noise.getChannelData(0);
        for (let i = 0; i < length; i++) {
            data[i] = Math.random() * 2 - 1;
        }
    }

    return noise;
}

export function noiseSource(start, duration) {
    const source = ctx.createBufferSource();
    source.buffer = noiseBuffer();
    source.loop = true;
    source.start(start, Math.random() * 0.5, duration);
    return source;
}

/** Percussive attack/decay envelope, the shape used by the one-shot effects. */
export function envelope(start, peak, attack, decay) {
    const gain = ctx.createGain();
    gain.gain.setValueAtTime(0.0001, start);
    gain.gain.linearRampToValueAtTime(peak, start + attack);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + attack + decay);
    return gain;
}

export function midiToFrequency(midi) {
    return 440 * Math.pow(2, (midi - 69) / 12);
}

/** Runs `start` on the next user gesture when autoplay policy blocks audio now. */
export function onFirstGesture(start) {
    const events = ["pointerdown", "keydown", "touchstart"];
    const handler = () => {
        events.forEach(name => window.removeEventListener(name, handler));
        start();
    };

    events.forEach(name => window.addEventListener(name, handler, { once: true }));
}
