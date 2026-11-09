using LuckyStep;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates or updates Assets/Levels/LevelCatalog.asset (Story 006): four worlds of four levels (Level01..Level16 in
/// play order; World 1 hides par), par.json assigned. Safe to run again; it overwrites the worlds list.
/// </summary>
/// <example>
/// Menu "Lucky Step/Create Level Catalog", or from another editor script:
/// <code>LevelCatalog catalog = LevelCatalogBuilder.CreateOrUpdateDefault();</code>
/// </example>
public static class LevelCatalogBuilder
{
    /// <summary>Asset path of the catalog.</summary>
    public const string CatalogPath = "Assets/Levels/LevelCatalog.asset";

    private const string LevelFolder = "Assets/Levels/";

    /// <summary>Creates the catalog asset if missing, fills the default world layout, saves and returns it.</summary>
    [MenuItem("Lucky Step/Create Level Catalog")]
    public static LevelCatalog CreateOrUpdateDefault()
    {
        LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        catalog.worlds = new[]
        {
            new LevelCatalog.World
            {
                storyKey = "story.world1",
                ruleSetName = "default",
                levels = new[] { Load("Level01"), Load("Level02"), Load("Level03"), Load("Level04") },
                showPar = false, // no cat: every solution is exactly 7 moves
            },
            new LevelCatalog.World
            {
                storyKey = "story.world2",
                ruleSetName = "default",
                levels = new[] { Load("Level05"), Load("Level06"), Load("Level07"), Load("Level08") },
            },
            new LevelCatalog.World
            {
                storyKey = "story.world3",
                ruleSetName = "default",
                levels = new[] { Load("Level09"), Load("Level10"), Load("Level11"), Load("Level12") },
            },
            new LevelCatalog.World
            {
                storyKey = "story.world4",
                ruleSetName = "world4",
                levels = new[] { Load("Level13"), Load("Level14"), Load("Level15"), Load("Level16") },
            },
        };
        catalog.parTable = AssetDatabase.LoadAssetAtPath<TextAsset>(LevelFolder + "par.json");
        if (catalog.parTable == null) Debug.LogError("LevelCatalogBuilder: " + LevelFolder + "par.json not found.");

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        return catalog;
    }

    private static TextAsset Load(string levelName)
    {
        TextAsset level = AssetDatabase.LoadAssetAtPath<TextAsset>(LevelFolder + levelName + ".txt");
        if (level == null) Debug.LogError("LevelCatalogBuilder: " + LevelFolder + levelName + ".txt not found.");
        return level;
    }
}
