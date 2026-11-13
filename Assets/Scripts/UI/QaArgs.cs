using System;
using System.Globalization;

namespace LuckyStep
{
    /// <summary>
    /// Command-line QA capture arguments (run-and-observe, Story 006), same pattern as ScreenshotOnArg:
    /// <c>--ui menu|select|settings|story|ending|hud|win</c>, <c>--lang en|tr</c>, <c>--level LevelId</c>, <c>--progress-demo</c>,
    /// <c>--win-delay seconds</c> (with <c>--ui win</c>), plus the Story 005 <c>--moves</c> replay. Read once at startup
    /// (allocates). Screenshot runs advance a fixed 1/<see cref="CaptureFrameRate"/> s per frame, so <c>--after N</c>
    /// frames is N/60 s of UI time however long the real frames take.
    /// </summary>
    /// <example>
    /// <code>Game.exe --ui select --progress-demo --lang tr --screenshot C:/qa/select_tr.png --after 60</code>
    /// </example>
    public static class QaArgs
    {
        /// <summary>Frame rate of QA runs; screenshot runs also step time by exactly one frame of it.</summary>
        public const int CaptureFrameRate = 60;

        /// <summary>Default for <see cref="WinDelaySeconds"/>: long enough for the first slow frames (shader warm-up) to pass.</summary>
        public const float DefaultWinDelaySeconds = 1f;

        /// <summary>
        /// Seconds before <c>--ui win</c> opens the win panel: <c>--win-delay</c> (invariant culture, clamped to 0 or more),
        /// else <see cref="DefaultWinDelaySeconds"/>.
        /// </summary>
        public static float WinDelaySeconds =>
            float.TryParse(Value("--win-delay"), NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds)
                ? Math.Max(0f, seconds) : DefaultWinDelaySeconds;

        /// <summary>The value after <paramref name="name"/>, or null if absent.</summary>
        public static string Value(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }

        /// <summary>True if <paramref name="name"/> is on the command line.</summary>
        public static bool Has(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;

        /// <summary>True for any QA capture or replay run; such runs keep progress in memory and never touch the save.</summary>
        public static bool IsQaRun => Has("--ui") || Has("--moves") || Has("--progress-demo") || Has("--screenshot")
            || Has("--unlock-all");
    }

    /// <summary>Progress store that lives only in memory (QA runs: <c>--progress-demo</c> must not write PlayerPrefs).</summary>
    /// <example><code>var progress = new Progress(new MemoryProgressStore());</code></example>
    public sealed class MemoryProgressStore : IProgressStore
    {
        private string _json = string.Empty;

        /// <inheritdoc />
        public string Load() => _json;

        /// <inheritdoc />
        public void Save(string json) => _json = json;
    }
}
