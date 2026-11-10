# Story 009: World 5 "The Old Bazaar": wooden tile and evil-eye bead

> **Epic**: Lucky Step
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-10

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 6 (World 5) + Rules detail (Wooden tile, Evil-eye bead)
**Spec**: `design/quick-specs/world5-wood-and-bead-addition-2026-11-10.md`

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.5.2f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From the spec's Acceptance Criteria:*

- [ ] Stepping straight onto wood leaves the counter unchanged; the cat still moves.
- [ ] A wood step can shift the lucky count onto the exit.
- [ ] A bead wards off a spike death once; a second spike kills.
- [ ] A bead does not save from a gap.
- [ ] With a bead, the pawn survives 13 and enters the exit on 14.
- [ ] Re-entering a taken bead tile gives no extra bead.
- [ ] Undo after a pickup or a ward restores the bead on the board and the HUD count (observed in build).
- [ ] Levels 25-30 are solver-verified (`ok` against their par lines), and in each one removing a wood or bead tile changes par or makes it unsolvable.
- [ ] Wood planks, bead, pickup and ward puff are visible and readable in a Win64 build (retained screenshots).
- [ ] No regression: levels 01-24 keep their par (solver), and all earlier EditMode tests pass.

---

## Implementation Notes

- Rules live only in `StepRules.Step`; new state (`Beads`, `TakenBeads`) is in `GameState`, so undo, restart and the BFS solver cover it with no extra code.
- Wood is judged on the tile entered before a dice fling; bead pickup happens before the ward in the same step.
- Presentation reads state only: `BoardView` (planks, `BeadView`), `StepFeedback` (no lucky glow on an uncounted step), `Hud` (bead line, hidden in levels without beads).
- Models come from `tools/blender/make_models.py` (`bead`, `tile_wood`); `BuildBoardScene` maps their slots and assigns the theme.

---

## Out of Scope

- Tutorial or in-level hints for the new objects (a separate change).
- Audio for pickup and ward (Story 008).

---

## Files

- Assets/Scripts/Core/StepRules.cs, GameState.cs, Level.cs, LevelLoader.cs
- Assets/Scripts/Presentation/BoardView.cs, BoardTheme.cs, StepFeedback.cs
- Assets/Scripts/UI/Hud.cs, ScreenFlow.cs, UiStyle.cs
- Assets/Scripts/Gameplay/PlayerInputController.cs (replay `Z` = undo, QA only)
- Assets/Editor/BuildBoardScene.cs, LevelCatalogBuilder.cs
- tools/blender/make_models.py, Assets/Art/Models/bead.fbx, tile_wood.fbx
- Assets/Resources/Localization/strings_en.json, strings_tr.json
- Assets/Levels/Level25.txt .. Level30.txt, par.json
- Assets/Tests/EditMode/Editor/WoodAndBeadTests.cs

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory).*

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Editor/WoodAndBeadTests.cs` (advisory at qa.level minimal); screenshots in `production/qa/evidence/story-009-world5/`

**Status**: [x] Created (9 tests; watched fail 9/9 before the rules existed)

---

## Dependencies

- Depends on: `production/epics/lucky-step/story-002-chance-objects.md`, `production/epics/lucky-step/story-003-cat-and-mirror.md`, `production/epics/lucky-step/story-006-levels-and-menus.md`
- Unlocks: none

---

## Completion Notes
**Completed**: 2026-11-10
**Criteria**: 10/10 passing
**Deviations**: Advisory: the spec says levels 29 and 30 combine wood and bead with earlier objects; level 30 has no wooden tile (two beads, mirror, bridge, flowerpot, cat). Level 26 has 3 shortest solutions (the others are unique).
**Test Evidence**: `Assets/Tests/EditMode/Editor/WoodAndBeadTests.cs` (13 tests; waived at qa.level minimal); screenshots `production/qa/evidence/story-009-world5/` 01-19
**Reviewed by**: self, no independent review (`/code-review` of 2026-11-10: APPROVED WITH SUGGESTIONS, suggestions applied)
