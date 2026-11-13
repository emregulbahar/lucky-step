# Quick Design Spec: Final-review tweaks (world access, ending)

**Type**: Tweak
**System**: Progress, LevelSelect, ScreenFlow, WinPanel
**Date**: 2026-11-13
**Source**: final review report D:/GameDev/Jamler/lucky_step_son_test.html; user chose options 1 and 2.

## Change Summary
1. **World access**: the first level of every world is open from the start; inside a world a level opens when the
   one before it is done (`Progress.IsUnlocked(levelId, worldLevelIds)`). Reason: judges play 5-10 minutes and
   World 1 is six 7-step levels, so the cat, mirror, World 4 rules and bead were hidden behind it.
2. **Ending**: after the last level the win panel says "You reached the last square!" and Next opens an ending
   card (`story.ending`, StoryCard); any key then opens the menu. Replaces "More worlds are on the way."

## Acceptance Criteria
- [x] Tests: first level of a later world open from start, its second level locked (ProgressTests). EditMode 64/64.
- [x] Screenshots: level select with five open firsts, last-level win panel, ending card EN/TR
      (production/qa/evidence/final-review/).
- [x] Release check: every shipped level solvable at par and its stored solution wins (CatalogLevelsTests).
