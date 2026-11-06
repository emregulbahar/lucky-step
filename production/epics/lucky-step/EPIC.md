# Epic: Lucky Step

> **Tier**: minimal (built from `design/game-brief.md`; no GDD or ADRs)
> **Status**: Stories created

## Goal

You are a tiny board-game pawn on a wooden board where every chance object (spike, dice, coin bridge, falling flowerpot) works against you, except on steps 7, 14 and 21, and step 13 is always death.

## Scope

The brief's MVP list (features 1-7) and its Rules detail section. Out of scope: as listed in the brief.

## Order

The brief's Build order:
1. Grid + counter + 7/13 rules + spike (prove the core idea is fun with ugly boxes). Test WebGL on itch on day 2.
2. Dice + coin bridge + cat, then the first 3 levels.
3. Visual checkpoint (Sun 8 Nov video), then mirror, flowerpot, solver, worlds 2-4.
4. Audio, then the full-run balance pass.

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Core rules: grid, step counter, lucky/deadly, exit, undo, restart, text levels | Logic | Ready | N/A (minimal) |
| 002 | Chance objects: spike, dice tile, coin bridge, falling flowerpot | Logic | Ready | N/A (minimal) |
| 003 | Black cat route and broken mirror | Logic | Ready | N/A (minimal) |
| 004 | BFS solver editor tool + par | Logic | Ready | N/A (minimal) |
| 005 | Wooden board look, lucky/13 feedback, audio | Visual/Feel | Ready | N/A (minimal) |
| 006 | 16 levels in 4 worlds, menu, level select, golden pawn, EN/TR | UI | Ready | N/A (minimal) |
| 007 | WebGL release on itch.io with credits and AI disclosure | Integration | Ready | N/A (minimal) |
