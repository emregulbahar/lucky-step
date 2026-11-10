# Evidence: Story 005 — Wooden board look, lucky/13 feedback

**Story**: `production/epics/lucky-step/story-005-look-and-sound.md` (Visual/Feel)
**Date**: 2026-11-10
**Screenshots**: `production/qa/evidence/story-005-look-and-sound/` (74 images, 01-74)

## What was checked

| Criterion | Evidence | Result |
|---|---|---|
| 3D board game on a card table, own Blender models, Kenney cat and furniture | 61-66 (felt table, printed board, props, rope pot, key legend) | Approved |
| Gold glow on lucky steps; edges darken near 13 | 55 (glow at step 7), 10 (darkening at step 12, earlier look) | Approved |
| Dice roll, coin flip, flowerpot fall match the outcome | 04, 05, 06, 07, 08, 20 | Approved |
| Visual direction checkpoint | style frozen by the user on 2026-11-10 ("no more visuals after this") | Approved |
| Lamp light and tilt-shift blur in WebGL | dropped with the Parlour look; art direction now says no depth of field | DEFERRED (not applicable) |
| 60 fps in the WebGL browser build | WebGL build not tested yet | DEFERRED to Story 007 |

Later look fixes on 2026-11-10 (MSAA instead of FXAA, 2x track atlas, Bold hinted UI font, pixel-perfect canvas):
67-74.

## Sign-off

| Role | Name | Status |
|---|---|---|
| Lead / Art direction | project owner | [x] Approved |
| Programmer | project owner | [x] Approved |
| QA | project owner | [x] Approved |

Approved on the basis of the user's play of `Builds/Win64-bridge` and the retained screenshots above.
Some older screenshots (10, 04-08) show earlier looks; the behaviour they prove is unchanged.
