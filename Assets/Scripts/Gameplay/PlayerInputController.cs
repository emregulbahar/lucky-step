using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LuckyStep
{
    /// <summary>
    /// Scene glue: loads a text level, reads WASD/arrows (one move per press), Z (undo) and R (restart),
    /// and keeps the undo stack. All rules live in <see cref="StepRules"/>. Levels can be swapped at runtime with
    /// <see cref="LoadLevel"/> (Story 006: menus, level select, next level). A plain debug readout (OnGUI) is shown
    /// only in the "--moves" QA replay mode.
    /// </summary>
    /// <example>
    /// Add to an empty GameObject and assign a .txt level from Assets/Levels to Level File, or call
    /// <code>controller.LoadLevel(catalog.First);</code>
    /// </example>
    public sealed class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private TextAsset _levelFile;

        private readonly Stack<GameState> _history = new Stack<GameState>();
        private Level _level;
        private GameState _state;
        private string _error;
        private bool _debugReadout;

        /// <summary>The loaded level, or null if loading failed.</summary>
        public Level Level => _level;

        /// <summary>The current state, or null if no level is loaded.</summary>
        public GameState State => _state;

        /// <summary>The level file last passed to <see cref="LoadLevel"/> (or assigned in the Inspector), even if it failed to parse.</summary>
        public TextAsset CurrentLevelFile => _levelFile;

        /// <summary>Moves taken to reach the current state: the undo stack depth (undo lowers it, restart resets it to 0).</summary>
        public int Moves => _history.Count;

        /// <summary>
        /// When false, keyboard moves, undo and restart are ignored (menus, story cards and the win panel set this).
        /// Default true.
        /// </summary>
        public bool InputEnabled { get; set; } = true;

        /// <summary>
        /// Raised when a move is rejected by <see cref="StepRules.Step"/> (wall, or exit closed because the step would
        /// not be lucky) while the state is Playing, with the direction that was tried. Not raised once the pawn is
        /// dead or has won. Presentation uses it for the blocked-move bump (Story 005); the state does not change.
        /// </summary>
        public event System.Action<Direction> MoveBlocked;

        /// <summary>Raised after <see cref="LoadLevel"/> succeeds; <see cref="Level"/> and <see cref="State"/> are the new level's.</summary>
        public event System.Action LevelLoaded;

        /// <summary>
        /// Raised once when a move wins the level (state becomes Won). <see cref="Moves"/> is the winning move count
        /// and <see cref="CurrentLevelFile"/> the level won.
        /// </summary>
        public event System.Action LevelWon;

        private void Start()
        {
            _debugReadout = HasArg("--moves");
            // A menu may have called LoadLevel before Start; only load the Inspector level if nothing is loaded yet.
            if (_level == null)
            {
                if (_levelFile == null) _error = "No level file assigned.";
                else LoadLevel(_levelFile);
            }
            if (_level != null) StartCoroutine(ReplayMovesFromArgs());
        }

        /// <summary>
        /// Parses <paramref name="levelFile"/> and starts it from its initial state with an empty undo stack.
        /// Returns false (and logs) if it does not parse; the controller then has no level.
        /// </summary>
        public bool LoadLevel(TextAsset levelFile)
        {
            _levelFile = levelFile;
            _history.Clear();
            _state = null;
            if (levelFile == null)
            {
                _level = null;
                _error = "No level file assigned.";
                return false;
            }
            if (!LevelLoader.TryParse(levelFile.text, out _level, out _error))
            {
                _level = null;
                Debug.LogError("Level load failed (" + levelFile.name + "): " + _error);
                return false;
            }
            _state = _level.CreateInitialState();
            LevelLoaded?.Invoke();
            return true;
        }

        private static bool HasArg(string name)
        {
            foreach (string arg in System.Environment.GetCommandLineArgs())
            {
                if (arg == name) return true;
            }
            return false;
        }

        /// <summary>
        /// QA capture aid (run-and-observe, Story 005): "--moves UDLR" plays those moves (Z = undo) at startup, one every
        /// ReplaySeconds, so a ScreenshotOnArg capture can show a mid-level state. Not a gameplay value.
        /// </summary>
        private System.Collections.IEnumerator ReplayMovesFromArgs()
        {
            const float ReplaySeconds = 0.8f;
            string[] args = System.Environment.GetCommandLineArgs();
            string moves = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--moves") moves = args[i + 1];
            }
            if (moves == null) yield break;
            // Fixed 60 fps in replay mode so ScreenshotOnArg's frame count maps to time (glow and vignette timing).
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            foreach (char c in moves.ToUpperInvariant())
            {
                yield return new WaitForSeconds(ReplaySeconds);
                switch (c)
                {
                    case 'U': TryMove(Direction.Up); break;
                    case 'D': TryMove(Direction.Down); break;
                    case 'L': TryMove(Direction.Left); break;
                    case 'R': TryMove(Direction.Right); break;
                    case 'Z': Undo(); break;
                }
            }
        }

        private void Update()
        {
            if (_level == null || !InputEnabled) return;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.rKey.wasPressedThisFrame)
            {
                _history.Clear();
                _state = _level.CreateInitialState();
                return;
            }
            if (kb.zKey.wasPressedThisFrame)
            {
                Undo();
                return;
            }

            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) TryMove(Direction.Up);
            else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) TryMove(Direction.Down);
            else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) TryMove(Direction.Left);
            else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) TryMove(Direction.Right);
        }

        /// <summary>Steps back one move (Z key, or 'Z' in a --moves replay); does nothing at the start.</summary>
        private void Undo()
        {
            if (_history.Count > 0) _state = _history.Pop();
        }

        private void TryMove(Direction direction)
        {
            GameState next = StepRules.Step(_level, _state, direction);
            if (ReferenceEquals(next, _state))
            {
                if (_state.Status == GameStatus.Playing) MoveBlocked?.Invoke(direction);
                return;
            }
            _history.Push(_state);
            _state = next;
            if (next.IsWon) LevelWon?.Invoke();
        }

        private void OnGUI()
        {
            if (!_debugReadout) return;
            if (_level == null)
            {
                GUILayout.Label("Level error: " + _error);
                return;
            }
            GUILayout.Label("Pos (" + _state.X + "," + _state.Y + ")  Steps " + _state.Counter + "  " + _state.Status);
        }
    }
}
