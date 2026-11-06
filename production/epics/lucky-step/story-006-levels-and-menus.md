# Story 006: 16 levels in 4 worlds, menu, level select, golden pawn, EN/TR

> **Epic**: Lucky Step
> **Status**: Ready
> **Layer**: Feature
> **Type**: UI
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 6 (+ Rules detail: par / golden pawn)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.5.2f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md`, scoped to this story:*

- [ ] 16 levels in 4 worlds (4 each), each world a different region of the board and each teaching its new object, then combining earlier ones. Fallback per brief: 12 good levels.
- [ ] World 4 is the Chinese neighborhood and uses the World 4 rule set (multiples of 8 lucky, digit 4 deadly, 13 harmless).
- [ ] Every level passes the solver (Story 004): solvable, with a recorded par.
- [ ] Level 1 gives an "aha" moment within about 10 seconds (brief risk note).
- [ ] Main menu and level select; level progress (completed levels) is kept between sessions.
- [ ] Completing a level in par moves earns a golden pawn, shown in level select; it is optional.
- [ ] HUD shows the step counter and par.
- [ ] English is the default language, with a Turkish option; all UI text switches.
- [ ] A short story card at the start of each world (the pawn's board regions).

---

## Implementation Notes

- Keep strings in a table (EN/TR) from the start; no inline UI text.
- Progress save is limited to completed levels and golden pawns (brief: no save system beyond level progress).

---

## Out of Scope

- Out of scope per brief: player level editor, saves beyond level progress, ladder, clover.

---

## Files

- Assets/Levels/World1..World4/
- Assets/Scripts/UI/MainMenu.cs
- Assets/Scripts/UI/LevelSelect.cs
- Assets/Scripts/UI/Hud.cs
- Assets/Scripts/UI/Localization.cs
- Assets/Scripts/Core/Progress.cs
- Assets/Scenes/

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: a retained screenshot of each screen touched (main menu, level select, HUD, story card, EN and TR), in `production/qa/evidence/` (not waived at qa.level minimal)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/lucky-step/story-002-chance-objects.md`, `production/epics/lucky-step/story-003-cat-and-mirror.md`, `production/epics/lucky-step/story-004-bfs-solver.md`
- Unlocks: `production/epics/lucky-step/story-007-itch-release.md`
