using LuckyStep;
using NUnit.Framework;

/// <summary>
/// EditMode tests for the World 5 objects in <see cref="StepRules"/> (Story 009): the wooden tile ("knock on wood":
/// stepping onto it does not count) and the evil-eye bead (picked up, wards off one death, but not a fall).
/// </summary>
public sealed class WoodAndBeadTests
{
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
    public void StepOntoWood_CounterUnchanged()
    {
        // Arrange
        Level level = Parse("rules: default\n######\n#SW.E#\n######\n");

        // Act
        GameState state = StepRules.Step(level, level.CreateInitialState(), Direction.Right);

        // Assert
        Assert.AreEqual(2, state.X);
        Assert.AreEqual(0, state.Counter);
        Assert.AreEqual(GameStatus.Playing, state.Status);
    }

    [Test]
    public void StepOntoWood_CatStillMoves()
    {
        // Arrange: the cat paces row 2.
        Level level = Parse("rules: default\n######\n#SW.E#\n#....#\n######\ncat: 1,2 2,2 3,2 2,2\n");

        // Act
        GameState state = StepRules.Step(level, level.CreateInitialState(), Direction.Right);

        // Assert
        Assert.AreEqual(1, state.CatIndex);
        Assert.AreEqual(0, state.Counter);
    }

    [Test]
    public void WoodStep_ShiftsLuckyCountOntoExit()
    {
        // Arrange: the exit is 8 steps away; one uncounted wood step makes the 8th step the lucky 7th count.
        Level level = Parse("rules: default\n###########\n#S......WE#\n###########\n");

        // Act
        GameState state = Walk(level, level.CreateInitialState(), Direction.Right, 8);

        // Assert
        Assert.AreEqual(GameStatus.Won, state.Status);
        Assert.AreEqual(7, state.Counter);
    }

    [Test]
    public void StepOffWood_CountsNormally()
    {
        // Arrange
        Level level = Parse("rules: default\n######\n#SW.E#\n######\n");

        // Act
        GameState state = Walk(level, level.CreateInitialState(), Direction.Right, 2);

        // Assert
        Assert.AreEqual(3, state.X);
        Assert.AreEqual(1, state.Counter);
    }

    [Test]
    public void Bead_SavesFromSpike()
    {
        // Arrange
        Level level = Parse("rules: default\n#######\n#SN^.E#\n#######\n");

        // Act: pick up the bead, then step on the spike at count 2.
        GameState state = Walk(level, level.CreateInitialState(), Direction.Right, 2);

        // Assert
        Assert.AreEqual(GameStatus.Playing, state.Status);
        Assert.AreEqual(3, state.X);
    }

    [Test]
    public void Bead_IsUsedUp_SecondSpikeKills()
    {
        // Arrange
        Level level = Parse("rules: default\n#######\n#SN^^E#\n#######\n");

        // Act
        GameState state = Walk(level, level.CreateInitialState(), Direction.Right, 3);

        // Assert
        Assert.AreEqual(GameStatus.Dead, state.Status);
    }

    [Test]
    public void Bead_DoesNotSaveFromGap()
    {
        // Arrange
        Level level = Parse("rules: default\n#######\n#SN~.E#\n#######\n");

        // Act
        GameState state = Walk(level, level.CreateInitialState(), Direction.Right, 2);

        // Assert
        Assert.AreEqual(GameStatus.Dead, state.Status);
    }

    [Test]
    public void Bead_SurvivesThirteen_FourteenOpensExit()
    {
        // Arrange: S, bead, 12 floor tiles (counts 2..13), exit at count 14 (a multiple of 7).
        Level level = Parse("rules: default\n#################\n#SN............E#\n#################\n");

        // Act
        GameState atThirteen = Walk(level, level.CreateInitialState(), Direction.Right, 13);
        GameState state = StepRules.Step(level, atThirteen, Direction.Right);

        // Assert
        Assert.AreEqual(13, atThirteen.Counter);
        Assert.AreEqual(GameStatus.Playing, atThirteen.Status);
        Assert.AreEqual(GameStatus.Won, state.Status);
    }

    [Test]
    public void DiceFlingOntoWood_CountsNormally()
    {
        // Arrange: a normal dice step flings one tile on, onto the wood.
        Level level = Parse("rules: default\n#######\n#SDW.E#\n#######\n");

        // Act
        GameState state = StepRules.Step(level, level.CreateInitialState(), Direction.Right);

        // Assert
        Assert.AreEqual(3, state.X);
        Assert.AreEqual(1, state.Counter);
    }

    [Test]
    public void BeadPickedUpOnThirteen_WardsThatStep()
    {
        // Arrange: S, 12 floor tiles (counts 1..12), the bead on count 13, then the exit on 14.
        Level level = Parse("rules: default\n#################\n#S............NE#\n#################\n");

        // Act
        GameState atThirteen = Walk(level, level.CreateInitialState(), Direction.Right, 13);

        // Assert
        Assert.AreEqual(GameStatus.Playing, atThirteen.Status);
        Assert.AreEqual(0, atThirteen.Beads);
        Assert.AreEqual(GameStatus.Won, StepRules.Step(level, atThirteen, Direction.Right).Status);
    }

    [Test]
    public void Bead_ClosedBridgeFall_KillsAndBeadIsNotUsed()
    {
        // Arrange
        Level level = Parse("rules: default\n#######\n#SNB.E#\n#######\n");

        // Act
        GameState state = Walk(level, level.CreateInitialState(), Direction.Right, 2);

        // Assert
        Assert.AreEqual(GameStatus.Dead, state.Status);
        Assert.AreEqual(1, state.Beads);
    }

    [Test]
    public void TwoBeads_WardTwoSpikes_ThirdKills()
    {
        // Arrange
        Level level = Parse("rules: default\n#########\n#SNN^^^E#\n#########\n");

        // Act
        GameState afterTwo = Walk(level, level.CreateInitialState(), Direction.Right, 4);
        GameState third = StepRules.Step(level, afterTwo, Direction.Right);

        // Assert: one bead per step, so two beads cover exactly two spikes.
        Assert.AreEqual(GameStatus.Playing, afterTwo.Status);
        Assert.AreEqual(0, afterTwo.Beads);
        Assert.AreEqual(GameStatus.Dead, third.Status);
    }

    [Test]
    public void Bead_PickedUpOnce_TileStaysEmpty()
    {
        // Arrange: walk onto the bead, off and back on, then into two spikes.
        Level level = Parse("rules: default\n########\n#SN.^^E#\n########\n");
        GameState state = level.CreateInitialState();

        // Act
        state = Walk(level, state, Direction.Right, 2);
        state = StepRules.Step(level, state, Direction.Left);
        GameState firstSpike = Walk(level, state, Direction.Right, 2);
        GameState secondSpike = StepRules.Step(level, firstSpike, Direction.Right);

        // Assert: re-entering the bead tile gave nothing, so the one bead covers only the first spike.
        Assert.AreEqual(GameStatus.Playing, firstSpike.Status);
        Assert.AreEqual(GameStatus.Dead, secondSpike.Status);
    }
}
