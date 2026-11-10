using System;
using System.Collections.Generic;

namespace LuckyStep
{
    /// <summary>
    /// Parses text levels. Format: a header line "rules: default" (or "world4"), then grid rows using
    /// '#' wall, '.' floor, 'S' start, 'E' exit, '^' spike, 'D' dice tile, 'B' coin bridge, 'P' falling flowerpot,
    /// '~' gap (fatal on foot, jumped by a dice fling). Blank lines are ignored. Exactly one S and one E; all rows
    /// equal length; at most 64 bridges. 'M' is the broken mirror (counting flips to down, see StepRules).
    /// 'W' is a wooden tile (a step onto it does not count), 'N' an evil-eye bead (at most 64; see StepRules).
    /// Optional line "cat: x,y x,y x,y" (anywhere after the header): the black cat's route as grid coordinates, x = column,
    /// y = row, both 0-based from the top-left. The route LOOPS: after the last tile the cat goes back to the first, one
    /// tile per player step (for ping-pong, list the way out and back). It starts on the first tile. Needs at least 2
    /// tiles, all Floor tiles ('.' or 'S'), and each tile (including last to first) orthogonally adjacent to the next.
    /// Optional line "par: N" (anywhere after the header, at most one): the designer's intended par, a positive whole
    /// number of moves (Level.IntendedPar). The solver flags a shorter solution as a shortcut.
    /// </summary>
    public static class LevelLoader
    {
        /// <summary>
        /// Parses level text. On failure returns false, sets <paramref name="error"/>, and level is null (nothing half-loaded).
        /// </summary>
        /// <example>
        /// <code>
        /// if (LevelLoader.TryParse("rules: default\n#####\n#S.E#\n#####", out var level, out var error)) { }
        /// </code>
        /// </example>
        public static bool TryParse(string text, out Level level, out string error)
        {
            level = null;
            error = null;

            if (string.IsNullOrEmpty(text))
            {
                error = "Level text is empty.";
                return false;
            }

            text = text.TrimStart((char)0xFEFF); // strip a UTF-8 byte-order mark
            var rows = new List<string>();
            string catLine = null;
            int? intendedPar = null;
            RuleSet rules = null;
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (line.Trim().Length == 0) continue;

                if (rules == null)
                {
                    const string prefix = "rules:";
                    if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        error = "First line must be a 'rules: <name>' header.";
                        return false;
                    }
                    string name = line.Substring(prefix.Length).Trim();
                    rules = RuleSet.FindByName(name);
                    if (rules == null)
                    {
                        error = "Unknown rule set '" + name + "'.";
                        return false;
                    }
                    continue;
                }
                if (line.StartsWith("cat:", StringComparison.OrdinalIgnoreCase))
                {
                    if (catLine != null)
                    {
                        error = "Level has more than one 'cat:' line.";
                        return false;
                    }
                    catLine = line.Substring(4);
                    continue;
                }
                if (line.StartsWith("par:", StringComparison.OrdinalIgnoreCase))
                {
                    if (intendedPar != null)
                    {
                        error = "Level has more than one 'par:' line.";
                        return false;
                    }
                    if (!int.TryParse(line.Substring(4).Trim(), out int par) || par < 1)
                    {
                        error = "'par:' needs a positive whole number of moves, got '" + line.Substring(4).Trim() + "'.";
                        return false;
                    }
                    intendedPar = par;
                    continue;
                }
                rows.Add(line);
            }

            if (rules == null)
            {
                error = "Missing 'rules: <name>' header.";
                return false;
            }
            if (rows.Count == 0)
            {
                error = "Level has no grid rows.";
                return false;
            }

            int width = rows[0].Length;
            int height = rows.Count;
            var tiles = new Tile[width * height];
            int startX = -1, startY = -1, starts = 0, exits = 0, bridges = 0, beads = 0;

            for (int y = 0; y < height; y++)
            {
                if (rows[y].Length != width)
                {
                    error = "Row " + (y + 1) + " has length " + rows[y].Length + ", expected " + width + ".";
                    return false;
                }
                for (int x = 0; x < width; x++)
                {
                    switch (rows[y][x])
                    {
                        case '#': tiles[y * width + x] = Tile.Wall; break;
                        case '.': tiles[y * width + x] = Tile.Floor; break;
                        case 'S': tiles[y * width + x] = Tile.Floor; startX = x; startY = y; starts++; break;
                        case 'E': tiles[y * width + x] = Tile.Exit; exits++; break;
                        case '^': tiles[y * width + x] = Tile.Spike; break;
                        case 'D': tiles[y * width + x] = Tile.Dice; break;
                        case 'B': tiles[y * width + x] = Tile.Bridge; bridges++; break;
                        case 'P': tiles[y * width + x] = Tile.Flowerpot; break;
                        case '~': tiles[y * width + x] = Tile.Gap; break;
                        case 'M': tiles[y * width + x] = Tile.Mirror; break;
                        case 'W': tiles[y * width + x] = Tile.Wood; break;
                        case 'N': tiles[y * width + x] = Tile.Bead; beads++; break;
                        default:
                            error = "Unknown character '" + rows[y][x] + "' at row " + (y + 1) + ", column " + (x + 1) + ".";
                            return false;
                    }
                }
            }

            if (starts != 1)
            {
                error = "Level needs exactly one start 'S', found " + starts + ".";
                return false;
            }
            if (exits != 1)
            {
                error = "Level needs exactly one exit 'E', found " + exits + ".";
                return false;
            }

            if (bridges > 64)
            {
                error = "Level has " + bridges + " bridges, the maximum is 64.";
                return false;
            }
            if (beads > 64)
            {
                error = "Level has " + beads + " beads, the maximum is 64.";
                return false;
            }

            (int X, int Y)[] route = null;
            if (catLine != null)
            {
                string[] parts = catLine.Split(new[] { ' ', '\t' },StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    error = "Cat route needs at least 2 tiles.";
                    return false;
                }
                route = new (int X, int Y)[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    string[] xy = parts[i].Split(',');
                    if (xy.Length != 2 || !int.TryParse(xy[0], out int cx) || !int.TryParse(xy[1], out int cy))
                    {
                        error = "Cat route entry '" + parts[i] + "' is not 'x,y'.";
                        return false;
                    }
                    if (cx < 0 || cy < 0 || cx >= width || cy >= height || tiles[cy * width + cx] != Tile.Floor)
                    {
                        error = "Cat route tile " + parts[i] + " is not a floor tile inside the grid.";
                        return false;
                    }
                    route[i] = (cx, cy);
                }
                for (int i = 0; i < route.Length; i++)
                {
                    var next = route[(i + 1) % route.Length];
                    if (Math.Abs(route[i].X - next.X) + Math.Abs(route[i].Y - next.Y) != 1)
                    {
                        error = "Cat route tiles " + i + " and " + ((i + 1) % route.Length) + " are not adjacent.";
                        return false;
                    }
                }
            }

            level = new Level(width, height, tiles, startX, startY, rules, route, intendedPar);
            return true;
        }
    }
}
