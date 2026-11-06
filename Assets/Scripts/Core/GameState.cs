using System;

namespace LuckyStep
{
    /// <summary>Whether the level is still being played.</summary>
    public enum GameStatus
    {
        /// <summary>Accepting moves.</summary>
        Playing,
        /// <summary>A deadly step was taken; only undo and restart are allowed.</summary>
        Dead,
        /// <summary>The open exit was reached.</summary>
        Won
    }

    /// <summary>
    /// Immutable snapshot of everything that changes during play. Undo is a stack of these.
    /// Later stories add fields to the constructor
    /// and to <see cref="With"/>, Equals and GetHashCode; callers of Step do not change.
    /// </summary>
    /// <example>
    /// <code>
    /// var s = new GameState(1, 1, 0, GameStatus.Playing);
    /// var moved = s.With(x: 2, counter: 1);
    /// </code>
    /// </example>
    public sealed class GameState : IEquatable<GameState>
    {
        /// <summary>Pawn column.</summary>
        public int X { get; }

        /// <summary>Pawn row (0 is the top row).</summary>
        public int Y { get; }

        /// <summary>Step counter. Never capped.</summary>
        public int Counter { get; }

        /// <summary>Playing, dead or won.</summary>
        public GameStatus Status { get; }

        /// <summary>
        /// Bitmask of opened coin bridges; bit i is the bridge with index i (see <see cref="Level.BridgeIndex"/>).
        /// A level has at most 64 bridges.
        /// </summary>
        public ulong OpenBridges { get; }

        /// <summary>Index into the level's cat route of the tile the black cat stands on. 0 in levels without a cat.</summary>
        public int CatIndex { get; }

        /// <summary>True while the broken mirror makes the counter count down.</summary>
        public bool CountingDown { get; }

        /// <summary>True if the pawn died.</summary>
        public bool IsDead => Status == GameStatus.Dead;

        /// <summary>True if the level is complete.</summary>
        public bool IsWon => Status == GameStatus.Won;

        /// <summary>Creates a state.</summary>
        public GameState(int x, int y, int counter, GameStatus status, ulong openBridges = 0,
            int catIndex = 0, bool countingDown = false)
        {
            CatIndex = catIndex;
            CountingDown = countingDown;
            OpenBridges = openBridges;
            X = x;
            Y = y;
            Counter = counter;
            Status = status;
        }

        /// <summary>Returns a copy with the given fields replaced.</summary>
        public GameState With(int? x = null, int? y = null, int? counter = null, GameStatus? status = null, ulong? openBridges = null,
            int? catIndex = null, bool? countingDown = null)
        {
            return new GameState(x ?? X, y ?? Y, counter ?? Counter, status ?? Status, openBridges ?? OpenBridges,
                catIndex ?? CatIndex, countingDown ?? CountingDown);
        }

        /// <inheritdoc/>
        public bool Equals(GameState other)
        {
            return other != null && X == other.X && Y == other.Y && Counter == other.Counter && Status == other.Status
                && OpenBridges == other.OpenBridges
                && CatIndex == other.CatIndex && CountingDown == other.CountingDown;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as GameState);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(X, Y, Counter, (int)Status, OpenBridges, CatIndex, CountingDown);
    }
}
