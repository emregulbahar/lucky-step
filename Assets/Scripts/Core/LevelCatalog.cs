using System;
using System.Collections.Generic;
using UnityEngine;

namespace LuckyStep
{
    /// <summary>
    /// The game's level order (Story 006): worlds in play order, each with its story card key and level files, plus
    /// the par table written by the solver. A level's id is its TextAsset name (e.g. "Level02"), the same id
    /// <see cref="ParEntry.level"/> and <see cref="Progress"/> use. Built by the editor menu
    /// "Lucky Step/Create Level Catalog" (LevelCatalogBuilder) at Assets/Levels/LevelCatalog.asset.
    /// </summary>
    /// <example>
    /// <code>
    /// TextAsset next = catalog.Next(controller.CurrentLevelFile);   // null after the last level
    /// if (next != null &amp;&amp; catalog.IsFirstOfWorld(next)) ShowStoryCard(catalog.Worlds[catalog.WorldIndexOf(next)].storyKey);
    /// </code>
    /// </example>
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Lucky Step/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        /// <summary>One world: a story card and its levels in play order.</summary>
        [Serializable]
        public sealed class World
        {
            /// <summary>Localization key prefix of the story card; title is storyKey + ".title", body storyKey + ".body".</summary>
            public string storyKey;

            /// <summary>Rule set the world's levels use (informational; each level file declares its own rules).</summary>
            public string ruleSetName;

            /// <summary>Level files in play order. May be empty for a world still being made.</summary>
            public TextAsset[] levels = new TextAsset[0];

            /// <summary>
            /// False hides par and the Golden pawn for this world's levels (<see cref="Par"/> returns -1). World 1 has
            /// no cat, so every solution is exactly 7 moves and par would reward nothing.
            /// </summary>
            public bool showPar = true;
        }

        /// <summary>Worlds in play order.</summary>
        public World[] worlds = new World[0];

        /// <summary>Assets/Levels/par.json (written by the editor solver).</summary>
        public TextAsset parTable;

        [NonSerialized] private ParTable _parsedPar;

        /// <summary>Worlds in play order.</summary>
        public IReadOnlyList<World> Worlds => worlds;

        /// <summary>Every level file across all worlds, in play order (allocates; call when a screen opens).</summary>
        public List<TextAsset> Levels
        {
            get
            {
                var list = new List<TextAsset>();
                foreach (World world in worlds)
                {
                    foreach (TextAsset level in world.levels)
                    {
                        if (level != null) list.Add(level);
                    }
                }
                return list;
            }
        }

        /// <summary>Level ids in play order, for <see cref="Progress.IsUnlocked"/> (allocates).</summary>
        public List<string> LevelIds => Levels.ConvertAll(level => level.name);

        /// <summary>First level of the game, or null if the catalog is empty.</summary>
        public TextAsset First
        {
            get
            {
                List<TextAsset> levels = Levels;
                return levels.Count > 0 ? levels[0] : null;
            }
        }

        /// <summary>Index into <see cref="Worlds"/> of the world containing <paramref name="level"/>, or -1.</summary>
        public int WorldIndexOf(TextAsset level)
        {
            for (int w = 0; w < worlds.Length; w++)
            {
                if (Array.IndexOf(worlds[w].levels, level) >= 0) return w;
            }
            return -1;
        }

        /// <summary>True if <paramref name="level"/> is the first level of its world (its story card shows before it).</summary>
        public bool IsFirstOfWorld(TextAsset level)
        {
            int w = WorldIndexOf(level);
            return w >= 0 && worlds[w].levels.Length > 0 && worlds[w].levels[0] == level;
        }

        /// <summary>The level after <paramref name="level"/> in play order, crossing worlds; null at the end or if not found.</summary>
        public TextAsset Next(TextAsset level)
        {
            List<TextAsset> levels = Levels;
            int i = levels.IndexOf(level);
            return i >= 0 && i + 1 < levels.Count ? levels[i + 1] : null;
        }

        /// <summary>
        /// Par moves for a level id from par.json; -1 when unknown, unsolved, or its world hides par
        /// (<see cref="World.showPar"/>). -1 means no Golden pawn is possible.
        /// </summary>
        public int Par(string levelId)
        {
            foreach (World world in worlds)
            {
                if (!world.showPar && Array.Exists(world.levels, level => level != null && level.name == levelId)) return -1;
            }
            if (_parsedPar == null)
            {
                _parsedPar = parTable != null ? JsonUtility.FromJson<ParTable>(parTable.text) : new ParTable();
                if (_parsedPar == null) _parsedPar = new ParTable();
            }
            ParEntry entry = _parsedPar.levels.Find(e => e.level == levelId);
            return entry != null ? entry.par : -1;
        }

        private void OnValidate()
        {
            _parsedPar = null;
        }
    }
}
