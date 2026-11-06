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
        /// A successful move increments the counter by exactly 1 (a dice fling is still ONE step: the step onto the dice
        /// tile). "Lucky" means the new count is lucky and not deadly (deadly beats lucky). The exit needs a lucky count,
        /// otherwise it is closed and blocks. A deadly count kills wherever the pawn lands.
        /// Dice: after stepping onto a dice tile the pawn is flung on in the same direction, RuleSet.DiceLuckyDistance
        /// tiles on a lucky step, DiceNormalDistance otherwise. It stops on the last free tile before a wall (or a closed
        /// exit); with a wall right behind the dice tile it stays on the dice tile. Tiles passed over never trigger
        /// (gaps, spikes, bridges, pots are jumped); only the landing tile does. No chaining: landing on a dice tile
        /// does not fling again.
        /// Landing tile: Spike and Flowerpot kill unless lucky; Gap always kills; a closed Bridge opens permanently on a
        /// lucky step (GameState.OpenBridges) and kills on a normal step; an open Bridge is safe.
        /// Order of operations in one successful step: (1) the new count is the old one +1, or -1 while
        /// GameState.CountingDown; (2) lucky/deadly are judged on that count; (3) dice fling and landing-tile effect;
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
            int count = state.CountingDown ? state.Counter - 1 : state.Counter + 1;
            bool deadly = rules.IsDeadly(count);
            bool lucky = !deadly && rules.IsLucky(count);

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

            bool dies = deadly;
            ulong bridges = state.OpenBridges;
            switch (tile)
            {
                case Tile.Spike:
                case Tile.Flowerpot:
                    dies |= !lucky;
                    break;
                case Tile.Gap:
                    dies = true;
                    break;
                case Tile.Bridge:
                    ulong bit = 1UL << level.BridgeIndex(nx, ny);
                    if ((bridges & bit) == 0)
                    {
                        if (lucky) bridges |= bit;
                        else dies = true;
                    }
                    break;
            }

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

            return new GameState(nx, ny, count, status, bridges, catIndex, countingDown);
        }
    }
}
