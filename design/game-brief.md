# Game Brief: Lucky Step (TR: Yedinci Adım)

**One-sentence pitch:** You are a tiny toy figure lost inside a wooden board game where every chance object (spike, dice, coin bridge, falling flowerpot) works against you, except on step 7, step 13 is always death, and only the black cat's counter reset buys you another 7.

## Core loop
- Plan a path on the grid; every move adds 1 to the step counter and you can't wait in place.
- Step on chance objects: normal steps hurt, the lucky step (7) flips them in your favor.
- Use the black cat to reset the counter to 0 when you need another lucky 7.
- Burn steps by walking back and forth so the counter hits a lucky number on the right tile; avoid 13.
- Reach the goal, next level (R restarts).

## Player goal & fail state — what "working" looks like
- Goal: reach the exit tile of each of 16 levels (4 worlds x 4). No real randomness; every outcome is deterministic and solvable.
- Fail: die on a chance object or on step 13. Restart is instant. The black cat is not a fail: it only resets the counter (to 0; 5 in World 4).

## MVP — what must exist to be the game
1. Grid movement + step counter + lucky 7 + deadly 13 + R restart, levels loaded from text.
2. Chance objects: spike, dice tile (throws you 1 square, or 6 on lucky), coin bridge (opens on lucky), falling flowerpot (misses you on a lucky step, kills on a normal step).
3. Black cat (resets counter to the start value) and broken mirror (counter counts down).
4. BFS solver as an Editor tool, used to verify every level is solvable and has no shortcut.
5. Look and sound: 3D wooden board-game look (single painted-wood material, lamp light, tilt-shift blur), gold glow on lucky steps, edge darkening near 13. Audio: 7/8 aksak-rhythm music, bell on lucky steps, dice/coin/step sounds, music cuts briefly on 13.
6. 16 levels in 4 worlds, each a different region of the board and each teaching its new object; world 4 is the Chinese neighborhood (8 lucky / 4 deadly), the jam's "culturally symbolic number" hit. Menu, level select, English default + Turkish.
7. WebGL build running on itch.io, with credits and AI-use disclosure on the page.

## Out of scope — not building this
- Ladder, clover.
- Roulette wheel, salt, evil-eye bead, multiplayer, player level editor, saves beyond level progress.
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
- **Coin bridge:** stepping on it on a lucky step opens it permanently; on a normal step you fall and die.
- **Counter:** never caps and starts at the world's start value (0; 5 in World 4).
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

**Art & audio direction:** Warm 3D miniature board game on a wooden table, open board with a thin wooden rim (no dungeon walls), obstacles are board-game pieces (card stacks, chip stacks), exit is a glowing portal; Kenney CC0 Mini Characters figure as the hero, Cube Pets black cat, furniture; own Blender pieces (die, coin, spike, mirror), lamp light, tilt-shift blur; clicks, dice rattle, coin ring, bell on 7, 7/8 music.

**Reference game:** Baba Is You / GMTK-winner single-rule puzzles; MVP keeps the one-rule-teach-per-world structure only.
