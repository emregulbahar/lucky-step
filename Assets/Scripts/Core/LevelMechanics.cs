using System.Collections.Generic;

namespace LuckyStep
{
    /// <summary>
    /// Which mechanics a level uses, as ids for the new-mechanic cards (spec tutorial-guide-and-tips-addition-2026-11-13).
    /// Ids, in display order: spike, flowerpot, dice, bridge, gap, cat, mirror, wood, bead. Localization keys are
    /// "tip." + id + ".title" / ".body"; the UI icon sprite is "tip_" + id.
    /// </summary>
    /// <example><code>foreach (string id in LevelMechanics.Find(level)) if (!progress.HasSeenTip(id)) ShowCard(id);</code></example>
    public static class LevelMechanics
    {
        /// <summary>Every mechanic id, in display order.</summary>
        public static readonly string[] All = { "spike", "flowerpot", "dice", "bridge", "gap", "cat", "mirror", "wood", "bead" };

        /// <summary>The ids of the mechanics in <paramref name="level"/>, in display order (allocates).</summary>
        public static List<string> Find(Level level)
        {
            var tiles = new HashSet<Tile>();
            for (int y = 0; y < level.Height; y++)
            {
                for (int x = 0; x < level.Width; x++) tiles.Add(level.GetTile(x, y));
            }
            var found = new List<string>();
            if (tiles.Contains(Tile.Spike)) found.Add("spike");
            if (tiles.Contains(Tile.Flowerpot)) found.Add("flowerpot");
            if (tiles.Contains(Tile.Dice)) found.Add("dice");
            if (tiles.Contains(Tile.Bridge)) found.Add("bridge");
            if (tiles.Contains(Tile.Gap)) found.Add("gap");
            if (level.CatRouteLength > 0) found.Add("cat");
            if (tiles.Contains(Tile.Mirror)) found.Add("mirror");
            if (tiles.Contains(Tile.Wood)) found.Add("wood");
            if (tiles.Contains(Tile.Bead)) found.Add("bead");
            return found;
        }
    }
}
