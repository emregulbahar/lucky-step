using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LuckyStep
{
    /// <summary>
    /// Story 006 screen state machine. The UI only requests changes (LoadLevel, MarkCompleted, InputEnabled); the rules
    /// stay in the controller. Transition table:
    /// <list type="table">
    /// <item><term>Menu</term><description>Play: Story (first level of its world) or Playing, at the continue level. Level select: LevelSelect. Settings: Settings.</description></item>
    /// <item><term>Settings</term><description>Music and effects volume. Back, Esc or gamepad East: Menu.</description></item>
    /// <item><term>LevelSelect</term><description>Unlocked level: Story or Playing. Back, Esc or gamepad East: Menu.</description></item>
    /// <item><term>Story</term><description>Any key, click or gamepad South/Start: Tips (if the level has unseen mechanics) or Playing.</description></item>
    /// <item><term>Tips</term><description>New-mechanic card. Got it, Esc or gamepad East: Playing.</description></item>
    /// <item><term>Playing</term><description>LevelWon: Won (progress saved, panel after <see cref="UiStyle.winPanelDelay"/>). Esc or gamepad East: Menu.</description></item>
    /// <item><term>Won</term><description>Next: Story or Playing (next level). Retry: Playing (same level). Level select: LevelSelect. Esc or East: Menu.</description></item>
    /// </list>
    /// Player input is enabled only in Playing, from the frame after entering it, so the key that closed a screen does
    /// not also move the pawn. Every screen re-reads its text on <see cref="Localization.LanguageChanged"/>.
    /// The figure rank (worlds completed, <see cref="Progress.CompletedWorlds"/>) is pushed to the board on every level
    /// load; a win that raises it shows the promotion banner (progression spec 2026-11-12).
    /// Guided levels (<see cref="LevelCatalog.guidedLevels"/>) show the tutorial guide: a marker on the next space and a
    /// bubble over the figure, re-evaluated when the state changes (spec tutorial-guide-and-tips-addition-2026-11-13).
    /// </summary>
    /// <example><code>var flow = new ScreenFlow(ui, controller, catalog, progress, board); flow.Begin(); /* Update: */ flow.Tick(Time.unscaledDeltaTime);</code></example>
    public sealed class ScreenFlow : IDisposable
    {
        /// <summary>Screen states.</summary>
        public enum State { Menu, LevelSelect, Story, Tips, Playing, Won, Settings }

        private readonly UiRoot _ui;
        private readonly PlayerInputController _controller;
        private readonly LevelCatalog _catalog;
        private readonly Progress _progress;
        private readonly BoardView _board;
        private readonly Hud _hud;
        private readonly MainMenu _menu;
        private readonly LevelSelect _select;
        private readonly StoryCard _story;
        private readonly TipCard _tips;
        private readonly WinPanel _win;
        private readonly SettingsPanel _settings;
        private readonly UiScreen[] _screens;

        private State _state;

        /// <summary>True while a level is on screen (Playing or Won).</summary>
        public bool InGame => _state == State.Playing || _state == State.Won;
        private UiScreen _top;
        private int _enteredFrame = -1;
        private float _winTimer = -1f;
        private int _winMoves;
        private int _winPar;
        private bool _winGolden;
        private int _winRankBefore;
        private int _winRankAfter;
        private int _rankOverride = -1;
        private bool _guided;
        private GameState _guideState;
        private Level _guideBoardLevel;
        private Camera _camera;

        /// <summary>
        /// Builds every screen (hidden, HUD lowest) and subscribes to the controller and localization.
        /// <paramref name="board"/> may be null (no figure rank on screen).
        /// </summary>
        public ScreenFlow(UiRoot ui, PlayerInputController controller, LevelCatalog catalog, Progress progress, BoardView board)
        {
            _ui = ui;
            _controller = controller;
            _catalog = catalog;
            _progress = progress;
            _board = board;
            _hud = new Hud(ui);
            _menu = new MainMenu(ui);
            _select = new LevelSelect(ui, catalog, progress, Rank);
            _story = new StoryCard(ui);
            _tips = new TipCard(ui);
            _win = new WinPanel(ui);
            _settings = new SettingsPanel(ui, ui.Volume);
            _screens = new UiScreen[] { _hud, _menu, _select, _story, _tips, _win, _settings };

            _menu.PlayClicked += () => StartLevel(ContinueLevel());
            _menu.LevelSelectClicked += () => Enter(State.LevelSelect);
            _menu.SettingsClicked += () => Enter(State.Settings);
            _settings.BackClicked += () => Enter(State.Menu);
            _select.LevelChosen += StartLevel;
            _select.BackClicked += () => Enter(State.Menu);
            _win.NextClicked += () => StartLevel(_catalog.Next(_controller.CurrentLevelFile));
            _win.RetryClicked += Retry;
            _win.LevelSelectClicked += () => Enter(State.LevelSelect);
            _tips.Closed += () => Enter(State.Playing);

            _controller.LevelLoaded += OnLevelLoaded;
            _controller.LevelWon += OnLevelWon;
            Localization.LanguageChanged += OnLanguageChanged;
        }

        /// <summary>The current state.</summary>
        public State Current => _state;

        /// <summary>
        /// Opens the first screen: the main menu, or what the QA args ask for (see <see cref="QaArgs"/>). With
        /// <c>--moves</c> and no <c>--ui</c> it plays directly (Story 005 replay), on <c>--level</c> or the Inspector level.
        /// </summary>
        public void Begin()
        {
            string lang = QaArgs.Value("--lang");
            if (lang == "tr") Localization.SetLanguage(Language.Turkish);
            else if (lang == "en") Localization.SetLanguage(Language.English);
            if (QaArgs.Has("--progress-demo")) ApplyProgressDemo();
            if (QaArgs.Has("--unlock-all")) _progress.UnlockAll = true; // review build: every level open, nothing saved
            // QA: show the figure at this rank (its base) without changing progress.
            if (int.TryParse(QaArgs.Value("--rank"), out int rank)) _rankOverride = Mathf.Max(0, rank);
            ApplyRank();

            TextAsset level = FindLevel(QaArgs.Value("--level"));
            string ui = QaArgs.Value("--ui");
            if (ui == null && QaArgs.Has("--moves"))
            {
                if (level != null) _controller.LoadLevel(level);
                else if (_controller.Level != null) OnLevelLoaded();
                Enter(State.Playing);
                return;
            }

            if (level == null) level = ContinueLevel();
            if (level == null || !_controller.LoadLevel(level))
            {
                Debug.LogError("ScreenFlow: the level catalog has no playable level.");
                Enter(State.Menu);
                return;
            }
            switch (ui)
            {
                case "select":
                    Enter(State.LevelSelect);
                    break;
                case "settings":
                    Enter(State.Settings);
                    break;
                case "story":
                    EnterStory(_catalog.WorldIndexOf(level));
                    break;
                case "tips":
                    // QA: the level's card whatever has been seen.
                    _tips.SetMechanics(LevelMechanics.Find(_controller.Level));
                    Enter(State.Tips);
                    break;
                case "hud":
                    Enter(State.Playing);
                    break;
                case "win":
                    int par = _catalog.Par(level.name);
                    int now = Rank();
                    // QA --promote: the panel shows the promotion from the rank below to the current one.
                    _winRankBefore = QaArgs.Has("--promote") ? Mathf.Max(0, now - 1) : now;
                    _winRankAfter = now;
                    ShowWinAfter(QaArgs.WinDelaySeconds, Mathf.Max(par, 0), par, par >= 0);
                    break;
                default:
                    Enter(State.Menu);
                    break;
            }
        }

        /// <summary>Per-frame update (unscaled seconds): fades, back/dismiss input, HUD moves, the win panel delay.</summary>
        public void Tick(float deltaTime)
        {
            foreach (UiScreen screen in _screens)
            {
                if (screen.IsVisible) screen.Tick(deltaTime);
            }
            bool entered = Time.frameCount == _enteredFrame;
            switch (_state)
            {
                case State.Playing:
                    _hud.SetMoves(_controller.Moves);
                    _hud.SetBeads(BeadsForHud());
                    UpdateGuide();
                    if (entered) break;
                    _controller.InputEnabled = true;
                    if (BackPressed()) Enter(State.Menu); // Esc in a level opens the main menu (user request 2026-11-10)
                    break;
                case State.LevelSelect:
                case State.Settings:
                    if (!entered && BackPressed()) Enter(State.Menu);
                    break;
                case State.Story:
                    if (!entered && AnyPressed()) EnterTipsOrPlay();
                    break;
                case State.Tips:
                    if (!entered && BackPressed()) Enter(State.Playing);
                    break;
                case State.Won:
                    _hud.SetMoves(_controller.Moves);
                    _hud.SetBeads(BeadsForHud());
                    UpdateGuide();
                    if (_winTimer >= 0f)
                    {
                        _winTimer -= deltaTime;
                        if (_winTimer < 0f)
                        {
                            _top = _win;
                            _win.Show(_winMoves, _winPar, _winGolden, _catalog.Next(_controller.CurrentLevelFile) == null,
                                _winRankBefore, _winRankAfter);
                        }
                    }
                    else if (_top == _win && !entered && BackPressed())
                    {
                        Enter(State.Menu);
                    }
                    break;
            }
            if (_top != null && _top.IsVisible && NavigationPressed()) _top.EnsureSelection();
        }

        /// <summary>Unsubscribes from the controller and the static localization event.</summary>
        public void Dispose()
        {
            _controller.LevelLoaded -= OnLevelLoaded;
            _controller.LevelWon -= OnLevelWon;
            Localization.LanguageChanged -= OnLanguageChanged;
        }

        private void Enter(State state)
        {
            _state = state;
            _enteredFrame = Time.frameCount;
            _winTimer = -1f;
            _controller.InputEnabled = false;
            if (state != State.Playing && state != State.Won && _board != null)
            {
                _board.HideGuide(); // no marker behind menus; the guide re-evaluates when play resumes
                _guideState = null;
            }
            foreach (UiScreen screen in _screens) screen.Hide();
            switch (state)
            {
                case State.Menu: _top = _menu; break;
                case State.LevelSelect: _top = _select; break;
                case State.Story: _top = _story; break;
                case State.Tips: _top = _tips; break;
                case State.Settings: _top = _settings; break;
                default: _top = null; break; // Playing; Won opens its panel after the delay
            }
            if (state == State.Playing || state == State.Won) _hud.Show();
            if (_top != null) _top.Show();
        }

        private void StartLevel(TextAsset level)
        {
            if (level == null || !_controller.LoadLevel(level)) return;
            if (_catalog.IsFirstOfWorld(level)) EnterStory(_catalog.WorldIndexOf(level));
            else EnterTipsOrPlay();
        }

        /// <summary>Opens the new-mechanic card for this level's unseen mechanics (marking them seen), else plays.</summary>
        private void EnterTipsOrPlay()
        {
            var unseen = new List<string>();
            if (_controller.Level != null)
            {
                foreach (string id in LevelMechanics.Find(_controller.Level))
                {
                    if (!_progress.HasSeenTip(id) && unseen.Count < TipCard.MaxRows) unseen.Add(id);
                }
            }
            if (unseen.Count == 0)
            {
                Enter(State.Playing);
                return;
            }
            foreach (string id in unseen) _progress.MarkTipSeen(id);
            _tips.SetMechanics(unseen);
            Enter(State.Tips);
        }

        /// <summary>
        /// Tutorial guide: re-evaluates when the state or the built board changes (the solver allocates, so not every
        /// frame), then keeps the bubble on the figure.
        /// </summary>
        private void UpdateGuide()
        {
            if (!_guided || _board == null || _controller.Level == null || _controller.State == null) return;
            GameState state = _controller.State;
            if (!ReferenceEquals(state, _guideState) || !ReferenceEquals(_board.Level, _guideBoardLevel))
            {
                _guideState = state;
                _guideBoardLevel = _board.Level;
                GuideStep step = TutorialGuide.Evaluate(_controller.Level, state, _controller.Moves);
                if (step.HasTarget && _board.Level != null) _board.ShowGuide(step.TargetX, step.TargetY);
                else _board.HideGuide();
                _hud.SetGuide(GuideText(_controller.Level, step));
            }
            if (_camera == null) _camera = Camera.main;
            if (_camera != null && _board.Figure != null) _hud.PlaceGuide(_camera.WorldToScreenPoint(_board.GuideAnchor));
        }

        private static string GuideText(Level level, GuideStep step)
        {
            switch (step.Phase)
            {
                case GuidePhase.Start: return Localization.Get("guide.start");
                case GuidePhase.Counting: return Localization.Format("guide.counting", step.Counter, NextLucky(level, step.Counter));
                case GuidePhase.LuckyNext: return Localization.Format("guide.lucky", step.Counter + 1);
                case GuidePhase.Dead: return Localization.Get("guide.dead");
                default: return null; // won: the bubble goes away
            }
        }

        /// <summary>The first lucky, not deadly count above <paramref name="counter"/> (counting up).</summary>
        private static int NextLucky(Level level, int counter)
        {
            for (int c = counter + 1; c < counter + 100; c++)
            {
                if (!level.Rules.IsDeadly(c) && level.Rules.IsLucky(c)) return c;
            }
            return counter + 1;
        }

        private void Retry()
        {
            if (_controller.LoadLevel(_controller.CurrentLevelFile)) Enter(State.Playing);
        }

        private void EnterStory(int worldIndex)
        {
            string key = worldIndex >= 0 ? _catalog.Worlds[worldIndex].storyKey : null;
            _story.SetStory(string.IsNullOrEmpty(key) ? "story.world" + (worldIndex + 1) : key);
            Enter(State.Story);
        }

        private void ShowWinAfter(float delay, int moves, int par, bool golden)
        {
            _winMoves = moves;
            _winPar = par;
            _winGolden = golden;
            Enter(State.Won);
            _winTimer = delay;
        }

        private void OnLevelWon()
        {
            if (_state != State.Playing) return;
            string id = _controller.CurrentLevelFile.name;
            int par = _catalog.Par(id);
            _winRankBefore = Rank();
            bool golden = _progress.MarkCompleted(id, _controller.Moves, par);
            _winRankAfter = Rank();
            ShowWinAfter(_ui.Style.winPanelDelay, _controller.Moves, par, golden);
        }

        /// <summary>Figure rank: worlds completed (or the QA <c>--rank</c> override).</summary>
        private int Rank() => _rankOverride >= 0 ? _rankOverride : _progress.CompletedWorlds(_catalog.WorldLevelIds);

        private void ApplyRank()
        {
            if (_board != null) _board.Rank = Rank();
        }

        private void OnLevelLoaded()
        {
            ApplyRank(); // the new figure is built with the rank earned so far (a promotion shows from the next level)
            TextAsset level = _controller.CurrentLevelFile;
            int world = _catalog.WorldIndexOf(level);
            int index = world >= 0 ? Array.IndexOf(_catalog.Worlds[world].levels, level) : -1;
            _hud.SetLevel(world + 1, index + 1, level != null ? _catalog.Par(level.name) : -1);
            _guided = level != null && _catalog.IsGuided(level.name);
            _guideState = null;
            _guideBoardLevel = null;
            _hud.SetGuide(null);
            if (_board != null) _board.HideGuide();
            _hud.SetHint(level != null && !_guided ? level.name : null); // guided levels: the bubble replaces the strip
            _hud.SetMoves(_controller.Moves);
            _hud.SetBeads(BeadsForHud());
        }

        private void OnLanguageChanged()
        {
            foreach (UiScreen screen in _screens) screen.Refresh();
            _guideState = null; // the guide bubble re-reads its text too
        }

        private TextAsset ContinueLevel()
        {
            List<string> ids = _catalog.LevelIds;
            foreach (TextAsset level in _catalog.Levels)
            {
                if (_progress.IsUnlocked(level.name, ids) && !_progress.IsCompleted(level.name)) return level;
            }
            return _catalog.First;
        }

        private TextAsset FindLevel(string id)
        {
            if (id == null) return null;
            foreach (TextAsset level in _catalog.Levels)
            {
                if (level.name == id) return level;
            }
            Debug.LogWarning("ScreenFlow: --level " + id + " is not in the level catalog.");
            return null;
        }

        // QA only, in memory (UiRoot picks MemoryProgressStore for QA runs): first level done, second with a Golden pawn.
        private void ApplyProgressDemo()
        {
            List<string> ids = _catalog.LevelIds;
            if (ids.Count > 0) _progress.MarkCompleted(ids[0], int.MaxValue, _catalog.Par(ids[0]));
            if (ids.Count > 1)
            {
                int par = _catalog.Par(ids[1]);
                _progress.MarkCompleted(ids[1], Mathf.Max(par, 0), par);
            }
        }

        private static bool BackPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);
        }

        private static bool AnyPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            Gamepad gamepad = Gamepad.current;
            return (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                || (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
                || (gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame));
        }

        private static bool NavigationPressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.upArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame
                || keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame
                || keyboard.tabKey.wasPressedThisFrame)) return true;
            Gamepad gamepad = Gamepad.current;
            return gamepad != null && (gamepad.dpad.up.wasPressedThisFrame || gamepad.dpad.down.wasPressedThisFrame
                || gamepad.dpad.left.wasPressedThisFrame || gamepad.dpad.right.wasPressedThisFrame);
        }

        /// <summary>Beads carried, for the HUD; -1 when the level has no beads (the HUD hides its bead line).</summary>
        private int BeadsForHud()
        {
            Level level = _controller.Level;
            return level != null && level.BeadCount > 0 && _controller.State != null ? _controller.State.Beads : -1;
        }
    }
}
