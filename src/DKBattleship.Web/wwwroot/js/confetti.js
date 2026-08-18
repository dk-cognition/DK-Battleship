// Confetti rain drawn on a single full-screen canvas overlay driven by requestAnimationFrame.
// Hundreds of pieces stay smooth this way, and the canvas is released as soon as the run ends.

const COLORS = ["#f5d76e", "#7fc97f", "#4a90d9", "#e8674f", "#ffffff", "#d9a94a"];
const PIECES = 220;
const DURATION_MS = 4500;
// Pieces start just above the viewport so even the slowest ones fall into view well inside the run.
const SPAWN_BAND = 220;

let canvas = null;
let frame = 0;
let pieces = [];

function reducedMotion() {
    try {
        return window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    } catch {
        return false;
    }
}

function createPieces(width) {
    const list = [];
    for (let i = 0; i < PIECES; i++) {
        list.push({
            x: Math.random() * width,
            y: -20 - Math.random() * SPAWN_BAND,
            width: 5 + Math.random() * 6,
            height: 8 + Math.random() * 8,
            speed: 70 + Math.random() * 150,
            drift: (Math.random() - 0.5) * 70,
            wobble: Math.random() * Math.PI * 2,
            spin: (Math.random() - 0.5) * 6,
            angle: Math.random() * Math.PI * 2,
            color: COLORS[Math.floor(Math.random() * COLORS.length)]
        });
    }

    return list;
}

/** Starts (or restarts) the confetti rain. Safe to call when animation is unavailable. */
export function start() {
    try {
        stop();

        if (reducedMotion()) {
            return;
        }

        canvas = document.createElement("canvas");
        canvas.className = "confetti-layer";
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
        document.body.appendChild(canvas);

        const context = canvas.getContext("2d");
        if (!context) {
            stop();
            return;
        }

        pieces = createPieces(canvas.width);

        const began = performance.now();
        let previous = began;

        const paint = now => {
            const elapsed = now - began;
            const delta = Math.min((now - previous) / 1000, 0.05);
            previous = now;

            context.clearRect(0, 0, canvas.width, canvas.height);
            const fade = elapsed > DURATION_MS - 900
                ? Math.max(0, (DURATION_MS - elapsed) / 900)
                : 1;
            context.globalAlpha = fade;

            for (const piece of pieces) {
                piece.wobble += delta * 3;
                piece.y += piece.speed * delta;
                piece.x += (piece.drift + Math.sin(piece.wobble) * 30) * delta;
                piece.angle += piece.spin * delta;

                if (piece.y > canvas.height + 20) {
                    piece.y = -20;
                    piece.x = Math.random() * canvas.width;
                }

                context.save();
                context.translate(piece.x, piece.y);
                context.rotate(piece.angle);
                context.fillStyle = piece.color;
                context.fillRect(-piece.width / 2, -piece.height / 2, piece.width, piece.height);
                context.restore();
            }

            return elapsed < DURATION_MS;
        };

        const draw = now => {
            try {
                if (!paint(now)) {
                    stop();
                    return;
                }

                frame = requestAnimationFrame(draw);
            } catch {
                // Decoration only: never let it break the game, and never leave the canvas behind.
                stop();
            }
        };

        frame = requestAnimationFrame(draw);
    } catch {
        // Decoration only: never let it break the game.
        stop();
    }
}

/** Cancels the animation and removes the canvas. */
export function stop() {
    if (frame) {
        cancelAnimationFrame(frame);
        frame = 0;
    }

    if (canvas) {
        canvas.remove();
        canvas = null;
    }

    pieces = [];
}
