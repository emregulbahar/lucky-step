using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LuckyStep
{
    /// <summary>
    /// Scene glue: loads a text level, reads WASD/arrows (one move per press), Z (undo) and R (restart),
    /// and keeps the undo stack. All rules live in <see cref="StepRules"/>. Shows a plain debug readout via OnGUI.
    /// </summary>
    /// <example>
    /// Add to an empty GameObject and assign a .txt level from Assets/Levels to Level File.
    /// </example>
    public sealed class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private TextAsset _levelFile;

        private readonly Stack<GameState> _history = new Stack<GameState>();
        private Level _level;
        private GameState _state;
        private string _error;

        /// <summary>The loaded level, or null if loading failed.</summary>
        public Level Level => _level;

        /// <summary>The current state, or null if no level is loaded.</summary>
        public GameState State => _state;

        private void Start()
        {
            if (_levelFile == null)
            {
                _error = "No level file assigned.";
            }
            else if (!LevelLoader.TryParse(_levelFile.text, out _level, out _error))
            {
                Debug.LogError("Level load failed (" + _levelFile.name + "): " + _error);
                return;
            }
            if (_level != null) _state = _level.CreateInitialState();
        }

        private void Update()
        {
            if (_level == null) return;
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
                if (_history.Count > 0) _state = _history.Pop();
                return;
            }

            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) TryMove(Direction.Up);
            else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) TryMove(Direction.Down);
            else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) TryMove(Direction.Left);
            else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) TryMove(Direction.Right);
        }

        private void TryMove(Direction direction)
        {
            GameState next = StepRules.Step(_level, _state, direction);
            if (ReferenceEquals(next, _state)) return;
            _history.Push(_state);
            _state = next;
        }

        private void OnGUI()
        {
            if (_level == null)
            {
                GUILayout.Label("Level error: " + _error);
                return;
            }
            GUILayout.Label("Pos (" + _state.X + "," + _state.Y + ")  Steps " + _state.Counter + "  " + _state.Status);
        }
    }
}
