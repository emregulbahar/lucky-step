# Story 002: Chance objects: spike, dice tile, coin bridge, falling flowerpot

> **Epic**: Lucky Step
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-06

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 2 (+ Rules detail: dice fling path, coin bridge)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.5.2f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md`, scoped to this story:*

- [ ] Spike: stepping on it on a normal step kills; on a lucky step it is retracted and safe.
- [ ] Dice tile: flings the pawn in its movement direction, 1 tile on a normal step, 6 tiles on a lucky step; the fling counts as 1 step.
- [ ] Dice fling path: stops on the last free tile before a wall; tiles passed over do not trigger (gaps are jumped); only the landing tile triggers.
- [x] Coin bridge (rule changed 2026-11-10): any lucky step opens ALL bridges on the board permanently, wherever the pawn is (stays open for the rest of the level and across later steps); stepping onto a closed bridge on a normal step kills; stepping onto one on the lucky step is safe. Verified by `Assets/Tests/EditMode/Editor/StepRulesTests.cs` (6 tests).
- [ ] Falling flowerpot: passing under it on a normal step kills; on a lucky step it misses.
- [ ] Each object's outcome is deterministic: the same state and input always give the same result.
- [ ] Object state (e.g. bridge open) is part of `GameState`, so undo and restart restore it.
- [ ] All objects load from the text level format.

---

## Implementation Notes

- Each object resolves inside the shared `Step` function from Story 001; no randomness anywhere (the "roll" and "flip" are presentation only).
- Lucky/deadly come from the active rule set, so World 4 works without object changes.

---

## Out of Scope

- Story 003: black cat, broken mirror.
- Story 005: dice roll, coin flip and pot fall animations.

---

## Files

- Assets/Scripts/Core/StepRules.cs
- Assets/Scripts/Core/GameState.cs
- Assets/Scripts/Core/Level.cs
- Assets/Scripts/Core/RuleSet.cs
- Assets/Scripts/Core/LevelLoader.cs
- Assets/Levels/Level02.txt

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory).*

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/ChanceObjects/ChanceObjectsTests.cs` (advisory at qa.level minimal)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/lucky-step/story-001-core-rules.md`
- Unlocks: `production/epics/lucky-step/story-004-bfs-solver.md`, `production/epics/lucky-step/story-006-levels-and-menus.md`

---

## Completion Notes
**Completed**: 2026-11-06
**Criteria**: 3/8 verified (determinism, object state in GameState, loader characters). Untested criteria: spike, dice 1/6, dice fling path, coin bridge, flowerpot (user answered "not tested yet"). Verdict was NOT ASSESSED; the user chose to close and log as tech debt (TD-003).
**Deviations**: ADVISORY — preset rule-set values still hardcoded (TD-002); unity-specialist hardening items not applied (TD-004).
**Test Evidence**: waived at qa.level minimal; Level01 regression capture `production/qa/evidence/story-002-chance-objects/01-level01-regression.png`.
**Reviewed by**: self, no independent review
**Rule change 2026-11-10**: coin bridge now opens on ANY lucky step (all bridges at once). Old rule ("be on THIS tile at 7") made the bridge a spike with memory. `Level.AllBridgesMask` added; `StepRulesTests` written first and watched fail (3/6) against the old rule. Bridge levels redesigned (07, 08, 12, 16); World 1 bridges in 03 and 04 became gaps, since without a cat or mirror a bridge can never matter.
