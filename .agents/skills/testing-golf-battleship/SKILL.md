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

## Gotchas
- `wmctrl -r :ACTIVE: -b add,maximized_vert,maximized_horz` may fail with "Cannot get client list properties" in this environment; the browser window is already usable, just proceed.
- Bare console expressions return `undefined`; wrap diagnostics in `console.log(...)`.
- Verify placement counts with `document.querySelectorAll('.grid')[1].querySelectorAll('.ship').length === 17` (Driver 5 + Fairway Wood 4 + Hybrid 3 + Iron 3 + Putter 2) — `BoardGrid.razor` renders one `button.cell` per board cell, so a count below 17 means a club is missing or overlapping.

## Devin Secrets Needed
None — the app is local and credential-free.
