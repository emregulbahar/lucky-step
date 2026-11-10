# Quick Design Spec: World 5 "The Old Bazaar": wooden tile and evil-eye bead

**Type**: Addition
**System**: Step rules (StepRules / GameState / LevelLoader), chance objects
**GDD Reference**: `design/game-brief.md` (Rules detail; no GDD at workflow: minimal), `production/epics/lucky-step/story-002-chance-objects.md`
**Date**: 2026-11-10

## Change Summary

Two new board objects for a fifth world, "The Old Bazaar" (6 levels, default rules: 7 lucky, 13 deadly):
a wooden tile that makes a step not count ("knock on wood"), and an evil-eye bead (nazar boncuğu) that is
picked up and wards off one death by bad luck.

## Motivation

Every step changes the counter by 1, so in a level without a cat or mirror the lucky count is locked to
tile parity and to one lucky step per run. The wooden tile adds a "wait" move: it shifts which tile gets
the lucky count and lets the player time the black cat without spending count. The bead lets the player
survive one bad-luck death, most notably 13, so 14 (a multiple of 7) becomes a second lucky step without
a cat: a new answer to "where do I spend my luck". Both are superstitions players recognise; the bead is
Turkish culture (the game's TR title is Yedinci Adım) and was listed as out of scope in the brief; the user
reopened it on 2026-11-10.

## Design Delta

Brief currently says (`design/game-brief.md`, Rules detail and Out of scope):

> - **Counter:** never caps and starts at the world's start value (0; 5 in World 4).
> - Roulette wheel, salt, evil-eye bead, multiplayer, player level editor, saves beyond level progress.

This spec adds:

### Wooden tile (level character `W`)
1. A step whose target tile (before any dice fling) is Wood is **not counted**: the counter keeps its
   value, and the step is neither lucky nor deadly (so it never opens bridges or the exit, and never kills
   by count).
2. Everything else about the step happens as usual: the black cat advances one route index, and cat
   collision still resets the counter to the start value.
3. Counting direction is unchanged (a mirror's count-down stays on).
4. A dice fling that lands on wood is the dice step and counts normally (the counted/lucky decision is made
   on the tile entered, before the fling).
5. Wood can be stepped onto any number of times; it is never used up.

### Evil-eye bead (level character `N`)
1. Landing on a Bead tile that has not been taken picks it up: `GameState.Beads` + 1, and the bead's bit is
   set in `GameState.TakenBeads` (index from `Level.BeadIndex`, row-major, at most 64 beads). A taken bead
   tile behaves as floor. Landing includes a dice fling's landing tile.
2. Beads stack; the player can carry any number.
3. **Bad-luck death**: a deadly count (13 under default rules; any count containing 4 in World 4), or a spike
   or flowerpot on a normal step. If the step would cause a bad-luck death and the pawn carries at least
   one bead, one bead is used up (`Beads` - 1) and the pawn lives, staying on the landing tile with the
   counter at that count.
4. **Falls are not bad luck**: a gap, or a bridge closed on that step, kills even with beads; no bead is used.
5. A bead picked up on the same step already counts (pickup comes before the ward).
6. One bead covers one step, whatever caused the death on that step.
7. Undo and restart restore `Beads` and `TakenBeads` (they are part of `GameState`, so the undo stack and
   the solver cover them).

### Order of operations (extends StepRules)
(1) new count: +1, -1 while counting down, or unchanged onto wood; (2) lucky/deadly on that count (both
false on a wood step); (3) dice fling; on a lucky step all bridges open; bead pickup; landing-tile effect;
bead ward; (4)-(7) cat, cat collision, mirror, floor as before.

### Presentation
- Wood: three nailed honey planks laid on the printed space (`tile_wood.fbx`, slot `plank` -> `Wood_Plank`).
- Bead: upright cobalt / white / light blue / black disc with a small gold loop (`bead.fbx`), hovering,
  bobbing and swaying over its space; flies up and shrinks away when picked up; snaps back on undo.
- A bead breaking (ward used) emits a puff at the figure.
- HUD: a cobalt line "Evil-eye beads: N" (TR "Nazar boncuğu: N") in levels that have beads; hidden otherwise.
- A wood step on a lucky count does not replay the lucky glow (the count did not change).

### World 5
- Name "The Old Bazaar" / "Eski Çarşı"; story card: "Knock on wood and the step does not count. Carry a blue
  bead and bad luck passes you by." (TR: "Tahtaya vurursan o adım sayılmaz. Nazar boncuğu taşırsan uğursuzluk
  sana dokunmaz.")
- Levels 25-30 in play order: 25 wood intro, 26 wood + cat timing, 27 bead intro (spike), 28 bead past 13
  (exit on 14), 29 and 30 combine wood and bead with earlier objects. Each level is solver-verified with a
  par line.

## New Rules / Values

| Name | Value | Where |
|------|-------|-------|
| Wood character | `W` | LevelLoader |
| Bead character | `N` | LevelLoader |
| Max beads per level | 64 | Level, LevelLoader (bitmask) |
| New GameState fields | `int Beads`, `ulong TakenBeads` | GameState (Equals, GetHashCode, With) |
| beadHover / beadBob / beadBobSpeed | 0.12 / 0.04 / 0.6 | BoardTheme |
| beadSwayDegrees / beadSwaySpeed | 35 / 1.4 | BoardTheme |
| beadPickupSeconds | 0.35 | BoardTheme |

## Affected Systems

| System | Impact | Action Required |
|--------|--------|-----------------|
| StepRules / GameState / Level / LevelLoader | New tiles, new state fields | Done; tests in `WoodAndBeadTests.cs` |
| LevelSolver | State space includes Beads and TakenBeads | None (uses GameState equality) |
| BoardView / BoardTheme / BuildBoardScene | New models, bead animation, materials | Rebuild board scene |
| StepFeedback | No lucky glow on an uncounted wood step | Done |
| Hud / ScreenFlow / UiStyle / Localization | Bead line, World 5 strings | Done |
| LevelCatalog / LevelSelect | Fifth world row; level buttons 112 -> 96 px so 5 rows fit | Catalog entry pending |
| Game brief | Rules detail, MVP list, Out of scope | Update (see below) |

## Acceptance Criteria

- [ ] Stepping straight onto wood leaves the counter unchanged; the cat still moves (EditMode tests).
- [ ] A wood step can shift the lucky count onto the exit (EditMode test).
- [ ] A bead wards off a spike death once; a second spike kills (EditMode tests).
- [ ] A bead does not save from a gap (EditMode test).
- [ ] With a bead, the pawn survives 13 and enters the exit on 14 (EditMode test).
- [ ] Re-entering a taken bead tile gives no extra bead (EditMode test).
- [ ] Undo after a pickup or a ward restores the bead on the board and the HUD count (observed in build).
- [ ] Levels 25-30 are solver-verified (`ok` against their par lines), and in each one removing a wood or bead tile changes par or makes it unsolvable.
- [ ] Wood planks, bead, pickup and ward puff are visible and readable in a Win64 build (retained screenshots).
- [ ] No regression: levels 01-24 keep their par (solver), and all earlier EditMode tests pass.

## GDD Update Required?

Yes. No GDD exists; the brief is the design record. Update `design/game-brief.md`:
Rules detail (add Wooden tile and Evil-eye bead bullets), MVP item 6 (5 worlds, 30 levels), Out of scope
(remove "evil-eye bead"), and the Goal line (30 levels).
