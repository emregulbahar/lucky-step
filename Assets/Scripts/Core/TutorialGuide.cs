namespace LuckyStep
{
    /// <summary>What the tutorial guide is telling the player (spec tutorial-guide-and-tips-addition-2026-11-13).</summary>
    public enum GuidePhase
    {
        /// <summary>No move made yet: how to move (or, in a teaching level, what is new).</summary>
        Start,
        /// <summary>Playing, the next step is not lucky: each step counts.</summary>
        Counting,
        /// <summary>Playing, the next step is lucky.</summary>
        LuckyNext,
        /// <summary>The pawn died: undo or restart.</summary>
        Dead,
        /// <summary>The level is won.</summary>
        Won
    }

    /// <summary>
    /// One evaluation of the guide: the phase, the counter, the next count, the next lucky count, the space to step on
    /// next (if any) and what is on that space.
    /// </summary>
    public readonly struct GuideStep
    {
        /// <summary>Creates a step.</summary>
        public GuideStep(GuidePhase phase, int counter, int next, int nextLucky, bool hasTarget, int targetX, int targetY,
            bool targetIsExit, string targetObject)
        {
            Phase = phase;
            Counter = counter;
            Next = next;
            NextLucky = nextLucky;
            HasTarget = hasTarget;
            TargetX = targetX;
            TargetY = targetY;
            TargetIsExit = targetIsExit;
            TargetObject = targetObject;
        }

        /// <summary>What to tell the player.</summary>
        public GuidePhase Phase { get; }

        /// <summary>The step counter in the evaluated state.</summary>
        public int Counter { get; }

        /// <summary>The count after one more counted step (counter - 1 while counting down).</summary>
        public int Next { get; }

        /// <summary>The first lucky, not deadly count after the counter, in the current counting direction.</summary>
        public int NextLucky { get; }

        /// <summary>True when a winning move exists; TargetX/TargetY are then the space it steps towards.</summary>
        public bool HasTarget { get; }

        /// <summary>Grid x of the space to step on next.</summary>
        public int TargetX { get; }

        /// <summary>Grid y of the space to step on next.</summary>
        public int TargetY { get; }

        /// <summary>True when the target space is the exit door.</summary>
        public bool TargetIsExit { get; }

        /// <summary>
        /// Id of the chance object on the target space while it still matters (spike, flowerpot, dice, bridge while
        /// closed, mirror, wood, bead while not taken), else null. Localization keys "tip." + id + ".title" / ".body".
        /// </summary>
        public string TargetObject { get; }
    }

    /// <summary>
    /// Tutorial guide logic for guided levels: from a state, which phase applies, which neighbouring space starts a
    /// shortest win (solved from that state, so the guide keeps working after the player strays) and what is on that
    /// space, so the bubble can explain an object right before the pawn meets it. Pure; allocates in the solver, so
    /// call it when the state changes, not every frame.
    /// </summary>
    /// <example><code>GuideStep step = TutorialGuide.Evaluate(level, controller.State, controller.Moves);</code></example>
    public static class TutorialGuide
    {
        /// <summary>Evaluates <paramref name="state"/>; <paramref name="movesTaken"/> 0 gives the Start phase while playing.</summary>
        public static GuideStep Evaluate(Level level, GameState state, int movesTaken)
        {
            int direction = state.CountingDown ? -1 : 1;
            int next = state.Counter + direction;
            int nextLucky = NextLucky(level.Rules, state.Counter, direction);
            if (state.Status == GameStatus.Won) return new GuideStep(GuidePhase.Won, state.Counter, next, nextLucky, false, 0, 0, false, null);
            if (state.Status == GameStatus.Dead) return new GuideStep(GuidePhase.Dead, state.Counter, next, nextLucky, false, 0, 0, false, null);

            GuidePhase phase;
            if (movesTaken == 0) phase = GuidePhase.Start;
            else if (!level.Rules.IsDeadly(next) && level.Rules.IsLucky(next)) phase = GuidePhase.LuckyNext;
            else phase = GuidePhase.Counting;

            SolveResult solve = LevelSolver.SolveFrom(level, state);
            if (!solve.Solvable || solve.Moves.Count == 0)
            {
                return new GuideStep(phase, state.Counter, next, nextLucky, false, 0, 0, false, null);
            }
            int x = state.X;
            int y = state.Y;
            switch (solve.Moves[0])
            {
                case Direction.Up: y--; break;
                case Direction.Down: y++; break;
                case Direction.Left: x--; break;
                case Direction.Right: x++; break;
            }
            return new GuideStep(phase, state.Counter, next, nextLucky, true, x, y, level.GetTile(x, y) == Tile.Exit,
                ObjectAt(level, state, x, y));
        }

        /// <summary>The chance object id on (x, y) while it still matters in <paramref name="state"/>, else null.</summary>
        public static string ObjectAt(Level level, GameState state, int x, int y)
        {
            switch (level.GetTile(x, y))
            {
                case Tile.Spike: return "spike";
                case Tile.Flowerpot: return "flowerpot";
                case Tile.Dice: return "dice";
                case Tile.Mirror: return "mirror";
                case Tile.Wood: return "wood";
                case Tile.Bridge:
                    int bridge = level.BridgeIndex(x, y);
                    return bridge >= 0 && (state.OpenBridges & (1UL << bridge)) != 0 ? null : "bridge";
                case Tile.Bead:
                    int bead = level.BeadIndex(x, y);
                    return bead >= 0 && (state.TakenBeads & (1UL << bead)) != 0 ? null : "bead";
                default: return null;
            }
        }

        /// <summary>The first lucky, not deadly count after <paramref name="counter"/> going <paramref name="direction"/>.</summary>
        private static int NextLucky(RuleSet rules, int counter, int direction)
        {
            for (int i = 1; i < 100; i++)
            {
                int c = counter + direction * i;
                if (c < 0) break;
                if (!rules.IsDeadly(c) && rules.IsLucky(c)) return c;
            }
            return counter + direction;
        }
    }
}
