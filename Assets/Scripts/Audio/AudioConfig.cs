using UnityEngine;

namespace LuckyStep
{
    /// <summary>
    /// Clips and mix values for <see cref="AudioFeedback"/> (Story 008, brief MVP 5). Every event takes an array; one
    /// clip is picked at random each time. Built by Assets/Editor/BuildBoardScene.cs at Assets/Audio/AudioConfig.asset.
    /// </summary>
    /// <example><code>AudioClip bell = AudioFeedback.Pick(config.lucky);</code></example>
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Lucky Step/Audio Config")]
    public sealed class AudioConfig : ScriptableObject
    {
        /// <summary>A counted step (the pawn's token on the board).</summary>
        public AudioClip[] step = new AudioClip[0];
        /// <summary>A step straight onto a wooden tile (knock on wood).</summary>
        public AudioClip[] wood = new AudioClip[0];
        /// <summary>Stepping onto a dice tile.</summary>
        public AudioClip[] dice = new AudioClip[0];
        /// <summary>A coin bridge tosses its coin.</summary>
        public AudioClip[] coin = new AudioClip[0];
        /// <summary>The bell on every lucky step.</summary>
        public AudioClip[] lucky = new AudioClip[0];
        /// <summary>A flowerpot lands.</summary>
        public AudioClip[] pot = new AudioClip[0];
        /// <summary>The black cat resets the counter.</summary>
        public AudioClip[] catReset = new AudioClip[0];
        /// <summary>Stepping onto a broken mirror.</summary>
        public AudioClip[] mirror = new AudioClip[0];
        /// <summary>An evil-eye bead is picked up.</summary>
        public AudioClip[] beadPickup = new AudioClip[0];
        /// <summary>A bead breaks warding off a death.</summary>
        public AudioClip[] beadBreak = new AudioClip[0];
        /// <summary>A death by bad luck (deadly count, spike, flowerpot).</summary>
        public AudioClip[] death = new AudioClip[0];
        /// <summary>A fall (gap, closed bridge).</summary>
        public AudioClip[] fall = new AudioClip[0];
        /// <summary>A move into a wall or a closed exit.</summary>
        public AudioClip[] blocked = new AudioClip[0];
        /// <summary>The exit is reached.</summary>
        public AudioClip[] win = new AudioClip[0];
        /// <summary>Undo or restart.</summary>
        public AudioClip[] undo = new AudioClip[0];
        /// <summary>A UI button press.</summary>
        public AudioClip[] click = new AudioClip[0];

        /// <summary>Looping music on the menu, level select and story cards.</summary>
        public AudioClip menuMusic;
        /// <summary>Looping music while a level is played.</summary>
        public AudioClip gameMusic;

        /// <summary>Volume of every sound effect (0..1).</summary>
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        /// <summary>Volume of the music (0..1).</summary>
        [Range(0f, 1f)] public float musicVolume = 0.35f;
        /// <summary>Random pitch spread of step-like sounds, so repeats do not sound mechanical.</summary>
        [Range(0f, 0.3f)] public float pitchJitter = 0.06f;
        /// <summary>Seconds to cross-fade between the menu and the game music.</summary>
        public float musicFadeSeconds = 0.8f;
        /// <summary>The music drops to this fraction of its volume after a deadly count (13), then comes back.</summary>
        [Range(0f, 1f)] public float deadlyDuck = 0.1f;
        /// <summary>Seconds the music stays down after a deadly count before it fades back.</summary>
        public float deadlyDuckSeconds = 1.4f;
        /// <summary>Delay from landing on a flowerpot to the pot's crash sound (the pot falls first).</summary>
        public float potDelay = 0.3f;
    }
}
