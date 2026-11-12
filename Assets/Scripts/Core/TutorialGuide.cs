namespace LuckyStep
{
    /// <summary>What the tutorial guide is telling the player (spec tutorial-guide-and-tips-addition-2026-11-13).</summary>
    public enum GuidePhase
    {
        /// <summary>No move made yet: how to move.</summary>
        Start,
        /// <summary>Playing, the next step is not lucky: each step counts.</summary>
        Counting,
        /// <summary>Playing, the next step is lucky: go through the door now.</summary>
        LuckyNext,
        /// <summary>The pawn died: undo or restart.</summary>
        Dead,
        /// <summary>The level is won.</summary>
        Won
    }

    /// <summary>One evaluation of the guide: the phase, the counter and the space to step on next (if any).</summary>
    public readonly struct GuideStep
    {
        /// <summary>Creates a step.</summary>
        public GuideStep(GuidePhase phase, int counter, bool hasTarget, int targetX, int targetY)
        {
            Phase = phase;
            Counter = counter;
            HasTarget = hasTarget;
            TargetX = targetX;
            TargetY = targetY;
        }

        /// <summary>What to tell the player.</summary>
        public GuidePhase Phase { get; }

        /// <summary>The step counter in the evaluated state.</summary>
        public int Counter { get; }

        /// <summary>True when a winning move exists; TargetX/TargetY are then the space it steps towards.</summary>
        public bool HasTarget { get; }

        /// <summary>Grid x of the space to step on next.</summary>
        public int TargetX { get; }

        /// <summary>Grid y of the space to step on next.</summary>
        public int TargetY { get; }
    }

    /// <summary>
    /// Tutorial guide logic for guided levels: from a state, which phase applies and which neighbouring space starts a
    /// shortest win (solved from that state, so the guide keeps working after the player strays). Pure; allocates in
    /// the solver, so call it when the state changes, not every frame.
    /// </summary>
    /// <example><code>GuideStep step = TutorialGuide.Evaluate(level, controller.State, controller.Moves);</code></example>
    public static class TutorialGuide
    {
        /// <summary>Evaluates <paramref name="state"/>; <paramref name="movesTaken"/> 0 gives the Start phase while playing.</summary>
        public static GuideStep Evaluate(Level level, GameState state, int movesTaken)
        {
            if (state.Status == GameStatus.Won) return new GuideStep(GuidePhase.Won, state.Counter, false, 0, 0);
            if (state.Status == GameStatus.Dead) return new GuideStep(GuidePhase.Dead, state.Counter, false, 0, 0);

            GuidePhase phase;
            int next = state.CountingDown ? state.Counter - 1 : state.Counter + 1;
            if (movesTaken == 0) phase = GuidePhase.Start;
            else if (!level.Rules.IsDeadly(next) && level.Rules.IsLucky(next)) phase = GuidePhase.LuckyNext;
            else phase = GuidePhase.Counting;

            SolveResult solve = LevelSolver.SolveFrom(level, state);
            if (!solve.Solvable || solve.Moves.Count == 0) return new GuideStep(phase, state.Counter, false, 0, 0);
            int x = state.X;
            int y = state.Y;
            switch (solve.Moves[0])
            {
                case Direction.Up: y--; break;
                case Direction.Down: y++; break;
                case Direction.Left: x--; break;
                case Direction.Right: x++; break;
            }
            return new GuideStep(phase, state.Counter, true, x, y);
        }
    }
}
