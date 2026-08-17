// Tiny synthesized sound layer: no audio assets, everything is built with Web Audio nodes.
// Recipes are keyed by name so new sounds ("win", "lose", ...) are a one-entry addition.

const MUTE_KEY = "battlegolf.muted";

let ctx = null;
let master = null;
let noise = null;
let muted = readMuted();

function readMuted() {
    try {
        return localStorage.getItem(MUTE_KEY) === "true";
    } catch {
        return false;
    }
}

function ensureContext() {
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

/** White noise buffer, built once and shared by every noise-based recipe. */
function noiseBuffer() {
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

function noiseSource(start, duration) {
    const source = ctx.createBufferSource();
    source.buffer = noiseBuffer();
    source.loop = true;
    source.start(start, Math.random() * 0.5, duration);
    return source;
}

/** Tears the whole chain down once `source` finishes, so nodes cannot accumulate. */
function releaseWhenDone(source, ...nodes) {
    source.onended = () => {
        source.disconnect();
        for (const node of nodes) {
            node.disconnect();
        }
    };
}

function envelope(start, peak, attack, decay) {
    const gain = ctx.createGain();
    gain.gain.setValueAtTime(0.0001, start);
    gain.gain.linearRampToValueAtTime(peak, start + attack);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + attack + decay);
    return gain;
}

// A golf strike: a very short, bright noise transient with an almost instant decay,
// plus a fast falling sine "ping" for the compressed-ball ring.
function swing(now) {
    const body = noiseSource(now, 0.12);
    const bandpass = ctx.createBiquadFilter();
    bandpass.type = "bandpass";
    bandpass.frequency.setValueAtTime(2600, now);
    bandpass.frequency.exponentialRampToValueAtTime(1100, now + 0.07);
    bandpass.Q.value = 0.8;

    const highpass = ctx.createBiquadFilter();
    highpass.type = "highpass";
    highpass.frequency.value = 900;

    const noiseGain = envelope(now, 0.9, 0.001, 0.06);
    body.connect(bandpass).connect(highpass).connect(noiseGain).connect(master);
    body.stop(now + 0.12);
    releaseWhenDone(body, bandpass, highpass, noiseGain);

    const ping = ctx.createOscillator();
    ping.type = "triangle";
    ping.frequency.setValueAtTime(1900, now);
    ping.frequency.exponentialRampToValueAtTime(520, now + 0.05);
    const pingGain = envelope(now, 0.28, 0.001, 0.05);
    ping.connect(pingGain).connect(master);
    ping.start(now);
    ping.stop(now + 0.09);
    releaseWhenDone(ping, pingGain);
}

// A hit: percussive low sine sweep (the thump) under a lowpassed noise burst (the crack).
function bang(now) {
    const boom = ctx.createOscillator();
    boom.type = "sine";
    boom.frequency.setValueAtTime(180, now);
    boom.frequency.exponentialRampToValueAtTime(38, now + 0.3);
    const boomGain = envelope(now, 0.85, 0.004, 0.34);
    boom.connect(boomGain).connect(master);
    boom.start(now);
    boom.stop(now + 0.4);
    releaseWhenDone(boom, boomGain);

    const crack = noiseSource(now, 0.3);
    const lowpass = ctx.createBiquadFilter();
    lowpass.type = "lowpass";
    lowpass.frequency.setValueAtTime(1800, now);
    lowpass.frequency.exponentialRampToValueAtTime(320, now + 0.22);
    lowpass.Q.value = 1.4;
    const crackGain = envelope(now, 0.55, 0.002, 0.24);
    crack.connect(lowpass).connect(crackGain).connect(master);
    crack.stop(now + 0.3);
    releaseWhenDone(crack, lowpass, crackGain);
}

// A splash: filtered noise whose bandpass sweeps up fast (the entry) then falls away
// (the spray settling), with a low "gloop" for the displaced water.
function splash(now) {
    const water = noiseSource(now, 0.8);
    const bandpass = ctx.createBiquadFilter();
    bandpass.type = "bandpass";
    bandpass.Q.value = 1.1;
    bandpass.frequency.setValueAtTime(420, now);
    bandpass.frequency.exponentialRampToValueAtTime(3200, now + 0.09);
    bandpass.frequency.exponentialRampToValueAtTime(300, now + 0.6);

    const gain = ctx.createGain();
    gain.gain.setValueAtTime(0.0001, now);
    gain.gain.linearRampToValueAtTime(0.75, now + 0.012);
    gain.gain.exponentialRampToValueAtTime(0.22, now + 0.16);
    gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.62);
    water.connect(bandpass).connect(gain).connect(master);
    water.stop(now + 0.7);
    releaseWhenDone(water, bandpass, gain);

    const gloop = ctx.createOscillator();
    gloop.type = "sine";
    gloop.frequency.setValueAtTime(520, now + 0.02);
    gloop.frequency.exponentialRampToValueAtTime(130, now + 0.22);
    const gloopGain = envelope(now + 0.02, 0.3, 0.006, 0.24);
    gloop.connect(gloopGain).connect(master);
    gloop.start(now + 0.02);
    gloop.stop(now + 0.32);
    releaseWhenDone(gloop, gloopGain);
}

const recipes = {
    swing,
    bang,
    splash
};

// Seconds a layered sound trails the start of the group it is played with.
const layerOffsets = {
    bang: 0.05,
    splash: 0.06
};

/** Creates/resumes the AudioContext. Must be called from a user gesture. */
export function unlock() {
    try {
        ensureContext();
    } catch {
        // Audio is optional: never let it break the game.
    }
}

/**
 * Plays one or more recipes by name. Every name after the first is layered onto the same
 * start time using the offsets baked into `layers`, so layering never drifts with interop latency.
 */
export function play(...names) {
    if (muted) {
        return;
    }

    try {
        if (!ensureContext()) {
            return;
        }

        const start = ctx.currentTime + 0.005;
        for (const name of names) {
            const recipe = recipes[name];
            if (recipe) {
                recipe(start + (layerOffsets[name] ?? 0));
            }
        }
    } catch {
        // Blocked or unsupported audio degrades silently.
    }
}

export function isMuted() {
    return muted;
}

export function setMuted(value) {
    muted = !!value;
    try {
        localStorage.setItem(MUTE_KEY, muted ? "true" : "false");
    } catch {
        // Persistence is best effort.
    }

    return muted;
}
