# Quick Design Spec: Guided First Level and New-Mechanic Cards

**Type**: Addition
**System**: Tutorial (HUD, ScreenFlow, BoardView), Progress
**GDD Reference**: `design/game-brief.md`; replaces the Level01 text hint of `tutorial-hints-addition-2026-11-10.md`
**Date**: 2026-11-13

## Change Summary
1. **Guided first level**: in Level01 a glowing marker sits on the next space
   to step on and a speech bubble above the figure says what to do and why,
   changing as the player moves.
2. **New-mechanic cards**: the first time a level contains a mechanic the
   player has not met (spike, flowerpot, dice, coin bridge, gap, black cat,
   mirror, wood, evil-eye bead), a "New!" card opens before play: icon, name,
   one sentence per new mechanic, and an "OK" button. Each mechanic's card is
   shown once (saved).

## Motivation
User request 2026-11-13: "tutorial'ı daha düzgün, daha profesyonel yapalım".
The current tutorial is a plain text strip; players read instead of doing.
Approved options (user, 2026-11-13: "önerilerini yap"): A (guided first
level) + C (mechanic cards).

## New Rules / Values
- Guided levels: `LevelCatalog.guidedLevels` (default `Level01`). The top hint
  strip is hidden there; the bubble replaces it.
- Guide state is a pure function `TutorialGuide.Evaluate(level, state, moves)`:
  - phase Start (no moves yet), Counting, LuckyNext (the next step is lucky),
    Dead, Won;
  - target space = the first move of a shortest solution from the current state
    (`LevelSolver.Solve(level, state)`); none when dead, won or unsolvable.
- Bubble text per phase (EN/TR keys `guide.*`), counter shown in Counting.
- Marker: a pulsing gold disc on the target space (BoardTheme values).
- Mechanic ids, in display order: spike, flowerpot, dice, bridge, gap, cat,
  mirror, wood, bead (`LevelMechanics.Find(level)`). A card lists at most 3
  rows; any more wait for the next level. Seen ids are saved in Progress.
- Flow: Story card (first level of a world) -> mechanic card (if any unseen)
  -> Playing. OK, Enter, Space or gamepad South closes the card.
- QA: `--ui tips --level LevelNN` opens that level's card regardless of what
  has been seen.

## Also (progression follow-up, user 2026-11-13)
The rank-5 star above the figure is removed (the user did not understand it).
Rank 5 becomes a diamond base (pale cyan), so every rank still looks different.

## Affected Systems
| System | Impact | Action |
|---|---|---|
| LevelSolver | solve from any state | overload + test |
| TutorialGuide, LevelMechanics (Core) | new pure logic | code + tests |
| Progress | seen mechanic ids | code + tests |
| BoardView / BoardTheme | guide marker; star removed | code |
| Hud, TipCard, ScreenFlow | bubble, card, flow | code |
| Localization EN/TR, UI sprites | new strings, 9 icons | data |

## Acceptance Criteria
- [ ] Tests: guide phases and target on Level01 states; mechanics found in a parsed level; seen tips saved
- [ ] Screenshot: Level01 start with marker + bubble; mid-level bubble with counter; LuckyNext bubble
- [ ] Screenshot: mechanic card for Level02 (TR) and for a cat level
- [ ] No regression: existing hint strip in other levels, story cards, 52 tests
