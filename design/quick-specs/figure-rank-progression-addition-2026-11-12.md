# Quick Design Spec: Figure Rank (progression) and Win Confetti

**Type**: Addition
**System**: Progress, BoardView (figure base), WinPanel, LevelSelect
**GDD Reference**: `design/game-brief.md` (no GDD at minimal); Story 006 progress/win panel
**Date**: 2026-11-12

## Change Summary
The player figure's base disc shows how far the player has come: it upgrades
each time a whole world is completed. The win that completes a world shows a
"Promotion!" row on the win panel. Level select shows per-world and total
progress. The board win gets a confetti burst.

## Motivation
User request 2026-11-12: "oyuna bir kazanma hissi, gelişme hissi eklemeliyiz".
Today a solved level only ticks a box; nothing in the game itself changes as
the player advances. Approved direction (user, 2026-11-12, "evet yap"):
figure rank + promotion moment + visible progress + win moment.

## New Rules / Values
- **Rank** = number of worlds whose levels are all completed (0-5). Pure
  function `Progress.CompletedWorlds(worlds)`; order of completion does not
  matter (with `--unlock-all` a later world may finish first).
- **Base tier per rank** (BoardTheme.figureRanks, index = rank, clamped):

  | Rank | Base | Extra |
  |---|---|---|
  | 0 | wood | - |
  | 1 | bronze | - |
  | 2 | silver | - |
  | 3 | gold | - |
  | 4 | glowing gold | - |
  | 5 | glowing gold | small gold star spinning above the head |

  The disc also widens slightly per rank (figureBaseRankGrowth, default
  0.02 cells per rank). The star size and height are theme values.
- **Promotion moment**: when a win raises the rank, the win panel shows a
  second banner "Promotion! New base: <tier>" with a base icon that punches
  in and changes colour from the old tier to the new one. It plays after the
  Golden pawn banner (if any), and the lucky bell rings.
  The figure on the board shows the new base from the next level on.
- **Level select**: each world row ends with "done/total" (e.g. 4/6); the
  legend line shows Golden pawns earned / possible (levels with par shown)
  and the current base tier.
- **Win confetti**: when the figure vanishes into the portal, a burst of
  multi-coloured paper confetti (<= 40 particles, WebGL budget) flies up and
  falls; the existing gold sparks stay.
- All values in BoardTheme / UiStyle (data-driven); text EN/TR in
  `Resources/Localization`.

## Affected Systems
| System | Impact | Action |
|---|---|---|
| Progress | new pure rank count | code + unit tests |
| BoardView / BoardTheme | base material per rank, star, confetti | code + theme data |
| ScreenFlow / WinPanel | promotion banner | code |
| LevelSelect | progress counts | code |
| AudioFeedback | bell on promotion | small hook |
| Localization EN/TR | new strings | data |

## Acceptance Criteria
- [ ] EditMode tests: rank 0 with nothing done, counts only fully completed worlds, any order
- [ ] Screenshot: figure base in rank 0, 2 and 5 (star) looks distinct and clean
- [ ] Screenshot: win panel after finishing a world shows the promotion banner
- [ ] Screenshot: level select shows per-world counts and the totals line (TR)
- [ ] Screenshot: confetti visible after the portal hop
- [ ] No regression: Golden pawn moment, existing 49 tests, WebGL boots

## GDD Update Required?
No.
