using System;
using System.Collections.Generic;

namespace LuckyStep
{
    /// <summary>
    /// Par per level, stored in Assets/Levels/par.json. Written by the editor solver (menu "Lucky Step"); the game
    /// reads it as a TextAsset, so it works in WebGL without file access. Plain serializable data for
    /// UnityEngine.JsonUtility, which needs public fields and cannot read dictionaries (hence a list).
    /// </summary>
    /// <example>
    /// <code>
    /// ParTable table = UnityEngine.JsonUtility.FromJson&lt;ParTable&gt;(parJson.text);
    /// ParEntry entry = table.levels.Find(e => e.level == levelAsset.name);
    /// </code>
    /// </example>
    [Serializable]
    public sealed class ParTable
    {
        /// <summary>One entry per level file, sorted by level name (ordinal).</summary>
        public List<ParEntry> levels = new List<ParEntry>();
    }

    /// <summary>Solver result for one level in <see cref="ParTable"/>.</summary>
    [Serializable]
    public sealed class ParEntry
    {
        /// <summary>Level file name without extension, equal to the level TextAsset's name (e.g. "Level02").</summary>
        public string level;

        /// <summary>Shortest solution length in moves; -1 when the solver found none (unsolvable or no solution within cap).</summary>
        public int par;

        /// <summary>One shortest solution as U/D/L/R letters (e.g. "RRRRRRR"); empty when par is -1.</summary>
        public string moves;
    }
}
