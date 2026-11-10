namespace LuckyStep
{
    /// <summary>Movement direction. Up decreases Y.</summary>
    public enum Direction
    {
        /// <summary>Y - 1.</summary>
        Up,
        /// <summary>Y + 1.</summary>
        Down,
        /// <summary>X - 1.</summary>
        Left,
        /// <summary>X + 1.</summary>
        Right
    }

    /// <summary>The single source of truth for what a move does. Pure C#, shared by play and solver.</summary>
    public static class StepRules
    {
        /// <summary>
        /// Applies one move. Returns the SAME instance (reference-equal to <paramref name="state"/>) when the move is
        /// rejected: level not in Playing status, wall, or closed exit. A rejected move adds no step.
        /// A successful move changes the counter by exactly 1 (a dice fling is still ONE step: the step onto the dice
        /// tile), except a step onto wood, which leaves it unchanged. "Lucky" means the new count is lucky and not deadly (deadly beats lucky). The exit needs a lucky count,
        /// otherwise it is closed and blocks. A deadly count kills wherever the pawn lands.
        /// Dice: after stepping onto a dice tile the pawn is flung on in the same direction, RuleSet.DiceLuckyDistance
        /// tiles on a lucky step, DiceNormalDistance otherwise. It stops on the last free tile before a wall (or a closed
        /// exit); with a wall right behind the dice tile it stays on the dice tile. Tiles passed over never trigger
        /// (gaps, spikes, bridges, pots are jumped); only the landing tile does. No chaining: landing on a dice tile
        /// does not fling again.
        /// Coin bridges: a lucky step opens EVERY bridge on the board permanently (GameState.OpenBridges becomes
        /// Level.AllBridgesMask), wherever the pawn lands; a spike asks WHERE the lucky count is spent, a bridge asks WHEN.
        /// Landing tile: Spike and Flowerpot kill unless lucky; Gap always kills; a closed Bridge kills on a normal step
        /// (on a lucky step it is already open, so stepping onto it then is safe); an open Bridge is safe.
        /// Wood ("knock on wood"): a step straight onto a Wood tile is not counted: the counter keeps its value and the
        /// step is neither lucky nor deadly (the cat still moves). A dice fling that lands on wood is the dice step and
        /// counts normally.
        /// Evil-eye bead: landing on a Bead tile not yet taken picks it up (GameState.Beads + 1, its bit set in
        /// GameState.TakenBeads). If the step would kill by bad luck (a deadly count, or a spike or flowerpot on a
        /// normal step) and the pawn carries a bead, one bead is used up and the pawn lives (a bead picked up this very
        /// step already counts). Falls (a gap, a closed bridge) are not bad luck: a bead does not help.
        /// Order of operations in one successful step: (1) the new count is the old one +1, or -1 while
        /// GameState.CountingDown, or unchanged onto wood; (2) lucky/deadly are judged on that count; (3) dice fling,
        /// then on a lucky step all bridges open, then bead pickup, then the landing-tile effect and the bead ward;
        /// (4) the black cat advances one route index (looping); (5) cat collision: if the cat is on the pawn's landing
        /// tile, or the two swapped tiles, and the pawn is still alive and has not won, the counter becomes
        /// RuleSet.StartValue. The step's own count therefore resolves the tile first (a lucky 7 can open a bridge and
        /// be reset in the same step); (6) mirror: landing on a Mirror tile sets CountingDown (the stepping-on step itself
        /// counted normally); (7) floor: if counting down and the counter is at or below StartValue (reached by counting
        /// down, or reset by the cat), CountingDown goes back to false, so the next step counts up.
        /// </summary>
        /// <example>
        /// <code>
        /// var next = StepRules.Step(level, state, Direction.Right);
        /// bool moved = !ReferenceEquals(next, state);
        /// </code>
        /// </example>
        public static GameState Step(Level level, GameState state, Direction direction)
        {
            if (state.Status != GameStatus.Playing) return state;

            int dx = 0;
            int dy = 0;
            switch (direction)
            {
                case Direction.Up: dy = -1; break;
                case Direction.Down: dy = 1; break;
                case Direction.Left: dx = -1; break;
                case Direction.Right: dx = 1; break;
            }
            int nx = state.X + dx;
            int ny = state.Y + dy;

            Tile tile = level.GetTile(nx, ny);
            if (tile == Tile.Wall) return state;

            RuleSet rules = level.Rules;
            // Knock on wood: a step straight onto a wooden tile is not counted, so it is neither lucky nor deadly.
            bool counted = tile != Tile.Wood;
            int count = !counted ? state.Counter : state.CountingDown ? state.Counter - 1 : state.Counter + 1;
            bool deadly = counted && rules.IsDeadly(count);
            bool lucky = counted && !deadly && rules.IsLucky(count);

            if (tile == Tile.Exit && !lucky) return state;

            if (tile == Tile.Dice)
            {
                int distance = lucky ? rules.DiceLuckyDistance : rules.DiceNormalDistance;
                for (int i = 0; i < distance; i++)
                {
                    Tile ahead = level.GetTile(nx + dx, ny + dy);
                    if (ahead == Tile.Wall || (ahead == Tile.Exit && !lucky)) break;
                    nx += dx;
                    ny += dy;
                }
                tile = level.GetTile(nx, ny);
            }

            int beads = state.Beads;
            ulong takenBeads = state.TakenBeads;
            if (tile == Tile.Bead)
            {
                ulong beadBit = 1UL << level.BeadIndex(nx, ny);
                if ((takenBeads & beadBit) == 0)
                {
                    takenBeads |= beadBit;
                    beads++;
                }
            }

            bool cursed = deadly; // bad luck a bead wards off
            bool falls = false;   // gravity: no bead helps
            ulong bridges = lucky ? level.AllBridgesMask : state.OpenBridges;
            switch (tile)
            {
                case Tile.Spike:
                case Tile.Flowerpot:
                    cursed |= !lucky;
                    break;
                case Tile.Gap:
                    falls = true;
                    break;
                case Tile.Bridge:
                    falls = (bridges & (1UL << level.BridgeIndex(nx, ny))) == 0;
                    break;
            }
            if (cursed && !falls && beads > 0)
            {
                beads--;
                cursed = false;
            }
            bool dies = cursed || falls;

            GameStatus status = tile == Tile.Exit ? GameStatus.Won : dies ? GameStatus.Dead : GameStatus.Playing;

            int catIndex = state.CatIndex;
            if (level.CatRouteLength > 0)
            {
                catIndex = (catIndex + 1) % level.CatRouteLength;
                var oldCat = level.GetCatPosition(state.CatIndex);
                var newCat = level.GetCatPosition(catIndex);
                bool same = newCat.X == nx && newCat.Y == ny;
                bool swapped = newCat.X == state.X && newCat.Y == state.Y && oldCat.X == nx && oldCat.Y == ny;
                if ((same || swapped) && status == GameStatus.Playing) count = rules.StartValue;
            }

            bool countingDown = state.CountingDown || tile == Tile.Mirror;
            if (countingDown && count <= rules.StartValue) countingDown = false;

            return new GameState(nx, ny, count, status, bridges, catIndex, countingDown, beads, takenBeads);
        }
    }
}
