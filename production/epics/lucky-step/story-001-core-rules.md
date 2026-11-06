# Story 001: Core rules: grid, step counter, lucky/deadly, exit, undo, restart, text levels

> **Epic**: Lucky Step
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-06

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 1 (+ Rules detail: exit door, controls, counter, 13, World 4)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.5.2f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md`, scoped to this story:*

- [ ] A level loads from a text file into a grid (walls, floor, start, exit); a malformed file reports an error instead of loading.
- [ ] WASD/arrows move the pawn exactly one tile per press; moving into a wall does not move the pawn and does not add a step. The pawn cannot wait in place.
- [ ] Every successful move adds 1 to the step counter; the counter never caps.
- [ ] Default rule set: every multiple of 7 (7, 14, 21, 28...) is lucky; 0 is never lucky; only exactly 13 kills, from either direction (113, 213 are harmless).
- [ ] World 4 rule set: multiples of 8 are lucky; any number containing the digit 4 is deadly (4, 14, 24...); deadly wins over lucky (24, 48 kill); 13 is harmless.
- [ ] The rule set is data (selected per level/world), not hardcoded in movement code.
- [ ] The exit door opens only on a lucky step; on any other step it is closed and blocks movement. Reaching the open exit completes the level.
- [ ] Z undoes the last step, unlimited, restoring the full previous state (position, counter); R restarts the level to its initial state.
- [ ] Death (deadly step) stops input except Z and R.
- [ ] The rules run as pure C# (no MonoBehaviour), so the solver (Story 004) can reuse them.

---

## Implementation Notes

- Keep state in one immutable/clonable `GameState` (position, counter, per-object state); undo = a stack of states.
- One `Step(state, direction) -> state` function is the single source of truth for both play and solver.
- Rule set as a ScriptableObject or plain data: `IsLucky(n)`, `IsDeadly(n)`.

---

## Out of Scope

- Story 002: spike, dice, coin bridge, flowerpot.
- Story 003: black cat, broken mirror.
- Story 004: solver and par.
- Story 005: visuals and audio (ugly boxes are fine here).

---

## Files

- Assets/Scripts/Core/RuleSet.cs
- Assets/Scripts/Core/GameState.cs
- Assets/Scripts/Core/Level.cs
- Assets/Scripts/Core/StepRules.cs
- Assets/Scripts/Core/LevelLoader.cs
- Assets/Scripts/Gameplay/PlayerInputController.cs
- Assets/Levels/Level01.txt
- Assets/Scenes/SampleScene.unity (Game object with PlayerInputController + Level01)
- Assets/Scripts/ScreenshotOnArg.cs (run-and-observe capture, CCGS verbatim)

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory).*

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/StepRules/StepRulesTests.cs` (advisory at qa.level minimal)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None
- Unlocks: `production/epics/lucky-step/story-002-chance-objects.md`, `production/epics/lucky-step/story-003-cat-and-mirror.md`, `production/epics/lucky-step/story-004-bfs-solver.md`

---

## Completion Notes
**Completed**: 2026-11-06
**Criteria**: 2/10 verified (AC-6, AC-10); AC-1 half observed (level load seen in `production/qa/evidence/story-001-core-rules/01-level01-initial.png`). Untested criteria: AC-1 (malformed-file error), AC-2, AC-3, AC-4, AC-5, AC-7, AC-8, AC-9. Verdict was NOT ASSESSED; the user chose to close and log as tech debt.
**Deviations**: ADVISORY — rule-set preset values hardcoded in RuleSet.cs (TD-002). Unity-generated files outside scope: Assets/Resources/BillingMode.json, Data/Plugins, ProjectSettings/Packages/com.unity.ai.assistant, Packages/manifest.json (com.unity.ai.assistant added), yedinci-adim.slnx.
**Test Evidence**: waived at qa.level minimal; run observed (Win64 player screenshot above). Untested behaviour logged as TD-001 in `docs/tech-debt-register.md`.
**Reviewed by**: self, no independent review
