using System.Collections.Generic;
using System.Text;

namespace LuckyStep
{
    /// <summary>How a solver run ended.</summary>
    public enum SolveOutcome
    {
        /// <summary>A shortest winning move sequence was found.</summary>
        Solved,
        /// <summary>Every reachable state was searched without hitting a cap and none wins: the level cannot be won.</summary>
        ProvenUnsolvable,
        /// <summary>The depth or state cap stopped the search before a win was found. The level may still be winnable.</summary>
        NoSolutionWithinCap
    }

    /// <summary>Result of <see cref="LevelSolver.Solve"/>. Immutable.</summary>
    public sealed class SolveResult
    {
        /// <summary>How the search ended.</summary>
        public SolveOutcome Outcome { get; }

        /// <summary>One shortest winning move sequence; empty unless <see cref="Outcome"/> is Solved.</summary>
        public IReadOnlyList<Direction> Moves { get; }

        /// <summary>Distinct live (Playing) states reached, the initial state included. Dead and won states are not counted.</summary>
        public int StatesExplored { get; }

        /// <summary>True if a solution was found.</summary>
        public bool Solvable => Outcome == SolveOutcome.Solved;

        /// <summary>True if a cap stopped the search (no solution within cap).</summary>
        public bool HitCap => Outcome == SolveOutcome.NoSolutionWithinCap;

        /// <summary>Shortest solution length in moves; -1 unless solved.</summary>
        public int Par => Solvable ? Moves.Count : -1;

        /// <summary>The moves as letters U, D, L, R (e.g. "RRRRRRR"); empty unless solved.</summary>
        public string MoveString
        {
            get
            {
                var text = new StringBuilder(Moves.Count);
                foreach (Direction move in Moves)
                {
                    text.Append(move == Direction.Up ? 'U' : move == Direction.Down ? 'D' : move == Direction.Left ? 'L' : 'R');
                }
                return text.ToString();
            }
        }

        /// <summary>Creates a result.</summary>
        public SolveResult(SolveOutcome outcome, IReadOnlyList<Direction> moves, int statesExplored)
        {
            Outcome = outcome;
            Moves = moves;
            StatesExplored = statesExplored;
        }
    }

    /// <summary>
    /// Breadth-first search over full game states (position, counter, count direction, cat index, open bridges, status)
    /// using <see cref="StepRules.Step"/>, the same function gameplay uses. Pure C#, no engine dependency.
    /// A rejected move (Step returns the same instance) is no edge; dead states are dropped; the goal is Status Won.
    /// Moves are tried in the fixed order Up, Down, Left, Right, so the same level always gives the same par and the
    /// same move sequence.
    /// <para>
    /// Termination: the counter never caps in play, so the search is capped by moves and by state count. With the
    /// built-in rule sets a live counter stays small in practice (13, or any count containing a 4 in World 4, kills
    /// on the way up; the cat and the mirror only lower it), so the reachable state space is finite and the frontier
    /// normally empties: the result is then Solved or ProvenUnsolvable. The caps are a safety net (deep or
    /// bridge-heavy levels); hitting one gives NoSolutionWithinCap, never ProvenUnsolvable.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// LevelLoader.TryParse(text, out Level level, out string error);
    /// SolveResult result = LevelSolver.Solve(level);
    /// string summary = result.Solvable ? result.Par + " " + result.MoveString : result.Outcome.ToString();
    /// </code>
    /// </example>
    public static class LevelSolver
    {
        /// <summary>
        /// Default move cap. A counter run from the start value has at most 12 safe steps before a deadly count, and
        /// every extra lucky event needs a cat reset, so jam-sized pars are well under 100; 200 leaves more than 2x margin.
        /// </summary>
        public const int DefaultMaxDepth = 200;

        /// <summary>
        /// Default state cap. About 100-150 bytes per stored state (object, hash-set slot, parent and depth entries), so
        /// 1,000,000 states stays near 150 MB in the editor, while a 20x10 level with 13 counter values, a cat route and
        /// a few bridges is in the tens of thousands.
        /// </summary>
        public const int DefaultMaxStates = 1000000;

        private static readonly Direction[] Order = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

        /// <summary>
        /// Finds a shortest winning move sequence from <see cref="Level.CreateInitialState"/>.
        /// Any solution of at most <paramref name="maxDepth"/> moves is found; the search stops with NoSolutionWithinCap
        /// when a state at maxDepth would need expanding or when <paramref name="maxStates"/> live states are stored.
        /// </summary>
        /// <param name="level">Parsed level; its rule set comes from the level file.</param>
        /// <param name="maxDepth">Longest solution searched, in moves.</param>
        /// <param name="maxStates">Most distinct live states stored before giving up.</param>
        public static SolveResult Solve(Level level, int maxDepth = DefaultMaxDepth, int maxStates = DefaultMaxStates)
        {
            return SolveFrom(level, level.CreateInitialState(), maxDepth, maxStates);
        }

        /// <summary>
        /// Finds a shortest winning move sequence from <paramref name="start"/> (any reachable state; the tutorial
        /// guide asks it for the next move). Same caps and outcomes as <see cref="Solve"/>; a start that is already
        /// won or dead gives ProvenUnsolvable with no moves.
        /// </summary>
        /// <example><code>Direction next = LevelSolver.SolveFrom(level, controller.State).Moves[0];</code></example>
        public static SolveResult SolveFrom(Level level, GameState start, int maxDepth = DefaultMaxDepth, int maxStates = DefaultMaxStates)
        {
            if (start.Status != GameStatus.Playing) return new SolveResult(SolveOutcome.ProvenUnsolvable, new Direction[0], 0);
            var visited = new HashSet<GameState> { start };
            // The list doubles as the BFS queue (head index); parents/moves/depths are parallel to it.
            var states = new List<GameState> { start };
            var parents = new List<int> { -1 };
            var moves = new List<Direction> { Direction.Up }; // unused for the initial state
            var depths = new List<int> { 0 };
            bool depthCapped = false;

            for (int head = 0; head < states.Count; head++)
            {
                if (depths[head] >= maxDepth)
                {
                    depthCapped = true;
                    continue;
                }
                GameState state = states[head];
                foreach (Direction direction in Order)
                {
                    GameState next = StepRules.Step(level, state, direction);
                    if (ReferenceEquals(next, state) || next.Status == GameStatus.Dead) continue;
                    if (next.Status == GameStatus.Won)
                    {
                        return new SolveResult(SolveOutcome.Solved, BuildPath(parents, moves, head, direction), states.Count);
                    }
                    if (!visited.Add(next)) continue;
                    if (states.Count >= maxStates)
                    {
                        return new SolveResult(SolveOutcome.NoSolutionWithinCap, new Direction[0], states.Count);
                    }
                    states.Add(next);
                    parents.Add(head);
                    moves.Add(direction);
                    depths.Add(depths[head] + 1);
                }
            }

            SolveOutcome outcome = depthCapped ? SolveOutcome.NoSolutionWithinCap : SolveOutcome.ProvenUnsolvable;
            return new SolveResult(outcome, new Direction[0], states.Count);
        }

        private static List<Direction> BuildPath(List<int> parents, List<Direction> moves, int head, Direction last)
        {
            var path = new List<Direction>();
            for (int i = head; parents[i] >= 0; i = parents[i])
            {
                path.Add(moves[i]);
            }
            path.Reverse();
            path.Add(last);
            return path;
        }
    }
}
