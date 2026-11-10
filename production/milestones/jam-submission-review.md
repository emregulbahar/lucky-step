# Milestone Review: Jam submission (Lucky Step / Yedinci Adım)

Inputs: story files (FOUND), design/game-brief.md (FOUND), docs/tech-debt-register.md (FOUND),
production/qa/smoke-2026-11-10.md (FOUND); milestone definition, sprint reports, bug files, risk register,
sprint-status.yaml (ABSENT — workflow: minimal).

## Overview
- **Target Date**: 2026-11-14 12:00 TR (team), 15:00 TR (jam)
- **Current Date**: 2026-11-10 (evening)
- **Days Remaining**: ~3.5
- **Sprints Completed**: N/A — workflow: minimal, no sprints

## Feature Completeness

### Fully Complete
| Feature | Acceptance Criteria | Test Status |
|---------|-------------------|-------------|
| 001 Core rules | met in play | partial (TD-001: 8 ACs never run as tests) |
| 002 Chance objects | met; bridge rule changed 2026-11-10 | StepRulesTests (6) |
| 003 Cat and mirror | met | no tests (TD-003) |
| 004 BFS solver + par | met; 30/30 levels `ok` | no unit tests |

### Partially Complete
| Feature | % Done | Remaining Work | Risk to Milestone |
|---------|--------|---------------|------------------|
| 005 Look and feedback | ~95% | /story-done (visuals frozen by user); vignette-near-13 decision | Low |
| 006 Levels and menus | ~95% | /story-done; 30 levels in 5 worlds built | Low |
| 009 World 5 (wood, bead) | ~95% | manual play of World 5, /story-done | Low |

### Blocked
None (no story Blocked).

### Not Started
| Feature | Priority | Can Cut? | Impact of Cutting |
|---------|----------|----------|------------------|
| 007 WebGL on itch.io (credits, AI disclosure) | MUST | No — it is the submission | No entry |
| 008 Audio (7/8 music, bell, dice/coin/step) | High | Partly — keep bell + step + dice/coin, cut music cut-on-13 | Game feels flat |
| Tutorial / onboarding (user request, not in brief) | High | Simplify — short hints per new object | Players stuck (user stuck on 4-5) |

## Quality Metrics
- **Open bugs**: NOT ASSESSED — NO DATA (no production/qa/bugs)
- **Tests**: EditMode 43/43 pass; Logic stories 001, 003, 004 without own tests
- **Performance**: NOT ASSESSED — no profile; WebGL fps never measured; the 2026-11-09 WebGL build was killed for low memory
- **Smoke check**: production/qa/smoke-2026-11-10.md — NOT ASSESSED (perf and World 5 not checked by hand), nothing failed

## Code Health
- **TODO / FIXME / HACK**: 0 / 0 / 0 in Assets/Scripts and Assets/Editor
- **Technical debt**: TD-001..TD-005 open (tests, rule sets in code, Core hardening; TD-005 Level01 note is stale)

## Risk Assessment
| Risk | Status | Impact if Realized | Mitigation Status |
|------|--------|-------------------|------------------|
| WebGL build fails, too big, or slow (memory) | Open, untested since 2026-11-09 | No playable entry | Build + browser fps check on 2026-11-11 |
| Difficulty spike / no onboarding | Realized (user stuck on 4-5) | Judges quit early | Tutorial hints + soften generated levels |
| Audio not started | Open | Weak feel | Kenney CC0 packs; small scope |
| itch page (credits, AI disclosure) | Open | No prize without AI disclosure | Text from Assets/Art/CREDITS.md |
| Large uncommitted work | Open | Loss of work | Commit per story after /story-done |

## Velocity Analysis
NOT ASSESSED — NO DATA (no sprints). Observed on 2026-11-10 alone: bridge rule, blur fixes, 14 new levels, World 5.

## Scope Recommendations
### Protect
- 007 WebGL itch release (the submission)
- Onboarding for each object (short tutorial hints)
### At Risk
- 008 Audio at full scope
### Cut Candidates
- Music cut on 13, vignette near 13, TD-002/TD-004 refactors, more levels

## Go/No-Go Assessment
**Recommendation**: CONDITIONAL GO

**Conditions**:
- WebGL build plays in a browser at an acceptable fps by end of 2026-11-11
- Tutorial hints for every new object, plus a difficulty check of levels 17-30, by 2026-11-12
- Minimal audio (bell, step, dice, coin) by 2026-11-13
- itch page with credits + AI disclosure uploaded by 2026-11-14 10:00 TR

**Rationale**: Gameplay, content (30 levels, 5 worlds), look and menus are done and verified; the remaining
work is release, onboarding and audio, which fit in 3.5 days only if WebGL is proven first.
PR-MILESTONE skipped — Solo mode.

## Action Items
| # | Action | Owner | Deadline |
|---|--------|-------|----------|
| 1 | /story-done 005, 006, 009 + commit/push per story | user + Claude | 2026-11-11 |
| 2 | WebGL build + browser fps test | Claude | 2026-11-11 |
| 3 | Tutorial (/quick-design, then build) | Claude | 2026-11-12 |
| 4 | Difficulty pass on levels 17-30 | user plays, Claude tunes | 2026-11-12 |
| 5 | Audio minimal set (Story 008) | Claude | 2026-11-13 |
| 6 | itch page, credits, AI disclosure, full run (Story 007) | user + Claude | 2026-11-14 10:00 TR |
