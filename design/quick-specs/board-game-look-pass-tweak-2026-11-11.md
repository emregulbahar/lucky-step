# Quick Design Spec: Board-Game Look Pass 2

**Type**: Tweak
**System**: Presentation (BoardTheme, BoardView, TableView, BoardCameraRig, Hud/ScreenFlow UI)
**GDD Reference**: design/game-brief.md — MVP item 5; Story 005 (Printed Board look)
**Date**: 2026-11-11

## Change Summary

Make the board read as a printed toy-box board game: coloured squares per world,
clean printed card backs and coloured chip stacks instead of grey obstacles, a
tighter camera with a dressed game-night table, and HUD/menu panels styled as
game cards and the box lid. No rule or level changes.

## Motivation

Screenshots (`production/qa/evidence/story-009-world5/08-Level30-start.png`,
`production/qa/evidence/tutorial-hints/02-level07-en.png`) show a cream board
with grey/black obstacles: low colour, obstacles read as clutter, the card backs
look like blank phone screens. About 20% of the frame is a dead dark wall band at
the top plus an empty felt strip at the bottom. The jam judges on first impression.

Earlier user feedback that still applies: blue cards/chips were rejected, and so
were "scribbly" hand-drawn card/chip patterns.

## New Rules / Values

1. **Squares** — world-tinted checker: alternate cream (`Board_Cream`) and one
   world colour (W1 green, W2 warm yellow, W3 coral, W4 Chinese red, W5
   wood/ochre), desaturated enough that the spike strip, hole, paw prints and
   the lucky glow stay readable. Colours come from `BoardTheme` data, not code.
2. **Card-deck obstacles** — card backs get a clean printed design: deep red
   field, cream border, simple diamond lattice. No blue, no hand-drawn scribbles.
3. **Chip-stack obstacles** — banded stacks in red / cream / gold (+ green
   accent). No blue.
4. **Camera** — the board fills more of the frame: lower margin, pitch tuned so
   the top wall band is gone (or shows lit table, not black) and the bottom felt
   strip is at most ~8% of the frame. Works for the smallest (Level01) and the
   largest board.
5. **Table dressing** (`TableView`) — besides the mug and the game box: a box
   lid with the "Lucky Step" logo, 2 dice, a small fan of cards, a rule booklet.
   Own Blender models or CC0 packs (Kenney/KayKit) only; never covering the
   board or the HUD.
6. **UI** — HUD panels and the key-hint panel become game cards (coloured header
   strip, rounded card, corner pip); the main menu becomes the box lid (logo on
   top, buttons as cards). Same layout, text and localisation keys.

Order: 1–5 first, 6 after; the WebGL build (Story 007) keeps its time slot
before the jam deadline (14 Nov 12:00 UTC).

## Affected Systems

| System | Impact | Action Required |
|---|---|---|
| BoardTheme / materials | new colours, card-back texture | update asset + `tools/blender`/print-texture script |
| BoardView | per-world floor tint | small code change |
| BoardCameraRig | margin/pitch values | tune serialized values |
| TableView | new props | add props |
| Hud / ScreenFlow / UiStyle | card styling | restyle, no flow change |
| Level data, rules, solver | none | no action |

## Acceptance Criteria

- [ ] Each world shows its own tinted checker; screenshots of the first level of all 5 worlds
- [ ] Spike, hole, bridge, paw prints, lucky glow and the 13 pulse stay readable on every tint
- [ ] No blue on cards or chips; card backs are clean printed patterns
- [ ] No dead black band at the top; the board fills the frame on Level01 and on the largest level
- [ ] Table props never overlap the board or the HUD in any level
- [ ] HUD, key-hint panel, main menu and level select restyled; EN and TR screenshots
- [ ] No regression: EditMode tests pass (47/47), levels unchanged
- [ ] Evidence: `production/qa/evidence/look-pass-2/` before/after screenshots

## GDD Update Required?

No — the brief's MVP item 5 still describes the look; no rule changes.
