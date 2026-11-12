# Quick Design Spec: In-level tutorial hints

**Type**: Addition
**System**: UI (HUD)
**GDD Reference**: `design/game-brief.md` (risk: "Level 1 aha within ~10 s"; no GDD at workflow: minimal); Story 006 deferred AC
**Date**: 2026-11-10

## Change Summary
Levels that introduce a rule or object show a short hint banner at the top centre of the screen, between the
level card and the key legend, for the whole level. Non-blocking: no extra key press; the board stays visible.

## Motivation
The user got stuck on World 4 level 5, and no screen ever explains the rules: the exit opening only on 7, the
track, or what each object does. A banner that stays up while the player looks at the board teaches without
interrupting play.

## New Rules / Values
1. A level has a hint when the strings file has `hint.<LevelId>` (EN and TR). No key, no banner.
2. The banner shows while the level is played (start, restart and undo alike); it hides on the win panel and in menus.
3. Text is one or two short sentences, sentence case.

## Hints (EN; TR translated in strings_tr.json)
- Level01: Every move adds 1 to the count on the track below. The door opens only if you step in on 7.
- Level02: Spikes and flowerpots hurt on normal steps. On a lucky step (7) they can't touch you.
- Level03: A die throws you 1 tile, or 6 tiles on a lucky step. Tiles you fly over don't count.
- Level05: The black cat resets your count to 0 when your paths cross: a second chance at 7.
- Level07: On any lucky step, every coin bridge on the board opens for good.
- Level09: The broken mirror makes the count go down. At 0 it turns and counts up again.
- Level13: Here 8 is lucky, any number with a 4 kills, and 13 is harmless. You start at 5.
- Level25: Knock on wood: a step onto a wooden tile does not count.
- Level27: Pick up the blue evil eye. It saves you once from bad luck, but not from a fall.

## Affected Systems
| System | Impact | Action Required |
|---|---|---|
| Hud | new hint banner, hidden when the level has no hint | code |
| ScreenFlow | passes the level id to the HUD | code |
| Localization | `hint.Level*` keys EN/TR | strings |

## Acceptance Criteria
- [ ] The nine levels show their hint; other levels show none; EN/TR switch live.
- [ ] The banner never covers the board, the level card, the key legend or the track at 1920x1080.
- [ ] No regression: HUD, win panel and menus unchanged; EditMode tests pass.

## GDD Update Required?
No.
