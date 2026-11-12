using System;
using System.Collections.Generic;
using UnityEngine;

namespace LuckyStep
{
    /// <summary>Where <see cref="Progress"/> keeps its JSON. Swap in an in-memory store for tests.</summary>
    public interface IProgressStore
    {
        /// <summary>Returns the saved JSON, or null/empty when nothing is saved yet.</summary>
        string Load();

        /// <summary>Persists <paramref name="json"/>, replacing any earlier save.</summary>
        void Save(string json);
    }

    /// <summary>
    /// Stores progress under one PlayerPrefs key. On WebGL PlayerPrefs is backed by the browser's IndexedDB, so
    /// progress survives a page reload; <see cref="Save"/> calls PlayerPrefs.Save() to flush it immediately.
    /// </summary>
    /// <example><code>var progress = new Progress(new PlayerPrefsProgressStore());</code></example>
    public sealed class PlayerPrefsProgressStore : IProgressStore
    {
        private const string Key = "LuckyStep.Progress";

        /// <inheritdoc/>
        public string Load() => PlayerPrefs.GetString(Key, string.Empty);

        /// <inheritdoc/>
        public void Save(string json)
        {
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Player progress (Story 006): which levels are completed and which earned a Golden pawn (finished in par moves
    /// or fewer). Levels unlock in catalog order. Pure data plus JSON; persistence goes through
    /// <see cref="IProgressStore"/>, which is written on every change.
    /// </summary>
    /// <example>
    /// <code>
    /// var progress = new Progress(new PlayerPrefsProgressStore());
    /// bool pawn = progress.MarkCompleted("Level02", controller.Moves, catalog.Par("Level02"));
    /// bool open = progress.IsUnlocked("Level03", catalog.LevelIds);
    /// </code>
    /// </example>
    public sealed class Progress
    {
        [Serializable]
        private sealed class SaveData
        {
            public List<string> completed = new List<string>();
            public List<string> goldenPawns = new List<string>();
        }

        private readonly IProgressStore _store;
        private readonly HashSet<string> _completed = new HashSet<string>();
        private readonly HashSet<string> _goldenPawns = new HashSet<string>();

        /// <summary>Creates progress and loads whatever <paramref name="store"/> holds (unreadable data starts empty).</summary>
        public Progress(IProgressStore store)
        {
            _store = store;
            LoadFromJson(store.Load());
        }

        /// <summary>Number of levels that earned a Golden pawn.</summary>
        public int GoldenPawnCount => _goldenPawns.Count;

        /// <summary>True if the level with this id (its TextAsset name) has been completed.</summary>
        public bool IsCompleted(string levelId) => _completed.Contains(levelId);

        /// <summary>True if the level was ever completed in par moves or fewer.</summary>
        public bool HasGoldenPawn(string levelId) => _goldenPawns.Contains(levelId);

        /// <summary>
        /// Records a completion and saves. Returns true if this run earned the Golden pawn: <paramref name="par"/> is
        /// known (&gt;= 0) and <paramref name="moves"/> &lt;= par. A pawn once earned is never taken away.
        /// </summary>
        public bool MarkCompleted(string levelId, int moves, int par)
        {
            bool pawn = par >= 0 && moves <= par;
            _completed.Add(levelId);
            if (pawn) _goldenPawns.Add(levelId);
            _store.Save(ToJson());
            return pawn;
        }

        /// <summary>
        /// Developer/review switch (QA arg <c>--unlock-all</c>): every catalog level counts as unlocked without being
        /// marked completed. Not saved.
        /// </summary>
        public bool UnlockAll { get; set; }

        /// <summary>
        /// True if the level can be played: the first id in <paramref name="orderedLevelIds"/> always is, any other when
        /// the id before it is completed. Ids not in the list are locked.
        /// </summary>
        public bool IsUnlocked(string levelId, IReadOnlyList<string> orderedLevelIds)
        {
            for (int i = 0; i < orderedLevelIds.Count; i++)
            {
                if (orderedLevelIds[i] == levelId) return UnlockAll || i == 0 || _completed.Contains(orderedLevelIds[i - 1]);
            }
            return false;
        }

        /// <summary>Serializes progress to JSON (ids sorted, so equal progress gives equal text).</summary>
        public string ToJson()
        {
            var data = new SaveData();
            data.completed.AddRange(_completed);
            data.completed.Sort(StringComparer.Ordinal);
            data.goldenPawns.AddRange(_goldenPawns);
            data.goldenPawns.Sort(StringComparer.Ordinal);
            return JsonUtility.ToJson(data);
        }

        /// <summary>Replaces progress with the JSON from <see cref="ToJson"/>. Null, empty or malformed JSON gives empty progress.</summary>
        public void LoadFromJson(string json)
        {
            _completed.Clear();
            _goldenPawns.Clear();
            if (string.IsNullOrEmpty(json)) return;
            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (ArgumentException e)
            {
                Debug.LogWarning("Progress data unreadable, starting fresh: " + e.Message);
                return;
            }
            if (data == null) return;
            if (data.completed != null) _completed.UnionWith(data.completed);
            if (data.goldenPawns != null) _goldenPawns.UnionWith(data.goldenPawns);
        }
    }
}
