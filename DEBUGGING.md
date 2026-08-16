# Debugging log

Defects found while building DK Battleship, all surfaced by tests in `tests/DKBattleship.Tests`
rather than by guesswork. Each entry lists the symptom, the root cause, the fix and the test that
now guards it.

## 1. A ship instance could be placed twice, permanently blocking cells

- **Symptom:** placing the same `Ship` object a second time (a plausible UI double-click or a
  "re-place this club" flow) returned `true`. The board then listed the club twice and the cells
  from the first placement stayed `CellState.Ship` forever — no club occupied them, so
  `AllShipsSunk` could never become true and the game was unwinnable.
- **Root cause:** `Board.PlaceShip` validated only geometry (bounds + overlap). `Ship.Place`
  overwrote the ship's coordinate list, while `Board` never cleared the cells the ship used to
  occupy and appended the ship to `_ships` again.
- **Fix:** a ship instance now belongs to exactly one placement. `Board.CanPlace(Ship, ...)` rejects
  a ship that `IsPlaced` or is already in the board's ship list, so `PlaceShip` returns `false` and
  leaves the board untouched (`src/DKBattleship.Core/Board.cs`).
- **Test:** `RegressionTests.PlacingTheSameShipInstanceTwice_IsRejected`.

## 2. AI could hand out the same cell twice before a result was recorded

- **Symptom:** calling `IAiPlayer.NextShot` twice without reporting the first result returned the
  same coordinate again (`AI offered B8 twice before any result was recorded`). In the Blazor UI —
  where the AI turn is asynchronous and the component re-renders — that produced wasted swings and
  an `AlreadyShot` result attributed to a fresh swing.
- **Root cause:** `HuntTargetAi` only remembered shots in `RecordResult`. Between `NextShot` and
  `RecordResult` the chosen cell was invisible to the random hunt selection, which looks at the
  board's revealed cells plus its own shot history.
- **Fix:** the AI tracks cells it has already handed out in a `_pendingShots` set; both hunt
  selection and the target queue skip them until the result arrives
  (`src/DKBattleship.Core/Ai/HuntTargetAi.cs`).
- **Test:** `RegressionTests.NextShot_NeverRepeatsWhenResultsAreNotRecordedYet` plus
  `AiTests.NeverRepeatsAShot_OverAFullBoard`.

## 3. Sinking a club threw away a confirmed hit on the neighbouring club

- **Symptom:** with a `Putter` at E5–F5 and an `Iron` starting directly below at E6, the AI hit the iron,
  then sank the putter, and immediately dropped back to hunt mode (`Expected: Target / Actual:
  Hunt`) — abandoning a known wounded club and costing many swings.
- **Root cause:** `RecordResult` handled `ShotResult.Sunk` by clearing the whole target queue, which
  also discarded follow-up cells that came from hits on a *different* ship.
- **Fix:** the AI keeps a set of "open" hits not yet attributed to a sunk club. On a sink it walks
  the straight run of hits through the sinking cell (the longer of the horizontal/vertical run),
  marks exactly those cells resolved, and rebuilds the target queue from any remaining open hits.
- **Test:** `RegressionTests.SinkingAShip_KeepsWorkingAHitOnANeighbouringShip` and
  `AiTests.ReturnsToHuntModeAfterSinking` (the latter pins the opposite case: after the *last* open
  hit is resolved the AI does go back to hunting).

## 4. Sinking a club in line with another swallowed the neighbour's hits and mis-counted the bag

- **Symptom:** found while adding the four skill levels. With a `Putter` at A1–B1 and a `Hybrid` at
  C1–E1, the AI hit C1 and D1, then hit A1 and sank the putter at B1 — and went straight back to
  hunting (`Expected: Target / Actual: Hunt`) even though two hits on the hybrid were still live.
  Tiger's density hunting was also poisoned: its bag of remaining club sizes lost the `4` (the
  fairway wood, still afloat) instead of the `2`.
- **Root cause:** fix #3 attributes a sink to "the straight run of open hits through the sinking
  cell". Clubs lying end to end make one continuous run, so the four-cell run A1–D1 was credited
  entirely to the two-cell putter: the hybrid's hits were marked resolved and `_remainingSizes` had
  the wrong size removed.
- **Fix:** the sunk club's size is now reported alongside the result —
  `IAiPlayer.RecordResult(shot, result, sunkClubSize)`, filled in by `Game.AiFire` from the club it
  just sank, exactly like an opponent calling out "you sank my Putter". `HuntTargetAi` trims the run
  to a window of that length containing the last swing, preferring the window that butts up against
  the end of the run (a sunk club's ends touch water or the board edge, never another club's hits).
- **Test:** `RegressionTests.SinkingAClubInLineWithAnother_LeavesTheNeighboursHitsOpen`.

## Edge cases checked and deliberately left as-is

These were probed with tests and behave correctly; they are documented so the behaviour is not
"fixed" into a bug later.

- **Board bounds are exclusive of `Rows`/`Cols`.** `Coordinate(10, 0)` is off a 10×10 course, and a
  5-cell driver may start at column 5 but not column 6. Both directions of the off-by-one are pinned
  by `BoardTests.PlaceShip_RejectsHorizontalOverhang` / `..._RejectsVerticalOverhang` /
  `..._SucceedsAtBoardEdge`.
- **Target cells around a corner hit.** Neighbours of a hit are generated without bounds filtering,
  so the queue is pruned against the board before use; corner hits never yield an off-board swing
  (`AiTests.TargetMode_NeverReturnsOffBoardCellsForCornerHits`).
- **Repeat swings do not consume a turn.** `Board.ReceiveShot` returns `AlreadyShot` for a cell that
  was already played, and `Game.PlayerFire` leaves the turn and swing count untouched in that case
  (`GameTests.PlayerFire_PassesTurnToAi_AndRepeatShotDoesNot`).
- **`AllShipsSunk` on an empty board is `false`.** Otherwise a game would be won before any club was
  placed (`BoardTests.AllShipsSunk_FalseOnEmptyBoardAndUntilEveryShipIsSunk`).
- **Out-of-turn / post-game AI swings throw.** `Game.AiFire` throws `InvalidOperationException`
  rather than silently mutating a finished match, so the UI has to gate the AI turn on the game
  status (`RegressionTests.AiFire_AfterGameOver_Throws`, `GameTests.AiFire_ThrowsWhenNotItsTurn`).
- **A course too small for the bag fails loudly.** `new Game(rows: 4, cols: 4)` throws from the
  random placer instead of starting a game with missing clubs
  (`RegressionTests.BoardTooSmallForTheBag_FailsLoudly`).
