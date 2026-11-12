using System.Collections.Generic;
using LuckyStep;
using NUnit.Framework;

/// <summary>
/// EditMode tests for the tutorial (spec tutorial-guide-and-tips-addition-2026-11-13): guide phases and target space,
/// mechanic detection, seen-card persistence.
/// </summary>
public sealed class TutorialTests
{
    // Level01's layout: 6-space corridor to the exit plus a second row, so 7 steps need one detour.
    private const string Corridor = "rules: default\n########\n#S....E#\n#......#\n########";

    private sealed class MemoryStore : IProgressStore
    {
        public string Json;
        public string Load() => Json;
        public void Save(string json) => Json = json;
    }

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

    [Test]
    public void Find_SpikePotAndCat_InDisplayOrder()
    {
        Level level = Parse("rules: default\n#######\n#S..PE#\n#.^...#\n#######\ncat: 2,1 3,1");

        List<string> found = LevelMechanics.Find(level);

        CollectionAssert.AreEqual(new[] { "spike", "flowerpot", "cat" }, found);
    }

    [Test]
    public void Find_PlainLevel_Empty()
    {
        CollectionAssert.IsEmpty(LevelMechanics.Find(Parse(Corridor)));
    }

    [Test]
    public void MarkTipSeen_SavedAndReloaded()
    {
        var store = new MemoryStore();
        var first = new Progress(store);

        first.MarkTipSeen("cat");
        var second = new Progress(store);

        Assert.IsTrue(second.HasSeenTip("cat"));
        Assert.IsFalse(second.HasSeenTip("mirror"));
    }
}
