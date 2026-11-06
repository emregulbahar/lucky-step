using System;
using System.IO;
using LuckyStep;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor entry points for the BFS level solver (Story 004). Solves the .txt levels in Assets/Levels, prints one
/// report line per level to the Console and writes par to Assets/Levels/par.json (see <see cref="ParTable"/>).
/// Search caps: <see cref="LevelSolver.DefaultMaxDepth"/> moves and <see cref="LevelSolver.DefaultMaxStates"/> states.
/// Report outcomes: "solved", "proven-unsolvable" (every reachable state searched, no win) and
/// "no-solution-within-cap" (a cap stopped the search; the level may still be winnable).
/// Flags against the level's optional "par:" line: "shortcut" (solver par lower), "longer" (higher),
/// "no-solution" (intended par given but nothing found), "ok" (equal), "-" (no intended par).
/// </summary>
public static class LevelSolverMenu
{
    private const string LevelsFolder = "Assets/Levels";
    private const string ParFilePath = "Assets/Levels/par.json";

    /// <summary>Solves every .txt level in Assets/Levels and rewrites par.json from scratch.</summary>
    [MenuItem("Lucky Step/Solve All Levels")]
    public static void SolveAll()
    {
        SolveAllLevels();
    }

    /// <summary>
    /// Batch-mode entry point: same as Solve All Levels, then exits the editor with 0, or 1 if any level failed to
    /// parse or an exception occurred. Unsolvable levels and shortcut flags are warnings, not errors.
    /// <c>Unity.exe -batchmode -nographics -projectPath . -executeMethod LevelSolverMenu.SolveAllBatch -logFile -</c>
    /// </summary>
    public static void SolveAllBatch()
    {
        int errors;
        try
        {
            errors = SolveAllLevels();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            errors = 1;
        }
        EditorApplication.Exit(errors == 0 ? 0 : 1);
    }

    /// <summary>Solves the level TextAsset selected in the Project window and updates its entry in par.json.</summary>
    [MenuItem("Lucky Step/Solve Selected Level")]
    public static void SolveSelected()
    {
        if (!SolveSelectedValidate()) return;
        var asset = (TextAsset)Selection.activeObject;
        ParTable table = LoadParTable();
        if (SolveOne(asset.name, asset.text, table)) WriteParTable(table);
    }

    [MenuItem("Lucky Step/Solve Selected Level", true)]
    private static bool SolveSelectedValidate()
    {
        return Selection.activeObject is TextAsset asset
            && AssetDatabase.GetAssetPath(asset).StartsWith(LevelsFolder + "/", StringComparison.Ordinal)
            && AssetDatabase.GetAssetPath(asset).EndsWith(".txt", StringComparison.OrdinalIgnoreCase);
    }

    /// <returns>The number of levels that failed to parse.</returns>
    private static int SolveAllLevels()
    {
        string[] files = Directory.GetFiles(LevelsFolder, "*.txt");
        Array.Sort(files, StringComparer.Ordinal);
        var table = new ParTable();
        int errors = 0;
        foreach (string file in files)
        {
            if (!SolveOne(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file), table)) errors++;
        }
        WriteParTable(table);
        Debug.Log($"LevelSolver: {files.Length} level(s), {errors} error(s), par written to {ParFilePath}");
        return errors;
    }

    /// <summary>Solves one level, logs its report line and puts its entry into the table. False on a parse error.</summary>
    private static bool SolveOne(string name, string text, ParTable table)
    {
        if (!LevelLoader.TryParse(text, out Level level, out string error))
        {
            Debug.LogError($"LevelSolver: {name} | parse error: {error}");
            return false;
        }

        var watch = System.Diagnostics.Stopwatch.StartNew();
        SolveResult result = LevelSolver.Solve(level);
        watch.Stop();

        string outcome = result.Solvable ? "solved"
            : result.HitCap ? "no-solution-within-cap"
            : "proven-unsolvable";
        string flag = level.IntendedPar == null ? "-"
            : !result.Solvable ? "no-solution"
            : result.Par < level.IntendedPar ? "shortcut"
            : result.Par > level.IntendedPar ? "longer"
            : "ok";
        string line = $"LevelSolver: {name} | {outcome} | par {(result.Solvable ? result.Par.ToString() : "-")}"
            + $" | intended {(level.IntendedPar?.ToString() ?? "-")} | {flag}"
            + $" | moves {(result.Solvable ? result.MoveString : "-")} | states {result.StatesExplored}"
            + $" | {watch.ElapsedMilliseconds} ms";
        if (result.Solvable && (flag == "ok" || flag == "-")) Debug.Log(line);
        else Debug.LogWarning(line);

        table.levels.RemoveAll(entry => entry.level == name);
        table.levels.Add(new ParEntry { level = name, par = result.Par, moves = result.MoveString });
        table.levels.Sort((a, b) => string.CompareOrdinal(a.level, b.level));
        return true;
    }

    private static ParTable LoadParTable()
    {
        if (!File.Exists(ParFilePath)) return new ParTable();
        ParTable table = JsonUtility.FromJson<ParTable>(File.ReadAllText(ParFilePath)) ?? new ParTable();
        if (table.levels == null) table.levels = new System.Collections.Generic.List<ParEntry>();
        return table;
    }

    private static void WriteParTable(ParTable table)
    {
        File.WriteAllText(ParFilePath, JsonUtility.ToJson(table, true) + "\n");
        AssetDatabase.ImportAsset(ParFilePath);
    }
}
