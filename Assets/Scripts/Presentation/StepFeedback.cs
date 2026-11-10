using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LuckyStep
{
    /// <summary>
    /// Step feedback of Story 005 (brief MVP 5): a gold glow pulse (figure emission + warm flash light) on every
    /// successful lucky step, and a URP Vignette that darkens the screen edges as the next deadly count comes within
    /// <see cref="StepFeedbackConfig.nearSteps"/> steps. A death impact (<see cref="BoardView.DeathImpact"/>) pulses the vignette towards red, then
    /// it settles back to the darkening value; undo and restart end the pulse at once. Lucky/deadly come from the
    /// level's RuleSet. Read-only.
    /// </summary>
    public sealed class StepFeedback : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private BoardView _board;
        [SerializeField] private Volume _volume;
        [SerializeField] private Light _flashLight;
        [SerializeField] private StepFeedbackConfig _config;

        private MaterialPropertyBlock _block;
        private Vignette _vignette;
        private float _glowTime = float.MaxValue;
        private float _vignetteTarget;
        private float _vignetteValue;
        private Color _vignetteColor = Color.black;
        private float _pulseTime = float.MaxValue;

        private void OnEnable()
        {
            if (_board == null) return;
            _board.StateChanged += OnStateChanged;
            _board.DeathImpact += OnDeathImpact;
        }

        private void OnDisable()
        {
            if (_board == null) return;
            _board.StateChanged -= OnStateChanged;
            _board.DeathImpact -= OnDeathImpact;
        }

        private void OnDeathImpact() => _pulseTime = 0f;

        private void Start()
        {
            _block = new MaterialPropertyBlock();
            if (_volume != null && !_volume.profile.TryGet(out _vignette)) _vignette = _volume.profile.Add<Vignette>(true);
            if (_vignette != null)
            {
                _vignette.intensity.overrideState = true;
                _vignetteColor = _vignette.color.value;
                _vignette.color.overrideState = true;
            }
            _vignetteValue = _vignetteTarget = _config.vignetteBase;
            if (_flashLight != null)
            {
                _flashLight.intensity = 0f;
                _flashLight.enabled = false;
            }
        }

        private void OnStateChanged(GameState previous, GameState current, bool isForwardStep)
        {
            RuleSet rules = _board.Level.Rules;
            // A step onto wood keeps the count, so standing on a lucky count and knocking on wood is not a new lucky step.
            bool counted = previous == null || previous.Counter != current.Counter;
            if (isForwardStep && counted && !current.IsDead && !rules.IsDeadly(current.Counter) && rules.IsLucky(current.Counter))
            {
                _glowTime = 0f;
            }
            if (!isForwardStep) _pulseTime = float.MaxValue;
            _vignetteTarget = VignetteIntensity(StepsToDeadly(rules, current, _config.nearSteps), _config);
            if (previous == null) _vignetteValue = _vignetteTarget;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _glowTime += dt;
            float glow = _glowTime < _config.glowSeconds ? Mathf.Sin(Mathf.PI * _glowTime / _config.glowSeconds) : 0f;
            ApplyGlow(glow);

            _vignetteValue = Mathf.MoveTowards(_vignetteValue, _vignetteTarget, _config.vignetteChangePerSecond * dt);
            _pulseTime += dt;
            float pulse = 0f;
            if (_pulseTime < _config.deathPulseSeconds)
            {
                float rise = Mathf.Max(0.001f, _config.deathPulseRiseSeconds);
                float fall = Mathf.Max(0.001f, _config.deathPulseSeconds - rise);
                pulse = _pulseTime < rise ? _pulseTime / rise : 1f - Mathf.SmoothStep(0f, 1f, (_pulseTime - rise) / fall);
            }
            if (_vignette != null)
            {
                _vignette.intensity.value = Mathf.Lerp(_vignetteValue, _config.deathPulseIntensity, pulse);
                _vignette.color.value = Color.Lerp(_vignetteColor, _config.deathPulseColor, pulse);
            }
        }

        private void ApplyGlow(float k)
        {
            Color emission = _config.glowColor * (_config.glowEmission * k);
            foreach (Renderer r in _board != null ? _board.FigureRenderers : new Renderer[0])
            {
                r.GetPropertyBlock(_block);
                _block.SetColor(EmissionColorId, emission);
                r.SetPropertyBlock(_block);
            }
            if (_flashLight == null) return;
            _flashLight.enabled = k > 0f;
            _flashLight.intensity = _config.flashIntensity * k;
            _flashLight.range = _config.flashRange;
            _flashLight.color = _config.glowColor;
            if (_board != null && _board.Figure != null)
                _flashLight.transform.position = _board.Figure.position + Vector3.up * _config.flashHeight;
        }

        /// <summary>
        /// Steps until the counter first reaches a deadly count, looking at most <paramref name="maxSteps"/> ahead:
        /// 0 if the player is dead, -1 if none is that near. Follows the broken mirror's countdown, including the turn back
        /// to counting up at RuleSet.StartValue; it cannot foresee a cat reset.
        /// </summary>
        public static int StepsToDeadly(RuleSet rules, GameState state, int maxSteps)
        {
            if (state.IsDead) return 0;
            if (state.IsWon) return -1;
            int c = state.Counter;
            int down = state.CountingDown ? Mathf.Max(0, c - rules.StartValue) : 0;
            for (int k = 1; k <= maxSteps; k++)
            {
                int value = k <= down ? c - k : (state.CountingDown ? rules.StartValue + (k - down) : c + k);
                if (rules.IsDeadly(value)) return k;
            }
            return -1;
        }

        /// <summary>
        /// Vignette intensity for a <see cref="StepsToDeadly"/> result: base when none is near, max at 0 or 1 step, and
        /// linear in between (e.g. with nearSteps 3: 3 steps = one third of the way, 1 step = max).
        /// </summary>
        public static float VignetteIntensity(int stepsToDeadly, StepFeedbackConfig config)
        {
            if (stepsToDeadly < 0) return config.vignetteBase;
            if (stepsToDeadly <= 1) return config.vignetteMax;
            float t = (config.nearSteps + 1f - stepsToDeadly) / config.nearSteps;
            return Mathf.Lerp(config.vignetteBase, config.vignetteMax, t);
        }
    }
}
