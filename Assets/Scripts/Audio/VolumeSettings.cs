using System;
using UnityEngine;

namespace LuckyStep
{
    /// <summary>
    /// The player's music and effects volume in percent, 0 to 100 in steps of <see cref="Step"/>
    /// (design/quick-specs/audio-settings-addition-2026-11-10.md). With <c>persist</c> the values are read from and
    /// written to PlayerPrefs; without it (QA runs) they live in memory only. Owned by <see cref="UiRoot"/>; read by
    /// <see cref="AudioFeedback"/>.
    /// </summary>
    /// <example><code>var volume = new VolumeSettings(persist: true); volume.ChangeMusic(-1); float m = volume.MusicFactor;</code></example>
    public sealed class VolumeSettings
    {
        /// <summary>Percent per press.</summary>
        public const int Step = 10;
        /// <summary>Music default, percent.</summary>
        public const int DefaultMusic = 70;
        /// <summary>Effects default, percent.</summary>
        public const int DefaultEffects = 100;

        private const string MusicKey = "audio.music";
        private const string EffectsKey = "audio.sfx";
        private readonly bool _persist;

        /// <summary>Creates the settings, loading saved values when <paramref name="persist"/> is true.</summary>
        public VolumeSettings(bool persist)
        {
            _persist = persist;
            Music = persist ? Clamp(PlayerPrefs.GetInt(MusicKey, DefaultMusic)) : DefaultMusic;
            Effects = persist ? Clamp(PlayerPrefs.GetInt(EffectsKey, DefaultEffects)) : DefaultEffects;
        }

        /// <summary>Raised after either value changes.</summary>
        public event Action Changed;

        /// <summary>Music volume, percent (0-100).</summary>
        public int Music { get; private set; }

        /// <summary>Effects volume, percent (0-100).</summary>
        public int Effects { get; private set; }

        /// <summary>Music as a 0..1 factor.</summary>
        public float MusicFactor => Music / 100f;

        /// <summary>Effects as a 0..1 factor.</summary>
        public float EffectsFactor => Effects / 100f;

        /// <summary>Moves the music volume by <paramref name="steps"/> steps (negative is quieter), clamped to 0-100.</summary>
        public void ChangeMusic(int steps) => Set(Clamp(Music + steps * Step), Effects);

        /// <summary>Moves the effects volume by <paramref name="steps"/> steps (negative is quieter), clamped to 0-100.</summary>
        public void ChangeEffects(int steps) => Set(Music, Clamp(Effects + steps * Step));

        private void Set(int music, int effects)
        {
            if (music == Music && effects == Effects) return;
            Music = music;
            Effects = effects;
            if (_persist)
            {
                PlayerPrefs.SetInt(MusicKey, Music);
                PlayerPrefs.SetInt(EffectsKey, Effects);
                PlayerPrefs.Save();
            }
            Changed?.Invoke();
        }

        private static int Clamp(int percent) => Mathf.Clamp(percent, 0, 100);
    }
}
