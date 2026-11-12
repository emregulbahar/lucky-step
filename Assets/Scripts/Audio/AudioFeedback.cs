using System;
using UnityEngine;

namespace LuckyStep
{
    /// <summary>
    /// Sound of Story 008 (brief MVP 5): reacts to the step <see cref="StepRules"/> already computed, as
    /// <see cref="StepFeedback"/> does for light. Per forward step it plays the token on the board (or a knock on wood),
    /// the dice, the coin toss of opening bridges, the bell on a lucky step, the mirror, the cat reset, a bead picked up
    /// or broken, the flowerpot crash, death or fall, and the win; undo and restart play one soft sound. A blocked move
    /// and UI buttons click. Two looping music sources cross-fade between the menu and the level, and the music ducks
    /// briefly after a deadly count. Read-only: it never changes game state. Clips and levels come from
    /// <see cref="AudioConfig"/>, scaled by the player's <see cref="VolumeSettings"/> (UiRoot.Volume). Browsers start audio
    /// after the first input; Unity's Web player handles that.
    /// </summary>
    /// <example>Add next to the board and assign the board, controller, UI root and config (BuildBoardScene does).</example>
    public sealed class AudioFeedback : MonoBehaviour
    {
        [SerializeField] private BoardView _board;
        [SerializeField] private PlayerInputController _controller;
        [SerializeField] private UiRoot _ui;
        [SerializeField] private AudioConfig _config;

        private AudioSource _sfx;
        private AudioSource _menuMusic;
        private AudioSource _gameMusic;
        private float _gameMix;   // 0 = menu music, 1 = game music
        private float _duckTimer; // seconds the music stays ducked
        private float _duck = 1f;
        private float _potAt = -1f;

        /// <summary>A random clip from <paramref name="clips"/>, or null when there is none.</summary>
        public static AudioClip Pick(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        private void Awake()
        {
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _menuMusic = MusicSource(_config != null ? _config.menuMusic : null);
            _gameMusic = MusicSource(_config != null ? _config.gameMusic : null);
        }

        private AudioSource MusicSource(AudioClip clip)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
            if (clip != null) source.Play();
            return source;
        }

        private void OnEnable()
        {
            if (_board != null) _board.StateChanged += OnStateChanged;
            if (_controller != null) _controller.MoveBlocked += OnMoveBlocked;
            if (_ui != null) _ui.ButtonClicked += OnButtonClicked;
            if (_ui != null) _ui.GoldenPawnShown += OnGoldenPawnShown;
        }

        private void OnDisable()
        {
            if (_board != null) _board.StateChanged -= OnStateChanged;
            if (_controller != null) _controller.MoveBlocked -= OnMoveBlocked;
            if (_ui != null) _ui.ButtonClicked -= OnButtonClicked;
            if (_ui != null) _ui.GoldenPawnShown -= OnGoldenPawnShown;
        }

        private void Update()
        {
            if (_config == null) return;
            float dt = Time.unscaledDeltaTime;
            bool inGame = _ui == null || _ui.InGame;
            _gameMix = Mathf.MoveTowards(_gameMix, inGame ? 1f : 0f, dt / Mathf.Max(0.01f, _config.musicFadeSeconds));
            if (_duckTimer > 0f) _duckTimer -= dt;
            _duck = Mathf.MoveTowards(_duck, _duckTimer > 0f ? _config.deadlyDuck : 1f, dt * 2f);
            float music = _config.musicVolume * _duck * (_ui != null && _ui.Volume != null ? _ui.Volume.MusicFactor : 1f);
            _menuMusic.volume = music * (1f - _gameMix);
            _gameMusic.volume = music * _gameMix;

            if (_potAt >= 0f && Time.time >= _potAt)
            {
                _potAt = -1f;
                Play(_config.pot, false);
            }
        }

        private void Play(AudioClip[] clips, bool jitter)
        {
            AudioClip clip = Pick(clips);
            if (clip == null) return;
            _sfx.pitch = jitter ? 1f + UnityEngine.Random.Range(-_config.pitchJitter, _config.pitchJitter) : 1f;
            float effects = _ui != null && _ui.Volume != null ? _ui.Volume.EffectsFactor : 1f;
            if (effects <= 0f) return;
            _sfx.PlayOneShot(clip, _config.sfxVolume * effects);
        }

        private void OnMoveBlocked(Direction direction)
        {
            if (_config != null) Play(_config.blocked, false);
        }

        private void OnGoldenPawnShown()
        {
            if (_config != null) Play(_config.lucky, false); // the lucky bell marks the Golden pawn
        }

        private void OnButtonClicked()
        {
            if (_config != null) Play(_config.click, true);
        }

        private void OnStateChanged(GameState previous, GameState state, bool isForwardStep)
        {
            if (_config == null || previous == null) return;
            if (!isForwardStep)
            {
                _potAt = -1f;
                if (!state.Equals(previous)) Play(_config.undo, false);
                return;
            }

            Level level = _board.Level;
            RuleSet rules = level.Rules;
            int dx = Math.Sign(state.X - previous.X);
            int dy = Math.Sign(state.Y - previous.Y);
            Tile entered = level.GetTile(previous.X + dx, previous.Y + dy);
            Tile landing = level.GetTile(state.X, state.Y);
            bool counted = entered != Tile.Wood;
            int count = !counted ? previous.Counter : previous.CountingDown ? previous.Counter - 1 : previous.Counter + 1;
            bool deadly = counted && rules.IsDeadly(count);
            bool lucky = counted && !deadly && rules.IsLucky(count);

            Play(counted ? _config.step : _config.wood, true);
            if (entered == Tile.Dice) Play(_config.dice, true);
            if (state.OpenBridges != previous.OpenBridges || landing == Tile.Bridge) Play(_config.coin, true);
            if (lucky) Play(_config.lucky, false);
            if (landing == Tile.Mirror) Play(_config.mirror, false);
            if (landing == Tile.Flowerpot) _potAt = Time.time + _config.potDelay;

            int picked = state.TakenBeads != previous.TakenBeads ? 1 : 0;
            if (picked > 0) Play(_config.beadPickup, false);
            if (state.Beads < previous.Beads + picked) Play(_config.beadBreak, false);

            bool catReset = level.CatRouteLength > 0 && state.Status == GameStatus.Playing
                && state.Counter == rules.StartValue && count != rules.StartValue;
            if (catReset) Play(_config.catReset, true);

            if (state.IsWon) Play(_config.win, false);
            if (state.IsDead)
            {
                bool fall = landing == Tile.Gap
                    || (landing == Tile.Bridge && (state.OpenBridges & (1UL << level.BridgeIndex(state.X, state.Y))) == 0);
                Play(fall ? _config.fall : _config.death, false);
            }
            if (deadly) _duckTimer = _config.deadlyDuckSeconds; // the music cuts briefly on 13, warded or not
        }
    }
}
