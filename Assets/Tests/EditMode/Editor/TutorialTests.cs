using LuckyStep;
using NUnit.Framework;

/// <summary>
/// EditMode tests for the tutorial (spec tutorial-guide-and-tips-addition-2026-11-13): guide phases, target space and
/// what is on it, next and next lucky counts.
/// </summary>
public sealed class TutorialTests
{
    // Level01's layout: 6-space corridor to the exit plus a second row, so 7 steps need one detour.
    private const string Corridor = "rules: default\n########\n#S....E#\n#......#\n########";

    private static Level Parse(string text)
    {
        Assert.IsTrue(LevelLoader.TryParse(text, out Level level, out string error), error);
        return level;
    }

    private static GameState Play(Level level, params Direction[] moves)
    {
        GameState state = level.CreateInitialState();
        foreach (Direction d in moves) state = StepRules.Step(level, state, d);
        return state;
    }

    [Test]
    public void Evaluate_NoMoves_StartPhaseWithAdjacentTarget()
    {
        Level level = Parse(Corridor);

        GuideStep step = TutorialGuide.Evaluate(level, level.CreateInitialState(), 0);

        Assert.AreEqual(GuidePhase.Start, step.Phase);
        Assert.IsTrue(step.HasTarget);
        Assert.AreEqual(1, System.Math.Abs(step.TargetX - level.StartX) + System.Math.Abs(step.TargetY - level.StartY));
    }

    [Test]
    public void Evaluate_NextStepLucky_LuckyNextTargetsExit()
    {
        Level level = Parse(Corridor);
        GameState state = Play(level, Direction.Down, Direction.Up, Direction.Right, Direction.Right, Direction.Right, Direction.Right);

        GuideStep step = TutorialGuide.Evaluate(level, state, 6);

        Assert.AreEqual(GuidePhase.LuckyNext, step.Phase);
        Assert.AreEqual(6, step.Counter);
        Assert.IsTrue(step.HasTarget);
        Assert.AreEqual(Tile.Exit, level.GetTile(step.TargetX, step.TargetY));
        Assert.IsTrue(step.TargetIsExit);
        Assert.AreEqual(7, step.Next);
        Assert.IsNull(step.TargetObject);
    }

    [Test]
    public void Evaluate_StrayedOffPath_StillFindsWinningTarget()
    {
        Level level = Parse(Corridor);
        // Straight to the space beside the exit: counter 4 there, so the guide must lead away and back.
        GameState state = Play(level, Direction.Right, Direction.Right, Direction.Right, Direction.Right);

        GuideStep step = TutorialGuide.Evaluate(level, state, 4);

        Assert.AreEqual(GuidePhase.Counting, step.Phase);
        Assert.IsTrue(step.HasTarget);
        Assert.AreNotEqual(Tile.Exit, level.GetTile(step.TargetX, step.TargetY));
    }

    [Test]
    public void Evaluate_Dead_DeadPhaseNoTarget()
    {
        Level level = Parse("rules: default\n#####\n#S^E#\n#####");
        GameState state = Play(level, Direction.Right);

        GuideStep step = TutorialGuide.Evaluate(level, state, 1);

        Assert.AreEqual(GuidePhase.Dead, step.Phase);
        Assert.IsFalse(step.HasTarget);
    }

    // One row: the wooden space (x 3) does not count, so the exit (x 9) is step 7.
    private const string WoodRow = "rules: default\n###########\n#S.W.....E#\n###########";

    [Test]
    public void Evaluate_NextSpaceWood_NamesWood()
    {
        Level level = Parse(WoodRow);
        GameState state = Play(level, Direction.Right);

        GuideStep step = TutorialGuide.Evaluate(level, state, 1);

        Assert.AreEqual(GuidePhase.Counting, step.Phase);
        Assert.AreEqual(3, step.TargetX);
        Assert.AreEqual("wood", step.TargetObject);
        Assert.IsFalse(step.TargetIsExit);
    }

    [Test]
    public void Evaluate_PlainNextSpace_NoObject()
    {
        Level level = Parse(WoodRow);
        GameState state = Play(level, Direction.Right, Direction.Right, Direction.Right);

        GuideStep step = TutorialGuide.Evaluate(level, state, 3);

        Assert.AreEqual(GuidePhase.Counting, step.Phase);
        Assert.IsTrue(step.HasTarget);
        Assert.IsNull(step.TargetObject);
        Assert.AreEqual(7, step.NextLucky);
    }

    [Test]
    public void ObjectAt_Spike_Named()
    {
        Level level = Parse("rules: default\n######\n#S^.E#\n######");

        Assert.AreEqual("spike", TutorialGuide.ObjectAt(level, level.CreateInitialState(), 2, 1));
    }

    [Test]
    public void ObjectAt_BridgeOnlyWhileClosed()
    {
        Level level = Parse("rules: default\n######\n#SB.E#\n######");
        GameState closed = level.CreateInitialState();
        GameState open = closed.With(openBridges: level.AllBridgesMask);

        Assert.AreEqual("bridge", TutorialGuide.ObjectAt(level, closed, 2, 1));
        Assert.IsNull(TutorialGuide.ObjectAt(level, open, 2, 1));
        Assert.IsNull(TutorialGuide.ObjectAt(level, closed, 3, 1));
    }

    [Test]
    public void Evaluate_CountingDown_NextAndNextLuckyGoDown()
    {
        Level level = Parse(Corridor);
        GameState state = level.CreateInitialState().With(counter: 10, countingDown: true);

        GuideStep step = TutorialGuide.Evaluate(level, state, 3);

        Assert.AreEqual(9, step.Next);
        Assert.AreEqual(7, step.NextLucky);
    }
}
