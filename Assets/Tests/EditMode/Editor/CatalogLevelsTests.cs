using System.IO;
using LuckyStep;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Release check over the shipped level files: every level parses, the solver finds a solution whose length equals
/// the par in Assets/Levels/par.json, and replaying the stored par moves through StepRules wins in exactly par moves.
/// Prints par and states explored per level (a rough difficulty signal) to the test output.
/// </summary>
public sealed class CatalogLevelsTests
{
    private const string LevelsFolder = "Assets/Levels";

    private static ParTable LoadPar()
    {
        string json = File.ReadAllText(Path.Combine(LevelsFolder, "par.json"));
        return JsonUtility.FromJson<ParTable>(json);
    }

    [Test]
    public void EveryLevel_SolvableAtPar_StoredSolutionWins()
    {
        ParTable table = LoadPar();
        Assert.IsNotEmpty(table.levels);
        foreach (ParEntry entry in table.levels)
        {
            string text = File.ReadAllText(Path.Combine(LevelsFolder, entry.level + ".txt"));
            Assert.IsTrue(LevelLoader.TryParse(text, out Level level, out string error), entry.level + ": " + error);

            SolveResult solve = LevelSolver.Solve(level);
            Assert.IsTrue(solve.Solvable, entry.level + " is not solvable");
            Assert.AreEqual(entry.par, solve.Par, entry.level + " par changed");

            GameState state = level.CreateInitialState();
            foreach (char c in entry.moves)
            {
                Assert.IsFalse(state.IsWon || state.IsDead, entry.level + " ended before its stored moves ran out");
                state = StepRules.Step(level, state, c == 'U' ? Direction.Up : c == 'D' ? Direction.Down
                    : c == 'L' ? Direction.Left : Direction.Right);
            }
            Assert.IsTrue(state.IsWon, entry.level + " stored solution does not win");
            TestContext.WriteLine(entry.level + " par " + entry.par + " states " + solve.StatesExplored);
        }
    }
}
