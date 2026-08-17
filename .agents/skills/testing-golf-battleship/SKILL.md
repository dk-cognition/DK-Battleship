---
name: testing-golf-battleship
description: How to run and browser-test the DK Battleship (golf-themed Blazor WebAssembly) app locally, including how to drive full matches quickly from the browser console.
---

# Testing DK Battleship locally

## Run it
```bash
export PATH=$PATH:/home/ubuntu/.dotnet          # SDK is not on PATH by default
cd /home/ubuntu/repos/DK-Battleship
dotnet run --project src/DKBattleship.Web --urls http://0.0.0.0:5099   # blocks; run in its own shell
```
```bash
dotnet test tests/DKBattleship.Tests             # unit tests (separate shell)
```
Open http://localhost:5099 in Chrome. No auth, no backend (static SPA); first load pulls ~7 MB of WASM, so allow a few seconds.

## UI landmarks (stable selectors)
- Grid cells are `button.cell` with `aria-label`/`title` set to the coordinate (e.g. `A1`); the first `.grid` on the battle screen is the opponent board, the second is your own board.
- Hit glyph is `✕`, miss glyph is `●`; empty cells have no text. Non-clickable boards render `disabled` buttons — this is the safest way to assert "clicking your own board does nothing".
- Sunk clubs show as `li.sunk` in the bag list with the text `in the hole`.
- Blazor's error UI is `#blazor-error-ui`; check `getComputedStyle(...).display === 'none'` to prove no unhandled exception occurred.

## Driving a full match fast
Clicking 100 cells by hand is slow. A console interval that clicks fresh cells works well (the UI has a ~650 ms AI delay, so stay above it):
```js
window.__driver=setInterval(()=>{
  const grids=document.querySelectorAll('.grid');
  if(document.querySelector('.game-over')||grids.length<2){clearInterval(window.__driver);return;}   // game over
  const next=[...grids[0].querySelectorAll('button.cell')].find(b=>!b.disabled && !b.textContent.trim());
  if(next) next.click();
},800);
```
A naive sequential scan almost always LOSES (the AI is efficient). To demonstrate the win panel, use a probability-density / hunt-target picker: weight every legal placement of remaining ship sizes over unknown cells, boost cells adjacent to existing `✕` hits, and click the max. That reliably produces a win within a few attempts (Rematch is cheap).

## Auditing Web Audio (sound effects + background music)
The box has no sound device and Chrome runs with `--mute-audio`, so "did it play?" must be proven
numerically. Tap the shared master gain from `wwwroot/js/audio.js` with an `AnalyserNode` and read
time-domain RMS; render it as an on-page HUD so the numbers show up in recordings:
```js
import('./js/audio.js').then(a=>{
  const c=a.context();                       // null/suspended before the first gesture
  const an=c.createAnalyser(); an.fftSize=2048; a.masterGain().connect(an);
  window.__rms=()=>{const d=new Float32Array(an.fftSize);an.getFloatTimeDomainData(d);
    let s=0;for(let i=0;i<d.length;i++)s+=d[i]*d[i];return Math.sqrt(s/d.length);};
});
```
Also patch `c.createOscillator` / `c.createBufferSource` to count scheduled voices — a frozen counter
proves a look-ahead scheduler stopped scheduling, a climbing one proves it is still alive.
Reference magnitudes observed for the course-music feature: music bed RMS ≈ 0.005 during fade-in and
0.018–0.033 once running (~22 voices/s); a shot effect peaks ≈ 0.14; silence reads 0.0000.

Tips:
- Top-level `await` is rejected in the Chrome console; use `import('./js/x.js').then(...)`.
- Autoplay: `context().state` is `suspended` until the first real user gesture. Click a neutral part
  of the page (not a sound button) to prove the app's own gesture handling works. If a resume-based
  start hangs forever with the context still `suspended`, suspect a pending (never-rejecting)
  `ctx.resume()` promise — the fix is to always arm a `pointerdown`/`keydown` listener too.
- Sub-second timing checks (e.g. "does toggling off mid-fade-in glide or cut?") can't be hit with
  two separate tool calls; run one console snippet that samples RMS every 50 ms and fires
  `button.click()` at scheduled offsets.
- Throttling/pile-up: block the main thread with `const t=Date.now();while(Date.now()-t<6000){}` so
  the scheduler timer stalls while the audio clock advances, then compare pre/post peak RMS
  (a healthy resync stays near 1x–2x; a pile-up spikes much higher).
- Preference keys: `battlegolf.musicOn` (music) and `battlegolf.muted` (shot effects) are separate;
  with music off the app may never create an AudioContext at all, which is expected.

## Gotchas
- `wmctrl -r :ACTIVE: -b add,maximized_vert,maximized_horz` may fail with "Cannot get client list properties" in this environment; the browser window is already usable, just proceed.
- Bare console expressions return `undefined`; wrap diagnostics in `console.log(...)`.
- Verify placement counts with `document.querySelectorAll('.grid')[1].querySelectorAll('.ship').length === 17` (Driver 5 + Fairway Wood 4 + Hybrid 3 + Iron 3 + Putter 2) — `BoardGrid.razor` renders one `button.cell` per board cell, so a count below 17 means a club is missing or overlapping.

## Devin Secrets Needed
None — the app is local and credential-free.
