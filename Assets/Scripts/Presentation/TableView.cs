using UnityEngine;
using UnityEngine.Rendering;

namespace LuckyStep
{
    /// <summary>
    /// The card table the board lies on (Story 005, "Felt Table" look; world table themes 2026-11-12): a table top,
    /// a wooden rail around it and props beside the board, all from the current world's <see cref="WorldDecor"/>
    /// (World 1: green felt, mug, game box, two dice, card fan, rule booklet). The world's key light colour and
    /// ambient fill are applied at the same time. Rebuilt from <see cref="BoardView.BoardBounds"/> after every board
    /// build, so it fits every level size. Pure decoration: nothing here takes part in the game.
    /// </summary>
    public sealed class TableView : MonoBehaviour
    {
        private static readonly int BaseMapSt = Shader.PropertyToID("_BaseMap_ST");

        [SerializeField] private BoardView _board;
        [SerializeField] private BoardTheme _theme;
        [Tooltip("Directional key light tinted per world; RenderSettings.sun if empty.")]
        [SerializeField] private Light _keyLight;

        private Transform _root;
        private MaterialPropertyBlock _surfaceBlock;
        private bool _sceneLightingRead;
        private SphericalHarmonicsL2 _sceneProbe;
        private Color _sceneAmbient; // linear

        private void OnEnable()
        {
            if (_board == null) return;
            ReadSceneLighting();
            _board.Built += Build;
            if (_board.Level != null) Build();
        }

        private void OnDisable()
        {
            if (_board != null) _board.Built -= Build;
        }

        private void Build()
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = new GameObject("Table").transform;
            _root.SetParent(transform, false);
            WorldDecor decor = CurrentDecor();

            Bounds b = _board.BoardBounds;
            float top = b.min.y; // the board's plate rests on the felt
            float hx = b.extents.x + _theme.tableMarginX;
            // Look pass 2: the felt runs on past the far rail, so the top of the frame shows table, not the dark floor.
            float hz = b.extents.z + _theme.tableMarginZ + _theme.tableMarginFar * 0.5f;
            Vector3 c = new Vector3(b.center.x, top, b.center.z + _theme.tableMarginFar * 0.5f);

            const float feltThickness = 0.1f;
            MeshRenderer felt = Box("Felt", c + Vector3.down * (feltThickness * 0.5f), new Vector3(hx * 2f, feltThickness, hz * 2f),
                decor != null ? decor.surface : null, false);
            if (decor != null && decor.surfaceTile > 0f)
            {
                // Patterned cloth repeats at a fixed world size instead of stretching with the level's table.
                if (_surfaceBlock == null) _surfaceBlock = new MaterialPropertyBlock();
                _surfaceBlock.SetVector(BaseMapSt, new Vector4(hx * 2f / decor.surfaceTile, hz * 2f / decor.surfaceTile, 0f, 0f));
                felt.SetPropertyBlock(_surfaceBlock);
            }

            float rw = _theme.tableRailWidth, rh = _theme.tableRailHeight;
            float ry = top + rh * 0.5f - feltThickness * 0.5f; // rails stand on the felt slab's bottom
            float railH = rh + feltThickness * 0.5f;
            Box("Rail N", new Vector3(c.x, ry, c.z + hz + rw * 0.5f), new Vector3(hx * 2f + rw * 2f, railH, rw), _theme.tableRail, true);
            Box("Rail S", new Vector3(c.x, ry, c.z - hz - rw * 0.5f), new Vector3(hx * 2f + rw * 2f, railH, rw), _theme.tableRail, true);
            Box("Rail E", new Vector3(c.x + hx + rw * 0.5f, ry, c.z), new Vector3(rw, railH, hz * 2f), _theme.tableRail, true);
            Box("Rail W", new Vector3(c.x - hx - rw * 0.5f, ry, c.z), new Vector3(rw, railH, hz * 2f), _theme.tableRail, true);

            if (decor == null) return;
            ApplyLighting(decor);
            if (decor.props == null) return;

            // Wide, shallow boards leave the sides off-screen but room in front: low props move to the near edge.
            bool wide = _board.IsWide;
            float front = b.min.z - _theme.propGap * 0.75f;
            foreach (TableProp p in decor.props)
            {
                if (p == null || p.model == null) continue;
                Vector3 size = p.sizeIsHeight ? new Vector3(0f, p.size, 0f) : new Vector3(p.size, 0f, p.size);
                Transform prop = ModelFit.Spawn(p.model, _root, p.model.name, size, true);
                prop.position = PropPosition(p, b, top, wide, front);
                prop.rotation = Quaternion.Euler(0f, p.yaw, 0f);
            }
        }

        /// <summary>The current world's decor; entry 0 for a level outside the catalog. Null if the theme has none.</summary>
        private WorldDecor CurrentDecor()
        {
            WorldDecor[] all = _theme.worldDecor;
            if (all == null || all.Length == 0) return null;
            int world = _board.WorldIndex;
            return all[world >= 0 && world < all.Length ? world : 0];
        }

        /// <summary>Beside the board (Left/Right), beyond it (Far), or in the front row on a wide board; off the board's bounds.</summary>
        private Vector3 PropPosition(TableProp p, Bounds b, float top, bool wide, float front)
        {
            if (wide && p.frontWhenWide) return new Vector3(b.center.x + p.wideAlong * b.extents.x, top, front + p.wideFront);
            float gap = p.gap * _theme.propGap;
            switch (p.side)
            {
                case TableSide.Left: return new Vector3(b.min.x - gap, top, b.center.z + p.along * b.extents.z);
                case TableSide.Right: return new Vector3(b.max.x + gap, top, b.center.z + p.along * b.extents.z);
                default: return new Vector3(b.center.x + p.along * b.extents.x, top, b.max.z + gap);
            }
        }

        /// <summary>Remembers the scene's ambient probe and colour once, so worlds can rescale it (World 1 = unchanged).</summary>
        private void ReadSceneLighting()
        {
            if (_sceneLightingRead) return;
            _sceneProbe = RenderSettings.ambientProbe;
            _sceneAmbient = RenderSettings.ambientLight.linear;
            _sceneLightingRead = true;
        }

        /// <summary>Tints the key light and sets the flat ambient (the Toon shader's only fill) for this world.</summary>
        private void ApplyLighting(WorldDecor decor)
        {
            Light key = _keyLight != null ? _keyLight : RenderSettings.sun;
            if (key != null)
            {
                key.color = decor.keyColor;
                key.intensity = decor.keyIntensity;
            }

            RenderSettings.ambientLight = decor.ambient;
            // The Toon shader reads ambient through the SH probe. A flat probe is linear in its colour, so scaling the
            // scene's own probe per channel gives the new colour and leaves World 1's probe bit-identical.
            Color want = decor.ambient.linear;
            SphericalHarmonicsL2 probe = _sceneProbe;
            bool scalable = probe != new SphericalHarmonicsL2()
                && _sceneAmbient.r > 1e-4f && _sceneAmbient.g > 1e-4f && _sceneAmbient.b > 1e-4f;
            if (scalable)
            {
                for (int ch = 0; ch < 3; ch++)
                {
                    float k = want[ch] / _sceneAmbient[ch];
                    for (int i = 0; i < 9; i++) probe[ch, i] *= k;
                }
            }
            else
            {
                probe.Clear();
                probe.AddAmbientLight(want);
            }
            RenderSettings.ambientProbe = probe;
        }

        private MeshRenderer Box(string name, Vector3 centre, Vector3 size, Material material, bool castShadows)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.localPosition = centre;
            go.transform.localScale = size;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return renderer;
        }
    }
}
