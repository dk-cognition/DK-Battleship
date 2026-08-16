# DK Battleship — Golf Edition ⛳

Battleship played with a bag of golf clubs, against an AI opponent, in the browser.

**Play it:** https://dk-cognition.github.io/DK-Battleship/
**Debugging log:** [DEBUGGING.md](DEBUGGING.md)

## The game

- Your bag is the fleet: **Driver** (5), **Fairway Wood** (4), **Hybrid** (3), **Iron** (3), **Putter** (2).
- Place clubs by hand (click + rotate) or let the caddie place them for you.
- Take turns swinging at cells on your opponent's course. Sinking a club puts it "in the hole".
- Opponents are golf characters, each with its own AI strategy. Seed roster: **The Pro** (parity
  sweep, efficient) and **The Weekend Hacker** (sprays it around). More personalities can be added
  without touching the game rules.

## Solution layout

| Project | What it is |
| --- | --- |
| `src/DKBattleship.Core` | All game logic — board, ships, shots, turn flow, AI strategies, characters. No UI dependencies. |
| `src/DKBattleship.Web` | Blazor WebAssembly front end, published as a static site. |
| `tests/DKBattleship.Tests` | xUnit tests for placement, shot results, win detection, AI behaviour and past regressions. |

Key core types: `Board` (`CanPlace` / `PlaceShip` / `ReceiveShot` / `AllShipsSunk`), `Ship`
(`IsSunk`), `Game` (`PlayerFire` / `AiFire`, status + golf-flavoured messages), `ShipPlacer`
(random valid placement), `IAiPlayer` + `HuntTargetAi` (hunt/target with parity sweep), and
`GolfCharacter` / `GolfCharacters` (roster mapping a personality to a strategy).

Adding a new opponent means adding one `IAiPlayer` implementation and one `GolfCharacter` entry —
the board, rules and UI stay unchanged.

## Running locally

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet restore DKBattleship.sln
dotnet test tests/DKBattleship.Tests
dotnet run --project src/DKBattleship.Web    # then open the printed http://localhost:… URL
```

## Deployment

`.github/workflows/deploy.yml` builds the solution, runs the tests, publishes the Blazor WASM app,
rewrites `<base href>` to the Pages subpath (`/DK-Battleship/`), adds a `404.html` SPA fallback and a
`.nojekyll` marker, then deploys to GitHub Pages on every push to `main`.

One-time setup: in **Settings → Pages**, set **Source** to **GitHub Actions**.
