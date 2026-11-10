# Quick Design Spec: Audio settings (music and effects volume) + step sound tuning

**Type**: Addition
**System**: Audio (AudioFeedback / AudioConfig), main menu UI
**GDD Reference**: `design/game-brief.md` (MVP 5 audio; no GDD at workflow: minimal), `production/epics/lucky-step/story-008-audio.md`
**Date**: 2026-11-10

## Change Summary
Players can set the music volume and the effects volume separately from a Settings card on the main menu.
The step sound changes from a thin chip click to a low footstep thud (user feedback: "too thin").

## Motivation
Player request: the mix is a matter of taste (some want music off, some want quieter steps), and jam judges
often play with other audio running. A thin, high click on every step tires the ear; a low thud reads as a
wooden piece moving on a board.

## New Rules / Values
1. Main menu gets a "Settings" button (EN "Settings", TR "Ayarlar") between "Level select" and "Language".
2. It opens a cream Settings card: title, two rows, Back.
   - Row "Music" / "Müzik": [-] NN% [+]
   - Row "Effects" / "Efektler": [-] NN% [+]
   - Each press changes the value by 10, clamped to 0-100. The value text updates at once.
   - Back (and Esc) returns to the main menu.
3. Keyboard and gamepad navigate the buttons like the other menus; mouse works too. Every button plays the UI click,
   so the effects level is heard while it is set.
4. Final music volume = AudioConfig.musicVolume x Music% / 100; final effects volume = AudioConfig.sfxVolume x Effects% / 100.
   Changes apply live.
5. Defaults: Music 70%, Effects 100%. Values persist in PlayerPrefs (keys `audio.music`, `audio.sfx`); QA runs
   (any QA arg) keep them in memory and never write PlayerPrefs.

## Tuning (step sound)
| Parameter | Old Value | New Value | Rationale |
|---|---|---|---|
| AudioConfig.step clips | chip-lay-1..3 (centroid ~5300 Hz, 0% energy < 400 Hz) | footstep02, 03, 06, 08 (centroid 340-570 Hz, 74-81% < 400 Hz) | user: step "too thin"; wood keeps bookPlace (870 Hz) so the two stay distinct |

## Affected Systems
| System | Impact | Action Required |
|---|---|---|
| AudioFeedback | multiplies volumes by the settings | code |
| MainMenu / ScreenFlow | new Settings button and state | code |
| New SettingsPanel (UiScreen) | the card | code |
| Localization | settings.* keys EN/TR | strings |
| BuildBoardScene | step clips | done |
| Story 008 | ACs gain "volumes adjustable and persisted" | update story when closing |

## Acceptance Criteria
- [ ] Settings button on the main menu opens the card; Back and Esc return.
- [ ] Music and Effects change in 10% steps between 0 and 100; 0% is silent.
- [ ] Changes are audible at once; values survive a restart of the game (PlayerPrefs), not in QA runs.
- [ ] Keyboard/gamepad navigation reaches every button; EN and TR texts fit.
- [ ] Step sound is the low footstep set; wood knock stays different.
- [ ] No regression: menu flow (play, level select, language, quit) and EditMode tests unchanged.

## GDD Update Required?
No GDD. Brief MVP 5 line unchanged (settings are an addition); Story 008 gets the extra AC when it is closed.
