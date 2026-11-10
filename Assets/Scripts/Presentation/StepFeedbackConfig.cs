using UnityEngine;

namespace LuckyStep
{
    /// <summary>Tunables of the lucky-step glow and the near-deadly vignette (Story 005, brief MVP 5).</summary>
    [CreateAssetMenu(menuName = "Lucky Step/Step Feedback Config", fileName = "StepFeedbackConfig")]
    public sealed class StepFeedbackConfig : ScriptableObject
    {
        /// <summary>Glow colour of the player figure and the flash light.</summary>
        [Header("Lucky glow")]
        public Color glowColor = new Color(1f, 0.78f, 0.3f);
        /// <summary>Peak HDR emission multiplier on the player figure (bloom picks it up).</summary>
        public float glowEmission = 1.1f;
        /// <summary>Seconds the glow pulse lasts.</summary>
        public float glowSeconds = 0.7f;
        /// <summary>Peak intensity of the warm flash light.</summary>
        public float flashIntensity = 4f;
        /// <summary>Range of the flash light.</summary>
        public float flashRange = 3f;
        /// <summary>Height of the flash light above the player figure.</summary>
        public float flashHeight = 1.1f;

        /// <summary>"Near" = the next deadly count is at most this many steps ahead.</summary>
        [Header("Near-deadly vignette")]
        [Min(1)] public int nearSteps = 3;
        /// <summary>Vignette intensity when no deadly count is near (0: the Printed Board look has no resting vignette).</summary>
        [Range(0f, 1f)] public float vignetteBase = 0f;
        /// <summary>Vignette intensity one step before a deadly count, and after death.</summary>
        [Range(0f, 1f)] public float vignetteMax = 0.55f;
        /// <summary>Vignette intensity change per second while easing to the target.</summary>
        public float vignetteChangePerSecond = 1.2f;

        /// <summary>Vignette colour at the peak of the death pulse (it settles back to the profile's colour).</summary>
        [Header("Death pulse")]
        [Tooltip("Vignette colour at the peak of the death pulse (it settles back to the profile's colour).")]
        public Color deathPulseColor = new Color(0.75f, 0.05f, 0.03f);
        /// <summary>Vignette intensity at the peak of the death pulse.</summary>
        [Tooltip("Vignette intensity at the peak of the death pulse.")]
        [Range(0f, 1f)] public float deathPulseIntensity = 0.5f;
        /// <summary>Seconds from the death step to the pulse's peak.</summary>
        [Tooltip("Seconds from the death step to the pulse's peak.")]
        public float deathPulseRiseSeconds = 0.06f;
        /// <summary>Total seconds of the death pulse, rise included.</summary>
        [Tooltip("Total seconds of the death pulse, rise included.")]
        public float deathPulseSeconds = 0.5f;
    }
}
