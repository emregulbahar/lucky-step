# Story 003: Black cat route and broken mirror

> **Epic**: Lucky Step
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-06

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 3 (+ Rules detail: black cat, broken mirror)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.5.2f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md`, scoped to this story:*

- [ ] Black cat walks a fixed route defined in the level file, 1 tile per player step (deterministic).
- [ ] If the cat and pawn end on the same tile, or swap tiles in one step, the counter resets to the rule set's start value (0 by default, 5 in World 4). This is not a fail; play continues.
- [ ] Broken mirror: stepping on it makes the counter count down on each following step.
- [ ] While counting down, the counter stops at the rule set's start value and then counts up again; the start value is never lucky.
- [ ] The rule set carries a start value (default 0, World 4 = 5); a level starts with the counter at that value (`CreateInitialState`), and restart returns to it.
- [ ] Lucky and deadly checks (including exactly 13 killing) apply to the counter value while counting down too.
- [ ] Cat position and mirror direction are part of `GameState`, so undo and restart restore them.

---

## Implementation Notes

- Cat route as an ordered list of tiles (loop or ping-pong, defined in the level file); cat index advances inside `Step`.
- Swap detection: compare old/new positions of both pawn and cat in the same step.

---

## Out of Scope

- Story 005: cat model (Kenney Cube Pets, painted black) and animation.
- Out of scope per brief: ladder, clover.

---

## Files

- Assets/Scripts/Core/StepRules.cs
- Assets/Scripts/Core/GameState.cs
- Assets/Scripts/Core/Level.cs
- Assets/Scripts/Core/RuleSet.cs
- Assets/Scripts/Core/LevelLoader.cs
- Assets/Levels/Level03.txt

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory).*

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Modifiers/CatMirrorTests.cs` (advisory at qa.level minimal)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/lucky-step/story-001-core-rules.md`
- Unlocks: `production/epics/lucky-step/story-004-bfs-solver.md`

---

## Completion Notes
**Completed**: 2026-11-06
**Criteria**: 7/7 passing, verified by running StepRules in Unity batch mode (`production/qa/evidence/story-003-cat-and-mirror/replay-log.txt`, `replay2-checks.txt`, 15 checks, 0 failures). Limits: undo/restart checked at state-transition level, not through PlayerInputController keys; "counting down onto 13 kills" checked with a hand-built state because normal play cannot reach it.
**Deviations**: ADVISORY — remaining unity-specialist hardening (Core .asmdef, array cloning, InvariantCulture, count clamp) logged in TD-004; mirror sets the direction rather than toggling it (design confirmation pending); cat route may start on the pawn's start tile; Level01 unsolvable (TD-005, from Story 001).
**Test Evidence**: waived at qa.level minimal; replay evidence above.
**Reviewed by**: self, no independent review
