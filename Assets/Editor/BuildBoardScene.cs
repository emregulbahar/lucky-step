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
/// Story 005 (visual slice): builds the "Printed Board" look (design/game-brief.md, Art and audio direction) into
/// SampleScene without hand-editing YAML. Creates/updates the LuckyStep/Toon materials (two-band cel shading, ink
/// outlines on pieces only, printed paper spaces), remaps every model's material slots onto them, writes the
/// BoardTheme, StepFeedbackConfig and volume profile assets, then rebuilds the camera (dark floor colour beyond the felt card table), the
/// single soft-shadow key light, global volume (no tonemapping so palette hexes print as authored, no DoF, no
/// resting vignette, gold-only bloom) and presentation
/// objects. No room, table or candle. Keeps the "Game" object and points it at Level08.
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
    private const string KayKitFolder = "Assets/Art/KayKit"; // world table themes: KayKit Dungeon candles (CC0)
    private const string ProfilePath = "Assets/Art/Config/BoardVolumeProfile.asset";
    private const string ThemePath = "Assets/Art/Config/BoardTheme.asset";
    private const string FeedbackPath = "Assets/Art/Config/StepFeedbackConfig.asset";
    private const string AudioConfigPath = "Assets/Audio/AudioConfig.asset";
    private const string SfxFolder = "Assets/Audio/Sfx";
    private const string MusicFolder = "Assets/Audio/Music";
    private const string CatTexturePath = "Assets/Art/Kenney/CubePets/Textures/colormap.png";
    private const string CharacterTexturePath = "Assets/Art/Kenney/MiniCharacters/Textures/colormap.png";
    private const string CharacterFolderTag = "/MiniCharacters/";
    // World table themes: Kenney Food Kit and KayKit models use one texture atlas each (slots "colormap" / "texture").
    private const string FoodFolderTag = "/FoodKit/";
    private const string FoodTexturePath = "Assets/Art/Kenney/FoodKit/Textures/colormap.png";
    private const string KayKitTexturePath = "Assets/Art/KayKit/DungeonPack/dungeon_texture.png";
    private const string GeneratedFolder = "Assets/Art/Generated";
    // Score-track numbers in Fredoka Bold (tools/textures/make_track_atlas.py), layout in TrackAtlas.
    private const string TrackAtlasPath = "Assets/Art/Generated/TrackAtlas.png";
    // Printed space textures (tools/textures/make_print_textures.py): paper with an ink border; hazard adds a red band.
    private const string PrintSpacePath = "Assets/Art/Generated/PrintSpace.png";
    private const string PrintHazardPath = "Assets/Art/Generated/PrintHazard.png";
    private const string TableFeltPath = "Assets/Art/Generated/TableFelt.png";
    private const string BoxLogoPath = "Assets/Art/Generated/BoxLogo.png"; // look pass 2: game-box lid logo
    private const string TableKilimPath = "Assets/Art/Generated/TableKilim.png"; // World 5 table cloth, tiles (Repeat)
    private const string ToonShaderName = "LuckyStep/Toon";

    // Bump when BoardTheme's layout defaults change: the builder then resets the theme's tunables to the code defaults.
    private const int ThemeVersion = 10;

    // The one key light comes from the back-left, above the board (space tops at y = 0).
    private static readonly Vector3 KeyLightFrom = new Vector3(-6.2f, 2.15f, 3.8f);
    // Neutral flat ambient (gamma 0.735 = linear 0.5); the Toon shader adds 0.15 of it, so lit = base * (0.925 + 0.075).
    private static readonly Color Ambient = new Color(0.735f, 0.735f, 0.735f);
    private const float KeyIntensity = 0.925f;

    // Printed Board palette (brief): colour carries meaning only.
    private static readonly Color Ink = C(0.110f, 0.102f, 0.141f);        // #1C1A24 outlines, pips, track borders
    private static readonly Color Paper = C(1f, 0.965f, 0.878f);          // #FFF6E0 space A
    private static readonly Color PaperB = C(0.929f, 0.878f, 0.761f);     // #EDE0C2 space B
    private static readonly Color BoardWood = C(0.353f, 0.227f, 0.141f);  // #5A3A24 board plate and frames
    private static readonly Color RimWood = C(0.431f, 0.290f, 0.180f);    // #6E4A2E board border
    private static readonly Color Felt = C(0.180f, 0.471f, 0.302f);       // #2E784D table felt (tints TableFelt.png)
    private static readonly Color TableWood = C(0.788f, 0.545f, 0.306f);  // #C98B4E table rail
    private static readonly Color CardRed = C(0.639f, 0.149f, 0.180f);    // #A3262E card backs, red chips, game box (deeper than hazard red)
    private static readonly Color ChipGreen = C(0.184f, 0.541f, 0.357f);  // #2F8A5B green chips (no blue: user feedback)
    // Look pass 2: one checker tint per world (pastel, so hazard red, gold and the paw prints stay the loudest colours).
    private static readonly Color[] WorldTint =
    {
        C(0.804f, 0.898f, 0.741f), // #CDE5BD W1 mint
        C(0.965f, 0.863f, 0.557f), // #F6DC8E W2 butter
        C(0.965f, 0.749f, 0.612f), // #F6BF9C W3 peach
        C(0.937f, 0.690f, 0.682f), // #EFB0AE W4 rose (Chinese neighbourhood)
        C(0.706f, 0.867f, 0.827f), // #B4DDD3 W5 turquoise tile (Old Bazaar)
    };
    // World table themes (design/quick-specs/world-table-themes-addition-2026-11-12.md). Starting values for
    // art-director review: table tops stay dark and muted so the printed board and its hazards stay the loudest.
    private static readonly Color FeltNavy = C(0.153f, 0.212f, 0.341f); // #273657 W2 Black Cat Alley
    private static readonly Color Velvet = C(0.353f, 0.192f, 0.420f);   // #5A316B W3 Hall of Mirrors
    private static readonly Color Lacquer = C(0.431f, 0.102f, 0.102f);  // #6E1A1A W4 oxblood lacquer (darker than hazard red)
    private static readonly Color Background = C(0.125f, 0.090f, 0.071f); // #201712 floor beyond the table
    private static readonly Color Red = C(0.886f, 0.231f, 0.231f);        // #E23B3B hazard
    private static readonly Color Gold = C(1f, 0.761f, 0.102f);           // #FFC21A lucky, the only bloom
    private static readonly Color Teal = C(0.094f, 0.651f, 0.627f);       // #18A6A0 player
    // Gold's bloom: with no tonemapping the lit colour clips at 1, so the HDR push goes mostly into red. Lit gold
    // (linear 1, 0.54, 0.01) + this = (3, 0.62, 0.01): it still prints as #FFCF1A and is the only thing over the bloom
    // threshold. More green would bloom yellower but print lemon-yellow.
    private static readonly Color GoldGlow = Hdr(2.0f, 0.08f, 0f);
    private const float Px = 3f; // piece outline width in px at 1080p (3 px reads better than 2 px at board distance)

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
        /// <summary>Added after shading (HDR, authored in gamma like Color); only gold goes above the bloom threshold.</summary>
        public Color Emission = Color.black;
        public string BaseMap;
        /// <summary>URP/Unlit (particles); every other material is LuckyStep/Toon.</summary>
        public bool Unlit;
        /// <summary>Ink outline width in px at 1080p; 0 for spaces, prints and tiny parts.</summary>
        public float Outline;
        /// <summary>Own Blender models carry smoothed outline normals in their vertex colour; Kenney models do not.</summary>
        public bool SmoothNormals;
        public Color OutlineColor = Ink;
    }

    private static readonly Paint[] Palette =
    {
        // Spaces and board: printed, no geometric outline.
        new Paint { Name = "Board_Cream", Color = Paper, BaseMap = PrintSpacePath },
        new Paint { Name = "Board_Linen", Color = PaperB, BaseMap = PrintSpacePath },
        new Paint { Name = "Board_W1", Color = WorldTint[0], BaseMap = PrintSpacePath },
        new Paint { Name = "Board_W2", Color = WorldTint[1], BaseMap = PrintSpacePath },
        new Paint { Name = "Board_W3", Color = WorldTint[2], BaseMap = PrintSpacePath },
        new Paint { Name = "Board_W4", Color = WorldTint[3], BaseMap = PrintSpacePath },
        new Paint { Name = "Board_W5", Color = WorldTint[4], BaseMap = PrintSpacePath },
        new Paint { Name = "Space_Hazard", Color = Color.white, BaseMap = PrintHazardPath },
        new Paint { Name = "Paint_Hole", Color = C(0.07f, 0.08f, 0.15f) },
        new Paint { Name = "Wood_Plate", Color = BoardWood },
        new Paint { Name = "Wood_Rim", Color = RimWood },
        // Card table (TableView): background surfaces, no outline.
        new Paint { Name = "Table_Felt", Color = Felt, BaseMap = TableFeltPath },
        new Paint { Name = "Table_Rail", Color = TableWood },
        // World table themes: one table top per world (Table_Felt above is World 1). Felt-like tops reuse the felt
        // texture's lamp pool; the kilim is a full-colour tiling texture (tint white, tiled by WorldDecor.surfaceTile).
        new Paint { Name = "Table_FeltNavy", Color = FeltNavy, BaseMap = TableFeltPath },
        new Paint { Name = "Table_Velvet", Color = Velvet, BaseMap = TableFeltPath },
        new Paint { Name = "Table_Lacquer", Color = Lacquer, BaseMap = TableFeltPath },
        new Paint { Name = "Table_Kilim", Color = C(0.70f, 0.62f, 0.56f), BaseMap = TableKilimPath }, // tinted down: the cloth must stay quieter than the board
        new Paint { Name = "Paw_Print", Color = C(0.25f, 0.27f, 0.40f) },
        new Paint { Name = "Paw_PrintNext", Color = Gold, Emission = C(0.25f, 0.17f, 0f) },
        // Score track: number atlas (lucky colours are baked into the atlas, so its tint is white).
        new Paint { Name = "Track_Space", Color = Paper, BaseMap = TrackAtlasPath },
        new Paint { Name = "Track_Lucky", Color = Color.white, Emission = C(0.10f, 0.07f, 0f), BaseMap = TrackAtlasPath },
        new Paint { Name = "Track_Deadly", Color = Color.white, BaseMap = TrackAtlasPath },
        new Paint { Name = "Paint_Black", Color = Ink }, // die pips and track borders
        // Own pieces (smoothed outline normals in vertex colour).
        new Paint { Name = "Paint_Ivory", Color = C(0.98f, 0.95f, 0.88f), Outline = Px, SmoothNormals = true },
        new Paint { Name = "Paint_Gold", Color = Gold, Emission = GoldGlow, Outline = Px, SmoothNormals = true },
        new Paint { Name = "Paint_Hazard", Color = Red, Outline = Px, SmoothNormals = true },
        new Paint { Name = "Paint_Metal", Color = C(0.80f, 0.82f, 0.86f), Outline = Px },
        new Paint { Name = "Paint_CardFace", Color = Paper, Outline = Px, SmoothNormals = true },
        new Paint { Name = "Paint_CardBack", Color = CardRed, Outline = Px, SmoothNormals = true },
        new Paint { Name = "Paint_CardTrim", Color = Paper }, // printed border and diamond on the card back (flat, no outline)
        new Paint { Name = "Paint_ChipRed", Color = CardRed, Outline = Px, SmoothNormals = true },
        new Paint { Name = "Paint_ChipWhite", Color = Paper, Outline = Px, SmoothNormals = true },
        new Paint { Name = "Paint_ChipGreen", Color = ChipGreen, Outline = Px, SmoothNormals = true },
        new Paint { Name = "Paint_ChipLine", Color = C(0.62f, 0.55f, 0.47f), Outline = Px, SmoothNormals = true }, // chip edge lines; outlined too, or the stack outline breaks at every line
        new Paint { Name = "Paint_BoxBody", Color = CardRed, Outline = Px, SmoothNormals = true },
        new Paint { Name = "Paint_BoxLid", Color = Paper, Outline = Px, SmoothNormals = true },
        new Paint { Name = "Paint_BoxLogo", Color = Color.white, BaseMap = BoxLogoPath }, // printed lid logo (flat decal)
        // Muted twins for the card/chip stacks ON the board (wall tiles): softer, closer to the printed board, so the
        // blockers stay quiet next to the hazards (user feedback 2026-11-12). Table props keep the vivid originals.
        new Paint { Name = "Board_CardBackMuted", Color = C(0.769f, 0.541f, 0.525f), Outline = Px, SmoothNormals = true }, // #C48A86 dusty rose
        new Paint { Name = "Board_ChipRedMuted", Color = C(0.769f, 0.541f, 0.525f), Outline = Px, SmoothNormals = true },
        new Paint { Name = "Board_ChipGreenMuted", Color = C(0.584f, 0.714f, 0.627f), Outline = Px, SmoothNormals = true }, // #95B6A0 sage
        new Paint { Name = "Wood_Wall", Color = BoardWood, Outline = Px, SmoothNormals = true }, // mirror and portal frames
        new Paint { Name = "Paint_MirrorGlass", Color = C(0.72f, 0.84f, 0.95f), Emission = C(0.08f, 0.10f, 0.14f), Outline = Px, SmoothNormals = true },
        new Paint { Name = "Paint_MirrorCrack", Color = Ink },
        new Paint { Name = "Paint_BeadBlue", Color = C(0.12f, 0.31f, 0.75f), Emission = C(0.02f, 0.05f, 0.14f), Outline = Px, SmoothNormals = true }, // evil-eye bead
        new Paint { Name = "Paint_BeadLight", Color = C(0.49f, 0.78f, 0.95f), Outline = Px, SmoothNormals = true },
        new Paint { Name = "Wood_Plank", Color = C(0.84f, 0.62f, 0.38f), Outline = Px, SmoothNormals = true }, // knock-on-wood planks
        new Paint { Name = "Portal_Stone", Color = C(0.95f, 0.72f, 0.12f), Outline = Px, SmoothNormals = true }, // horseshoe arch
        new Paint { Name = "Portal_SwirlA", Color = Gold, Emission = GoldGlow },
        new Paint { Name = "Portal_SwirlB", Color = C(1f, 0.88f, 0.5f), Emission = Hdr(2.2f, 0.6f, 0.05f) }, // pale-yellow core
        new Paint { Name = "Token_Player", Color = Teal, Outline = Px, SmoothNormals = true }, // track token (coin model)
        new Paint { Name = "Paint_Pawn", Color = Red, Outline = Px, SmoothNormals = true },
        new Paint { Name = "Prop_Mug", Color = C(0.96f, 0.93f, 0.86f), Outline = Px, SmoothNormals = true },
        new Paint { Name = "Prop_Coffee", Color = C(0.36f, 0.22f, 0.13f) },
        // World table props (own Blender models, tools/blender/make_models.py).
        new Paint { Name = "Prop_Milk", Color = C(0.97f, 0.96f, 0.92f) }, // flat milk surface in the saucer
        new Paint { Name = "Prop_Yarn", Color = C(0.851f, 0.510f, 0.231f), Outline = Px, SmoothNormals = true }, // #D9823B
        new Paint { Name = "Prop_YarnLine", Color = C(0.659f, 0.369f, 0.133f), Outline = Px, SmoothNormals = true }, // #A85E22
        new Paint { Name = "Prop_LanternGlass", Color = C(1f, 0.85f, 0.5f), Emission = C(0.22f, 0.15f, 0.02f), Outline = Px, SmoothNormals = true }, // under the bloom threshold
        new Paint { Name = "Prop_Flame", Color = C(1f, 0.78f, 0.35f), Emission = C(0.3f, 0.18f, 0f) }, // candle flame, under the bloom threshold
        new Paint { Name = "Prop_LanternPaper", Color = C(0.769f, 0.200f, 0.165f), Outline = Px, SmoothNormals = true }, // #C4332A paper lantern
        new Paint { Name = "Prop_Copper", Color = C(0.776f, 0.451f, 0.251f), Outline = Px, SmoothNormals = true }, // #C67340 tea tray
        new Paint { Name = "Prop_Tea", Color = C(0.612f, 0.231f, 0.102f), Outline = Px, SmoothNormals = true }, // #9C3B1A tea in a tulip glass
        // Third-party atlases (Kenney Food Kit, KayKit Dungeon): untinted texture, outlined along mesh normals.
        new Paint { Name = "Food_Paint", Color = Color.white, BaseMap = FoodTexturePath, Outline = Px },
        new Paint { Name = "KayKit_Paint", Color = Color.white, BaseMap = KayKitTexturePath, Outline = Px },
        new Paint { Name = "Prop_Rope", Color = C(0.72f, 0.58f, 0.40f), Outline = 2f }, // flowerpot rope (Unity cylinders)
        // Kenney pieces and Unity primitives: outlined along mesh normals.
        new Paint { Name = "Token_Base", Color = Teal, Outline = Px }, // figure base disc (Unity cylinder)
        // Figure rank bases (progression spec 2026-11-12), index = worlds completed; Unity cylinder, mesh-normal outline.
        new Paint { Name = "Base_Wood", Color = C(0.80f, 0.58f, 0.36f), Outline = Px },
        new Paint { Name = "Base_Bronze", Color = C(0.80f, 0.47f, 0.22f), Emission = C(0.06f, 0.03f, 0f), Outline = Px },
        new Paint { Name = "Base_Silver", Color = C(0.74f, 0.75f, 0.78f), Emission = C(0.05f, 0.05f, 0.05f), Outline = Px },
        new Paint { Name = "Base_Gold", Color = Gold, Outline = Px },
        new Paint { Name = "Base_GoldGlow", Color = Gold, Emission = Hdr(1.2f, 0.05f, 0f), Outline = Px }, // gentler than GoldGlow
        new Paint { Name = "Base_Diamond", Color = C(0.70f, 0.92f, 0.98f), Emission = C(0.10f, 0.18f, 0.22f), Outline = Px }, // top rank, under the bloom threshold
        new Paint { Name = "Character_Paint", Color = Color.white, BaseMap = CharacterTexturePath, Outline = Px },
        new Paint { Name = "Cat_Black", Color = C(0.14f, 0.13f, 0.16f), BaseMap = CatTexturePath, Outline = Px, OutlineColor = Paper },
        new Paint { Name = "Paint_PotGlaze", Color = Red, Outline = Px },
        new Paint { Name = "Wood_Natural", Color = C(0.80f, 0.58f, 0.36f), Outline = Px }, // coin bridge deck, pot body
        new Paint { Name = "Wood_Dark", Color = C(0.55f, 0.38f, 0.24f), Outline = Px },
        new Paint { Name = "Paint_Green", Color = C(0.36f, 0.62f, 0.42f), Outline = Px },
        new Paint { Name = "Paint_Stone", Color = C(0.72f, 0.74f, 0.78f), Outline = Px },
        new Paint { Name = "Paint_MetalDark", Color = C(0.36f, 0.42f, 0.46f), Outline = Px },
        new Paint { Name = "Paint_GlassCracked", Color = C(0.62f, 0.72f, 0.78f), Outline = Px },
        new Paint { Name = "Paint_White", Color = C(0.92f, 0.92f, 0.88f), Outline = Px },
        // Slots of models the board no longer shows (old room, candle, pawn); kept so every slot resolves.
        new Paint { Name = "Wood_Light", Color = C(0.90f, 0.68f, 0.45f), Outline = Px },
        new Paint { Name = "Wood_Table", Color = C(0.40f, 0.24f, 0.14f), Outline = Px },
        new Paint { Name = "Paint_Red", Color = C(0.80f, 0.33f, 0.30f), Outline = Px },
        new Paint { Name = "Paint_RedDark", Color = C(0.55f, 0.27f, 0.26f), Outline = Px },
        new Paint { Name = "Lamp_Shade", Color = C(1f, 0.86f, 0.55f), Outline = Px },
        new Paint { Name = "Paint_Brass", Color = C(0.714f, 0.541f, 0.227f), Outline = Px },
        new Paint { Name = "Candle_Wax", Color = C(0.94f, 0.89f, 0.78f), Outline = Px },
        new Paint { Name = "Room_Fabric", Color = C(0.20f, 0.14f, 0.11f), Outline = Px },
        // Particles.
        new Paint { Name = "Portal_Spark", Color = C(2.4f, 1.6f, 0.3f), Unlit = true },
        new Paint { Name = "Cat_Puff", Color = C(0.85f, 0.85f, 0.92f), Unlit = true },
    };

    // Model material slot name (lower case) -> palette material. Own slots come from tools/blender/make_models.py.
    private static readonly Dictionary<string, string> SlotMap = new Dictionary<string, string>
    {
        { "wood", "Wood_Natural" }, { "wooddark", "Wood_Dark" }, { "woodbark", "Wood_Dark" },
        { "plant", "Paint_Green" }, { "metal", "Paint_Metal" }, { "metaldark", "Paint_MetalDark" },
        { "glass", "Paint_GlassCracked" }, { "lamp", "Lamp_Shade" }, { "carpet", "Room_Fabric" },
        { "carpetdarker", "Room_Fabric" }, { "carpetwhite", "Room_Fabric" }, { "stone", "Paint_Stone" },
        { "colormap", "Cat_Black" }, { "_defaultmat", "Paint_White" },
        { "pawnpaint", "Paint_Pawn" }, { "ivory", "Paint_Ivory" }, { "pipblack", "Paint_Black" },
        { "gold", "Paint_Gold" }, { "spikebase", "Paint_Hazard" }, { "tile", "Board_Cream" },
        { "wallwood", "Wood_Wall" }, { "hole", "Paint_Hole" }, { "rim", "Wood_Rim" },
        { "cardface", "Paint_CardFace" }, { "cardback", "Paint_CardBack" }, { "chipred", "Paint_ChipRed" },
        { "chipwhite", "Paint_ChipWhite" }, { "chipgreen", "Paint_ChipGreen" }, { "cardtrim", "Paint_CardTrim" }, { "boxlogo", "Paint_BoxLogo" }, { "chipline", "Paint_ChipLine" }, { "boxbody", "Paint_BoxBody" },
        { "boxlid", "Paint_BoxLid" }, { "mirrorframe", "Wood_Wall" }, { "mirrorglass", "Paint_MirrorGlass" },
        { "mirrorcrack", "Paint_MirrorCrack" }, { "portalframe", "Wood_Wall" }, { "portalstone", "Portal_Stone" },
        { "portalswirla", "Portal_SwirlA" }, { "portalswirlb", "Portal_SwirlB" },
        { "pawprint", "Paw_Print" }, { "mugbody", "Prop_Mug" }, { "beadblue", "Paint_BeadBlue" }, { "beadlight", "Paint_BeadLight" }, { "plank", "Wood_Plank" }, { "coffee", "Prop_Coffee" }, { "brass", "Paint_Brass" }, { "wax", "Candle_Wax" },
        { "milk", "Prop_Milk" }, { "yarn", "Prop_Yarn" }, { "yarnline", "Prop_YarnLine" }, { "lanternglass", "Prop_LanternGlass" },
        { "flame", "Prop_Flame" }, { "lanternpaper", "Prop_LanternPaper" }, { "copper", "Prop_Copper" }, { "tea", "Prop_Tea" },
        { "texture", "KayKit_Paint" }, // KayKit Dungeon models (only KayKit uses this slot name)
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
        ConfigureTrackAtlas();
        ConfigurePrintTexture(PrintSpacePath);
        ConfigurePrintTexture(PrintHazardPath);
        ConfigurePrintTexture(TableFeltPath);
        ConfigurePrintTexture(BoxLogoPath);
        ConfigureTiledTexture(TableKilimPath);
        LimitTextureSize(KayKitTexturePath, 512); // 1024 atlas in the pack; 512 keeps the WebGL texture budget
        Dictionary<string, Material> mats = CreateMaterials();
        RemapModelMaterials(mats);
        BoardTheme theme = CreateTheme(mats);
        StepFeedbackConfig feedback = LoadOrCreate<StepFeedbackConfig>(FeedbackPath);
        VolumeProfile profile = CreateVolumeProfile(feedback);
        AudioConfig audio = CreateAudioConfig();
        AssetDatabase.SaveAssets();
        BuildScene(theme, feedback, profile, audio);
        Debug.Log("BuildBoardScene: done -> " + ScenePath);
    }

    /// <summary>Score-track number atlas (TrackAtlas layout): sRGB, mipmapped, clamped, uncompressed (keeps the numerals crisp).</summary>
    private static void ConfigureTrackAtlas()
    {
        var importer = AssetImporter.GetAtPath(TrackAtlasPath) as TextureImporter;
        if (importer == null) throw new FileNotFoundException("Track atlas not imported (run tools/textures/make_track_atlas.py)", TrackAtlasPath);
        if (importer.textureCompression == TextureImporterCompression.Uncompressed && importer.wrapMode == TextureWrapMode.Clamp
            && importer.mipmapEnabled && importer.sRGBTexture && importer.maxTextureSize == TrackAtlas.Height
            && importer.anisoLevel == 16) return;
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 16; // the track is seen at a glancing angle: keeps the numerals sharp
        importer.maxTextureSize = TrackAtlas.Height;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    /// <summary>Printed space texture: sRGB, mipmapped, clamped, uncompressed (keeps the thin ink border clean).</summary>
    private static void ConfigurePrintTexture(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new FileNotFoundException("Print texture not imported (run tools/textures/make_print_textures.py)", path);
        if (importer.textureCompression == TextureImporterCompression.Uncompressed && importer.wrapMode == TextureWrapMode.Clamp
            && importer.mipmapEnabled && importer.sRGBTexture) return;
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 4;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    /// <summary>Tiling cloth texture (World 5 kilim): like a print texture, but wrapped (Repeat) so it can tile.</summary>
    private static void ConfigureTiledTexture(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new FileNotFoundException("Table texture not imported (run tools/textures/make_print_textures.py)", path);
        if (importer.textureCompression == TextureImporterCompression.Uncompressed && importer.wrapMode == TextureWrapMode.Repeat
            && importer.mipmapEnabled && importer.sRGBTexture) return;
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 4;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    /// <summary>Caps a third-party texture's imported size (the source file stays unmodified).</summary>
    private static void LimitTextureSize(string path, int size)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new FileNotFoundException("Texture not imported", path);
        if (importer.maxTextureSize == size) return;
        importer.maxTextureSize = size;
        importer.SaveAndReimport();
    }

    private static Dictionary<string, Material> CreateMaterials()
    {
        Shader toon = Shader.Find(ToonShaderName);
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (toon == null || unlit == null) throw new InvalidOperationException("LuckyStep/Toon or URP Unlit shader not found.");
        var result = new Dictionary<string, Material>();
        foreach (Paint p in Palette)
        {
            string path = MaterialsFolder + "/" + p.Name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = p.Unlit ? unlit : toon;
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            m.shaderKeywords = new string[0]; // drop URP/Lit keywords left from the old look
            m.SetColor("_BaseColor", p.Color);
            Texture2D baseMap = p.BaseMap != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(p.BaseMap) : null;
            if (p.BaseMap != null && baseMap == null) throw new FileNotFoundException("Base map not imported", p.BaseMap);
            m.SetTexture("_BaseMap", baseMap);
            if (!p.Unlit)
            {
                m.SetColor("_EmissionColor", p.Emission);
                m.SetColor("_OutlineColor", p.OutlineColor);
                m.SetFloat("_OutlineWidth", p.Outline);
                m.SetFloat("_OutlineSmoothNormals", p.SmoothNormals ? 1f : 0f);
            }
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            result[p.Name] = m;
        }
        return result;
    }

    private static void RemapModelMaterials(Dictionary<string, Material> mats)
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ModelsFolder, KenneyFolder, KayKitFolder });
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
                else if (path.Contains(FoodFolderTag) && key == "colormap")
                {
                    target = "Food_Paint";
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
            Model(ModelsFolder + "/wall_cards.fbx"), Model(ModelsFolder + "/wall_chips.fbx"),
        };
        theme.wallMutedFrom = new[] { mats["Paint_CardBack"], mats["Paint_ChipRed"], mats["Paint_ChipGreen"] };
        theme.wallMutedTo = new[] { mats["Board_CardBackMuted"], mats["Board_ChipRedMuted"], mats["Board_ChipGreenMuted"] };
        theme.spike = Model(ModelsFolder + "/spike.fbx");
        theme.die = Model(ModelsFolder + "/die.fbx");
        theme.coin = Model(ModelsFolder + "/coin.fbx");
        theme.mirror = Model(ModelsFolder + "/mirror.fbx");
        theme.bead = Model(ModelsFolder + "/bead.fbx");
        theme.woodPlanks = Model(ModelsFolder + "/tile_wood.fbx");
        theme.portalFrame = Model(ModelsFolder + "/portal_frame.fbx");
        theme.portalDisc = Model(ModelsFolder + "/portal_disc.fbx");
        theme.character = Model(KenneyFolder + "/MiniCharacters/character-female-b.fbx");
        theme.flowerpot = Model(KenneyFolder + "/FurnitureKit/pottedPlant.fbx");
        theme.bridge = Model(KenneyFolder + "/NatureKit/bridge_woodNarrow.fbx");
        theme.cat = Model(KenneyFolder + "/CubePets/animal-cat.fbx");
        theme.floorLight = mats["Board_Cream"];
        theme.floorColors = new[] { mats["Board_Cream"], mats["Board_Linen"] };
        theme.worldTints = new[] { mats["Board_W1"], mats["Board_W2"], mats["Board_W3"], mats["Board_W4"], mats["Board_W5"] };
        theme.wallTile = mats["Board_Linen"];
        theme.hazardTile = mats["Space_Hazard"];
        theme.flowerpotBody = mats["Wood_Natural"]; // pottedPlant.fbx "wood" slot = the pot body
        theme.flowerpotGlaze = mats["Paint_PotGlaze"];
        theme.trackSpace = mats["Track_Space"];
        theme.trackLucky = mats["Track_Lucky"];
        theme.trackDeadly = mats["Track_Deadly"];
        theme.trackBlock = mats["Paint_Black"];
        theme.trackToken = Model(ModelsFolder + "/coin.fbx");
        theme.trackTokenMaterial = mats["Token_Player"];
        theme.boardPlate = mats["Wood_Plate"];
        theme.portalSpark = mats["Portal_Spark"];
        theme.figureBase = mats["Token_Base"];
        theme.figureRanks = new[] { mats["Base_Wood"], mats["Base_Bronze"], mats["Base_Silver"], mats["Base_Gold"], mats["Base_GoldGlow"], mats["Base_Diamond"] };
        theme.guideMarkerMaterial = mats["Paw_PrintNext"]; // the same gold "step here" glow as the cat's next paw print
        theme.confettiSize = 0.16f; // 0.09 vanished at board distance (screenshot 2026-11-12)
        theme.confettiSpeed = 4.2f;
        theme.confettiBurst = 40;
        theme.confettiMaterial = CreateConfettiMaterial();
        theme.pawPrint = Model(ModelsFolder + "/paw_print.fbx");
        theme.pawPrintMaterial = mats["Paw_Print"];
        theme.pawPrintNextMaterial = mats["Paw_PrintNext"];
        theme.catPuffMaterial = mats["Cat_Puff"];
        theme.ropeMaterial = mats["Prop_Rope"];
        theme.tableRail = mats["Table_Rail"];
        theme.worldDecor = CreateWorldDecor(mats);
        EditorUtility.SetDirty(theme);
        return theme;
    }

    /// <summary>
    /// World table themes (design/quick-specs/world-table-themes-addition-2026-11-12.md), index = LevelCatalog world.
    /// Rewritten on every build: tune decor here, not in the asset. World 1 reproduces the look-pass-2 table exactly
    /// (the old fixed TableView layout's props, sizes, yaws and positions; white key light; neutral ambient). Light
    /// values of worlds 2-5 are starting points for art-director review. Props stay off the board (beside, beyond or
    /// in front of it), at most 6 per world, and add no lights.
    /// </summary>
    private static WorldDecor[] CreateWorldDecor(Dictionary<string, Material> mats)
    {
        string food = KenneyFolder + "/FoodKit/", kay = KayKitFolder + "/DungeonPack/", own = ModelsFolder + "/";
        return new[]
        {
            new WorldDecor
            {
                name = "1 Starting Square", surface = mats["Table_Felt"],
                keyColor = Color.white, keyIntensity = KeyIntensity, ambient = Ambient,
                props = new[]
                {
                    Prop(own + "mug.fbx", TableSide.Left, 1f, 0.35f, 0.8f, 200f, height: true),
                    Prop(own + "wall_box.fbx", TableSide.Right, 1f, 0.1f, 1.25f, -14f, front: true, wideAlong: 0.7f),
                    Prop(own + "die.fbx", TableSide.Left, 1.05f, -0.45f, 0.42f, 23f, front: true, wideAlong: -0.75f, wideFront: 0.2f),
                    Prop(own + "die.fbx", TableSide.Left, 0.55f, -0.75f, 0.42f, -38f, front: true, wideAlong: -0.6f, wideFront: -0.15f),
                    Prop(own + "card_fan.fbx", TableSide.Right, 0.9f, -0.7f, 1.15f, 12f, front: true, wideAlong: 0.25f),
                    Prop(own + "rule_booklet.fbx", TableSide.Far, 0.75f, -0.3f, 1.25f, -8f),
                },
            },
            new WorldDecor
            {
                // Cool moonlight key; the warm "lamp" fill is the ambient (the Toon shader reads no extra lights).
                name = "2 Black Cat Alley", surface = mats["Table_FeltNavy"],
                keyColor = C(0.86f, 0.91f, 1f), keyIntensity = 0.95f, ambient = C(0.86f, 0.72f, 0.54f),
                props = new[]
                {
                    Prop(own + "milk_saucer.fbx", TableSide.Left, 1f, 0.2f, 0.62f, 0f, front: true, wideAlong: -0.6f),
                    Prop(own + "yarn_ball.fbx", TableSide.Right, 1f, -0.3f, 0.44f, 35f, front: true, wideAlong: 0.6f),
                    Prop(own + "street_lantern.fbx", TableSide.Far, 0.9f, 0.7f, 1.1f, 20f, height: true),
                },
            },
            new WorldDecor
            {
                name = "3 Hall of Mirrors", surface = mats["Table_Velvet"],
                keyColor = C(1f, 0.89f, 0.70f), keyIntensity = 0.82f, ambient = C(0.72f, 0.60f, 0.48f),
                props = new[]
                {
                    Prop(kay + "candle_lit.fbx", TableSide.Left, 0.9f, 0.4f, 0.7f, 0f, height: true),
                    Prop(kay + "candle_triple.fbx", TableSide.Far, 0.9f, 0.6f, 0.75f, -20f, height: true),
                    Prop(own + "candlestick.fbx", TableSide.Far, 0.8f, -0.55f, 0.75f, 0f, height: true),
                    Prop(own + "hand_mirror.fbx", TableSide.Right, 1f, -0.3f, 0.75f, 30f, front: true, wideAlong: 0.55f),
                    Prop(kay + "candle_lit.fbx", TableSide.Right, 0.9f, 0.5f, 0.6f, 40f, height: true),
                },
            },
            new WorldDecor
            {
                name = "4 Chinese Neighbourhood", surface = mats["Table_Lacquer"],
                keyColor = C(1f, 0.88f, 0.82f), keyIntensity = 0.92f, ambient = C(0.82f, 0.64f, 0.57f),
                props = new[]
                {
                    Prop(food + "steamer.fbx", TableSide.Left, 1f, 0.3f, 0.75f, 10f),
                    Prop(food + "bowl-soup.fbx", TableSide.Right, 1f, 0f, 0.6f, 15f, front: true, wideAlong: 0.6f),
                    Prop(food + "chopstick.fbx", TableSide.Right, 1.05f, -0.45f, 0.6f, 80f, front: true, wideAlong: 0.35f),
                    Prop(food + "chopstick.fbx", TableSide.Right, 1.15f, -0.5f, 0.6f, 86f, front: true, wideAlong: 0.38f, wideFront: -0.08f),
                    Prop(food + "cup-tea.fbx", TableSide.Left, 0.8f, -0.5f, 0.38f, -30f, front: true, wideAlong: -0.6f),
                    Prop(own + "paper_lantern.fbx", TableSide.Far, 0.9f, -0.5f, 0.75f, 0f, height: true),
                },
            },
            new WorldDecor
            {
                name = "5 Old Bazaar", surface = mats["Table_Kilim"], surfaceTile = 1.4f, // small repeat: reads as cloth, not a poster
                keyColor = C(1f, 0.90f, 0.72f), keyIntensity = 0.92f, ambient = C(0.82f, 0.70f, 0.52f),
                props = new[]
                {
                    Prop(own + "tea_tray.fbx", TableSide.Right, 1.05f, 0f, 1f, 0f, front: true, wideAlong: 0.55f),
                    // Coffee cup, not the nazar charm: a table nazar read as the collectable bead (screenshot 2026-11-12).
                    Prop(food + "cup-saucer.fbx", TableSide.Left, 1f, -0.4f, 0.45f, 15f, front: true, wideAlong: -0.6f),
                },
            },
        };
    }

    private static TableProp Prop(string model, TableSide side, float gap, float along, float size, float yaw,
        bool height = false, bool front = false, float wideAlong = 0f, float wideFront = 0f)
    {
        return new TableProp
        {
            model = Model(model), side = side, gap = gap, along = along, size = size, sizeIsHeight = height, yaw = yaw,
            frontWhenWide = front, wideAlong = wideAlong, wideFront = wideFront,
        };
    }

    private static VolumeProfile CreateVolumeProfile(StepFeedbackConfig feedback)
    {
        VolumeProfile profile = LoadOrCreate<VolumeProfile>(ProfilePath);

        // No tonemapping: the key light is tuned so lit paper comes out at 1.0, i.e. every palette hex prints as authored.
        Tonemapping tone = GetOrAdd<Tonemapping>(profile);
        tone.mode.Override(TonemappingMode.None);

        // Threshold is gamma; 1.4 is ~2.16 linear and URP's built-in soft knee (half the threshold) starts at ~1.08,
        // above lit paper (1.0), so only HDR gold (GoldGlow, red channel ~3) blooms.
        Bloom bloom = GetOrAdd<Bloom>(profile);
        bloom.threshold.Override(1.4f);
        bloom.intensity.Override(0.6f);
        bloom.scatter.Override(0.6f);
        bloom.highQualityFiltering.Override(false);

        // No resting vignette (vignetteBase 0); StepFeedback still drives it for the near-deadly darkening and death pulse.
        Vignette vignette = GetOrAdd<Vignette>(profile);
        vignette.intensity.Override(feedback.vignetteBase);
        vignette.smoothness.Override(0.45f);
        vignette.color.Override(Ink);

        // Printed Board: no depth of field. Kept in the profile but inactive, so BoardCameraRig leaves it alone.
        DepthOfField dof = GetOrAdd<DepthOfField>(profile);
        dof.active = false;

        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void BuildScene(BoardTheme theme, StepFeedbackConfig feedback, VolumeProfile profile, AudioConfig audio)
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

        // One key light with soft-filtered shadows; the Toon shader folds its shadow into the shade band. No fill lights.
        var lights = new GameObject("Lights").transform;
        var key = new GameObject("Key Light").AddComponent<Light>();
        key.transform.SetParent(lights, false);
        key.type = LightType.Directional;
        // Lit = base * (key + 0.15 * ambient) in the Toon shader; key + 0.15 * ambient = 1 makes lit colours equal the hexes.
        key.color = Color.white;
        key.intensity = KeyIntensity;
        key.shadows = LightShadows.Soft; // soft filtering hides shadow-map texel steps (they read as scribbles)
        key.shadowStrength = 1f;
        key.transform.rotation = Quaternion.LookRotation((Vector3.zero - KeyLightFrom).normalized + Vector3.down * 0.6f);
        RenderSettings.sun = key;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Ambient;
        RenderSettings.skybox = null;
        RenderSettings.fog = false;

        // Presentation: board view + feedback (+ its flash light).
        var presentation = new GameObject("Presentation");
        BoardView board = presentation.AddComponent<BoardView>();
        SetRef(board, "_controller", controller);
        SetRef(board, "_theme", theme);
        SetRef(board, "_catalog", LevelCatalogBuilder.CreateOrUpdateDefault()); // look pass 2: per-world checker tint
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
        TableView table = presentation.AddComponent<TableView>();
        SetRef(table, "_board", board);
        SetRef(table, "_theme", theme);
        SetRef(table, "_keyLight", key); // world table themes: tinted per world

        // Camera (BoardCameraRig frames it at runtime; this is the edit-time placeholder pose).
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Background;
        cam.fieldOfView = 30f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 80f;
        camGo.transform.SetPositionAndRotation(new Vector3(0f, 14f, -12f), Quaternion.Euler(50f, 0f, 0f));
        UniversalAdditionalCameraData camData = cam.GetUniversalAdditionalCameraData();
        camData.renderPostProcessing = true;
        camData.requiresDepthOption = CameraOverrideOption.On;
        camData.antialiasing = AntialiasingMode.None; // MSAA 4x in the URP assets: FXAA blurred the printed track numerals
        BoardCameraRig rig = camGo.AddComponent<BoardCameraRig>();
        SetRef(rig, "_board", board);
        SetRef(rig, "_volume", volume);

        UiRoot ui = BuildUi(controller);
        SetRef(ui, "_board", board); // figure rank (progression)
        AudioFeedback sound = presentation.AddComponent<AudioFeedback>();
        SetRef(sound, "_board", board);
        SetRef(sound, "_controller", controller);
        SetRef(sound, "_ui", ui);
        // Reload from disk: a config created earlier in this run can be replaced by the UI sprite reimport above.
        SetRef(sound, "_config", Load<AudioConfig>(AudioConfigPath));

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + ScenePath);
    }

    /// <summary>Story 006: writes the level catalog, sets up the UI sprite importers and adds the "UI" root with its references.</summary>
    private static UiRoot BuildUi(PlayerInputController controller)
    {
        LevelCatalog catalog = LevelCatalogBuilder.CreateOrUpdateDefault();
        ConfigureUiSprites();
        UiRoot ui = new GameObject("UI").AddComponent<UiRoot>();
        SetRef(ui, "_controller", controller);
        SetRef(ui, "_catalog", catalog);
        SetRef(ui, "_titleFont", Load<Font>(UiFontsFolder + "/Fredoka-Bold.ttf"));
        SetRef(ui, "_bodyFont", Load<Font>(UiFontsFolder + "/Fredoka-Bold.ttf")); // SemiBold read faint at caption sizes
        SetRef(ui, "_panel", Load<Sprite>(UiSpritesFolder + "/cream_panel.png"));
        SetRef(ui, "_buttonPrimary", Load<Sprite>(UiSpritesFolder + "/yellow_button_rectangle_depth_flat.png"));
        SetRef(ui, "_buttonSecondary", Load<Sprite>(UiSpritesFolder + "/cream_button_rectangle_depth_flat.png"));
        SetRef(ui, "_levelButton", Load<Sprite>(UiSpritesFolder + "/yellow_button_square_depth_flat.png"));
        SetRef(ui, "_levelButtonLocked", Load<Sprite>(UiSpritesFolder + "/cream_button_square_depth_flat.png"));
        SetRef(ui, "_lockIcon", Load<Sprite>(UiSpritesFolder + "/icon_lock.png"));
        SetRef(ui, "_tickIcon", Load<Sprite>(UiSpritesFolder + "/green_icon_checkmark.png"));
        SetRef(ui, "_pawnIcon", Load<Sprite>(UiSpritesFolder + "/icon_pawn.png"));
        SetRef(ui, "_arrowIcon", Load<Sprite>(UiSpritesFolder + "/icon_arrow.png"));
        SetRef(ui, "_baseIcon", Load<Sprite>(UiSpritesFolder + "/icon_base.png"));
        return ui;
    }

    /// <summary>
    /// Win confetti material: URP Particles/Unlit (opaque, white), so each piece shows its particle colour. The
    /// LuckyStep/Toon and URP/Unlit palette materials ignore vertex colour, so this one is made apart from the palette.
    /// </summary>
    private static Material CreateConfettiMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) throw new InvalidOperationException("URP Particles/Unlit shader not found.");
        string path = MaterialsFolder + "/Confetti.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = shader;
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Cull", 0f); // paper: both sides
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>
    /// Story 008 audio: import settings for Assets/Audio (effects decompressed on load, music compressed in memory so
    /// the Web build does not stream) and the AudioConfig asset with its clips. Mix values keep what the asset has.
    /// </summary>
    private static AudioConfig CreateAudioConfig()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { SfxFolder, MusicFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) continue;
            bool music = path.StartsWith(MusicFolder, StringComparison.Ordinal);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            AudioClipLoadType loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            if (settings.loadType == loadType && settings.compressionFormat == AudioCompressionFormat.Vorbis) continue;
            settings.loadType = loadType;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music ? 0.5f : 0.7f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        AudioConfig config = LoadOrCreate<AudioConfig>(AudioConfigPath);
        config.step = Clips("footstep02", "footstep03", "footstep06", "footstep08"); // low thuds; chip-lay was too thin
        config.wood = Clips("bookPlace1", "bookPlace2", "bookPlace3");
        config.dice = Clips("die-throw-1", "die-throw-2", "die-throw-3", "die-throw-4");
        config.coin = Clips("handleCoins", "handleCoins2");
        config.lucky = Clips("glass_004");
        config.pot = Clips("metalPot1", "metalPot2");
        config.catReset = Clips("scratch_001");
        config.mirror = Clips("glass_001");
        config.beadPickup = Clips("pluck_001", "pluck_002");
        config.beadBreak = Clips("glass_002");
        config.death = Clips("error_008");
        config.fall = Clips("drop_002");
        config.blocked = Clips("tick_002");
        config.win = Clips("confirmation_004");
        config.undo = Clips("back_001");
        config.click = Clips("click-a");
        config.menuMusic = Load<AudioClip>(MusicFolder + "/cozy_puzzle_title.ogg");
        config.gameMusic = Load<AudioClip>(MusicFolder + "/cozy_puzzle_in-game_3.ogg");
        EditorUtility.SetDirty(config);
        return config;
    }

    private static AudioClip[] Clips(params string[] names)
    {
        return names.Select(n => Load<AudioClip>(SfxFolder + "/" + n + ".ogg")).ToArray();
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

    /// <summary>HDR colour from LINEAR values; Material.SetColor linearises colours in a Linear project, so they go in as gamma.</summary>
    private static Color Hdr(float r, float g, float b) => new Color(r, g, b).gamma;
}
