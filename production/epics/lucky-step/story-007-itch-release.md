# Story 007: WebGL release on itch.io with credits and AI disclosure

> **Epic**: Lucky Step
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 7

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.5.2f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md`, scoped to this story:*

- [ ] `commands.build` produces a WebGL build that opens and plays in a browser on itch.io (tested from day 2, per brief Build order step 1).
- [ ] The uploaded zip contains only `index.html`, `Build/`, `TemplateData/`, `StreamingAssets/`; never the `*_BurstDebugInformation_DoNotShip` folder.
- [ ] The final build uses size-optimized code (not the "shorter build time" debug variant) and stays playable.
- [ ] The itch page lists credits for every asset (Kenney, KayKit, Freesound, fonts) with their licenses.
- [ ] The itch page discloses AI use (jam rule: no prize without it).
- [ ] A full run from level 1 to the last level is played on the itch build without a blocking bug.
- [ ] Submitted by the team's own deadline: 14 Nov 2026 12:00 (TR), before the jam's 15:00 (TR).

---

## Implementation Notes

- Compression is Disabled in Player Settings so the build opens without server headers.
- Unused packages (`com.unity.purchasing`, `com.unity.analytics`) inflate the WebGL size; remove them once the user approves.

---

## Out of Scope

- Story 005 and 006: content and visuals being shipped.

---

## Files

- Assets/Editor/BuildScript.cs
- ProjectSettings/ProjectSettings.asset
- production/qa/evidence/story-007-itch-playtest.md

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory).*

**Story Type**: Integration
**Required evidence**: playtest doc of the itch build, `production/qa/evidence/story-007-itch-playtest.md`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/lucky-step/story-005-look-and-sound.md`, `production/epics/lucky-step/story-006-levels-and-menus.md`, `production/epics/lucky-step/story-008-audio.md`
- Unlocks: None
