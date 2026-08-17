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

    const ping = ctx.createOscillator();
    ping.type = "triangle";
    ping.frequency.setValueAtTime(1900, now);
    ping.frequency.exponentialRampToValueAtTime(520, now + 0.05);
    const pingGain = envelope(now, 0.28, 0.001, 0.05);
    ping.connect(pingGain).connect(master);
    ping.start(now);
    ping.stop(now + 0.09);
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

    const crack = noiseSource(now, 0.3);
    const lowpass = ctx.createBiquadFilter();
    lowpass.type = "lowpass";
    lowpass.frequency.setValueAtTime(1800, now);
    lowpass.frequency.exponentialRampToValueAtTime(320, now + 0.22);
    lowpass.Q.value = 1.4;
    const crackGain = envelope(now, 0.55, 0.002, 0.24);
    crack.connect(lowpass).connect(crackGain).connect(master);
    crack.stop(now + 0.3);
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

    const gloop = ctx.createOscillator();
    gloop.type = "sine";
    gloop.frequency.setValueAtTime(520, now + 0.02);
    gloop.frequency.exponentialRampToValueAtTime(130, now + 0.22);
    const gloopGain = envelope(now + 0.02, 0.3, 0.006, 0.24);
    gloop.connect(gloopGain).connect(master);
    gloop.start(now + 0.02);
    gloop.stop(now + 0.32);
}

// A win: a bright rising arpeggio of plucked tones over a crowd-ish noise swell.
function fanfare(now) {
    const notes = [523.25, 659.25, 783.99, 1046.5, 1318.5];
    notes.forEach((frequency, index) => {
        const start = now + index * 0.11;

        const tone = ctx.createOscillator();
        tone.type = "triangle";
        tone.frequency.setValueAtTime(frequency, start);
        const toneGain = envelope(start, 0.32, 0.008, 0.34);
        tone.connect(toneGain).connect(master);
        tone.start(start);
        tone.stop(start + 0.4);

        const shine = ctx.createOscillator();
        shine.type = "sine";
        shine.frequency.setValueAtTime(frequency * 2, start);
        const shineGain = envelope(start, 0.1, 0.006, 0.2);
        shine.connect(shineGain).connect(master);
        shine.start(start);
        shine.stop(start + 0.26);
    });

    const crowd = noiseSource(now, 1.2);
    const bandpass = ctx.createBiquadFilter();
    bandpass.type = "bandpass";
    bandpass.Q.value = 0.7;
    bandpass.frequency.setValueAtTime(700, now);
    bandpass.frequency.exponentialRampToValueAtTime(2200, now + 0.5);
    bandpass.frequency.exponentialRampToValueAtTime(900, now + 1.1);

    const crowdGain = ctx.createGain();
    crowdGain.gain.setValueAtTime(0.0001, now);
    crowdGain.gain.linearRampToValueAtTime(0.2, now + 0.35);
    crowdGain.gain.exponentialRampToValueAtTime(0.0001, now + 1.15);
    crowd.connect(bandpass).connect(crowdGain).connect(master);
    crowd.stop(now + 1.2);
}

// A deflated gallery groan: a cluster of slightly detuned sawtooth voices sliding down
// through vowel-ish formant bandpasses, with a long sighing decay.
function lose(now) {
    const voices = [
        { start: 232, end: 138, detune: -9, level: 0.48 },
        { start: 228, end: 132, detune: 6, level: 0.42 },
        { start: 175, end: 98, detune: -14, level: 0.35 },
        { start: 348, end: 196, detune: 11, level: 0.19 }
    ];

    // "aw" formants, drifting toward a darker "uh" as the groan sags.
    const formants = [
        { start: 730, end: 570, q: 5, level: 1 },
        { start: 1090, end: 840, q: 7, level: 0.5 },
        { start: 2440, end: 2200, q: 9, level: 0.14 }
    ];

    const duration = 1.7;
    const sag = ctx.createGain();
    sag.gain.setValueAtTime(0.0001, now);
    sag.gain.linearRampToValueAtTime(0.85, now + 0.16);
    sag.gain.setValueAtTime(0.85, now + 0.45);
    sag.gain.exponentialRampToValueAtTime(0.0001, now + duration);
    sag.connect(master);

    // Slow tremolo so the cluster wobbles like a crowd rather than a single tone.
    const wobble = ctx.createOscillator();
    wobble.type = "sine";
    wobble.frequency.setValueAtTime(5.5, now);
    wobble.frequency.linearRampToValueAtTime(3.2, now + duration);
    const wobbleDepth = ctx.createGain();
    wobbleDepth.gain.value = 0.12;
    wobble.connect(wobbleDepth).connect(sag.gain);
    wobble.start(now);
    wobble.stop(now + duration);

    const bands = formants.map(formant => {
        const band = ctx.createBiquadFilter();
        band.type = "bandpass";
        band.Q.value = formant.q;
        band.frequency.setValueAtTime(formant.start, now);
        band.frequency.exponentialRampToValueAtTime(formant.end, now + duration);

        const trim = ctx.createGain();
        trim.gain.value = formant.level;
        band.connect(trim).connect(sag);
        return band;
    });

    for (const voice of voices) {
        const osc = ctx.createOscillator();
        osc.type = "sawtooth";
        osc.detune.value = voice.detune;
        osc.frequency.setValueAtTime(voice.start, now);
        osc.frequency.setValueAtTime(voice.start, now + 0.28);
        osc.frequency.exponentialRampToValueAtTime(voice.end, now + duration * 0.85);

        const level = ctx.createGain();
        level.gain.value = voice.level;
        osc.connect(level);
        for (const band of bands) {
            level.connect(band);
        }

        osc.start(now);
        osc.stop(now + duration);
    }

    // A breathy layer so the groan has crowd air in it, not just tone.
    const breath = noiseSource(now, duration);
    const air = ctx.createBiquadFilter();
    air.type = "bandpass";
    air.Q.value = 0.9;
    air.frequency.setValueAtTime(760, now);
    air.frequency.exponentialRampToValueAtTime(280, now + duration);
    const breathGain = envelope(now, 0.08, 0.2, duration - 0.2);
    breath.connect(air).connect(breathGain).connect(master);
    breath.stop(now + duration);
}

const recipes = {
    swing,
    bang,
    splash,
    fanfare,
    lose
};

/** Creates/resumes the AudioContext. Must be called from a user gesture. */
export function unlock() {
    try {
        ensureContext();
    } catch {
        // Audio is optional: never let it break the game.
    }
}

/** Plays a recipe by name, optionally offset by `delay` seconds so sounds can be layered. */
export function play(name, delay) {
    if (muted) {
        return;
    }

    const recipe = recipes[name];
    if (!recipe) {
        return;
    }

    try {
        if (!ensureContext()) {
            return;
        }

        recipe(ctx.currentTime + 0.005 + (delay > 0 ? delay : 0));
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
