# Lucky Step — itch.io page draft (Unity Game Jam 2026, theme "Lucky Number")

## Short description (one line)
A puzzle on a board game where luck is a number: everything works against you, except on step 7.

## Description
You are a tiny figure lost on a printed board game. Every move adds 1 to the step counter, and the board's chance
objects listen to it:

- **Step 7 is lucky.** Spikes pull back, falling flowerpots miss, a die throws you 6 spaces instead of 1, and every
  coin bridge on the board opens.
- **Step 13 is death.** Always.
- **The exit door only opens on a lucky step.**

So you plan your path, burn steps, and decide what your one lucky 7 is spent on. Then the superstitions arrive:
a **black cat** that resets your count when it crosses your path, a **broken mirror** that makes it count backwards,
a **Chinese neighbourhood** where 8 is lucky and every number with a 4 is deadly, and an **old bazaar** where you
knock on wood (that step does not count) and carry an **evil-eye bead** against bad luck.

- 30 hand-made puzzles in 5 worlds, every one checked by a solver
- A golden pawn for finishing in par moves
- No randomness at all: luck is just a number you can count to
- English and Turkish

## Controls
- Arrow keys / WASD: move one space
- Z: undo (unlimited) · R: restart · Esc: menu
- Click the game first if the keys do not respond.

## Theme
"Lucky Number" is the rule itself: the lucky 7 and the deadly 13 decide what every object on the board does, and
World 4 swaps them for 8 and 4, numbers that are lucky and unlucky in Chinese culture.

## Credits
Made by theyeg for the Official Unity Game Jam 2026.

All third-party assets are CC0 (public domain), credit given anyway:
- 3D models (furniture, food, bridge, cat, character) and sound effects: **Kenney** (www.kenney.nl), CC0
- Candles: **Kay Lousberg**, KayKit Dungeon Pack (www.kaylousberg.com), CC0
- Music: **Cozy Puzzle In-Game 3** and **Cozy Puzzle Title** by **MintoDog** (OpenGameArt), CC0
- Font: **Fredoka**, The Fredoka Project Authors, SIL Open Font License 1.1
- Board, dice, coins, spikes, mirror, portal, bead and table props: made for this game (Blender, scripted)

## AI disclosure (required by the jam rules)
Generative AI was used while making this game: **Claude Code (Anthropic)** helped write the game's C# code, the
editor tools, the level solver and the Blender scripts that build the game's own 3D models, and helped draft
in-game text. Game concept, rules, level choices, art direction and every final decision are by the developer.
No AI-generated images, music or sound effects are used; all audio and third-party models are the human-made CC0
assets listed above.

## Upload notes
- Kind of project: HTML. Upload a zip of Builds/WebGL with index.html, Build/, TemplateData (if any),
  StreamingAssets (if any); never the *_BurstDebugInformation_DoNotShip folder.
- Embed: 1280 x 720, "Mobile friendly" off, "Fullscreen button" on, "Click to launch in fullscreen" off.
- Tags: puzzle, board-game, turn-based, unity-game-jam, cute
