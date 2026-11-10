using LuckyStep;
using NUnit.Framework;

/// <summary>
/// EditMode tests for the coin bridge rule in <see cref="StepRules"/> (Story 002, decided 2026-11-10): a lucky step
/// opens every closed bridge on the board, wherever the pawn is; a closed bridge kills on a normal step.
/// </summary>
public sealed class StepRulesTests
{
    // S at x=1, seven floor tiles (x=2..8), bridges at x=9 and x=10, exit at x=11.
    private const string TwoBridgesAfterSevenFloors =
        "rules: default\n" +
        "#############\n" +
        "#S.......BBE#\n" +
        "#############\n";

    // S at x=1, six floor tiles (x=2..7), bridge at x=8 (the 7th step), floor, exit; a second bridge on row 2.
    private const string BridgeOnSeventhStep =
        "rules: default\n" +
        "############\n" +
        "#S......B.E#\n" +
        "#.B........#\n" +
        "############\n";

    private const string BridgeNextToStart =
        "rules: default\n" +
        "######\n" +
        "#SB.E#\n" +
        "######\n";

    private static Level Parse(string text)
    {
        Assert.IsTrue(LevelLoader.TryParse(text, out Level level, out string error), error);
        return level;
    }

    private static GameState Walk(Level level, GameState state, Direction direction, int steps)
    {
        for (int i = 0; i < steps; i++) state = StepRules.Step(level, state, direction);
        return state;
    }

    [Test]
    public void LuckyStepOnFloor_OpensAllBridges()
    {
        // Arrange
        Level level = Parse(TwoBridgesAfterSevenFloors);

        // Act: the 7th step lands on floor (x=8), not on a bridge.
        GameState state = Walk(level, level.CreateInitialState(), Direction.Right, 7);

        // Assert
        Assert.AreEqual(8, state.X);
        Assert.AreEqual(GameStatus.Playing, state.Status);
        Assert.AreEqual(0b11UL, state.OpenBridges);
    }

    [Test]
    public void OpenedBridges_NormalStepOnto_IsSafe()
    {
        // Arrange
        Level level = Parse(TwoBridgesAfterSevenFloors);
        GameState opened = Walk(level, level.CreateInitialState(), Direction.Right, 7);

        // Act: steps 8 and 9 are normal steps onto the two opened bridges.
        GameState state = Walk(level, opened, Direction.Right, 2);

        // Assert
        Assert.AreEqual(10, state.X);
        Assert.AreEqual(GameStatus.Playing, state.Status);
    }

    [Test]
    public void LuckyStepOntoClosedBridge_OpensEveryBridgeAndSurvives()
    {
        // Arrange
        Level level = Parse(BridgeOnSeventhStep);

        // Act
        GameState state = Walk(level, level.CreateInitialState(), Direction.Right, 7);

        // Assert
        Assert.AreEqual(8, state.X);
        Assert.AreEqual(GameStatus.Playing, state.Status);
        Assert.AreEqual(level.AllBridgesMask, state.OpenBridges);
        Assert.AreEqual(0b11UL, state.OpenBridges);
    }

    [Test]
    public void ClosedBridge_NormalStep_Kills()
    {
        // Arrange
        Level level = Parse(BridgeNextToStart);

        // Act
        GameState state = StepRules.Step(level, level.CreateInitialState(), Direction.Right);

        // Assert
        Assert.AreEqual(GameStatus.Dead, state.Status);
        Assert.AreEqual(0UL, state.OpenBridges);
    }

    [Test]
    public void NormalStepOnFloor_LeavesBridgesClosed()
    {
        // Arrange
        Level level = Parse(TwoBridgesAfterSevenFloors);

        // Act
        GameState state = Walk(level, level.CreateInitialState(), Direction.Right, 6);

        // Assert
        Assert.AreEqual(GameStatus.Playing, state.Status);
        Assert.AreEqual(0UL, state.OpenBridges);
    }

    [Test]
    public void AllBridgesMask_TwoBridges_HasTwoLowBits()
    {
        // Arrange / Act
        Level level = Parse(BridgeOnSeventhStep);

        // Assert
        Assert.AreEqual(0b11UL, level.AllBridgesMask);
    }
}
