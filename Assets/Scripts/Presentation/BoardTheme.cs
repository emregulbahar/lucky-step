using UnityEngine;

namespace LuckyStep
{
    /// <summary>
    /// Look and motion tunables of the 3D board (Story 005, brief MVP 5): which model is used for each tile, piece
    /// sizes and timings. Created and filled by Assets/Editor/BuildBoardScene.cs; designers tune the asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Lucky Step/Board Theme", fileName = "BoardTheme")]
    public sealed class BoardTheme : ScriptableObject
    {
        /// <summary>Walkable tile.</summary>
        [Header("Own models (top of a tile is y = 0)")]
        public GameObject floorTile;
        /// <summary>Hole tile, also under bridges.</summary>
        public GameObject gapTile;
        /// <summary>Unit bar (1x1x1, bottom at 0) scaled into the board's raised edge.</summary>
        public GameObject rimBar;
        /// <summary>Board-game pieces shown on interior wall tiles; one is picked per tile from its position.</summary>
        public GameObject[] wallPieces;
        /// <summary>
        /// Scale of a wall piece on the board (x/z footprint, y height): lower and smaller than the table props so the
        /// card and chip stacks read as quiet blockers, not the loudest thing on the board (user feedback 2026-11-12).
        /// </summary>
        public Vector3 wallPieceScale = new Vector3(0.8f, 0.45f, 0.8f);
        /// <summary>Whether wall pieces cast shadows (off keeps the board calm; they still receive shadows).</summary>
        public bool wallPieceShadows = false;
        /// <summary>Materials swapped on board wall pieces (wallMutedFrom[i] becomes wallMutedTo[i]); table props keep the originals.</summary>
        public Material[] wallMutedFrom;
        /// <summary>Muted counterparts of <see cref="wallMutedFrom"/>, same order.</summary>
        public Material[] wallMutedTo;
        /// <summary>Spike cluster placed on a floor tile.</summary>
        public GameObject spike;
        /// <summary>Six-sided die placed on a dice tile.</summary>
        public GameObject die;
        /// <summary>Coin floating over a closed bridge.</summary>
        public GameObject coin;
        /// <summary>Standing broken mirror.</summary>
        public GameObject mirror;
        /// <summary>Exit portal plinth and ring.</summary>
        public GameObject portalFrame;
        /// <summary>Emissive swirl disc inside the portal ring (spun at runtime).</summary>
        public GameObject portalDisc;

        /// <summary>The player's figure.</summary>
        [Header("Kenney models (fitted by size)")]
        public GameObject character;
        /// <summary>Falling flowerpot.</summary>
        public GameObject flowerpot;
        /// <summary>Coin bridge deck.</summary>
        public GameObject bridge;
        /// <summary>Black cat.</summary>
        public GameObject cat;

        /// <summary>Cream board space (every other floor space, and under objects).</summary>
        [Header("Materials")]
        public Material floorLight;
        /// <summary>Floor materials of the checkerboard: (x + y) % length picks the material of a non-wall space.</summary>
        public Material[] floorColors;
        /// <summary>
        /// Per-world tint of the checkerboard (index = world index in the LevelCatalog): when set, non-wall spaces
        /// alternate floorLight and worldTints[world]; otherwise floorColors is used.
        /// </summary>
        public Material[] worldTints;
        /// <summary>Tile under interior wall pieces (tiles carrying other objects use the cream floorLight).</summary>
        public Material wallTile;
        /// <summary>Crimson base of hazard spaces (spike, dice, flowerpot) and of a coin bridge's edge while it is closed.</summary>
        public Material hazardTile;
        /// <summary>Material on the flowerpot model's pot body that flowerpotGlaze replaces (shared with other furniture).</summary>
        public Material flowerpotBody;
        /// <summary>Crimson glaze put on the flowerpot's pot body so it reads as a hazard.</summary>
        public Material flowerpotGlaze;
        /// <summary>Wooden plate under the whole board.</summary>
        public Material boardPlate;
        /// <summary>Material of the portal's spark particles.</summary>
        public Material portalSpark;
        /// <summary>Material of the flat base disc under the player figure (tabletop-miniature look).</summary>
        public Material figureBase;
        /// <summary>World diameter of the figure's base disc, in grid cells.</summary>
        public float figureBaseDiameter = 0.55f;
        /// <summary>World height of the figure's base disc.</summary>
        public float figureBaseHeight = 0.04f;

        /// <summary>World size of one grid cell.</summary>
        [Header("Layout")]
        public float tileSize = 1f;
        /// <summary>Margin of the board plate around the playable tiles, in world units.</summary>
        public float plateMargin = 0.22f;
        /// <summary>Width of the wooden border along the board's edge.</summary>
        public float rimWidth = 0.16f;
        /// <summary>Height of the border's top above the space tops (small: the printed board is nearly flat).</summary>
        public float rimHeight = 0.03f;
        /// <summary>Flowerpot height.</summary>
        public float flowerpotHeight = 1.3f;
        /// <summary>Height the flowerpot hovers at, above the tile, ready to fall.</summary>
        public float flowerpotHover = 1.5f;
        /// <summary>Flowerpot pendulum swing amplitude in degrees (around the rope's top end).</summary>
        public float flowerpotSwayDegrees = 2f;
        /// <summary>Flowerpot swing speed in radians per second.</summary>
        public float flowerpotSwaySpeed = 1.6f;
        /// <summary>Material of the flowerpot's rope and hanger strings.</summary>
        public Material ropeMaterial;
        /// <summary>Length of the rope from the hanger knot up out of view.</summary>
        public float ropeLength = 6f;
        /// <summary>Rope and string diameter.</summary>
        public float ropeThickness = 0.035f;
        /// <summary>Height of the hanger knot above the pot's base, as a fraction of flowerpotHeight.</summary>
        public float potKnotHeight = 1.15f;
        /// <summary>Height of the pot rim the strings start from, as a fraction of flowerpotHeight.</summary>
        public float potRimHeight = 0.45f;
        /// <summary>Radius of the pot rim the strings start from, as a fraction of flowerpotHeight.</summary>
        public float potRimRadius = 0.17f;
        /// <summary>Mirror yaw in degrees (0 = glass faces along the grid's rows).</summary>
        public float mirrorYaw = 0f;
        /// <summary>Backward lean of the standing mirror in degrees.</summary>
        public float mirrorLeanDegrees = 15f;
        /// <summary>Mirror skew (roll) in degrees.</summary>
        public float mirrorSkewDegrees = 5f;
        /// <summary>Evil-eye bead (World 5): upright disc, bottom at its origin.</summary>
        public GameObject bead;
        /// <summary>Knock-on-wood planks laid on a space (World 5), bottom at its origin.</summary>
        public GameObject woodPlanks;
        /// <summary>Height of the bead's bottom above its space while it waits to be picked up.</summary>
        public float beadHover = 0.12f;
        /// <summary>Up-and-down bob amplitude of a waiting bead.</summary>
        public float beadBob = 0.04f;
        /// <summary>Bob cycles per second.</summary>
        public float beadBobSpeed = 0.6f;
        /// <summary>A waiting bead sways left and right by this many degrees, keeping its eye towards the camera.</summary>
        public float beadSwayDegrees = 35f;
        /// <summary>Sway speed in radians per second.</summary>
        public float beadSwaySpeed = 1.4f;
        /// <summary>Seconds a picked-up bead takes to fly up and shrink away.</summary>
        public float beadPickupSeconds = 0.35f;
        /// <summary>How far a picked-up bead rises while it shrinks away.</summary>
        public float beadPickupRise = 0.6f;
        /// <summary>Height above the space where the puff of a broken (used) bead appears.</summary>
        public float beadWardPuffHeight = 0.45f;
        /// <summary>Portal yaw in degrees (0 = disc faces along the grid's rows, towards the default camera).</summary>
        public float portalYaw = 0f;
        /// <summary>Height of the portal disc's centre above the tile (matches the ring in the model).</summary>
        public float portalDiscHeight = 0.47f;
        /// <summary>Portal swirl spin in degrees per second.</summary>
        public float portalSpinSpeed = 100f;
        /// <summary>Most spark particles alive per portal (keep low for WebGL).</summary>
        public int portalSparkMax = 30;
        /// <summary>Spark particles emitted per second per portal.</summary>
        public float portalSparkRate = 14f;
        /// <summary>Seconds the portal takes to change between its open and closed look.</summary>
        [Tooltip("Seconds the portal takes to change between its open and closed look (open = the next step is lucky).")]
        public float portalOpenSeconds = 0.25f;
        /// <summary>Swirl size while the exit is closed, as a fraction of the open size.</summary>
        [Tooltip("Swirl size while the exit is closed, as a fraction of the open size.")]
        public float portalClosedScale = 0.55f;
        /// <summary>Swirl spin speed while the exit is closed, as a fraction of portalSpinSpeed.</summary>
        [Tooltip("Swirl spin speed while the exit is closed, as a fraction of portalSpinSpeed.")]
        public float portalClosedSpin = 0.2f;
        /// <summary>Spark rate while the exit is closed, as a fraction of portalSparkRate.</summary>
        [Tooltip("Spark rate while the exit is closed, as a fraction of portalSparkRate.")]
        public float portalClosedSparks = 0f;
        /// <summary>Bridge deck length along its span.</summary>
        public float bridgeLength = 1f;
        /// <summary>Vertical offset of the bridge deck (the Kenney posts reach below the deck).</summary>
        public float bridgeYOffset = -0.04f;
        /// <summary>Angle a closed bridge is raised to, like a drawbridge, in degrees.</summary>
        public float bridgeClosedAngle = 70f;
        /// <summary>Seconds a bridge takes to lower once opened.</summary>
        public float bridgeLowerSeconds = 0.45f;
        /// <summary>Coin height above a closed bridge.</summary>
        public float coinHeight = 0.85f;
        /// <summary>Coin spin in degrees per second.</summary>
        public float coinSpinSpeed = 120f;
        /// <summary>Height of the figure's feet when it stands on a dice tile (on top of the die).</summary>
        public float dieStandHeight = 0.45f;
        /// <summary>Die yaw in degrees, for a less tidy look.</summary>
        public float dieYaw = 18f;
        /// <summary>Height of the die's centre above the tile (half its side); the die tumbles around this point.</summary>
        [Header("Dice roll, coin toss, flowerpot fall")]
        [Tooltip("Height of the die's centre above the tile (half its side); the die tumbles around this point.")]
        public float dieCenterHeight = 0.225f;
        /// <summary>Seconds a die roll takes.</summary>
        [Tooltip("Seconds a die roll takes.")]
        public float dieRollSeconds = 0.7f;
        /// <summary>Height of the die's toss arc.</summary>
        [Tooltip("Height of the die's toss arc.")]
        public float dieRollHeight = 0.6f;
        /// <summary>Full turns the die tumbles before it settles.</summary>
        [Tooltip("Full turns the die tumbles before it settles.")]
        public float dieRollTurns = 2f;
        /// <summary>Seconds a coin toss takes, before the bridge lowers.</summary>
        [Tooltip("Seconds a coin toss takes, before the bridge lowers.")]
        public float coinTossSeconds = 0.6f;
        /// <summary>Height of the coin's toss arc above its resting height.</summary>
        [Tooltip("Height of the coin's toss arc above its resting height.")]
        public float coinTossHeight = 0.7f;
        /// <summary>Whole flips the coin makes during a toss.</summary>
        [Tooltip("Whole flips the coin makes during a toss.")]
        public float coinFlipTurns = 3f;
        /// <summary>Seconds a flowerpot takes to fall.</summary>
        [Tooltip("Seconds a flowerpot takes to fall.")]
        public float potFallSeconds = 0.35f;
        /// <summary>Height the pot's base stops at when it lands on the pawn.</summary>
        [Tooltip("Height the pot's base stops at when it lands on the pawn.")]
        public float potHitHeight = 0.2f;
        /// <summary>Sideways distance (along the tile's X) a missing pot lands from the pawn.</summary>
        [Tooltip("Sideways distance (along the tile's X) a missing pot lands from the pawn.")]
        public float potMissOffset = 0.4f;
        /// <summary>Degrees a missing pot tumbles while it falls.</summary>
        [Tooltip("Degrees a missing pot tumbles while it falls.")]
        public float potTumbleDegrees = 90f;
        /// <summary>Cat height.</summary>
        public float catHeight = 0.5f;
        /// <summary>Added to the cat's facing yaw if the model's nose is not along +Z after import.</summary>
        public float catYawOffset = 0f;
        /// <summary>Player figure height.</summary>
        public float characterHeight = 0.95f;
        /// <summary>Added to the figure's facing yaw if its front is not along +Z after import.</summary>
        public float characterYawOffset = 0f;
        /// <summary>Legacy animation clip name (or part of it) looped while standing; empty for none.</summary>
        public string characterIdleClip = "idle";
        /// <summary>Legacy animation clip name (or part of it) looped while moving; empty for none.</summary>
        public string characterWalkClip = "walk";

        /// <summary>Space material (number atlas, cream tint) of the score track.</summary>
        [Header("Score track (step counter)")]
        public Material trackSpace;
        /// <summary>Lucky space material (number atlas, gold tint).</summary>
        public Material trackLucky;
        /// <summary>Deadly space material (number atlas, skull block, untinted).</summary>
        public Material trackDeadly;
        /// <summary>Material of the little blocks the track spaces are printed on (ink: it shows as each space's border).</summary>
        public Material trackBlock;
        /// <summary>Marker token model (round, lying flat, centred).</summary>
        public GameObject trackToken;
        /// <summary>Marker token material (player colour).</summary>
        public Material trackTokenMaterial;
        /// <summary>Depth of the board strip that carries the track, along the near edge.</summary>
        public float trackStripDepth = 0.8f;
        /// <summary>Size of one printed space.</summary>
        public float trackSpaceSize = 0.48f;
        /// <summary>Centre-to-centre distance of spaces (shrinks automatically if the track is longer than the board).</summary>
        public float trackSpacing = 0.56f;
        /// <summary>Highest count the track ever shows (it ends at the first deadly count reached by counting up).</summary>
        public int trackCap = 30;
        /// <summary>Uniform scale of the marker token.</summary>
        public float tokenScale = 1.15f;
        /// <summary>Seconds per space for a marker slide; long jumps take this times sqrt(spaces).</summary>
        public float tokenMoveSeconds = 0.14f;
        /// <summary>Hop height of the marker while it slides.</summary>
        public float tokenHopHeight = 0.18f;
        /// <summary>Pulse speed (radians per second) when the count is beyond the track's last space.</summary>
        public float tokenPulseSpeed = 7f;
        /// <summary>Pulse size (fraction of the scale) when the count is beyond the track's last space.</summary>
        public float tokenPulseAmount = 0.2f;

        /// <summary>Flat paw print marking the cat's route (toes point along the model's forward axis).</summary>
        [Header("Cat route and reset")]
        public GameObject pawPrint;
        /// <summary>Paw print material on route tiles.</summary>
        public Material pawPrintMaterial;
        /// <summary>Brighter paw print material on the tile the cat steps onto next.</summary>
        public Material pawPrintNextMaterial;
        /// <summary>Scale of a paw print model.</summary>
        public float pawPrintScale = 1f;
        /// <summary>Side offset of the two prints on a tile, from its centre.</summary>
        public float pawPrintSpacing = 0.13f;
        /// <summary>Added to the print yaw if the toes point backwards after import.</summary>
        public float pawPrintYawOffset = 0f;
        /// <summary>Material of the puff burst shown when the cat resets the counter.</summary>
        public Material catPuffMaterial;
        /// <summary>Seconds of the cat's hop when it resets the counter.</summary>
        public float catResetSeconds = 0.5f;
        /// <summary>Height of the cat's reset hop.</summary>
        public float catResetHopHeight = 0.45f;
        /// <summary>Back arch (pitch) of the cat at the top of the reset hop, in degrees.</summary>
        public float catResetArchDegrees = 25f;
        /// <summary>Particles in the reset puff (keep low for WebGL).</summary>
        public int catPuffCount = 14;
        /// <summary>Start size of a puff particle.</summary>
        public float catPuffSize = 0.14f;
        /// <summary>Start speed of a puff particle.</summary>
        public float catPuffSpeed = 1.1f;
        /// <summary>Lifetime of a puff particle in seconds.</summary>
        public float catPuffLifetime = 0.6f;

        /// <summary>Seconds for a one-tile move. Longer moves take this times sqrt(tiles).</summary>
        [Header("Motion")]
        public float moveSeconds = 0.16f;
        /// <summary>Height of the little hop during a move.</summary>
        public float hopHeight = 0.12f;
        /// <summary>How far the figure tips over when it dies, in degrees.</summary>
        public float deadTiltDegrees = 80f;
        /// <summary>Seconds the death tip-over takes.</summary>
        public float deadTiltSeconds = 0.3f;

        /// <summary>Seconds the figure takes to drop into a gap or a closed bridge (starts when its hop lands).</summary>
        [Header("Figure effects (hole death, win, blocked move)")]
        [Tooltip("Seconds the figure takes to drop into a gap or a closed bridge (starts when its hop lands).")]
        public float holeFallSeconds = 0.32f;
        /// <summary>How far the figure's feet sink below the tile top (enough to vanish under the dark pit floor).</summary>
        [Tooltip("How far the figure's feet sink below the tile top (enough to vanish under the dark pit floor).")]
        public float holeFallDepth = 1.3f;
        /// <summary>Figure scale at the bottom of the fall (1 = no shrink).</summary>
        [Tooltip("Figure scale at the bottom of the fall (1 = no shrink).")]
        public float holeFallEndScale = 0.6f;
        /// <summary>Seconds of the win hop into the portal (starts when the step's hop lands).</summary>
        [Tooltip("Seconds of the win hop into the portal (starts when the step's hop lands).")]
        public float winSeconds = 0.55f;
        /// <summary>Extra arc height of the win hop, on top of rising to the portal disc's centre.</summary>
        [Tooltip("Extra arc height of the win hop, on top of rising to the portal disc's centre.")]
        public float winHopHeight = 0.3f;
        /// <summary>Whole turns the figure spins while it shrinks into the portal.</summary>
        [Tooltip("Whole turns the figure spins while it shrinks into the portal.")]
        public float winSpinTurns = 2f;
        /// <summary>Spark particles burst from the portal when the figure vanishes (keep low for WebGL).</summary>
        [Tooltip("Spark particles burst from the portal when the figure vanishes (keep low for WebGL).")]
        public int winSparkBurst = 24;
        /// <summary>Start speed of a win burst spark.</summary>
        [Tooltip("Start speed of a win burst spark.")]
        public float winSparkSpeed = 1.2f;
        /// <summary>Start size of a win burst spark.</summary>
        [Tooltip("Start size of a win burst spark.")]
        public float winSparkSize = 0.08f;
        /// <summary>Lifetime of a win burst spark in seconds.</summary>
        [Tooltip("Lifetime of a win burst spark in seconds.")]
        public float winSparkLifetime = 0.6f;
        /// <summary>Seconds of the bump when a move is blocked (push out and spring back).</summary>
        [Tooltip("Seconds of the bump when a move is blocked (push out and spring back).")]
        public float bumpSeconds = 0.14f;
        /// <summary>How far the figure nudges towards a blocked cell, as a fraction of a tile.</summary>
        [Tooltip("How far the figure nudges towards a blocked cell, as a fraction of a tile.")]
        public float bumpDistance = 0.18f;
        /// <summary>Seconds the portal disc flickers when a move into the closed exit is blocked.</summary>
        [Tooltip("Seconds the portal disc flickers when a move into the closed exit is blocked.")]
        public float portalBlockedSeconds = 0.35f;
        /// <summary>Fraction of the disc's size lost at the deepest flicker pulse.</summary>
        [Tooltip("Fraction of the disc's size lost at the deepest flicker pulse.")]
        public float portalBlockedShrink = 0.35f;
        /// <summary>Shrink pulses in one closed-exit flicker (they fade out over the flicker).</summary>
        [Tooltip("Shrink pulses in one closed-exit flicker (they fade out over the flicker).")]
        public float portalBlockedPulses = 3f;

        /// <summary>Wooden rail around the table top (every world).</summary>
        [Header("Card table (TableView, decoration only)")]
        public Material tableRail;
        /// <summary>
        /// Per-world table look (index = world index in the LevelCatalog; unknown levels use entry 0): table surface,
        /// light colours and the props beside the board. Written by Assets/Editor/BuildBoardScene.cs.
        /// </summary>
        public WorldDecor[] worldDecor;
        /// <summary>Grid width / height (walls included) from which a board counts as wide: its side props move in front.</summary>
        public float wideBoardAspect = 2.3f;
        /// <summary>Felt the camera keeps beside a non-wide board, per side, so the side props stay in frame.</summary>
        public float framePropPad = 1.4f;
        /// <summary>Extra felt beyond the far rail, so the top of the frame shows table instead of the dark floor.</summary>
        public float tableMarginFar = 6f;
        /// <summary>Felt beyond the board's bounds on the left and right, in world units.</summary>
        public float tableMarginX = 4.2f;
        /// <summary>Felt beyond the board's bounds at the far and near sides, in world units.</summary>
        public float tableMarginZ = 1.4f;
        /// <summary>Width of the wooden rail.</summary>
        public float tableRailWidth = 0.6f;
        /// <summary>Height of the rail's top above the felt.</summary>
        public float tableRailHeight = 0.16f;
        /// <summary>Distance from the board's side to a prop's centre (TableProp.gap multiplies it).</summary>
        public float propGap = 1.0f;

        /// <summary>Layout version last written by the scene builder; it resets tunables when the layout changes.</summary>
        [HideInInspector] public int builderVersion;
    }

    /// <summary>Side of the board a table prop stands on (Far = beyond the board's far edge, away from the camera).</summary>
    public enum TableSide
    {
        Left,
        Right,
        Far
    }

    /// <summary>
    /// One world's table (world table themes, design/quick-specs/world-table-themes-addition-2026-11-12.md): the
    /// surface material, the key light and ambient (fill) colours, and the props. Pure decoration.
    /// Example: <c>theme.worldDecor[1].keyColor</c> is Black Cat Alley's cool moonlight.
    /// </summary>
    [System.Serializable]
    public sealed class WorldDecor
    {
        /// <summary>Inspector label only (the world's name).</summary>
        public string name;
        /// <summary>Table top material; its colour is the surface tint (set by the scene builder's palette).</summary>
        public Material surface;
        /// <summary>
        /// World units per repeat of the surface texture (patterned cloth); 0 stretches the texture once over the
        /// whole top (felt with its lamp pool).
        /// </summary>
        public float surfaceTile;
        /// <summary>Colour of the scene's directional key light (the only light the Toon shader reads).</summary>
        public Color keyColor = Color.white;
        /// <summary>Intensity of the key light; key + 0.15 * ambient = 1 prints palette colours as authored.</summary>
        public float keyIntensity = 0.925f;
        /// <summary>
        /// Flat ambient colour. The Toon shader ignores additional lights, so this is the world's fill: it lifts the
        /// shade band (lit = base * key + base * ambient * 0.15).
        /// </summary>
        public Color ambient = new Color(0.735f, 0.735f, 0.735f);
        /// <summary>Props beside, in front of or beyond the board (WebGL budget: at most 6).</summary>
        public TableProp[] props;
    }

    /// <summary>
    /// One table prop and where it goes. Left/Right props stand <c>gap * propGap</c> beside the board's side at
    /// <c>along</c> times its half depth from the centre (+ = far, - = near: left-back is Left with along &gt; 0).
    /// Far props stand <c>gap * propGap</c> beyond the far edge at <c>along</c> times the half width (+ = right).
    /// On a wide board (BoardView.IsWide) a prop with <c>frontWhenWide</c> moves to the row in front of the board.
    /// </summary>
    [System.Serializable]
    public sealed class TableProp
    {
        /// <summary>Model (bottom-centre placed on the table top by ModelFit).</summary>
        public GameObject model;
        /// <summary>Side of the board.</summary>
        public TableSide side;
        /// <summary>Distance out from the board's edge, in units of BoardTheme.propGap.</summary>
        public float gap = 1f;
        /// <summary>Position along the side, as a fraction of the board's half extent (see the class summary).</summary>
        public float along;
        /// <summary>Fitted size: the footprint's longest side, or the height if <see cref="sizeIsHeight"/>.</summary>
        public float size = 0.5f;
        /// <summary>True: <see cref="size"/> is the prop's height (tall props); false: its footprint.</summary>
        public bool sizeIsHeight;
        /// <summary>Yaw in degrees.</summary>
        public float yaw;
        /// <summary>
        /// Wide boards leave the sides off-screen: move this prop to the row in front of the board. Keep it false for
        /// tall props, which would hide the board's near edge from the camera.
        /// </summary>
        public bool frontWhenWide;
        /// <summary>Front-row x, as a fraction of the board's half width from its centre (+ = right).</summary>
        public float wideAlong;
        /// <summary>Front-row z offset in world units (+ = towards the board).</summary>
        public float wideFront;
    }
}
