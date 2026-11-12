using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LuckyStep
{
    /// <summary>
    /// Builds the 3D wooden board from <see cref="PlayerInputController.Level"/> and shows
    /// <see cref="PlayerInputController.State"/>: player figure, cat, coin bridges (Story 005, brief MVP 5).
    /// Read-only: it never changes game state. A new state is detected by reference each frame.
    /// Grid x maps to +X, grid row 0 (top) is the far side (+Z); tile tops are at local y = 0.
    /// When the grid's outer ring is all walls it is drawn as a thin raised rim instead of wall pieces
    /// (the logic is unchanged: outside the grid is still a wall).
    /// A score track along the near edge shows the step counter: numbered spaces from RuleSet.StartValue to the first
    /// deadly count reached by counting up, with a marker token on the current count.
    /// Figure effects: a death on a gap or closed bridge drops the figure into the hole (other deaths tip it over), a
    /// win hops it spinning into the portal with a spark burst, and a blocked move bumps it towards the blocked cell
    /// (a closed exit also flickers). They wait for the step's hop to land, never hold back input, and undo or restart
    /// ends them at once.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        private const float PlateBottom = -0.30f;
        // tile_floor model (tools/blender/make_models.py): a card slab 0.96 x 0.96, 0.04 thick, top at y = 0.
        private const float SpaceFootprint = 0.96f;
        private const float SlabThickness = 0.04f;

        private enum FigureFx
        {
            None,
            Fall,
            Win
        }

        [SerializeField] private PlayerInputController _controller;
        [SerializeField] private BoardTheme _theme;
        [SerializeField] private LevelCatalog _catalog;

        private readonly List<BridgeView> _bridges = new List<BridgeView>();
        private readonly List<PotView> _pots = new List<PotView>();
        private readonly List<DieView> _dice = new List<DieView>();
        private readonly List<PortalView> _portals = new List<PortalView>();
        private readonly List<BeadView> _beads = new List<BeadView>();
        private readonly List<Mesh> _spaceMeshes = new List<Mesh>();
        private Level _builtLevel;
        private Material _worldTint;
        private GameState _shown;
        private Transform _root;
        private Mover _figure;
        private Mover _cat;
        private Animation _figureAnimation;
        private string _idleClip;
        private string _walkClip;
        private bool _figureWalking;
        private float _figureTilt;
        private float _figureTiltTarget;
        private bool _awaitingPotImpact;
        private Vector3 _figureScale = Vector3.one;
        private float _figureFit = 1f;
        private FigureFx _figureFx;
        private float _figureFxT = 1f;
        private float _bumpT = 1f;
        private Vector3 _bumpDirection;
        private readonly Dictionary<(int X, int Y), Renderer[]> _routePrints = new Dictionary<(int X, int Y), Renderer[]>();
        private (int X, int Y) _highlightedPrint = (-1, -1);
        private Transform _catBody;
        private ParticleSystem _catPuff;
        private ParticleSystem _potDust;
        private ParticleSystem _beadPuff;
        private float _catResetT = 1f;
        private Vector3[] _trackSpaces = new Vector3[0];
        private int _trackStart;
        private int _trackEnd;
        private Mover _token;
        private bool _tokenBeyond;
        private int _rank;
        private Renderer _baseDisc;
        private float _figureTop;
        private Transform _guideMarker;
        private ParticleSystem _confetti;

        /// <summary>
        /// Raised after the shown state changes: (previous, current, isForwardStep). previous is null for the first
        /// state of a level. isForwardStep is true when current is exactly one legal move from previous
        /// (false for undo and restart).
        /// </summary>
        public event Action<GameState, GameState, bool> StateChanged;

        /// <summary>
        /// Raised when a forward step's death lands on screen: right after <see cref="StateChanged"/>, or, for a
        /// flowerpot death, on the frame the pot hits the pawn. Undo and restart never raise it (a pot still falling
        /// is reset). Camera shake and the red vignette pulse key off it.
        /// </summary>
        public event Action DeathImpact;

        /// <summary>Raised after the board for a level has been (re)built.</summary>
        public event Action Built;

        /// <summary>The level the board was built from, or null before the first build.</summary>
        public Level Level => _builtLevel;

        /// <summary>The player figure's root transform, or null before the first build.</summary>
        public Transform Figure => _figure?.Root;

        /// <summary>The player figure's renderers (for glow effects); empty before the first build.</summary>
        public Renderer[] FigureRenderers { get; private set; } = new Renderer[0];

        /// <summary>World-space bounds of the built board (tiles, rim and pieces), for camera framing.</summary>
        public Bounds BoardBounds { get; private set; }

        /// <summary>
        /// True if the built level's grid is wide (BoardTheme.wideBoardAspect): the table props then lie in front of
        /// the board instead of beside it.
        /// </summary>
        public bool IsWide { get; private set; }

        /// <summary>What the camera frames: BoardBounds, widened by BoardTheme.framePropPad per side unless IsWide.</summary>
        public Bounds FrameBounds { get; private set; }

        /// <summary>
        /// LevelCatalog world index of the built level, or -1 (no catalog, or the level is not in it). Set before
        /// <see cref="Built"/> is raised; TableView picks the world's table decor from it.
        /// </summary>
        public int WorldIndex { get; private set; } = -1;

        /// <summary>
        /// Figure rank (progression spec 2026-11-12): worlds completed, 0-5. Picks the base disc's material and size
        /// (BoardTheme.figureRanks). Applies at once to the figure on the board and to every later build; the UI sets
        /// it from Progress.
        /// </summary>
        /// <example><code>board.Rank = progress.CompletedWorlds(catalog.WorldLevelIds);</code></example>
        public int Rank
        {
            get => _rank;
            set
            {
                _rank = Mathf.Max(0, value);
                ApplyRank();
            }
        }

        /// <summary>
        /// World point just above the figure's head, where the tutorial guide's speech bubble points (follows every
        /// move). Zero before the first build.
        /// </summary>
        public Vector3 GuideAnchor => _figure != null ? _figure.Root.position + Vector3.up * (_figureTop + _theme.guideBubbleGap) : Vector3.zero;

        /// <summary>
        /// Shows the tutorial guide's pulsing marker on grid space (<paramref name="x"/>, <paramref name="y"/>) of the
        /// built level (spec tutorial-guide-and-tips-addition-2026-11-13). A rebuild removes it.
        /// </summary>
        /// <example><code>if (step.HasTarget) board.ShowGuide(step.TargetX, step.TargetY); else board.HideGuide();</code></example>
        public void ShowGuide(int x, int y)
        {
            if (_root == null || _builtLevel == null || _theme.guideMarkerMaterial == null) return;
            if (_guideMarker == null)
            {
                GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "GuideMarker";
                Destroy(disc.GetComponent<Collider>());
                var renderer = disc.GetComponent<Renderer>();
                renderer.sharedMaterial = _theme.guideMarkerMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                disc.transform.SetParent(_root, false);
                _guideMarker = disc.transform;
            }
            _guideMarker.localPosition = CellPosition(x, y) + Vector3.up * _theme.guideMarkerHeight;
            _guideMarker.gameObject.SetActive(true);
        }

        /// <summary>Hides the tutorial guide's marker.</summary>
        public void HideGuide()
        {
            if (_guideMarker != null) _guideMarker.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_controller != null) _controller.MoveBlocked += OnMoveBlocked;
        }

        private void OnDisable()
        {
            if (_controller != null) _controller.MoveBlocked -= OnMoveBlocked;
        }

        private void OnMoveBlocked(Direction direction)
        {
            GameState state = _controller.State;
            if (_figure == null || state == null || !ReferenceEquals(_controller.Level, _builtLevel)) return;
            int dx = direction == Direction.Left ? -1 : direction == Direction.Right ? 1 : 0;
            int dy = direction == Direction.Up ? -1 : direction == Direction.Down ? 1 : 0;
            _bumpDirection = new Vector3(dx, 0f, -dy); // grid y grows towards -Z
            _bumpT = 0f;
            _figure.Yaw = Mathf.Atan2(dx, -dy) * Mathf.Rad2Deg;

            if (_builtLevel.GetTile(state.X + dx, state.Y + dy) != Tile.Exit) return;
            foreach (PortalView portal in _portals)
            {
                if (portal.X == state.X + dx && portal.Y == state.Y + dy) portal.Flicker();
            }
        }

        private void Update()
        {
            Level level = _controller != null ? _controller.Level : null;
            if (level == null || _theme == null) return;
            if (!ReferenceEquals(level, _builtLevel)) Build(level);

            GameState state = _controller.State;
            if (state != null && !ReferenceEquals(state, _shown))
            {
                GameState previous = _shown;
                _shown = state;
                bool forward = previous != null && IsForwardStep(level, previous, state);
                Show(previous, state, forward);
                StateChanged?.Invoke(previous, state, forward);
                if (forward && state.IsDead && !_awaitingPotImpact) DeathImpact?.Invoke();
            }
            Animate(Time.deltaTime);
        }

        /// <summary>True if <paramref name="next"/> is the result of one legal move from <paramref name="previous"/>.</summary>
        public static bool IsForwardStep(Level level, GameState previous, GameState next)
        {
            for (int d = 0; d < 4; d++)
            {
                GameState candidate = StepRules.Step(level, previous, (Direction)d);
                if (!ReferenceEquals(candidate, previous) && candidate.Equals(next)) return true;
            }
            return false;
        }

        /// <summary>
        /// True if the forward step <paramref name="previous"/> -> <paramref name="next"/> had its counter reset by the
        /// black cat: both Playing, the counter became RuleSet.StartValue from another value, and the cat ended on the
        /// player's tile or the two swapped tiles (this tells it apart from the mirror countdown reaching StartValue).
        /// </summary>
        public static bool IsCatReset(Level level, GameState previous, GameState next, bool isForwardStep)
        {
            if (!isForwardStep || level.CatRouteLength == 0) return false;
            if (previous.Status != GameStatus.Playing || next.Status != GameStatus.Playing) return false;
            int start = level.Rules.StartValue;
            if (next.Counter != start || previous.Counter == start) return false;
            var oldCat = level.GetCatPosition(previous.CatIndex);
            var newCat = level.GetCatPosition(next.CatIndex);
            bool same = newCat.X == next.X && newCat.Y == next.Y;
            bool swapped = newCat.X == previous.X && newCat.Y == previous.Y && oldCat.X == next.X && oldCat.Y == next.Y;
            return same || swapped;
        }

        /// <summary>True if every cell on the grid's outer ring is a wall (it is then drawn as the board's rim).</summary>
        public static bool OuterRingIsWall(Level level)
        {
            for (int x = 0; x < level.Width; x++)
            {
                if (level.GetTile(x, 0) != Tile.Wall || level.GetTile(x, level.Height - 1) != Tile.Wall) return false;
            }
            for (int y = 0; y < level.Height; y++)
            {
                if (level.GetTile(0, y) != Tile.Wall || level.GetTile(level.Width - 1, y) != Tile.Wall) return false;
            }
            return true;
        }

        /// <summary>Local position of the top-centre of grid cell (x, y).</summary>
        public Vector3 CellPosition(int x, int y)
        {
            float t = _theme.tileSize;
            return new Vector3((x - (_builtLevel.Width - 1) * 0.5f) * t, 0f, ((_builtLevel.Height - 1) * 0.5f - y) * t);
        }

        private void OnDestroy()
        {
            DestroySpaceMeshes();
        }

        private void DestroySpaceMeshes()
        {
            foreach (Mesh mesh in _spaceMeshes) Destroy(mesh);
            _spaceMeshes.Clear();
        }

        private void Build(Level level)
        {
            if (_root != null) Destroy(_root.gameObject);
            _guideMarker = null; // destroyed with the old root at the end of this frame; ShowGuide makes a new one
            DestroySpaceMeshes();
            _bridges.Clear();
            _pots.Clear();
            _dice.Clear();
            _portals.Clear();
            _beads.Clear();
            _builtLevel = level;
            WorldIndex = _catalog != null ? _catalog.WorldIndexOf(_controller.CurrentLevelFile) : -1;
            _worldTint = WorldTint();
            _shown = null;
            _root = new GameObject("Board").transform;
            _root.SetParent(transform, false);

            bool ring = OuterRingIsWall(level) && level.Width > 2 && level.Height > 2;
            int inset = ring ? 1 : 0;
            BuildPlateAndRim(level, level.Width - 2 * inset, level.Height - 2 * inset);
            for (int y = inset; y < level.Height - inset; y++)
            {
                for (int x = inset; x < level.Width - inset; x++) BuildCell(level, x, y);
            }

            BuildFigure(level);
            _confetti = _portals.Count > 0 && _theme.confettiMaterial != null ? BuildConfetti() : null;
            _potDust = _pots.Count > 0 ? BuildCatPuff("Pot Dust") : null;
            _beadPuff = _beads.Count > 0 ? BuildCatPuff("Bead Puff") : null;

            _cat = null;
            _catBody = null;
            _catPuff = null;
            _routePrints.Clear();
            _highlightedPrint = (-1, -1);
            if (level.CatRouteLength > 0)
            {
                var c = level.GetCatPosition(0);
                var catRoot = new GameObject("Cat").transform;
                catRoot.SetParent(_root, false);
                catRoot.localPosition = CellPosition(c.X, c.Y);
                _catBody = ModelFit.Spawn(_theme.cat, catRoot, "Body", new Vector3(0f, _theme.catHeight, 0f), true);
                _cat = new Mover(catRoot) { Yaw = 180f };
                BuildCatRoute(level);
                _catPuff = BuildCatPuff("Cat Puff");
            }

            // Flowerpot ropes run up out of view; leave them out of the framing bounds (inactive renderers are skipped).
            foreach (PotView pot in _pots) pot.Rope.gameObject.SetActive(false);
            BoardBounds = ModelFit.WorldBounds(_root.gameObject);
            foreach (PotView pot in _pots) pot.Rope.gameObject.SetActive(true);
            IsWide = level.Width >= level.Height * _theme.wideBoardAspect;
            Bounds frame = BoardBounds;
            if (!IsWide) frame.Expand(new Vector3(2f * _theme.framePropPad, 0f, 0f));
            FrameBounds = frame;
            Built?.Invoke();
        }

        private void BuildPlateAndRim(Level level, int columns, int rows)
        {
            float w = columns * _theme.tileSize + _theme.plateMargin * 2f;
            float d = rows * _theme.tileSize + _theme.plateMargin * 2f;
            float strip = _theme.trackStripDepth;
            float north = d * 0.5f;
            float south = -d * 0.5f - strip; // the plate grows towards the camera to carry the score track
            float depth = north - south;
            float centreZ = (north + south) * 0.5f;
            // The plate is the slab model stretched to span PlateBottom..-SlabThickness, so the spaces sit on its top.
            const float plateTop = -SlabThickness;
            Transform plate = Place(_theme.floorTile, "Plate", new Vector3(0f, plateTop, centreZ));
            plate.localScale = new Vector3(w / SpaceFootprint, (plateTop - PlateBottom) / SlabThickness, depth / SpaceFootprint);
            SetMaterial(plate, _theme.boardPlate);
            SetShadows(plate, ShadowCastingMode.Off);

            float rw = _theme.rimWidth;
            float h = _theme.rimHeight - PlateBottom;
            Rim("Rim N", new Vector3(0f, PlateBottom, north + rw * 0.5f), new Vector3(w + 2f * rw, h, rw));
            Rim("Rim S", new Vector3(0f, PlateBottom, south - rw * 0.5f), new Vector3(w + 2f * rw, h, rw));
            Rim("Rim E", new Vector3((w + rw) * 0.5f, PlateBottom, centreZ), new Vector3(rw, h, depth));
            Rim("Rim W", new Vector3(-(w + rw) * 0.5f, PlateBottom, centreZ), new Vector3(rw, h, depth));

            BuildTrack(level.Rules, level.BeadCount, w, -d * 0.5f - strip * 0.5f);
        }

        /// <summary>
        /// Last count shown on the score track: the first deadly count from StartValue upwards, else the cap. With
        /// <paramref name="beads"/> evil-eye beads in the level, that many deadly counts can be survived, so the track
        /// runs past them to the next lucky count (the bead's reward), so the marker never parks on a passed 13.
        /// </summary>
        public static int TrackEnd(RuleSet rules, int cap, int beads = 0)
        {
            cap = Mathf.Min(cap, TrackAtlas.MaxNumber);
            bool survivedDeadly = false;
            for (int n = rules.StartValue; n <= cap; n++)
            {
                if (rules.IsDeadly(n))
                {
                    if (beads <= 0) return n;
                    beads--;
                    survivedDeadly = true;
                }
                else if (survivedDeadly && rules.IsLucky(n))
                {
                    return n;
                }
            }
            return Mathf.Max(rules.StartValue, cap);
        }

        /// <summary>Printed face of track count <paramref name="n"/>: deadly, else START on the first space, else lucky, else plain.</summary>
        public static TrackAtlas.Face TrackFace(RuleSet rules, int n, int trackStart)
        {
            if (rules.IsDeadly(n)) return TrackAtlas.Face.Deadly;
            if (n == trackStart) return TrackAtlas.Face.Start;
            return rules.IsLucky(n) ? TrackAtlas.Face.Lucky : TrackAtlas.Face.Plain;
        }

        private void BuildTrack(RuleSet rules, int beads, float boardWidth, float z)
        {
            _trackStart = Mathf.Clamp(rules.StartValue, 0, TrackAtlas.MaxNumber);
            _trackEnd = TrackEnd(rules, _theme.trackCap, beads);
            int count = _trackEnd - _trackStart + 1;
            float pitch = Mathf.Min(_theme.trackSpacing, (boardWidth - 0.2f) / count);
            float size = Mathf.Min(_theme.trackSpaceSize, pitch * 0.86f);
            const float blockTop = 0f; // block = one space slab standing on the plate top (-SlabThickness)
            _trackSpaces = new Vector3[count];
            var track = new GameObject("Score Track").transform;
            track.SetParent(_root, false);
            for (int i = 0; i < count; i++)
            {
                int n = _trackStart + i;
                bool deadly = rules.IsDeadly(n);
                bool lucky = !deadly && rules.IsLucky(n);
                var p = new Vector3((i - (count - 1) * 0.5f) * pitch, blockTop, z);
                _trackSpaces[i] = p;

                // The ink block shows as a thin printed border around the slightly smaller face.
                Transform block = Place(_theme.floorTile, "Space " + n, p, track);
                block.localScale = new Vector3(size / SpaceFootprint, 1f, size / SpaceFootprint);
                SetMaterial(block, _theme.trackBlock);
                SetShadows(block, ShadowCastingMode.Off);

                var face = new GameObject("Face " + n, typeof(MeshFilter), typeof(MeshRenderer));
                face.transform.SetParent(track, false);
                face.transform.localPosition = p + Vector3.up * 0.002f;
                face.GetComponent<MeshFilter>().sharedMesh = SpaceQuad(size * 0.9f, TrackAtlas.CellUV(n, TrackFace(rules, n, _trackStart)));
                var renderer = face.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = deadly ? _theme.trackDeadly : lucky ? _theme.trackLucky : _theme.trackSpace;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            Transform tokenRoot = new GameObject("Counter Token").transform;
            tokenRoot.SetParent(_root, false);
            tokenRoot.localPosition = TokenPosition(0);
            Transform token = Place(_theme.trackToken, "Token", Vector3.zero, tokenRoot);
            token.localScale = Vector3.one * _theme.tokenScale;
            SetMaterial(token, _theme.trackTokenMaterial);
            _token = new Mover(tokenRoot);
            _tokenBeyond = false;
        }

        private Vector3 TokenPosition(int spaceIndex)
        {
            // The token model is a centred flat disc about 0.05 thick.
            return _trackSpaces[spaceIndex] + Vector3.up * (0.03f * _theme.tokenScale + 0.004f);
        }

        /// <summary>Flat upward-facing quad of side <paramref name="size"/> showing atlas cell <paramref name="uv"/>; text reads from the near side.</summary>
        private Mesh SpaceQuad(float size, Vector4 uv)
        {
            float h = size * 0.5f;
            var mesh = new Mesh { name = "Track Space" };
            mesh.vertices = new[] { new Vector3(-h, 0f, -h), new Vector3(h, 0f, -h), new Vector3(h, 0f, h), new Vector3(-h, 0f, h) };
            mesh.uv = new[] { new Vector2(uv.x, uv.y), new Vector2(uv.z, uv.y), new Vector2(uv.z, uv.w), new Vector2(uv.x, uv.w) };
            mesh.triangles = new[] { 0, 3, 2, 0, 2, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _spaceMeshes.Add(mesh);
            return mesh;
        }

        private void Rim(string name, Vector3 position, Vector3 size)
        {
            Place(_theme.rimBar, name, position).localScale = size;
        }

        private void BuildCell(Level level, int x, int y)
        {
            Tile tile = level.GetTile(x, y);
            Vector3 p = CellPosition(x, y);
            string name = tile + " " + x + "," + y;

            bool hole = tile == Tile.Gap || tile == Tile.Bridge;
            Transform baseTile = Place(hole ? _theme.gapTile : _theme.floorTile, name, p);
            SetShadows(baseTile, ShadowCastingMode.Off);
            if (!hole) SetMaterial(baseTile, FloorMaterial(tile, x, y));

            switch (tile)
            {
                case Tile.Wall:
                    if (_theme.wallPieces == null || _theme.wallPieces.Length == 0) break;
                    int pick = (x * 7 + y * 13) % _theme.wallPieces.Length;
                    float yaw = ((x * 37 + y * 11) % 7 - 3) * 7f;
                    Transform piece = Place(_theme.wallPieces[pick], "Piece", Vector3.zero, baseTile);
                    piece.localRotation = Quaternion.Euler(0f, yaw, 0f);
                    piece.localScale = Vector3.Scale(piece.localScale, _theme.wallPieceScale);
                    if (!_theme.wallPieceShadows) SetShadows(piece, ShadowCastingMode.Off);
                    if (_theme.wallMutedFrom != null && _theme.wallMutedTo != null)
                    {
                        int n = Mathf.Min(_theme.wallMutedFrom.Length, _theme.wallMutedTo.Length);
                        for (int i = 0; i < n; i++) SwapMaterial(piece, _theme.wallMutedFrom[i], _theme.wallMutedTo[i]);
                    }
                    break;
                case Tile.Exit:
                    BuildPortal(x, y, baseTile);
                    break;
                case Tile.Spike:
                    Place(_theme.spike, "Spikes", Vector3.zero, baseTile);
                    break;
                case Tile.Dice:
                    // The die tumbles around its centre: a pivot at the centre carries the model.
                    Transform pivot = new GameObject("Die Pivot").transform;
                    pivot.SetParent(baseTile, false);
                    pivot.localPosition = Vector3.up * _theme.dieCenterHeight;
                    Place(_theme.die, "Die", Vector3.down * _theme.dieCenterHeight, pivot).localRotation =
                        Quaternion.Euler(0f, _theme.dieYaw, 0f);
                    _dice.Add(new DieView(x, y, pivot));
                    break;
                case Tile.Flowerpot:
                    Transform pot = ModelFit.Spawn(_theme.flowerpot, baseTile, "Pot", new Vector3(0f, _theme.flowerpotHeight, 0f), true);
                    SwapMaterial(pot, _theme.flowerpotBody, _theme.flowerpotGlaze);
                    pot.localPosition = new Vector3(0f, _theme.flowerpotHover, 0f);
                    _pots.Add(new PotView(x, y, pot, BuildPotRope(baseTile, pot)));
                    break;
                case Tile.Mirror:
                    Place(_theme.mirror, "Mirror", Vector3.zero, baseTile).localRotation =
                        Quaternion.Euler(_theme.mirrorLeanDegrees, _theme.mirrorYaw, _theme.mirrorSkewDegrees);
                    break;
                case Tile.Bridge:
                    BuildBridge(level, x, y, baseTile);
                    break;
                case Tile.Wood:
                    Place(_theme.woodPlanks, "Planks", Vector3.zero, baseTile);
                    break;
                case Tile.Bead:
                    Transform beadPivot = new GameObject("Bead Pivot").transform;
                    beadPivot.SetParent(baseTile, false);
                    Place(_theme.bead, "Bead", Vector3.zero, beadPivot);
                    _beads.Add(new BeadView(level.BeadIndex(x, y), beadPivot, (x * 0.37f + y * 0.61f) % 1f));
                    break;
            }
        }

        /// <summary>
        /// Hazard spaces (spike, dice, flowerpot) are crimson hazardTile; every other non-wall space follows the
        /// checkerboard of floorColors ((x + y) % length); walls sit on wood.
        /// </summary>
        private Material FloorMaterial(Tile tile, int x, int y)
        {
            if (tile == Tile.Wall) return _theme.wallTile;
            if ((tile == Tile.Spike || tile == Tile.Dice || tile == Tile.Flowerpot) && _theme.hazardTile != null) return _theme.hazardTile;
            if (_worldTint != null) return (x + y) % 2 == 0 ? _theme.floorLight : _worldTint;
            Material[] colours = _theme.floorColors;
            if (colours == null || colours.Length == 0) return _theme.floorLight;
            return colours[(x + y) % colours.Length];
        }

        /// <summary>The theme's tint for the current level's world, or null (no catalog, unknown level, no tint).</summary>
        private Material WorldTint()
        {
            Material[] tints = _theme.worldTints;
            if (_catalog == null || tints == null || tints.Length == 0) return null;
            return WorldIndex >= 0 ? tints[WorldIndex % tints.Length] : null;
        }

        private void BuildPortal(int x, int y, Transform baseTile)
        {
            Transform portal = Place(_theme.portalFrame, "Portal", Vector3.zero, baseTile);
            portal.localRotation = Quaternion.Euler(0f, _theme.portalYaw, 0f);
            Transform disc = Place(_theme.portalDisc, "Swirl", new Vector3(0f, _theme.portalDiscHeight, 0f), portal);
            SetShadows(disc, ShadowCastingMode.Off);

            var sparks = new GameObject("Sparks");
            sparks.transform.SetParent(portal, false);
            sparks.transform.localPosition = new Vector3(0f, _theme.portalDiscHeight, 0f);
            var ps = sparks.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.duration = 2f;
            main.startLifetime = 1.4f;
            main.startSpeed = 0.12f;
            main.startSize = 0.05f;
            main.maxParticles = Mathf.Max(1, _theme.portalSparkMax + _theme.winSparkBurst);
            main.gravityModifier = -0.04f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = _theme.portalSparkRate;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle; // circle in the local XY plane = the disc's plane
            shape.radius = 0.32f;
            shape.radiusThickness = 0f;
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var renderer = sparks.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _theme.portalSpark;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ps.Play();
            _portals.Add(new PortalView(x, y, disc, ps));
        }

        private void BuildBridge(Level level, int x, int y, Transform baseTile)
        {
            // Span along the row if both row neighbours are open, otherwise along the column.
            bool alongX = level.GetTile(x - 1, y) != Tile.Wall && level.GetTile(x + 1, y) != Tile.Wall;
            float half = _theme.tileSize * 0.5f;
            var span = new GameObject("Span").transform;
            span.SetParent(baseTile, false);
            span.localRotation = Quaternion.Euler(0f, alongX ? 0f : 90f, 0f);
            var hinge = new GameObject("Hinge").transform;
            hinge.SetParent(span, false);
            hinge.localPosition = new Vector3(-half, 0f, 0f);

            // Fit the longest horizontal side to the span length, then turn the long side along the span (local X).
            float length = _theme.bridgeLength;
            Transform deck = ModelFit.Spawn(_theme.bridge, null, "Deck", new Vector3(length, 0f, length), true);
            Bounds b = ModelFit.WorldBounds(deck.gameObject);
            deck.SetParent(hinge, false);
            deck.localRotation = b.size.z > b.size.x + 1e-3f ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            deck.localPosition = new Vector3(half, _theme.bridgeYOffset, 0f);

            Transform coin = Place(_theme.coin, "Coin", new Vector3(0f, _theme.coinHeight, 0f), baseTile);
            _bridges.Add(new BridgeView(level.BridgeIndex(x, y), hinge, coin, baseTile, _theme));
        }

        private void BuildCatRoute(Level level)
        {
            if (_theme.pawPrint == null) return;
            for (int i = 0; i < level.CatRouteLength; i++)
            {
                var tile = level.GetCatPosition(i);
                if (_routePrints.ContainsKey(tile)) continue;
                var next = level.GetCatPosition((i + 1) % level.CatRouteLength);
                float yaw = Mathf.Atan2(next.X - tile.X, tile.Y - next.Y) * Mathf.Rad2Deg + _theme.pawPrintYawOffset;
                Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
                var renderers = new List<Renderer>();
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 offset = rotation * new Vector3(side * _theme.pawPrintSpacing, 0f, side * 0.1f * _theme.tileSize);
                    Transform print = Place(_theme.pawPrint, "Paw " + tile.X + "," + tile.Y,
                        CellPosition(tile.X, tile.Y) + offset + Vector3.up * 0.002f);
                    print.localRotation = rotation;
                    print.localScale = Vector3.one * _theme.pawPrintScale;
                    SetMaterial(print, _theme.pawPrintMaterial);
                    SetShadows(print, ShadowCastingMode.Off);
                    renderers.AddRange(print.GetComponentsInChildren<Renderer>());
                }
                _routePrints[tile] = renderers.ToArray();
            }
        }

        /// <summary>Builds a one-shot puff emitter (cat reset, flowerpot crash) from the theme's puff settings.</summary>
        private ParticleSystem BuildCatPuff(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = _theme.catPuffLifetime;
            main.startSpeed = _theme.catPuffSpeed;
            main.startSize = _theme.catPuffSize;
            main.maxParticles = Mathf.Max(1, _theme.catPuffCount * 2);
            main.gravityModifier = -0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _theme.catPuffMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ps.Play();
            return ps;
        }

        private void HighlightNextCatTile(GameState state)
        {
            var next = _builtLevel.GetCatPosition((state.CatIndex + 1) % _builtLevel.CatRouteLength);
            if (next == _highlightedPrint) return;
            if (_routePrints.TryGetValue(_highlightedPrint, out Renderer[] old))
            {
                foreach (Renderer r in old) r.sharedMaterial = _theme.pawPrintMaterial;
            }
            if (_routePrints.TryGetValue(next, out Renderer[] now))
            {
                foreach (Renderer r in now) r.sharedMaterial = _theme.pawPrintNextMaterial;
            }
            _highlightedPrint = next;
        }

        private void BuildFigure(Level level)
        {
            Transform figure = ModelFit.Spawn(_theme.character, _root, "Figure", new Vector3(0f, _theme.characterHeight, 0f), true);
            figure.localPosition = CellPosition(level.StartX, level.StartY);
            _figure = new Mover(figure) { Yaw = 180f };
            FigureRenderers = figure.GetComponentsInChildren<Renderer>();
            _figureTilt = _figureTiltTarget = 0f;
            _figureScale = figure.localScale;
            _figureFx = FigureFx.None;
            _figureFxT = _bumpT = 1f;

            // Base disc: a flat cylinder under the feet (child of the figure, so it follows every move, tilt and fall).
            // Its material and width come from the rank (ApplyRank).
            _baseDisc = null;
            _figureFit = Mathf.Max(0.0001f, figure.lossyScale.x);
            if (_theme.figureBase != null || (_theme.figureRanks != null && _theme.figureRanks.Length > 0))
            {
                GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "Base";
                Destroy(disc.GetComponent<Collider>());
                disc.transform.SetParent(figure, false);
                _baseDisc = disc.GetComponent<Renderer>();
            }

            // Head height above the figure's root, for the guide bubble's anchor.
            Bounds body = FigureRenderers.Length > 0 ? FigureRenderers[0].bounds : new Bounds(figure.position, Vector3.zero);
            foreach (Renderer r in FigureRenderers) body.Encapsulate(r.bounds);
            _figureTop = body.max.y - figure.position.y;
            ApplyRank();

            _figureAnimation = figure.GetComponentInChildren<Animation>();
            _idleClip = FindClip(_figureAnimation, _theme.characterIdleClip);
            _walkClip = FindClip(_figureAnimation, _theme.characterWalkClip);
            _figureWalking = false;
            if (_idleClip != null) _figureAnimation.Play(_idleClip);
        }

        /// <summary>Puts the rank's material and width on the base disc.</summary>
        private void ApplyRank()
        {
            if (_theme == null) return;
            if (_baseDisc != null)
            {
                Material[] ranks = _theme.figureRanks;
                Material m = ranks != null && ranks.Length > 0 ? ranks[Mathf.Min(_rank, ranks.Length - 1)] : _theme.figureBase;
                if (m != null) _baseDisc.sharedMaterial = m;
                float d = (_theme.figureBaseDiameter + _theme.figureBaseRankGrowth * _rank) / _figureFit;
                float h = _theme.figureBaseHeight * 0.5f / _figureFit;
                _baseDisc.transform.localScale = new Vector3(d, h, d);
                _baseDisc.transform.localPosition = new Vector3(0f, h, 0f);
            }
        }

        /// <summary>One-shot confetti emitter for the win (pieces in the theme's colours, falling with gravity).</summary>
        private ParticleSystem BuildConfetti()
        {
            var go = new GameObject("Confetti");
            go.transform.SetParent(_root, false);
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // the cone opens upwards
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(_theme.confettiLifetime * 0.7f, _theme.confettiLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(_theme.confettiSpeed * 0.6f, _theme.confettiSpeed);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(_theme.confettiSize * 0.7f, _theme.confettiSize);
            main.startSizeY = new ParticleSystem.MinMaxCurve(_theme.confettiSize * 0.35f, _theme.confettiSize * 0.5f);
            main.startSizeZ = 1f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
            main.maxParticles = Mathf.Max(1, _theme.confettiBurst);
            main.gravityModifier = _theme.confettiGravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            Color[] colors = _theme.confettiColors;
            if (colors != null && colors.Length > 0)
            {
                // Fixed-mode gradient sampled at random: every piece takes one of the listed colours exactly.
                int n = Mathf.Min(8, colors.Length);
                var keys = new GradientColorKey[n];
                for (int i = 0; i < n; i++) keys[i] = new GradientColorKey(colors[i], (i + 1f) / n);
                var gradient = new Gradient { mode = GradientMode.Fixed };
                gradient.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f) });
                main.startColor = new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
            }
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = 0.15f;
            ParticleSystem.RotationOverLifetimeModule spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            ParticleSystem.LimitVelocityOverLifetimeModule drag = ps.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.limit = 1000f; // drag only: no speed cap (the module caps at 1 unit/s by default)
            drag.dampen = 0f;
            drag.multiplyDragByParticleSize = false;
            drag.drag = 0.9f; // paper: slows quickly and flutters down
            // Opaque material: pieces shrink away at the end instead of fading.
            ParticleSystem.SizeOverLifetimeModule shrink = ps.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0f)));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _theme.confettiMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ps.Play();
            return ps;
        }

        /// <summary>Name of the first legacy clip whose name contains <paramref name="part"/> (set to loop), or null.</summary>
        private static string FindClip(Animation animation, string part)
        {
            if (animation == null || string.IsNullOrEmpty(part)) return null;
            foreach (AnimationState state in animation)
            {
                if (state.name.IndexOf(part, StringComparison.OrdinalIgnoreCase) < 0) continue;
                state.wrapMode = WrapMode.Loop;
                return state.name;
            }
            return null;
        }

        private void Show(GameState previous, GameState state, bool isForwardStep)
        {
            Level level = _builtLevel;
            bool snap = previous == null;
            Vector3 target = CellPosition(state.X, state.Y);
            Tile landing = level.GetTile(state.X, state.Y);
            if (landing == Tile.Dice) target.y += _theme.dieStandHeight;
            _figure.MoveTo(target, snap, _theme);

            // Died in a hole: a gap, or a bridge that is still closed in this state. Other deaths tip over.
            bool inHole = state.IsDead && (landing == Tile.Gap || (landing == Tile.Bridge
                && (state.OpenBridges & (1UL << level.BridgeIndex(state.X, state.Y))) == 0));
            _figureFx = inHole ? FigureFx.Fall : state.IsWon ? FigureFx.Win : FigureFx.None;
            _figureFxT = isForwardStep ? 0f : 1f;

            int space = Mathf.Clamp(state.Counter - _trackStart, 0, _trackSpaces.Length - 1);
            _tokenBeyond = state.Counter > _trackEnd;
            _token.MoveTo(TokenPosition(space), snap, _theme.tokenMoveSeconds,
                Mathf.Max(0.01f, Mathf.Abs(_trackSpaces.Length > 1 ? _trackSpaces[1].x - _trackSpaces[0].x : 1f)), _theme.tokenHopHeight);
            _figureTiltTarget = state.IsDead && !inHole ? _theme.deadTiltDegrees : 0f;
            _awaitingPotImpact = isForwardStep && state.IsDead && landing == Tile.Flowerpot;
            if (!isForwardStep)
            {
                _figureTilt = _figureTiltTarget;
                _bumpT = 1f;
            }

            if (_cat != null)
            {
                var c = level.GetCatPosition(state.CatIndex);
                Vector3 catTarget = CellPosition(c.X, c.Y);
                _cat.MoveTo(catTarget, snap, _theme);
                HighlightNextCatTile(state);
                if (!snap && IsCatReset(level, previous, state, isForwardStep))
                {
                    _catResetT = 0f;
                    if (_catPuff != null)
                    {
                        _catPuff.transform.position = _root.TransformPoint(catTarget + Vector3.up * (_theme.catHeight * 0.5f));
                        _catPuff.Emit(Mathf.Max(1, _theme.catPuffCount));
                    }
                }
            }

            foreach (BridgeView bridge in _bridges)
            {
                bridge.Open = (state.OpenBridges & (1UL << bridge.Index)) != 0;
                if (!isForwardStep)
                {
                    bridge.Angle = bridge.Open ? 0f : _theme.bridgeClosedAngle;
                    bridge.CancelToss();
                }
            }

            // Evil-eye beads: a bead taken on this forward step flies up; undo and restart put taken/untaken at once.
            int pickedNow = 0;
            foreach (BeadView bead in _beads)
            {
                ulong beadBit = 1UL << bead.Index;
                bool taken = (state.TakenBeads & beadBit) != 0;
                bool takenBefore = previous != null && (previous.TakenBeads & beadBit) != 0;
                if (isForwardStep && taken && !takenBefore)
                {
                    bead.Pick();
                    pickedNow++;
                }
                else bead.SetTaken(taken);
            }
            // A bead broke on this step (it warded off a death): a puff where the figure stands.
            if (isForwardStep && _beadPuff != null && state.Beads < previous.Beads + pickedNow)
            {
                _beadPuff.transform.position = _root.TransformPoint(CellPosition(state.X, state.Y) + Vector3.up * _theme.beadWardPuffHeight);
                _beadPuff.Emit(Mathf.Max(1, _theme.catPuffCount));
            }

            // The exit lets the pawn through only on a lucky step: show whether the next step would be one.
            int nextCount = state.CountingDown ? state.Counter - 1 : state.Counter + 1;
            bool exitOpen = state.IsWon
                || (state.Status == GameStatus.Playing && !level.Rules.IsDeadly(nextCount) && level.Rules.IsLucky(nextCount));
            foreach (PortalView portal in _portals)
            {
                portal.Open = exitOpen;
                if (!isForwardStep) portal.SnapOpenness();
            }

            // Dice roll, coin toss and flowerpot fall react to the step StepRules already computed.
            if (!isForwardStep)
            {
                foreach (DieView die in _dice) die.Reset();
                foreach (PotView pot in _pots) pot.Reset(_theme);
                foreach (PortalView portal in _portals) portal.CancelFlicker();
            }
            else
            {
                int dx = Math.Sign(state.X - previous.X);
                int dy = Math.Sign(state.Y - previous.Y);
                int count = previous.CountingDown ? previous.Counter - 1 : previous.Counter + 1;
                bool lucky = !level.Rules.IsDeadly(count) && level.Rules.IsLucky(count);
                foreach (DieView die in _dice)
                {
                    if (die.X == previous.X + dx && die.Y == previous.Y + dy) die.Roll(lucky);
                }

                // A lucky step opens every closed bridge at once: each one tosses its coin and lowers together.
                // Landing on a closed bridge also tosses its coin (on a normal step the toss fails and it stays up).
                ulong opened = state.OpenBridges & ~previous.OpenBridges;
                int landedIndex = landing == Tile.Bridge ? level.BridgeIndex(state.X, state.Y) : -1;
                foreach (BridgeView bridge in _bridges)
                {
                    ulong bit = 1UL << bridge.Index;
                    bool landedClosed = bridge.Index == landedIndex && (previous.OpenBridges & bit) == 0;
                    if ((opened & bit) != 0 || landedClosed) bridge.Toss();
                }

                if (landing == Tile.Flowerpot)
                {
                    foreach (PotView pot in _pots)
                    {
                        if (pot.X == state.X && pot.Y == state.Y) pot.Fall(_theme, state.IsDead);
                    }
                }
            }
        }

        private void Animate(float dt)
        {
            // Hole fall and win start once the step's hop has landed; the bump plays at once, on top of any hop.
            Vector3 offset = Vector3.zero;
            float fxScale = 1f;
            float fxYaw = 0f;
            if (_figureFx != FigureFx.None)
            {
                bool wasRunning = _figureFxT < 1f;
                float seconds = _figureFx == FigureFx.Fall ? _theme.holeFallSeconds : _theme.winSeconds;
                if (!_figure.IsMoving) _figureFxT = Mathf.Min(1f, _figureFxT + dt / Mathf.Max(0.01f, seconds));
                float u = _figureFxT;
                if (_figureFx == FigureFx.Fall)
                {
                    offset.y = -_theme.holeFallDepth * u * u; // accelerating, like gravity
                    fxScale = Mathf.Lerp(1f, _theme.holeFallEndScale, u);
                }
                else
                {
                    // Rise to the disc centre with a little arc, spinning faster as it shrinks into the swirl.
                    offset.y = _theme.portalDiscHeight * Mathf.Sin(0.5f * Mathf.PI * u) + _theme.winHopHeight * Mathf.Sin(Mathf.PI * u);
                    fxScale = 1f - u * u;
                    fxYaw = 360f * _theme.winSpinTurns * u * u;
                    if (wasRunning && u >= 1f)
                    {
                        foreach (PortalView portal in _portals)
                        {
                            if (portal.X != _shown.X || portal.Y != _shown.Y) continue;
                            portal.Burst(_theme);
                            if (_confetti != null)
                            {
                                _confetti.transform.position = portal.BurstPoint;
                                _confetti.Emit(Mathf.Max(1, _theme.confettiBurst));
                            }
                        }
                    }
                }
            }
            if (_bumpT < 1f)
            {
                _bumpT = Mathf.Min(1f, _bumpT + dt / Mathf.Max(0.01f, _theme.bumpSeconds));
                // sqrt(t): a quick push out and a slower spring back.
                offset += _bumpDirection * (_theme.bumpDistance * _theme.tileSize * Mathf.Sin(Mathf.PI * Mathf.Sqrt(_bumpT)));
            }
            _figure.Offset = offset;
            _figure.Tick(dt);
            _figure.Root.localScale = _figureScale * fxScale;
            // Guide marker: a flat gold disc that breathes, so the eye finds the next space.
            if (_guideMarker != null && _guideMarker.gameObject.activeSelf)
            {
                float d = _theme.guideMarkerDiameter * (1f + _theme.guideMarkerPulse * Mathf.Sin(Time.time * _theme.guideMarkerPulseSpeed));
                _guideMarker.localScale = new Vector3(d, _theme.guideMarkerThickness * 0.5f, d);
            }

            _token.Tick(dt);
            float pulse = _tokenBeyond ? 1f + _theme.tokenPulseAmount * Mathf.Abs(Mathf.Sin(Time.time * _theme.tokenPulseSpeed)) : 1f;
            _token.Root.localScale = Vector3.one * pulse;
            float tiltSpeed = _theme.deadTiltDegrees / Mathf.Max(0.01f, _theme.deadTiltSeconds);
            _figureTilt = Mathf.MoveTowards(_figureTilt, _figureTiltTarget, tiltSpeed * dt);
            _figure.Root.localRotation = Quaternion.Euler(0f, _figure.Yaw + _theme.characterYawOffset + fxYaw, 0f)
                * Quaternion.Euler(0f, 0f, -_figureTilt);
            UpdateFigureClip();

            if (_cat != null)
            {
                _cat.Tick(dt);
                _cat.Root.localRotation = Quaternion.Euler(0f, _cat.Yaw + _theme.catYawOffset, 0f);
                _catResetT = Mathf.Min(1f, _catResetT + dt / Mathf.Max(0.01f, _theme.catResetSeconds));
                float k = _catResetT < 1f ? Mathf.Sin(Mathf.PI * _catResetT) : 0f;
                _catBody.localPosition = Vector3.up * (_theme.catResetHopHeight * k);
                _catBody.localRotation = Quaternion.Euler(-_theme.catResetArchDegrees * k, 0f, 0f);
            }

            float lowerSpeed = _theme.bridgeClosedAngle / Mathf.Max(0.01f, _theme.bridgeLowerSeconds);
            float spin = _theme.coinSpinSpeed * Time.time;
            foreach (BridgeView bridge in _bridges) bridge.Tick(dt, lowerSpeed, _theme.bridgeClosedAngle, spin);

            foreach (DieView die in _dice) die.Tick(dt, _theme);
            foreach (BeadView bead in _beads) bead.Tick(dt, _theme, Time.time);

            for (int i = 0; i < _pots.Count; i++)
            {
                if (!_pots[i].Tick(dt, _theme, i)) continue;
                if (_potDust != null)
                {
                    _potDust.transform.position = _pots[i].Position;
                    _potDust.Emit(Mathf.Max(1, _theme.catPuffCount));
                }
                if (_awaitingPotImpact && _pots[i].Hit)
                {
                    _awaitingPotImpact = false;
                    DeathImpact?.Invoke();
                }
            }

            foreach (PortalView portal in _portals) portal.Tick(dt, _theme);
        }

        private void UpdateFigureClip()
        {
            if (_figureAnimation == null) return;
            bool walking = _figure.IsMoving && _walkClip != null;
            if (walking == _figureWalking) return;
            _figureWalking = walking;
            string clip = walking ? _walkClip : _idleClip;
            if (clip != null) _figureAnimation.CrossFade(clip, 0.1f);
        }

        private Transform Place(GameObject prefab, string name, Vector3 localPosition, Transform parent = null)
        {
            GameObject go = Instantiate(prefab, parent != null ? parent : _root, false);
            go.name = name;
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static void SetMaterial(Transform t, Material material)
        {
            if (material == null) return;
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>()) r.sharedMaterial = material;
        }

        /// <summary>Replaces one material with another on every renderer under t, leaving the other slots alone.</summary>
        private static void SwapMaterial(Transform t, Material from, Material to)
        {
            if (from == null || to == null) return;
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != from) continue;
                    materials[i] = to;
                    changed = true;
                }
                if (changed) r.sharedMaterials = materials;
            }
        }

        private static void SetShadows(Transform t, ShadowCastingMode mode)
        {
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = mode;
        }

        /// <summary>Smooth hop from one cell to another.</summary>
        private sealed class Mover
        {
            private Vector3 _from;
            private Vector3 _to;
            private Vector3 _position;
            private float _t = 1f;
            private float _duration = 0.01f;
            private float _hop;

            public Mover(Transform root)
            {
                Root = root;
                _from = _to = _position = root.localPosition;
            }

            public Transform Root { get; }
            public float Yaw { get; set; }
            public bool IsMoving => _t < 1f;

            /// <summary>Added to the hop position on every Tick (fall, win hop, bump); not part of the move itself.</summary>
            public Vector3 Offset { get; set; }

            public void MoveTo(Vector3 target, bool snap, BoardTheme theme)
            {
                MoveTo(target, snap, theme.moveSeconds, theme.tileSize, theme.hopHeight);
            }

            /// <summary>Moves to target in secondsPerUnit * sqrt(distance / unit) seconds with a hop of the given height.</summary>
            public void MoveTo(Vector3 target, bool snap, float secondsPerUnit, float unit, float hop)
            {
                _hop = hop;
                Vector3 delta = target - _position;
                if (new Vector2(delta.x, delta.z).sqrMagnitude > 1e-4f) Yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                _from = _position;
                _to = target;
                float tiles = Mathf.Max(1f, delta.magnitude / Mathf.Max(0.01f, unit));
                _duration = Mathf.Max(0.01f, secondsPerUnit * Mathf.Sqrt(tiles));
                _t = snap ? 1f : 0f;
                if (!snap) return;
                _position = target;
                Root.localPosition = target + Offset;
            }

            public void Tick(float dt)
            {
                if (_t < 1f)
                {
                    _t = Mathf.Min(1f, _t + dt / _duration);
                    float s = Mathf.SmoothStep(0f, 1f, _t);
                    _position = Vector3.Lerp(_from, _to, s) + Vector3.up * (_hop * Mathf.Sin(_t * Mathf.PI));
                }
                Root.localPosition = _position + Offset;
            }
        }

        /// <summary>Six-sided die on a dice tile: tumbles in an arc and comes to rest with 6 up (lucky) or 1 up.</summary>
        private sealed class DieView
        {
            private readonly Transform _pivot;
            private readonly float _restY;
            private float _t = 1f;
            private Quaternion _rest = Quaternion.identity;

            public DieView(int x, int y, Transform pivot)
            {
                X = x;
                Y = y;
                _pivot = pivot;
                _restY = pivot.localPosition.y;
            }

            public int X { get; }
            public int Y { get; }

            /// <summary>Starts a roll; the die ends with 6 up if <paramref name="lucky"/>, else 1 up (opposite faces sum to 7).</summary>
            public void Roll(bool lucky)
            {
                _t = 0f;
                _rest = lucky ? Quaternion.AngleAxis(180f, Vector3.right) : Quaternion.identity;
            }

            /// <summary>Instantly back to the resting pose (1 up).</summary>
            public void Reset()
            {
                _t = 1f;
                _rest = Quaternion.identity;
                _pivot.localRotation = _rest;
                _pivot.localPosition = Vector3.up * _restY;
            }

            public void Tick(float dt, BoardTheme theme)
            {
                if (_t >= 1f) return;
                _t = Mathf.Min(1f, _t + dt / Mathf.Max(0.01f, theme.dieRollSeconds));
                float remaining = 1f - _t;
                float spin = 360f * theme.dieRollTurns * remaining * remaining;
                _pivot.localRotation = Quaternion.AngleAxis(spin, new Vector3(1f, 0f, 0.6f).normalized) * _rest;
                _pivot.localPosition = Vector3.up * (_restY + theme.dieRollHeight * Mathf.Sin(_t * Mathf.PI));
            }
        }

        /// <summary>
        /// Hangs a flowerpot on a rope so its hover reads as hanging: three short strings from the pot rim to a knot
        /// above the plant (children of the pot, they fall with it) and one long rope from the knot up out of view.
        /// Returns the rope's pivot at its top end; PotView swings it as a pendulum and the pot hangs from its end.
        /// </summary>
        private Transform BuildPotRope(Transform tile, Transform pot)
        {
            float h = _theme.flowerpotHeight;
            float scale = pot.localScale.x; // ModelFit scales the holder uniformly
            Vector3 knot = new Vector3(0f, h * _theme.potKnotHeight, 0f);
            for (int i = 0; i < 3; i++)
            {
                float a = (30f + 120f * i) * Mathf.Deg2Rad;
                Vector3 rim = new Vector3(Mathf.Cos(a) * h * _theme.potRimRadius, h * _theme.potRimHeight, Mathf.Sin(a) * h * _theme.potRimRadius);
                Transform str = RopeSegment(pot, "String " + i, rim, knot);
                str.localPosition /= scale;
                str.localScale /= scale;
            }

            var pivot = new GameObject("Pot Rope").transform;
            pivot.SetParent(tile, false);
            pivot.localPosition = new Vector3(0f, _theme.flowerpotHover + knot.y + _theme.ropeLength, 0f);
            RopeSegment(pivot, "Rope", Vector3.zero, Vector3.down * _theme.ropeLength);
            return pivot;
        }

        /// <summary>Thin cylinder from <paramref name="from"/> to <paramref name="to"/> in <paramref name="parent"/>'s (unscaled) space.</summary>
        private Transform RopeSegment(Transform parent, string name, Vector3 from, Vector3 to)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>());
            go.name = name;
            go.GetComponent<Renderer>().sharedMaterial = _theme.ropeMaterial;
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Transform t = go.transform;
            t.SetParent(parent, false);
            Vector3 d = to - from;
            t.localPosition = (from + to) * 0.5f;
            t.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            t.localScale = new Vector3(_theme.ropeThickness, d.magnitude * 0.5f, _theme.ropeThickness); // the primitive is 2 tall
            return t;
        }

        /// <summary>Flowerpot hanging on a rope over its tile; falls onto the pawn (death) or beside it and shatters (lucky).</summary>
        private sealed class PotView
        {
            private readonly Transform _pot;
            private readonly Transform _rope;
            private bool _fallen;
            private bool _hit;
            private float _fall = 1f;

            public PotView(int x, int y, Transform pot, Transform rope)
            {
                X = x;
                Y = y;
                _pot = pot;
                _rope = rope;
            }

            public int X { get; }
            public int Y { get; }
            public Vector3 Position => _pot.position;

            /// <summary>The rope's pivot at its top end (the long rope only; the hanger strings belong to the pot).</summary>
            public Transform Rope => _rope;

            /// <summary>True when the current or last fall lands on the pawn (a death), false when it misses.</summary>
            public bool Hit => _hit;

            /// <summary>Starts the fall from the hover height; <paramref name="hit"/> lands it on the pawn, else beside it.</summary>
            public void Fall(BoardTheme theme, bool hit)
            {
                Reset(theme);
                _fallen = true;
                _hit = hit;
                _fall = 0f;
            }

            /// <summary>Instantly back to hovering.</summary>
            public void Reset(BoardTheme theme)
            {
                _fallen = false;
                _fall = 1f;
                _pot.gameObject.SetActive(true);
                _pot.localPosition = new Vector3(0f, theme.flowerpotHover, 0f);
                _pot.localRotation = Quaternion.identity;
                _rope.localRotation = Quaternion.identity;
            }

            /// <summary>Sways while hovering or advances the fall. Returns true on the frame the pot hits the ground or the pawn.</summary>
            public bool Tick(float dt, BoardTheme theme, int index)
            {
                if (!_fallen)
                {
                    float t = Time.time * theme.flowerpotSwaySpeed;
                    float a = theme.flowerpotSwayDegrees;
                    // Pendulum: the rope swings around its top end and the pot hangs from its bottom end.
                    Quaternion swing = Quaternion.Euler(Mathf.Sin(t + index) * a, 0f, Mathf.Cos(t * 0.8f + index) * a * 0.5f);
                    float hang = _rope.localPosition.y - theme.flowerpotHover;
                    _rope.localRotation = swing;
                    _pot.localPosition = _rope.localPosition + swing * (Vector3.down * hang);
                    _pot.localRotation = swing;
                    return false;
                }
                if (_fall >= 1f) return false;
                _fall = Mathf.Min(1f, _fall + dt / Mathf.Max(0.01f, theme.potFallSeconds));
                float g = _fall * _fall; // accelerating, like gravity
                Vector3 end = _hit ? new Vector3(0f, theme.potHitHeight, 0f) : new Vector3(theme.potMissOffset, 0f, 0f);
                _pot.localPosition = Vector3.Lerp(new Vector3(0f, theme.flowerpotHover, 0f), end, g);
                _pot.localRotation = Quaternion.Euler(0f, 0f, _hit ? 0f : -theme.potTumbleDegrees * g);
                if (_fall < 1f) return false;
                if (!_hit) _pot.gameObject.SetActive(false); // shattered
                return true;
            }
        }

        /// <summary>
        /// Exit portal: spinning swirl disc and sparks; flickers when a move into the closed exit is blocked. Shows
        /// whether the exit is open for the next step: full size, fast spin and sparks when it is, small and slow when not.
        /// </summary>
        private sealed class PortalView
        {
            private readonly Transform _disc;
            private readonly Vector3 _discScale;
            private readonly ParticleSystem _sparks;
            private float _flicker = 1f;
            private float _angle;
            private float _openness = 1f;
            private float _shownRate = -1f;

            public PortalView(int x, int y, Transform disc, ParticleSystem sparks)
            {
                X = x;
                Y = y;
                _disc = disc;
                _discScale = disc.localScale;
                _sparks = sparks;
            }

            public int X { get; }
            public int Y { get; }

            /// <summary>True when the next step would be lucky, so the exit would let the pawn through.</summary>
            public bool Open { get; set; }

            /// <summary>Jumps to the current open/closed look without easing (first state, undo, restart).</summary>
            public void SnapOpenness() => _openness = Open ? 1f : 0f;

            /// <summary>Starts (or restarts) the closed-door flicker.</summary>
            public void Flicker() => _flicker = 0f;

            /// <summary>Ends a flicker at once; the disc is back to full size on the next Tick.</summary>
            public void CancelFlicker() => _flicker = 1f;

            /// <summary>World position of the swirl's centre (where the win confetti starts).</summary>
            public Vector3 BurstPoint => _disc.position;

            /// <summary>Sparks fly out of the ring in one burst (the figure has gone through).</summary>
            public void Burst(BoardTheme theme)
            {
                ParticleSystem.MainModule main = _sparks.main;
                ParticleSystem.MinMaxCurve speed = main.startSpeed;
                ParticleSystem.MinMaxCurve size = main.startSize;
                ParticleSystem.MinMaxCurve lifetime = main.startLifetime;
                main.startSpeed = theme.winSparkSpeed;
                main.startSize = theme.winSparkSize;
                main.startLifetime = theme.winSparkLifetime;
                _sparks.Emit(Mathf.Max(1, theme.winSparkBurst));
                main.startSpeed = speed;
                main.startSize = size;
                main.startLifetime = lifetime;
            }

            public void Tick(float dt, BoardTheme theme)
            {
                _openness = Mathf.MoveTowards(_openness, Open ? 1f : 0f, dt / Mathf.Max(0.01f, theme.portalOpenSeconds));
                float k = Mathf.SmoothStep(0f, 1f, _openness);
                float shrink = 0f;
                if (_flicker < 1f)
                {
                    _flicker = Mathf.Min(1f, _flicker + dt / Mathf.Max(0.01f, theme.portalBlockedSeconds));
                    shrink = theme.portalBlockedShrink * (1f - _flicker)
                        * Mathf.Abs(Mathf.Sin(Mathf.PI * theme.portalBlockedPulses * _flicker));
                }
                _angle = Mathf.Repeat(_angle + theme.portalSpinSpeed * Mathf.Lerp(theme.portalClosedSpin, 1f, k) * dt, 360f);
                _disc.localRotation = Quaternion.Euler(0f, 0f, _angle);
                _disc.localScale = _discScale * (Mathf.Lerp(theme.portalClosedScale, 1f, k) * (1f - shrink));

                float rate = theme.portalSparkRate * Mathf.Lerp(theme.portalClosedSparks, 1f, k);
                if (!Mathf.Approximately(rate, _shownRate))
                {
                    ParticleSystem.EmissionModule emission = _sparks.emission;
                    emission.rateOverTime = rate;
                    _shownRate = rate;
                }
            }
        }

        /// <summary>Evil-eye bead waiting on its space: hovers, bobs and sways; flies up and shrinks away when picked up.</summary>
        private sealed class BeadView
        {
            private readonly Transform _pivot;
            private readonly float _phase;
            private float _pickT = 1f;
            private bool _taken;

            public BeadView(int index, Transform pivot, float phase)
            {
                Index = index;
                _pivot = pivot;
                _phase = phase;
            }

            /// <summary>The bead's bit in GameState.TakenBeads.</summary>
            public int Index { get; }

            /// <summary>Shows the bead waiting (false) or gone (true) at once.</summary>
            public void SetTaken(bool taken)
            {
                _taken = taken;
                _pickT = 1f;
                if (_pivot.gameObject.activeSelf == taken) _pivot.gameObject.SetActive(!taken);
            }

            /// <summary>Starts the pick-up animation; the bead hides when it ends.</summary>
            public void Pick()
            {
                _taken = true;
                _pickT = 0f;
                if (!_pivot.gameObject.activeSelf) _pivot.gameObject.SetActive(true);
            }

            /// <summary>Hover, bob and sway while waiting; rise and shrink during a pick-up (hidden when it ends).</summary>
            public void Tick(float dt, BoardTheme theme, float time)
            {
                if (!_pivot.gameObject.activeSelf) return;
                float y = theme.beadHover + theme.beadBob * Mathf.Sin((time * theme.beadBobSpeed + _phase) * 2f * Mathf.PI);
                float scale = 1f;
                if (_taken)
                {
                    _pickT = Mathf.Min(1f, _pickT + dt / Mathf.Max(0.01f, theme.beadPickupSeconds));
                    if (_pickT >= 1f)
                    {
                        _pivot.gameObject.SetActive(false);
                        return;
                    }
                    y += theme.beadPickupRise * _pickT;
                    scale = 1f - _pickT * _pickT;
                }
                _pivot.localPosition = new Vector3(0f, y, 0f);
                _pivot.localRotation = Quaternion.Euler(0f, theme.beadSwayDegrees * Mathf.Sin(time * theme.beadSwaySpeed + _phase * 6.28f), 0f);
                _pivot.localScale = new Vector3(scale, scale, scale);
            }
        }

        /// <summary>Drawbridge-style coin bridge: raised while closed, lowered once opened (after the coin toss).</summary>
        private sealed class BridgeView
        {
            private readonly Transform _hinge;
            private readonly Transform _coin;
            private readonly Transform _edge;
            private readonly BoardTheme _theme;
            private float _toss = 1f;
            private bool _edgeHazard;

            public BridgeView(int index, Transform hinge, Transform coin, Transform edge, BoardTheme theme)
            {
                Index = index;
                _hinge = hinge;
                _coin = coin;
                _edge = edge;
                _theme = theme;
                Angle = theme.bridgeClosedAngle;
                SetEdge(true);
            }

            public int Index { get; }
            public bool Open { get; set; }
            public float Angle { get; set; }

            /// <summary>Starts the coin toss (arc and flip); a bridge that opens only lowers once the coin has landed.</summary>
            public void Toss() => _toss = 0f;

            /// <summary>Ends a toss at once, coin at rest.</summary>
            public void CancelToss() => _toss = 1f;

            // The hole's rim is crimson while the bridge is closed or its coin is in the air, cream once it lowers.
            private void SetEdge(bool hazard)
            {
                if (hazard == _edgeHazard) return;
                _edgeHazard = hazard;
                if (hazard) SwapMaterial(_edge, _theme.floorLight, _theme.hazardTile);
                else SwapMaterial(_edge, _theme.hazardTile, _theme.floorLight);
            }

            public void Tick(float dt, float speed, float closedAngle, float spinDegrees)
            {
                if (_toss < 1f) _toss = Mathf.Min(1f, _toss + dt / Mathf.Max(0.01f, _theme.coinTossSeconds));
                bool tossing = _toss < 1f;
                Angle = Mathf.MoveTowards(Angle, Open && !tossing ? 0f : closedAngle, speed * dt);
                SetEdge(!Open || tossing);
                _hinge.localRotation = Quaternion.Euler(0f, 0f, Angle);
                bool coinVisible = !Open || tossing;
                if (_coin.gameObject.activeSelf != coinVisible) _coin.gameObject.SetActive(coinVisible);
                float arc = tossing ? Mathf.Sin(_toss * Mathf.PI) : 0f;
                Vector3 p = _coin.localPosition;
                p.y = _theme.coinHeight + _theme.coinTossHeight * arc;
                _coin.localPosition = p;
                Quaternion flip = tossing ? Quaternion.Euler(360f * _theme.coinFlipTurns * _toss, 0f, 0f) : Quaternion.identity;
                _coin.localRotation = flip * Quaternion.Euler(0f, spinDegrees, 0f) * Quaternion.Euler(90f, 0f, 0f);
            }
        }
    }
}
