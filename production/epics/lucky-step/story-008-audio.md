# Story 008: Audio: 7/8 music, bell on lucky steps, dice/coin/step sounds

> **Epic**: Lucky Step
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 5 (audio half; split from Story 005 on 2026-11-09)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.5.2f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md`, scoped to this story:*

- [ ] 7/8 aksak-rhythm background music loops without a gap.
- [ ] A bell plays on every lucky step.
- [ ] Dice roll, coin flip and step sounds play in sync with their animations.
- [ ] The music cuts briefly on 13 (or a deadly number in World 4) and comes back.
- [ ] All audio is CC0 or otherwise credited in Assets/Art/CREDITS.md.
- [ ] Audio plays in the WebGL build (after the first user input, per browser autoplay rules).

---

## Implementation Notes

- Audio reacts to the same GameState changes and presentation events as Story 005; it never decides outcomes.
- Sound sources: Kenney Casino Audio + Interface Sounds + Impact Sounds (user download into D:/GameDev/Assets).
- Volumes and the music-cut length are data, not literals.

---

## Out of Scope

- Story 005: visuals and step feedback.
- Story 006: menu audio settings (mute toggle belongs there if added).

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: a documented playtest naming this story in `production/qa/playtests/` (sound cannot be screenshotted)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/lucky-step/story-001-core-rules.md`, `production/epics/lucky-step/story-002-chance-objects.md`
- Unlocks: `production/epics/lucky-step/story-007-itch-release.md`
