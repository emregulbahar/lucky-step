# Quick Design Spec: Gameplay Pass — Corridor and Burn Levels, Golden Pawn Moment

**Type**: Tweak
**System**: Level content (Assets/Levels) + Win panel (UI)
**GDD Reference**: `design/game-brief.md` (no GDD at minimal); stories 006, 009, 005
**Date**: 2026-11-12

## Change Summary
Rework 6 corridor levels so burning steps is a decision, shorten 3 long
back-and-forth levels, and make matching par (Golden pawn) a visible moment.

## Motivation
Gameplay score 6.5/10 (game-designer review 2026-11-12, corrected for the real
world order); the weakest criterion is variety (4.5): 3-row corridors leave no
choice (03, 06, 09, 25, 27, 28) and RLRL pacing makes 12/22/26 endurance tests.

## New Rules / Values
No rule changes. Per level:
1. Corridor levels 03, 06, 09, 25, 27, 28: keep the level's taught mechanic
   (03 dice, 06 pot+cat, 09 mirror, 25 wooden tile, 27 bead, 28 bead+spike) and
   its world's rule set; grid at least 4 rows OR one side pocket/loop, so that
   (a) at least two distinct first moves are legal and non-fatal, and
   (b) the shortest solution is not a straight walk (par.json moves contain
   at least one direction change) — except 03 may keep a straight finish if the
   choice is *where* to burn.
2. Burn levels 12, 22, 26: par cut by >= 25% (12 <= 20, 22 <= 18, 26 <= 17)
   and no alternating run of 4+ moves ("RLRL") in the solver's moves.
3. Every changed level: BFS-solvable, par regenerated in par.json, "par: N"
   header updated, par within the world's existing range (no new difficulty spike).
4. Golden pawn moment: when moves <= par on the win panel, show a gold banner
   with a short scale/punch animation (~0.35 s), a gold sparkle burst and the
   bell; values in UiStyle, not code literals. No change when par is hidden
   (World 1).

## Affected Systems
| System | Impact | Action |
|---|---|---|
| Levels 03, 06, 09, 12, 22, 25, 26, 27, 28 | layout rewritten | edit .txt, run SolveAllBatch |
| par.json / LevelCatalog | par values change | regenerate |
| Tutorial hints 03/09/25/27 | text may go stale | re-read, update EN+TR if needed |
| Progress saves | level IDs unchanged | none |
| WinPanel / UiStyle | gold moment | code + tunables |

## Acceptance Criteria
- [ ] Solver: all 30 levels solvable, 0 errors; changed levels meet 1-3 above
- [ ] Hints for 03/09/25/27 still describe the level (EN and TR)
- [ ] Screenshot of each changed level + the Golden pawn win panel, looked at
- [ ] No regression: other 21 levels' par unchanged; EditMode tests pass

## GDD Update Required?
No (no GDD at this tier).
