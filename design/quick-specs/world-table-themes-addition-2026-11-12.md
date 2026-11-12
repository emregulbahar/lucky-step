# Quick Design Spec: World Table Themes

**Type**: Addition
**System**: Presentation (TableView, BoardTheme, scene lights)
**GDD Reference**: `design/game-brief.md` (no GDD at minimal); Story 005 look, look pass 2
**Date**: 2026-11-12

## Change Summary
Each world's table matches its name: its own table surface, light colour and
3-4 table props. The board, pieces, rules and levels stay the same.

## Motivation
User request 2026-11-12: "Çin pazarı, eski pazar vs. — isimlere göre
maplerde oynama yapalım, masayı vs. değiştirelim." All 30 levels currently
sit on the same green felt; the world names (Black Cat Alley, Hall of
Mirrors, Chinese Neighbourhood, Old Bazaar) are not visible on screen.

## New Rules / Values
Per world (index = LevelCatalog world index; BoardView already resolves it):

| World | Table surface | Light | Props (beside/behind the board, never on it) |
|---|---|---|---|
| 1 Starting Square | green felt (current) | warm lamp (current) | mug, dice, card fan, box, booklet (current) |
| 2 Black Cat Alley | dark navy felt | cool moonlight key + warm lamp fill | milk saucer, yarn ball, small street lantern |
| 3 Hall of Mirrors | purple velvet cloth | dim, warm candle gold | lit candles (KayKit), hand mirror, candlestick |
| 4 Chinese Neighbourhood | red lacquer top | warm reddish | bamboo steamer, bowl with chopsticks, tea cup (Kenney Food Kit), paper lantern |
| 5 Old Bazaar | kilim-patterned cloth | amber | copper tray with tulip tea glasses, nazar charm |

- Data-driven: one `WorldDecor` entry per world in BoardTheme (surface
  material, key/fill light colours and intensities, prop list with model,
  size, yaw, placement slot). World 1 = today's values.
- Props reuse CC0 packs already approved for the public repo (Kenney Food
  Kit, Furniture Kit, KayKit Dungeon candles) with their License.txt, or own
  simple Blender models in `tools/blender/make_models.py`.
- WebGL budget: <= 6 extra props per world, no extra shadow-casting lights,
  generated textures <= 512 px.

## Affected Systems
| System | Impact | Action |
|---|---|---|
| TableView / BoardTheme | per-world surface and props | code + theme data |
| Scene lights (BuildBoardScene) | per-world colour | runtime tint from WorldDecor |
| CREDITS.md | new CC0 props | update |
| Levels, rules, UI | none | none |

## Acceptance Criteria
- [ ] Screenshot of one level per world shows that world's table, light and props
- [ ] Props never cover board spaces or the score track; camera framing unchanged
- [ ] World 1 looks as before
- [ ] WebGL build still runs; no new console errors
- [ ] CREDITS.md lists every new third-party asset

## GDD Update Required?
No.
