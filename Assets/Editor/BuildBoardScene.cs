using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LuckyStep;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Story 005 (visual slice): builds the wooden-board look into SampleScene without hand-editing YAML.
/// Creates/updates the painted-wood URP/Lit materials, remaps every model's material slots onto them, writes the
/// BoardTheme, StepFeedbackConfig and volume profile assets, then rebuilds the camera, lamp lights, global volume,
/// presentation objects and the room around the table. Keeps the "Game" object and points it at Level04.
/// Idempotent: objects it owns (by root name) are deleted and rebuilt, assets are updated in place.
/// Batch: <c>Unity.exe -batchmode -projectPath . -executeMethod BuildBoardScene.BuildBatch -logFile -</c>
/// </summary>
public static class BuildBoardScene
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string LevelPath = "Assets/Levels/Level08.txt";
    private const string MaterialsFolder = "Assets/Art/Materials";
    private const string ConfigFolder = "Assets/Art/Config";
    private const string ModelsFolder = "Assets/Art/Models";
    private const string KenneyFolder = "Assets/Art/Kenney";
    private const string ProfilePath = "Assets/Art/Config/BoardVolumeProfile.asset";
    private const string ThemePath = "Assets/Art/Config/BoardTheme.asset";
    private const string FeedbackPath = "Assets/Art/Config/StepFeedbackConfig.asset";
    private const string CatTexturePath = "Assets/Art/Kenney/CubePets/Textures/colormap.png";
    private const string CharacterTexturePath = "Assets/Art/Kenney/MiniCharacters/Textures/colormap.png";
    private const string CharacterFolderTag = "/MiniCharacters/";
    private const string GeneratedFolder = "Assets/Art/Generated";
    private const string TrackAtlasPath = "Assets/Art/Generated/TrackAtlas.png";

    // Bump when BoardTheme's layout defaults change: the builder then resets the theme's tunables to the code defaults.
    private const int ThemeVersion = 2;

    // Scene layout (world units; tile tops at y = 0, board plate bottom at TableTop).
    private const float TableTop = -0.30f;
    private const float TableHeight = 5.6f;
    private const float FloorY = TableTop - TableHeight;
    private static readonly Vector3 LampBase = new Vector3(-6.2f, TableTop, 3.8f);
    private const float LampHeight = 5f;

    private static readonly string[] OwnedRoots = { "Main Camera", "Directional Light", "Global Volume", "Lights", "Presentation", "Room", "UI" };

    // Story 006 UI: fonts and sprites (Kenney UI Pack + tools/ui/make_ui_sprites.py); UiRoot builds the canvas at runtime.
    private const string UiFontsFolder = "Assets/Art/UI/Fonts";
    private const string UiSpritesFolder = "Assets/Art/UI/Sprites";

    // 9-slice borders (left, bottom, right, top) in source px; the "depth" buttons have a 16 px darker band at the bottom.
    private static readonly Dictionary<string, Vector4> UiSpriteBorders = new Dictionary<string, Vector4>
    {
        { "yellow_button_rectangle_depth_flat", new Vector4(32f, 40f, 32f, 32f) },
        { "cream_button_rectangle_depth_flat", new Vector4(32f, 40f, 32f, 32f) },
        { "yellow_button_square_depth_flat", new Vector4(32f, 40f, 32f, 32f) },
        { "cream_button_square_depth_flat", new Vector4(32f, 40f, 32f, 32f) },
        { "cream_panel", new Vector4(32f, 32f, 32f, 32f) },
    };

    private sealed class Paint
    {
        public string Name;
        public Color Color;
        public float Smoothness;
        public float Metallic;
        public Color Emission = Color.black;
        public bool EmissionKeyword;
        public string BaseMap;
        public bool Unlit;
    }

    // One painted-wood family: every material is URP/Lit with a flat colour.
    private static readonly Paint[] Palette =
    {
        new Paint { Name = "Wood_Light", Color = C(0.90f, 0.68f, 0.45f), Smoothness = 0.35f },
        new Paint { Name = "Wood_Dark", Color = C(0.68f, 0.46f, 0.30f), Smoothness = 0.35f },
        new Paint { Name = "Wood_Natural", Color = C(0.90f, 0.60f, 0.39f), Smoothness = 0.30f },
        new Paint { Name = "Wood_Wall", Color = C(0.45f, 0.30f, 0.20f), Smoothness = 0.30f },
        new Paint { Name = "Wood_Plate", Color = C(0.55f, 0.36f, 0.22f), Smoothness = 0.35f },
        new Paint { Name = "Wood_Table", Color = C(0.40f, 0.24f, 0.14f), Smoothness = 0.45f },
        // Emission keyword on (black) so the lit-emission variant ships in the WebGL build for the glow.
        new Paint { Name = "Paint_Pawn", Color = C(0.80f, 0.18f, 0.15f), Smoothness = 0.50f, EmissionKeyword = true },
        new Paint { Name = "Paint_Ivory", Color = C(0.96f, 0.93f, 0.85f), Smoothness = 0.45f },
        new Paint { Name = "Paint_Black", Color = C(0.06f, 0.06f, 0.06f), Smoothness = 0.30f },
        new Paint { Name = "Paint_Gold", Color = C(0.95f, 0.72f, 0.22f), Smoothness = 0.70f, Metallic = 0.6f },
        new Paint { Name = "Paint_Metal", Color = C(0.62f, 0.66f, 0.68f), Smoothness = 0.60f, Metallic = 0.5f },
        new Paint { Name = "Paint_MetalDark", Color = C(0.31f, 0.39f, 0.39f), Smoothness = 0.50f, Metallic = 0.4f },
        new Paint { Name = "Paint_Green", Color = C(0.18f, 0.62f, 0.42f), Smoothness = 0.30f },
        new Paint { Name = "Paint_GlassCracked", Color = C(0.52f, 0.62f, 0.64f), Smoothness = 0.85f },
        new Paint { Name = "Paint_Red", Color = C(0.80f, 0.33f, 0.30f), Smoothness = 0.20f },
        new Paint { Name = "Paint_RedDark", Color = C(0.55f, 0.27f, 0.26f), Smoothness = 0.20f },
        new Paint { Name = "Paint_White", Color = C(0.92f, 0.92f, 0.88f), Smoothness = 0.30f },
        new Paint { Name = "Paint_Stone", Color = C(0.62f, 0.70f, 0.72f), Smoothness = 0.20f },
        new Paint { Name = "Paint_Hole", Color = C(0.03f, 0.025f, 0.02f), Smoothness = 0f },
        new Paint { Name = "Lamp_Shade", Color = C(1f, 0.88f, 0.60f), Smoothness = 0.3f, Emission = C(1.5f, 1.1f, 0.6f), EmissionKeyword = true },
        new Paint { Name = "Cat_Black", Color = C(0.14f, 0.13f, 0.14f), Smoothness = 0.25f, BaseMap = CatTexturePath },
        new Paint { Name = "Room_Wall", Color = C(0.30f, 0.22f, 0.18f), Smoothness = 0.10f },
        new Paint { Name = "Room_Floor", Color = C(0.32f, 0.20f, 0.13f), Smoothness = 0.25f },
        new Paint { Name = "Wood_Rim", Color = C(0.36f, 0.22f, 0.13f), Smoothness = 0.45f },
        new Paint { Name = "Paint_CardFace", Color = C(0.95f, 0.94f, 0.90f), Smoothness = 0.35f },
        new Paint { Name = "Paint_CardBack", Color = C(0.65f, 0.12f, 0.12f), Smoothness = 0.35f },
        new Paint { Name = "Paint_ChipRed", Color = C(0.75f, 0.12f, 0.10f), Smoothness = 0.45f },
        new Paint { Name = "Paint_ChipWhite", Color = C(0.92f, 0.90f, 0.85f), Smoothness = 0.45f },
        new Paint { Name = "Paint_ChipBlue", Color = C(0.15f, 0.28f, 0.62f), Smoothness = 0.45f },
        new Paint { Name = "Paint_BoxBody", Color = C(0.20f, 0.32f, 0.55f), Smoothness = 0.30f },
        new Paint { Name = "Paint_BoxLid", Color = C(0.90f, 0.72f, 0.25f), Smoothness = 0.30f },
        // Mirror glass: glossy, slightly metallic, pale blue; a faint emission keeps it bright without a reflection probe.
        new Paint { Name = "Paint_MirrorGlass", Color = C(0.72f, 0.84f, 0.95f), Smoothness = 0.95f, Metallic = 0.6f, Emission = C(0.10f, 0.14f, 0.19f), EmissionKeyword = true },
        new Paint { Name = "Paint_MirrorCrack", Color = C(0.08f, 0.08f, 0.10f), Smoothness = 0.2f },
        new Paint { Name = "Portal_Stone", Color = C(0.30f, 0.25f, 0.45f), Smoothness = 0.4f },
        new Paint { Name = "Portal_SwirlA", Color = C(0.25f, 0.15f, 0.45f), Smoothness = 0.5f, Emission = C(1.4f, 0.8f, 2.6f), EmissionKeyword = true },
        new Paint { Name = "Portal_SwirlB", Color = C(0.10f, 0.35f, 0.45f), Smoothness = 0.5f, Emission = C(0.5f, 1.8f, 2.4f), EmissionKeyword = true },
        new Paint { Name = "Portal_Spark", Color = C(2.2f, 1.9f, 3.0f), Unlit = true },
        // Cat route prints: faint dark brown; the next tile's prints are warm and slightly emissive.
        new Paint { Name = "Paw_Print", Color = C(0.30f, 0.19f, 0.12f), Smoothness = 0.1f },
        new Paint { Name = "Paw_PrintNext", Color = C(0.95f, 0.72f, 0.30f), Smoothness = 0.2f, Emission = C(0.45f, 0.28f, 0.06f), EmissionKeyword = true },
        new Paint { Name = "Cat_Puff", Color = C(0.16f, 0.14f, 0.17f), Unlit = true },
        // Box board-game palette: cream spaces alternating with bright red / blue / green (gold stays for lucky).
        new Paint { Name = "Board_Cream", Color = C(0.96f, 0.91f, 0.78f), Smoothness = 0.4f },
        new Paint { Name = "Board_Red", Color = C(0.88f, 0.26f, 0.22f), Smoothness = 0.4f },
        new Paint { Name = "Board_Blue", Color = C(0.24f, 0.48f, 0.86f), Smoothness = 0.4f },
        new Paint { Name = "Board_Green", Color = C(0.30f, 0.70f, 0.36f), Smoothness = 0.4f },
        // Score track: number atlas tinted per space type; the marker token in the player's colour.
        new Paint { Name = "Track_Space", Color = C(0.97f, 0.93f, 0.82f), Smoothness = 0.35f, BaseMap = TrackAtlasPath },
        new Paint { Name = "Track_Lucky", Color = C(1f, 0.78f, 0.25f), Smoothness = 0.6f, Metallic = 0.3f, Emission = C(0.15f, 0.10f, 0.02f), EmissionKeyword = true, BaseMap = TrackAtlasPath },
        new Paint { Name = "Track_Deadly", Color = Color.white, Smoothness = 0.3f, BaseMap = TrackAtlasPath },
        new Paint { Name = "Token_Player", Color = C(0.86f, 0.18f, 0.16f), Smoothness = 0.6f },
        // Emission keyword on (black) so the glow pulse works on the player figure in the WebGL build.
        new Paint { Name = "Character_Paint", Color = Color.white, Smoothness = 0.3f, BaseMap = CharacterTexturePath, EmissionKeyword = true },
    };

    // Model material slot name (lower case) -> palette material. Own slots come from tools/blender/make_models.py.
    private static readonly Dictionary<string, string> SlotMap = new Dictionary<string, string>
    {
        { "wood", "Wood_Natural" }, { "wooddark", "Wood_Dark" }, { "woodbark", "Wood_Dark" },
        { "plant", "Paint_Green" }, { "metal", "Paint_Metal" }, { "metaldark", "Paint_MetalDark" },
        { "glass", "Paint_GlassCracked" }, { "lamp", "Lamp_Shade" }, { "carpet", "Paint_Red" },
        { "carpetdarker", "Paint_RedDark" }, { "carpetwhite", "Paint_White" }, { "stone", "Paint_Stone" },
        { "colormap", "Cat_Black" }, { "_defaultmat", "Paint_White" },
        { "pawnpaint", "Paint_Pawn" }, { "ivory", "Paint_Ivory" }, { "pipblack", "Paint_Black" },
        { "gold", "Paint_Gold" }, { "spikebase", "Wood_Dark" }, { "tile", "Board_Cream" },
        { "wallwood", "Wood_Wall" }, { "hole", "Paint_Hole" }, { "rim", "Wood_Rim" },
        { "cardface", "Paint_CardFace" }, { "cardback", "Paint_CardBack" }, { "chipred", "Paint_ChipRed" },
        { "chipwhite", "Paint_ChipWhite" }, { "chipblue", "Paint_ChipBlue" }, { "boxbody", "Paint_BoxBody" },
        { "boxlid", "Paint_BoxLid" }, { "mirrorframe", "Wood_Wall" }, { "mirrorglass", "Paint_MirrorGlass" },
        { "mirrorcrack", "Paint_MirrorCrack" }, { "portalframe", "Wood_Dark" }, { "portalstone", "Portal_Stone" },
        { "portalswirla", "Portal_SwirlA" }, { "portalswirlb", "Portal_SwirlB" },
        { "pawprint", "Paw_Print" },
    };

    /// <summary>Menu entry: builds the board scene in the open editor.</summary>
    [MenuItem("Lucky Step/Build Board Scene")]
    public static void BuildMenu()
    {
        Build();
    }

    /// <summary>Batch entry point: builds the board scene, then exits with 0 on success or 1 on any exception.</summary>
    public static void BuildBatch()
    {
        try
        {
            Build();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    private static void Build()
    {
        EnsureFolder(MaterialsFolder);
        EnsureFolder(ConfigFolder);
        EnsureFolder(GeneratedFolder);
        WriteTrackAtlas();
        Dictionary<string, Material> mats = CreateMaterials();
        RemapModelMaterials(mats);
        BoardTheme theme = CreateTheme(mats);
        StepFeedbackConfig feedback = LoadOrCreate<StepFeedbackConfig>(FeedbackPath);
        VolumeProfile profile = CreateVolumeProfile(feedback);
        AssetDatabase.SaveAssets();
        BuildScene(theme, feedback, profile, mats);
        Debug.Log("BuildBoardScene: done -> " + ScenePath);
    }

    /// <summary>Writes the score-track number atlas (TrackAtlas) as an uncompressed PNG; rewritten only if it changed.</summary>
    private static void WriteTrackAtlas()
    {
        Texture2D atlas = TrackAtlas.Generate();
        byte[] png = atlas.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(atlas);
        bool exists = File.Exists(TrackAtlasPath);
        if (!exists || !File.ReadAllBytes(TrackAtlasPath).SequenceEqual(png))
        {
            File.WriteAllBytes(TrackAtlasPath, png);
            AssetDatabase.ImportAsset(TrackAtlasPath, ImportAssetOptions.ForceUpdate);
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath(TrackAtlasPath);
        if (importer.textureCompression == TextureImporterCompression.Uncompressed && importer.wrapMode == TextureWrapMode.Clamp
            && importer.mipmapEnabled && importer.maxTextureSize == TrackAtlas.Size) return;
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 4;
        importer.maxTextureSize = TrackAtlas.Size;
        importer.textureCompression = TextureImporterCompression.Uncompressed; // keeps the digits crisp
        importer.SaveAndReimport();
    }

    private static Dictionary<string, Material> CreateMaterials()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (lit == null || unlit == null) throw new InvalidOperationException("URP Lit/Unlit shader not found.");
        var result = new Dictionary<string, Material>();
        foreach (Paint p in Palette)
        {
            string path = MaterialsFolder + "/" + p.Name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = p.Unlit ? unlit : lit;
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            m.SetColor("_BaseColor", p.Color);
            m.SetFloat("_Smoothness", p.Smoothness);
            m.SetFloat("_Metallic", p.Metallic);
            Texture2D baseMap = p.BaseMap != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(p.BaseMap) : null;
            m.SetTexture("_BaseMap", baseMap);
            if (p.EmissionKeyword)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", p.Emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            result[p.Name] = m;
        }
        return result;
    }

    private static void RemapModelMaterials(Dictionary<string, Material> mats)
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ModelsFolder, KenneyFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) continue;

            var names = new HashSet<string>(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Select(m => m.name));
            foreach (var pair in importer.GetExternalObjectMap())
            {
                if (pair.Key.type == typeof(Material)) names.Add(pair.Key.name);
            }

            bool changed = false;
            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                changed = true;
            }
            // Only the player figure keeps its animations, as Legacy clips (idle/walk played by BoardView).
            bool isCharacter = path.Contains(CharacterFolderTag);
            ModelImporterAnimationType animationType = isCharacter ? ModelImporterAnimationType.Legacy : ModelImporterAnimationType.None;
            if (importer.importAnimation != isCharacter || importer.importCameras || importer.importLights || importer.animationType != animationType)
            {
                importer.importAnimation = isCharacter;
                importer.importCameras = false;
                importer.importLights = false;
                importer.animationType = animationType;
                changed = true;
            }
            foreach (string name in names)
            {
                string key = name.ToLowerInvariant();
                string target;
                if (isCharacter && key == "colormap")
                {
                    target = "Character_Paint";
                }
                else if (!SlotMap.TryGetValue(key, out target))
                {
                    Debug.LogWarning("BuildBoardScene: no palette entry for slot '" + name + "' in " + path + ", using Wood_Natural.");
                    target = "Wood_Natural";
                }
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), name);
                importer.GetExternalObjectMap().TryGetValue(id, out UnityEngine.Object current);
                if (current == mats[target]) continue;
                importer.RemoveRemap(id);
                importer.AddRemap(id, mats[target]);
                changed = true;
            }
            if (changed) importer.SaveAndReimport();
        }
    }

    private static BoardTheme CreateTheme(Dictionary<string, Material> mats)
    {
        BoardTheme theme = LoadOrCreate<BoardTheme>(ThemePath);
        if (theme.builderVersion < ThemeVersion)
        {
            // Layout changed since this asset was written: take the new code defaults for every tunable.
            BoardTheme defaults = ScriptableObject.CreateInstance<BoardTheme>();
            EditorUtility.CopySerialized(defaults, theme);
            UnityEngine.Object.DestroyImmediate(defaults);
            theme.name = Path.GetFileNameWithoutExtension(ThemePath);
            theme.builderVersion = ThemeVersion;
        }
        theme.floorTile = Model(ModelsFolder + "/tile_floor.fbx");
        theme.gapTile = Model(ModelsFolder + "/tile_gap.fbx");
        theme.rimBar = Model(ModelsFolder + "/rim_bar.fbx");
        theme.wallPieces = new[]
        {
            Model(ModelsFolder + "/wall_cards.fbx"), Model(ModelsFolder + "/wall_chips.fbx"), Model(ModelsFolder + "/wall_box.fbx"),
        };
        theme.spike = Model(ModelsFolder + "/spike.fbx");
        theme.die = Model(ModelsFolder + "/die.fbx");
        theme.coin = Model(ModelsFolder + "/coin.fbx");
        theme.mirror = Model(ModelsFolder + "/mirror.fbx");
        theme.portalFrame = Model(ModelsFolder + "/portal_frame.fbx");
        theme.portalDisc = Model(ModelsFolder + "/portal_disc.fbx");
        theme.character = Model(KenneyFolder + "/MiniCharacters/character-female-b.fbx");
        theme.flowerpot = Model(KenneyFolder + "/FurnitureKit/pottedPlant.fbx");
        theme.bridge = Model(KenneyFolder + "/NatureKit/bridge_woodNarrow.fbx");
        theme.cat = Model(KenneyFolder + "/CubePets/animal-cat.fbx");
        theme.floorLight = mats["Board_Cream"];
        theme.floorColors = new[] { mats["Board_Red"], mats["Board_Blue"], mats["Board_Green"] };
        theme.wallTile = mats["Wood_Dark"];
        theme.trackSpace = mats["Track_Space"];
        theme.trackLucky = mats["Track_Lucky"];
        theme.trackDeadly = mats["Track_Deadly"];
        theme.trackBlock = mats["Wood_Rim"];
        theme.trackToken = Model(ModelsFolder + "/coin.fbx");
        theme.trackTokenMaterial = mats["Token_Player"];
        theme.boardPlate = mats["Wood_Plate"];
        theme.portalSpark = mats["Portal_Spark"];
        theme.pawPrint = Model(ModelsFolder + "/paw_print.fbx");
        theme.pawPrintMaterial = mats["Paw_Print"];
        theme.pawPrintNextMaterial = mats["Paw_PrintNext"];
        theme.catPuffMaterial = mats["Cat_Puff"];
        EditorUtility.SetDirty(theme);
        return theme;
    }

    private static VolumeProfile CreateVolumeProfile(StepFeedbackConfig feedback)
    {
        VolumeProfile profile = LoadOrCreate<VolumeProfile>(ProfilePath);

        Tonemapping tone = GetOrAdd<Tonemapping>(profile);
        tone.mode.Override(TonemappingMode.Neutral);

        Bloom bloom = GetOrAdd<Bloom>(profile);
        bloom.threshold.Override(1f);
        bloom.intensity.Override(0.6f);
        bloom.scatter.Override(0.6f);
        bloom.highQualityFiltering.Override(false);

        Vignette vignette = GetOrAdd<Vignette>(profile);
        vignette.intensity.Override(feedback.vignetteBase);
        vignette.smoothness.Override(0.45f);
        vignette.color.Override(new Color(0.06f, 0.02f, 0.01f));

        // Distances are placeholders; BoardCameraRig sets them from the framed board at runtime.
        DepthOfField dof = GetOrAdd<DepthOfField>(profile);
        dof.mode.Override(DepthOfFieldMode.Gaussian);
        dof.gaussianStart.Override(20f);
        dof.gaussianEnd.Override(26f);
        dof.gaussianMaxRadius.Override(1.2f);
        dof.highQualitySampling.Override(false);

        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void BuildScene(BoardTheme theme, StepFeedbackConfig feedback, VolumeProfile profile, Dictionary<string, Material> mats)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (Array.IndexOf(OwnedRoots, root.name) >= 0) UnityEngine.Object.DestroyImmediate(root);
        }

        // Game object: keep it, point it at the showcase level.
        GameObject game = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Game") ?? new GameObject("Game");
        PlayerInputController controller = game.GetComponent<PlayerInputController>();
        if (controller == null) controller = game.AddComponent<PlayerInputController>();
        TextAsset level = AssetDatabase.LoadAssetAtPath<TextAsset>(LevelPath);
        if (level == null) throw new FileNotFoundException("Level not found", LevelPath);
        SetRef(controller, "_levelFile", level);

        // Global volume.
        var volumeGo = new GameObject("Global Volume");
        Volume volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;
        volume.sharedProfile = profile;

        // Lights: warm key from the lamp's side (the only shadow caster: WebGL's Mobile URP asset has no additional
        // light shadows), plus a shadowless point light at the lamp shade for the warm pool.
        var lights = new GameObject("Lights").transform;
        Vector3 bulb = LampBase + Vector3.up * (LampHeight * 0.78f);
        var key = new GameObject("Lamp Key Light").AddComponent<Light>();
        key.transform.SetParent(lights, false);
        key.type = LightType.Directional;
        key.color = new Color(1f, 0.83f, 0.62f);
        key.intensity = 1.25f;
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.8f;
        key.transform.rotation = Quaternion.LookRotation((Vector3.zero - bulb).normalized + Vector3.down * 0.6f);
        var lamp = new GameObject("Lamp Bulb Light").AddComponent<Light>();
        lamp.transform.SetParent(lights, false);
        lamp.transform.position = bulb;
        lamp.type = LightType.Point;
        lamp.color = new Color(1f, 0.72f, 0.42f);
        lamp.intensity = 3f;
        lamp.range = 16f;
        lamp.shadows = LightShadows.None;
        RenderSettings.sun = key;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.26f, 0.21f, 0.18f);
        RenderSettings.skybox = null;
        RenderSettings.fog = false;

        // Presentation: board view + feedback (+ its flash light).
        var presentation = new GameObject("Presentation");
        BoardView board = presentation.AddComponent<BoardView>();
        SetRef(board, "_controller", controller);
        SetRef(board, "_theme", theme);
        var flash = new GameObject("Lucky Flash Light").AddComponent<Light>();
        flash.transform.SetParent(presentation.transform, false);
        flash.type = LightType.Point;
        flash.color = feedback.glowColor;
        flash.range = feedback.flashRange;
        flash.intensity = 0f;
        flash.shadows = LightShadows.None;
        flash.enabled = false;
        StepFeedback stepFeedback = presentation.AddComponent<StepFeedback>();
        SetRef(stepFeedback, "_board", board);
        SetRef(stepFeedback, "_volume", volume);
        SetRef(stepFeedback, "_flashLight", flash);
        SetRef(stepFeedback, "_config", feedback);

        // Camera (BoardCameraRig frames it at runtime; this is the edit-time placeholder pose).
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.06f, 0.05f);
        cam.fieldOfView = 30f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 80f;
        camGo.transform.SetPositionAndRotation(new Vector3(0f, 14f, -12f), Quaternion.Euler(50f, 0f, 0f));
        UniversalAdditionalCameraData camData = cam.GetUniversalAdditionalCameraData();
        camData.renderPostProcessing = true;
        camData.requiresDepthOption = CameraOverrideOption.On;
        camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        BoardCameraRig rig = camGo.AddComponent<BoardCameraRig>();
        SetRef(rig, "_board", board);
        SetRef(rig, "_volume", volume);

        BuildRoom(mats);
        BuildUi(controller);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + ScenePath);
    }

    /// <summary>Story 006: writes the level catalog, sets up the UI sprite importers and adds the "UI" root with its references.</summary>
    private static void BuildUi(PlayerInputController controller)
    {
        LevelCatalog catalog = LevelCatalogBuilder.CreateOrUpdateDefault();
        ConfigureUiSprites();
        UiRoot ui = new GameObject("UI").AddComponent<UiRoot>();
        SetRef(ui, "_controller", controller);
        SetRef(ui, "_catalog", catalog);
        SetRef(ui, "_titleFont", Load<Font>(UiFontsFolder + "/KenneyFuture-TR.ttf"));
        SetRef(ui, "_bodyFont", Load<Font>(UiFontsFolder + "/KenneyFutureNarrow-TR.ttf"));
        SetRef(ui, "_panel", Load<Sprite>(UiSpritesFolder + "/cream_panel.png"));
        SetRef(ui, "_buttonPrimary", Load<Sprite>(UiSpritesFolder + "/yellow_button_rectangle_depth_flat.png"));
        SetRef(ui, "_buttonSecondary", Load<Sprite>(UiSpritesFolder + "/cream_button_rectangle_depth_flat.png"));
        SetRef(ui, "_levelButton", Load<Sprite>(UiSpritesFolder + "/yellow_button_square_depth_flat.png"));
        SetRef(ui, "_levelButtonLocked", Load<Sprite>(UiSpritesFolder + "/cream_button_square_depth_flat.png"));
        SetRef(ui, "_lockIcon", Load<Sprite>(UiSpritesFolder + "/icon_lock.png"));
        SetRef(ui, "_tickIcon", Load<Sprite>(UiSpritesFolder + "/green_icon_checkmark.png"));
        SetRef(ui, "_pawnIcon", Load<Sprite>(UiSpritesFolder + "/icon_pawn.png"));
    }

    /// <summary>Every PNG in the UI sprite folder: single Sprite, bilinear, no mips, uncompressed, 9-slice border from UiSpriteBorders.</summary>
    private static void ConfigureUiSprites()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { UiSpritesFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            UiSpriteBorders.TryGetValue(Path.GetFileNameWithoutExtension(path), out Vector4 border);
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single
                && importer.spriteBorder == border && !importer.mipmapEnabled && importer.filterMode == FilterMode.Bilinear
                && importer.textureCompression == TextureImporterCompression.Uncompressed) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new FileNotFoundException(typeof(T).Name + " not found (imported?)", path);
        return asset;
    }

    private static void BuildRoom(Dictionary<string, Material> mats)
    {
        Transform room = new GameObject("Room").transform;
        string kit = KenneyFolder + "/FurnitureKit/";

        Transform table = ModelFit.Spawn(Model(kit + "table.fbx"), room, "Table", new Vector3(15f, TableHeight, 11f), false);
        table.position = new Vector3(0f, FloorY, 0f);
        foreach (Renderer r in table.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mats["Wood_Table"];

        Spawn(room, kit + "rugRectangle.fbx", "Rug", new Vector3(28f, 0f, 18f), false, new Vector3(0f, FloorY + 0.01f, 0f), 0f);
        Spawn(room, kit + "lampSquareTable.fbx", "Lamp", new Vector3(0f, LampHeight, 0f), true, LampBase, 0f);
        Spawn(room, kit + "books.fbx", "Books", new Vector3(0f, 0.9f, 0f), true, new Vector3(6.0f, TableTop, 3.9f), 20f);
        Spawn(room, kit + "plantSmall1.fbx", "Plant", new Vector3(0f, 1.6f, 0f), true, new Vector3(6.4f, TableTop, 0.6f), 0f);
        Spawn(room, kit + "bookcaseOpen.fbx", "Bookcase", new Vector3(0f, 14f, 0f), true, new Vector3(10f, FloorY, 14.5f), 180f);
        Spawn(room, kit + "loungeSofa.fbx", "Sofa", new Vector3(0f, 7f, 0f), true, new Vector3(-9f, FloorY, 13.5f), 180f);
        Spawn(room, kit + "wallWindow.fbx", "Window", new Vector3(0f, 20f, 0f), true, new Vector3(0f, FloorY, 15.9f), 180f);

        Quad(room, "Room Floor", new Vector3(0f, FloorY, 6f), Quaternion.Euler(90f, 0f, 0f), new Vector3(80f, 60f, 1f), mats["Room_Floor"]);
        Quad(room, "Back Wall", new Vector3(0f, FloorY + 20f, 16f), Quaternion.identity, new Vector3(80f, 40f, 1f), mats["Room_Wall"]);
    }

    private static void Spawn(Transform parent, string path, string name, Vector3 size, bool uniform, Vector3 position, float yaw)
    {
        Transform t = ModelFit.Spawn(Model(path), parent, name, size, uniform);
        t.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
    }

    private static void Quad(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.SetParent(parent, false);
        quad.transform.SetPositionAndRotation(position, rotation);
        quad.transform.localScale = scale;
        var r = quad.GetComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = ShadowCastingMode.Off;
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet(out T component)) return component;
        component = profile.Add<T>(true);
        component.name = typeof(T).Name;
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    private static GameObject Model(string path)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go == null) throw new FileNotFoundException("Model not found (imported?)", path);
        return go;
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void SetRef(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) throw new MissingFieldException(target.GetType().Name, field);
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        if (p.objectReferenceValue == value) return;

        // Unity 6.5 rejected SerializedProperty assignment of these ScriptableObject assets (observed 2026-11-06:
        // objectReferenceValue stayed null); set the field directly and mark the object dirty instead.
        System.Reflection.FieldInfo f = target.GetType().GetField(field,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (f == null) throw new MissingFieldException(target.GetType().Name, field);
        f.SetValue(target, value);
        EditorUtility.SetDirty(target);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static Color C(float r, float g, float b) => new Color(r, g, b);
}
