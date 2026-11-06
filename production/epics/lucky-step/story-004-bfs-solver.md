# Story 004: BFS solver editor tool + par

> **Epic**: Lucky Step
> **Status**: Complete
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-06

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 4 (+ Rules detail: par, solver)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.5.2f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md`, scoped to this story:*

- [ ] The solver runs a BFS over full game states (position, counter, count direction, cat position, bridge states) using the same `Step` function as gameplay.
- [ ] It reports for a level: solvable yes/no, the shortest solution length (par) and the move sequence.
- [ ] Par for each level is saved where the game reads it (for the golden pawn in Story 006).
- [ ] It runs as a Unity Editor tool (menu item) on one level or all levels, and prints a per-level report.
- [ ] It flags unintended shortcuts: a solution clearly shorter than the designer's intended par, if one is noted in the level file.
- [ ] It terminates on every level (state space bounded, e.g. a counter cap for search, documented in the tool).
- [ ] Results are deterministic: the same level always gives the same par.

---

## Implementation Notes

- Hash states for the visited set; the counter is unbounded in play, so pick a search cap (well above any par) and report "no solution within cap".
- Rule set (default or World 4) comes from the level, as in Story 001.

---

## Out of Scope

- Story 006: designing the 16 levels and showing the golden pawn.

---

## Files

- Assets/Scripts/Core/Solver/LevelSolver.cs
- Assets/Scripts/Core/Solver/ParTable.cs
- Assets/Editor/LevelSolverMenu.cs (menu items instead of the planned LevelSolverWindow.cs)
- Assets/Scripts/Core/Level.cs (IntendedPar)
- Assets/Scripts/Core/LevelLoader.cs (par: header)
- Assets/Levels/par.json (generated)

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory).*

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Solver/LevelSolverTests.cs` (advisory at qa.level minimal)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/lucky-step/story-001-core-rules.md`, `production/epics/lucky-step/story-002-chance-objects.md`, `production/epics/lucky-step/story-003-cat-and-mirror.md`
- Unlocks: `production/epics/lucky-step/story-006-levels-and-menus.md`

---

## Completion Notes
**Completed**: 2026-11-06
**Criteria**: 7/7 passing; 6 verified by the batch run (`production/qa/evidence/story-004-bfs-solver/solver-report.txt`: Level01 proven-unsolvable, Level02 par 7, Level03 par 17, par.json written). Shortcut flag (`par:` header) only checked outside Unity; the menu items were not clicked in the editor (same code ran through SolveAllBatch).
**Deviations**: ADVISORY — LevelSolverMenu.cs menu items instead of the planned LevelSolverWindow.cs; unsolvable level without a par: line only warns (logged under TD-005); Level01 unsolvable (TD-005).
**Test Evidence**: waived at qa.level minimal; solver report and par.json snapshot above.
**Reviewed by**: self, no independent review
