using UnityEngine;
using UnityEngine.Rendering;

namespace LuckyStep
{
    /// <summary>
    /// The green felt card table the board lies on (Story 005, "Felt Table" look): a felt top, a wooden rail around
    /// it and props beside the board (a mug, a closed game box with its logo, two dice, a card fan, a rule booklet). Rebuilt from <see cref="BoardView.BoardBounds"/>
    /// after every board build, so it fits every level size. Pure decoration: nothing here takes part in the game.
    /// </summary>
    public sealed class TableView : MonoBehaviour
    {
        [SerializeField] private BoardView _board;
        [SerializeField] private BoardTheme _theme;

        private Transform _root;

        private void OnEnable()
        {
            if (_board == null) return;
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

            Bounds b = _board.BoardBounds;
            float top = b.min.y; // the board's plate rests on the felt
            float hx = b.extents.x + _theme.tableMarginX;
            // Look pass 2: the felt runs on past the far rail, so the top of the frame shows table, not the dark floor.
            float hz = b.extents.z + _theme.tableMarginZ + _theme.tableMarginFar * 0.5f;
            Vector3 c = new Vector3(b.center.x, top, b.center.z + _theme.tableMarginFar * 0.5f);

            const float feltThickness = 0.1f;
            Box("Felt", c + Vector3.down * (feltThickness * 0.5f), new Vector3(hx * 2f, feltThickness, hz * 2f), _theme.tableFelt, false);

            float rw = _theme.tableRailWidth, rh = _theme.tableRailHeight;
            float ry = top + rh * 0.5f - feltThickness * 0.5f; // rails stand on the felt slab's bottom
            float railH = rh + feltThickness * 0.5f;
            Box("Rail N", new Vector3(c.x, ry, c.z + hz + rw * 0.5f), new Vector3(hx * 2f + rw * 2f, railH, rw), _theme.tableRail, true);
            Box("Rail S", new Vector3(c.x, ry, c.z - hz - rw * 0.5f), new Vector3(hx * 2f + rw * 2f, railH, rw), _theme.tableRail, true);
            Box("Rail E", new Vector3(c.x + hx + rw * 0.5f, ry, c.z), new Vector3(rw, railH, hz * 2f), _theme.tableRail, true);
            Box("Rail W", new Vector3(c.x - hx - rw * 0.5f, ry, c.z), new Vector3(rw, railH, hz * 2f), _theme.tableRail, true);

            // Wide, shallow boards leave the sides off-screen but room in front: the side props move to the near edge.
            bool wide = _board.IsWide;
            float front = b.min.z - _theme.propGap * 0.75f;
            if (_theme.tableMug != null)
            {
                Transform mug = ModelFit.Spawn(_theme.tableMug, _root, "Mug", new Vector3(0f, _theme.mugHeight, 0f), true);
                mug.position = new Vector3(b.min.x - _theme.propGap, top, b.center.z + b.extents.z * 0.35f);
                mug.rotation = Quaternion.Euler(0f, _theme.mugYaw, 0f);
            }
            if (_theme.tableBox != null)
            {
                Transform box = ModelFit.Spawn(_theme.tableBox, _root, "Game Box", new Vector3(_theme.boxSize, 0f, _theme.boxSize), true);
                box.position = wide
                    ? new Vector3(b.max.x - b.extents.x * 0.3f, top, front)
                    : new Vector3(b.max.x + _theme.propGap, top, b.center.z + b.extents.z * 0.1f);
                box.rotation = Quaternion.Euler(0f, _theme.boxYaw, 0f);
            }
            // Look pass 2: game-night clutter, kept off the board (props sit beside, in front of or beyond its bounds).
            if (_theme.tableDie != null)
            {
                Vector3 a = wide ? new Vector3(b.min.x + b.extents.x * 0.25f, top, front + 0.2f)
                                 : new Vector3(b.min.x - _theme.propGap * 1.05f, top, b.center.z - b.extents.z * 0.45f);
                Vector3 c2 = wide ? new Vector3(b.min.x + b.extents.x * 0.4f, top, front - 0.15f)
                                  : new Vector3(b.min.x - _theme.propGap * 0.55f, top, b.center.z - b.extents.z * 0.75f);
                Prop(_theme.tableDie, "Die A", a, 23f, _theme.tableDieSize);
                Prop(_theme.tableDie, "Die B", c2, -38f, _theme.tableDieSize);
            }
            if (_theme.tableCardFan != null)
            {
                Vector3 at = wide ? new Vector3(b.max.x - b.extents.x * 0.75f, top, front)
                                  : new Vector3(b.max.x + _theme.propGap * 0.9f, top, b.center.z - b.extents.z * 0.7f);
                Prop(_theme.tableCardFan, "Card Fan", at, 12f, _theme.cardFanSize);
            }
            if (_theme.tableBooklet != null)
            {
                Prop(_theme.tableBooklet, "Rule Booklet", new Vector3(b.center.x - b.extents.x * 0.3f, top, b.max.z + _theme.propGap * 0.75f), -8f, _theme.bookletSize);
            }
        }

        private void Prop(GameObject model, string name, Vector3 position, float yaw, float footprint)
        {
            Transform prop = ModelFit.Spawn(model, _root, name, new Vector3(footprint, 0f, footprint), true);
            prop.position = position;
            prop.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void Box(string name, Vector3 centre, Vector3 size, Material material, bool castShadows)
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
        }
    }
}
