// Tiny synthesized effect layer: no audio assets, everything is built with Web Audio nodes.
// Recipes are keyed by name so new sounds ("win", "lose", ...) are a one-entry addition.

import { ensureContext, context, masterGain, noiseSource, envelope } from "./audio.js";

const MUTE_KEY = "battlegolf.muted";

let muted = readMuted();

function readMuted() {
    try {
        return localStorage.getItem(MUTE_KEY) === "true";
    } catch {
        return false;
    }
}

// A golf strike: a very short, bright noise transient with an almost instant decay,
// plus a fast falling sine "ping" for the compressed-ball ring.
function swing(ctx, master, now) {
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
function bang(ctx, master, now) {
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
function splash(ctx, master, now) {
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

const recipes = {
    swing,
    bang,
    splash
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

        const ctx = context();
        recipe(ctx, masterGain(), ctx.currentTime + 0.005 + (delay > 0 ? delay : 0));
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
