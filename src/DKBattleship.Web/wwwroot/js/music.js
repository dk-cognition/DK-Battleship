// Soft background music for the course: a slow, looping four-bar progression played by
// synthesized marimba plucks over a warm pad, with a light shaker and the odd bird call.
// Everything is generated with Web Audio nodes, so there is no audio asset to download.

import { ensureContext, context, masterGain, noiseSource, envelope, midiToFrequency, onFirstGesture } from "./audio.js";

const MUSIC_KEY = "battlegolf.musicOn";
const BEAT = 0.85;                 // ~70 BPM: unhurried, like a walk up the fairway
const BAR = BEAT * 4;
const LOOKAHEAD = 2.0;             // seconds of music scheduled ahead of the clock
const SCHEDULE_INTERVAL = 500;     // ms between scheduling passes

// F major: Fmaj9, Dm9, Bbmaj9, C(add9) -- lazy, resolved, never tense.
const progression = [
    { pad: [41, 60, 64, 67, 72], melody: [65, 69, 72, 76] },
    { pad: [38, 57, 62, 65, 69], melody: [62, 65, 69, 72] },
    { pad: [34, 58, 62, 65, 69], melody: [58, 62, 65, 70] },
    { pad: [36, 55, 60, 64, 67], melody: [60, 64, 67, 72] }
];

// Beat offsets of the melody plucks within a bar and which chord tone each one takes.
const pluckPattern = [
    { beat: 0, tone: 0, gain: 0.10 },
    { beat: 1.5, tone: 2, gain: 0.07 },
    { beat: 2, tone: 1, gain: 0.08 },
    { beat: 3, tone: 3, gain: 0.06 },
    { beat: 3.5, tone: 2, gain: 0.05 }
];

let on = readEnabled();
let musicGain = null;
let timer = null;
let nextBarTime = 0;
let bar = 0;

function readEnabled() {
    try {
        const stored = localStorage.getItem(MUSIC_KEY);
        return stored === null ? true : stored === "true";
    } catch {
        return true;
    }
}

/** Sustained bed of detuned sines under a gentle lowpass: the "warm afternoon" layer. */
function pad(ctx, notes, start) {
    notes.forEach((midi, index) => {
        const lowpass = ctx.createBiquadFilter();
        lowpass.type = "lowpass";
        lowpass.frequency.value = 1200;

        const gain = ctx.createGain();
        const peak = index === 0 ? 0.10 : 0.045;
        gain.gain.setValueAtTime(0.0001, start);
        gain.gain.linearRampToValueAtTime(peak, start + BAR * 0.35);
        gain.gain.linearRampToValueAtTime(peak * 0.75, start + BAR * 0.8);
        gain.gain.linearRampToValueAtTime(0.0001, start + BAR * 1.05);
        lowpass.connect(gain).connect(musicGain);

        [-4, 4].forEach(detune => {
            const osc = ctx.createOscillator();
            osc.type = index === 0 ? "sine" : "triangle";
            osc.frequency.value = midiToFrequency(midi);
            osc.detune.value = detune;
            osc.connect(lowpass);
            osc.start(start);
            osc.stop(start + BAR * 1.1);
        });
    });
}

/** Marimba-ish pluck: a sine fundamental with a quiet octave, both decaying quickly. */
function pluck(ctx, midi, start, level) {
    [{ ratio: 1, gain: level, decay: 1.1 }, { ratio: 2, gain: level * 0.3, decay: 0.4 }].forEach(part => {
        const osc = ctx.createOscillator();
        osc.type = "sine";
        osc.frequency.value = midiToFrequency(midi) * part.ratio;
        const gain = envelope(start, part.gain, 0.012, part.decay);
        osc.connect(gain).connect(musicGain);
        osc.start(start);
        osc.stop(start + part.decay + 0.1);
    });
}

/** Soft shaker brush, the percussion equivalent of a breeze through the trees. */
function shaker(ctx, start) {
    const source = noiseSource(start, 0.2);
    const highpass = ctx.createBiquadFilter();
    highpass.type = "highpass";
    highpass.frequency.value = 5200;
    const gain = envelope(start, 0.05, 0.01, 0.12);
    source.connect(highpass).connect(gain).connect(musicGain);
    source.stop(start + 0.2);
}

/** Two-note bird call, sprinkled in occasionally so the loop never feels mechanical. */
function bird(ctx, start) {
    const base = 2100 + Math.random() * 700;
    [0, 0.13].forEach((offset, index) => {
        const osc = ctx.createOscillator();
        osc.type = "sine";
        const at = start + offset;
        osc.frequency.setValueAtTime(base * (index === 0 ? 1 : 1.18), at);
        osc.frequency.exponentialRampToValueAtTime(base * 1.35, at + 0.05);
        const gain = envelope(at, 0.035, 0.01, 0.08);
        osc.connect(gain).connect(musicGain);
        osc.start(at);
        osc.stop(at + 0.2);
    });
}

function scheduleBar(ctx, index, start) {
    const chord = progression[index % progression.length];
    pad(ctx, chord.pad, start);

    pluckPattern.forEach(step => pluck(ctx, chord.melody[step.tone], start + step.beat * BEAT, step.gain));

    for (let beat = 0; beat < 4; beat += 2) {
        shaker(ctx, start + (beat + 1) * BEAT);
    }

    if (Math.random() < 0.3) {
        bird(ctx, start + Math.random() * BAR);
    }
}

function scheduleAhead() {
    const ctx = context();
    if (!ctx || !musicGain) {
        return;
    }

    while (nextBarTime < ctx.currentTime + LOOKAHEAD) {
        scheduleBar(ctx, bar, Math.max(nextBarTime, ctx.currentTime + 0.05));
        bar++;
        nextBarTime += BAR;
    }
}

function start() {
    if (timer !== null) {
        return;
    }

    const ctx = ensureContext();
    if (!ctx) {
        return;
    }

    if (ctx.state === "suspended") {
        // Autoplay is blocked until the visitor interacts with the page.
        onFirstGesture(() => {
            if (on) {
                start();
            }
        });
        return;
    }

    musicGain = ctx.createGain();
    musicGain.gain.setValueAtTime(0.0001, ctx.currentTime);
    musicGain.gain.linearRampToValueAtTime(0.5, ctx.currentTime + 1.5);
    musicGain.connect(masterGain());

    nextBarTime = ctx.currentTime + 0.2;
    scheduleAhead();
    timer = setInterval(scheduleAhead, SCHEDULE_INTERVAL);
}

function stop() {
    if (timer !== null) {
        clearInterval(timer);
        timer = null;
    }

    const ctx = context();
    if (musicGain && ctx) {
        const fadingOut = musicGain;
        fadingOut.gain.cancelScheduledValues(ctx.currentTime);
        fadingOut.gain.setValueAtTime(fadingOut.gain.value, ctx.currentTime);
        fadingOut.gain.linearRampToValueAtTime(0.0001, ctx.currentTime + 0.8);
        setTimeout(() => fadingOut.disconnect(), 1500);
    }

    musicGain = null;
    bar = 0;
}

/** Starts the loop when music is enabled and reports the current setting. */
export function init() {
    try {
        if (on) {
            start();
        }
    } catch {
        // Music is optional: never let it break the game.
    }

    return on;
}

/** Silences the loop without changing the saved preference (component teardown). */
export function stopMusic() {
    try {
        stop();
    } catch {
        // Nothing left to tear down.
    }
}

export function isMusicOn() {
    return on;
}

export function setMusicOn(value) {
    on = !!value;
    try {
        localStorage.setItem(MUSIC_KEY, on ? "true" : "false");
    } catch {
        // Persistence is best effort.
    }

    try {
        if (on) {
            start();
        } else {
            stop();
        }
    } catch {
        on = false;
    }

    return on;
}
