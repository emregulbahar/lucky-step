# Game Brief: Lucky Step (TR: Yedinci Adım)

**One-sentence pitch:** You are a tiny toy figure lost inside a wooden board game where every chance object (spike, dice, coin bridge, falling flowerpot) works against you, except on step 7, step 13 is always death, and only the black cat's counter reset buys you another 7.

## Core loop
- Plan a path on the grid; every move adds 1 to the step counter and you can't wait in place.
- Step on chance objects: normal steps hurt, the lucky step (7) flips them in your favor.
- Use the black cat to reset the counter to 0 when you need another lucky 7.
- Burn steps by walking back and forth so the counter hits a lucky number on the right tile; avoid 13.
- Reach the goal, next level (R restarts).

## Player goal & fail state — what "working" looks like
- Goal: reach the exit tile of each of 30 levels (5 worlds x 6; extended from 16 on 2026-11-10, World 5 added the same day). No real randomness; every outcome is deterministic and solvable.
- Fail: die on a chance object or on step 13. Restart is instant. The black cat is not a fail: it only resets the counter (to 0; 5 in World 4).

## MVP — what must exist to be the game
1. Grid movement + step counter + lucky 7 + deadly 13 + R restart, levels loaded from text.
2. Chance objects: spike, dice tile (throws you 1 square, or 6 on lucky), coin bridge (every lucky step opens all bridges, wherever you stand), falling flowerpot (misses you on a lucky step, kills on a normal step).
3. Black cat (resets counter to the start value) and broken mirror (counter counts down).
4. BFS solver as an Editor tool, used to verify every level is solvable and has no shortcut.
5. Look and sound: 3D wooden board-game look (single painted-wood material, lamp light, tilt-shift blur), gold glow on lucky steps, edge darkening near 13. Audio: 7/8 aksak-rhythm music, bell on lucky steps, dice/coin/step sounds, music cuts briefly on 13.
6. 30 levels in 5 worlds (6 each: 4 that teach the world's new object, then 2 harder ones that combine it with earlier objects); world 4 is the Chinese neighborhood (8 lucky / 4 deadly), the jam's "culturally symbolic number" hit; world 5 is the Old Bazaar (wooden tile and evil-eye bead, see design/quick-specs/world5-wood-and-bead-addition-2026-11-10.md). Menu, level select, English default + Turkish.
7. WebGL build running on itch.io, with credits and AI-use disclosure on the page.

## Out of scope — not building this
- Ladder, clover.
- Roulette wheel, salt, multiplayer, player level editor, saves beyond level progress.
- If 16 levels don't fit, drop to 12 good ones.

## Build order
1. Grid + counter + 7/13 rules + spike (prove the core idea is fun with ugly boxes). Test WebGL on itch on day 2.
2. Dice + coin bridge + cat, then the first 3 levels.
3. Visual checkpoint (Sun 8 Nov video), then mirror, flowerpot, solver, worlds 2-4.
4. Audio, then the full-run balance pass.

## Rules detail
- **Exit door:** opens only on a lucky step; otherwise it is closed and blocks.
- **Controls:** WASD/arrows, one tile per press. Z = undo last step (unlimited). R = restart level.
- **Black cat:** walks a fixed route, 1 tile per player step (deterministic). If it crosses your path (same tile, or swaps tiles with you), the counter resets to its start value (0; 5 in World 4).
- **Dice tile:** flings you in your movement direction: 1 tile on a normal step, 6 tiles on a lucky step. The fling counts as 1 step.
- **Coin bridge:** (decided 2026-11-10) when the counter reaches a lucky count (and not a deadly one), ALL closed bridges on the board open permanently, wherever the pawn is. A spike asks WHERE you spend the lucky step, a bridge asks WHEN: the player chooses what the one 7 is spent on. Stepping onto a closed bridge on a normal step is a fatal fall; stepping onto one on the lucky step is safe (it opens that step). A level needs a second lucky step (black cat or mirror) for a bridge to matter, so World 1 has no bridges; bridge levels are 07, 08, 12 and 16.
- **Counter:** never caps and starts at the world's start value (0; 5 in World 4).
- **Wooden tile (World 5, "knock on wood"):** a step straight onto it does not count: the counter keeps its value and the step is neither lucky nor deadly; the cat still moves. A dice fling landing on wood counts. Spec: design/quick-specs/world5-wood-and-bead-addition-2026-11-10.md.
- **Evil-eye bead (World 5, nazar boncuğu):** picked up on landing (beads stack); each bead wards off one death by bad luck (a deadly count such as 13, a spike, a flowerpot), not a fall (gap, closed bridge). Surviving 13 makes 14 a second lucky step without a cat. Undo restores beads.
- **World 4 (Chinese neighborhood):** multiples of 8 are lucky; any number containing the digit 4 is deadly (4, 14, 24...); 13 is harmless there. The counter starts at 5 in World 4, and the black cat resets it to 5 (not 0), so each run is 6, 7, 8 (lucky) ... 13, then 14 kills.
- **13:** only exactly 13 kills, from either direction (113, 213 are harmless).
- **One lucky step per counter run:** the counter rises by 1 per step and 13 kills, so 14, 21... are never reached; each run from 0 has exactly one lucky step (7). Several lucky events in one level need the black cat's reset. The exit also needs a lucky step, so a level without a cat spends its only 7 on the exit.
- **Broken mirror:** the counter counts down; at the start value (0; 5 in World 4) it stops and counts up again. The start value is never lucky.
- **World 4 precedence:** deadly wins. A multiple of 8 that contains a 4 (24, 48) kills.
- **Dice fling path:** the fling stops on the last free tile before a wall; tiles passed over do not trigger (gaps are jumped); only the landing tile triggers.
- **Par:** each level has a par (shortest solution from the BFS solver); matching it earns an optional golden star.
- **Solver:** the BFS solver models all of the above (cat position, bridge state, mirror direction) so it can verify solvability and par.

---
**Who it's for / what they feel:** Puzzle fans and jam judges who want a clever one-rule puzzle; the click of a lucky step feeling earned.

**Art & audio direction:** "Printed Board on a felt table" (decided 2026-11-09, replaces Parlour Board; the flat navy background was replaced on 2026-11-10): a professional printed board game lying on a green felt card table (#2E784D, soft lamp pool baked into the felt) with a honey wooden rail (#C98B4E) and a warm dark floor beyond (#201712); no room, walls or candle. Two table props beside the board, a mug and a closed game box, outlined like the pieces. Paper spaces (#FFF6E0 / #EDE0C2) as thin card slabs with printed ink borders; dark wooden board border (#5A3A24). Card backs, dark chips and the box are charcoal (#4A4751), other chips cream and brown (#8C5C3D); each chip stack and card deck is one outlined shape (no outline per chip or card). The flowerpot hangs on a rope. 3D pieces look drawn: two-band cel shading with a cool tinted shade, ~2 px ink (#1C1A24) outlines on pieces only (cream outline on the black cat); spaces get printed borders, not geometric outlines. Colour carries meaning only: red (#E23B3B) = hazard, gold (#FFC21A) = lucky and the only bloom, ink + skull = deadly, teal (#18A6A0) = player. Score track: START corner at 0, gold LUCKY star on 7, ink skull on 13. UI: cream cards with 3 px ink border and hard offset shadow, gold primary buttons, Fredoka (OFL), sentence case. Post: no depth of field or vignette, neutral tonemapping, gold-only bloom, one key light with soft-filtered shadows. Lite scope: no per-world tints; if outlines are not working by the evening of 10 Nov, ship the flat two-band look without outlines. No Monopoly name, logo, mascot, layout or trade dress. Audio: clicks, dice rattle, coin ring, bell on 7, 7/8 music.

**Reference game:** Baba Is You / GMTK-winner single-rule puzzles; MVP keeps the one-rule-teach-per-world structure only.
