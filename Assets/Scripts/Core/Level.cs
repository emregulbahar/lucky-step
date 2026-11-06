using System;

namespace LuckyStep
{
    /// <summary>Static tile kinds of a level grid.</summary>
    public enum Tile
    {
        /// <summary>Walkable floor (also under the start tile).</summary>
        Floor,
        /// <summary>Blocks movement.</summary>
        Wall,
        /// <summary>The exit door: enterable only on a lucky step.</summary>
        Exit,
        /// <summary>Spike: kills on a normal step, retracted (safe) on a lucky step.</summary>
        Spike,
        /// <summary>Dice tile: flings the pawn onward in its movement direction.</summary>
        Dice,
        /// <summary>Coin bridge over a gap: a lucky step opens it for good, a normal step on a closed one is a fatal fall.</summary>
        Bridge,
        /// <summary>Falling flowerpot: stepping on its tile kills on a normal step, misses on a lucky step.</summary>
        Flowerpot,
        /// <summary>Open gap/hole: entering it on foot is a fatal fall; a dice fling jumps over it.</summary>
        Gap,
        /// <summary>Broken mirror: stepping onto it makes the counter count down on the following steps.</summary>
        Mirror
    }

    /// <summary>Immutable parsed level: grid, start position and rule set. Pure C#.</summary>
    public sealed class Level
    {
        private readonly Tile[] _tiles;
        private readonly int[] _bridgeIndex;
        private readonly (int X, int Y)[] _catRoute;

        /// <summary>Grid width in tiles.</summary>
        public int Width { get; }

        /// <summary>Grid height in tiles.</summary>
        public int Height { get; }

        /// <summary>Start column.</summary>
        public int StartX { get; }

        /// <summary>Start row.</summary>
        public int StartY { get; }

        /// <summary>Rules used by this level.</summary>
        public RuleSet Rules { get; }

        /// <summary>Number of tiles in the black cat's route; 0 means the level has no cat.</summary>
        public int CatRouteLength => _catRoute.Length;

        /// <summary>
        /// The designer's intended par from the level file's optional "par:" line; null when the file has none.
        /// The solver compares it with the shortest solution to flag unintended shortcuts.
        /// </summary>
        public int? IntendedPar { get; }

        /// <summary>
        /// Creates a level from a row-major tile array of Width*Height tiles. <paramref name="catRoute"/> is the cat's
        /// looping route (null for no cat); the loader validates it. <paramref name="intendedPar"/> is the designer's
        /// par (null for none). Throws ArgumentException on a wrong tile count or more than 64 bridges.
        /// </summary>
        public Level(int width, int height, Tile[] tiles, int startX, int startY, RuleSet rules,
            (int X, int Y)[] catRoute = null, int? intendedPar = null)
        {
            IntendedPar = intendedPar;
            if (tiles.Length != width * height)
                throw new ArgumentException("tiles.Length must equal width * height.", nameof(tiles));
            _catRoute = catRoute ?? new (int X, int Y)[0];
            Width = width;
            Height = height;
            _tiles = tiles;
            StartX = startX;
            StartY = startY;
            Rules = rules;

            _bridgeIndex = new int[tiles.Length];
            int next = 0;
            for (int i = 0; i < tiles.Length; i++)
            {
                _bridgeIndex[i] = tiles[i] == Tile.Bridge ? next++ : -1;
            }
            if (next > 64) throw new ArgumentException("A level has at most 64 bridges.", nameof(tiles));
        }

        /// <summary>Position of the cat when it is at route index <paramref name="index"/>. Only valid when CatRouteLength > 0.</summary>
        public (int X, int Y) GetCatPosition(int index)
        {
            return _catRoute[index];
        }

        /// <summary>Index (row-major order, 0-based) of the bridge at (x, y); its bit in GameState.OpenBridges.</summary>
        public int BridgeIndex(int x, int y)
        {
            return _bridgeIndex[y * Width + x];
        }

        /// <summary>Tile at (x, y). Anything outside the grid is a wall.</summary>
        public Tile GetTile(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return Tile.Wall;
            return _tiles[y * Width + x];
        }

        /// <summary>The state a fresh play or restart begins in.</summary>
        public GameState CreateInitialState()
        {
            return new GameState(StartX, StartY, Rules.StartValue, GameStatus.Playing);
        }
    }
}
