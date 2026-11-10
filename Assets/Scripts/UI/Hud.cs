using UnityEngine;
using UnityEngine.UI;

namespace LuckyStep
{
    /// <summary>
    /// In-level HUD (Story 006): a cream card in the top-left with "WORLD 1 - LEVEL 3", the move count and par (and the
    /// evil-eye beads carried, in levels that have beads), and a
    /// key legend in the top-right: drawn key caps (arrow keys, Z, R, Esc) each followed by what the key does. Nothing is placed in the bottom of the screen, where the board's score track
    /// sits. Text only changes when a value changes (no per-frame allocation).
    /// </summary>
    /// <example><code>hud.SetLevel(1, 3, catalog.Par("Level03")); hud.SetMoves(controller.Moves);</code></example>
    public sealed class Hud : UiScreen
    {
        private readonly Text _level;
        private readonly Text _steps;
        private readonly Text _par;
        private readonly Text _beads;
        private readonly Text _keyMove;
        private readonly Text _keyUndo;
        private readonly Text _keyRestart;
        private readonly Text _keyMenu;
        private int _world;
        private int _levelNumber;
        private int _parValue = -1;
        private int _moves;
        private int _beadCount = -1; // -1: the level has no beads, the line is hidden

        /// <summary>Builds the HUD (hidden).</summary>
        public Hud(UiRoot ui) : base(ui, "Hud", null)
        {
            UiStyle s = ui.Style;
            RectTransform info = ui.CreateStack("Info", Root, 0f, true, s.hudPadding, s.hudSpacing);
            info.GetComponent<Image>().color = s.hudPlateColor;
            info.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
            UiRoot.Pin(info, new Vector2(0f, 1f), new Vector2(s.screenMargin, -s.screenMargin));
            _level = ui.CreateText("Level", info, true, s.headingSize, s.textDark, TextAnchor.UpperLeft);
            _steps = ui.CreateText("Steps", info, false, s.bodySize, s.textDark, TextAnchor.UpperLeft);
            _par = ui.CreateText("Par", info, false, s.bodySize, s.goldText, TextAnchor.UpperLeft);
            _beads = ui.CreateText("Beads", info, false, s.bodySize, s.beadText, TextAnchor.UpperLeft);

            RectTransform keys = ui.CreateStack("Keys", Root, 0f, true, s.hudPadding / 2, s.keyRowSpacing);
            keys.GetComponent<Image>().color = s.hudPlateColor;
            keys.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
            UiRoot.Pin(keys, new Vector2(1f, 1f), new Vector2(-s.screenMargin, -s.screenMargin));
            _keyMove = KeyRow(ui, keys, "Move", null, 90f, 0f, 180f, 270f); // left, up, down, right
            _keyUndo = KeyRow(ui, keys, "Undo", "Z");
            _keyRestart = KeyRow(ui, keys, "Restart", "R");
            _keyMenu = KeyRow(ui, keys, "Menu", "Esc");
        }

        /// <summary>
        /// One legend row: key caps then the action label. With <paramref name="letter"/> it is one cap showing the
        /// letter; without, one arrow cap per angle in <paramref name="arrowAngles"/> (degrees anticlockwise from up).
        /// </summary>
        private static Text KeyRow(UiRoot ui, Transform parent, string name, string letter, params float[] arrowAngles)
        {
            UiStyle s = ui.Style;
            RectTransform row = ui.CreateRow(name, parent, s.keyCapSpacing, TextAnchor.MiddleLeft);
            if (letter != null) KeyCap(ui, row, letter, 0f);
            foreach (float angle in arrowAngles) KeyCap(ui, row, null, angle);
            Text label = ui.CreateText("Label", row, false, s.captionSize, s.textDark, TextAnchor.MiddleLeft);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            return label;
        }

        /// <summary>Cream key cap (the square button face) showing a letter, or an arrow turned by <paramref name="angle"/>.</summary>
        private static void KeyCap(UiRoot ui, Transform row, string letter, float angle)
        {
            UiStyle s = ui.Style;
            Image cap = ui.CreateImage("Key", row, ui.LevelButtonLocked, Color.white);
            // The face's ink shadow takes cardShadowInset px at the right and bottom; centre the symbol on the face.
            Vector2 inset = new Vector2(-s.cardShadowInset, s.cardShadowInset) * 0.5f;
            float width = s.keyCapSize;
            if (letter != null)
            {
                Text text = ui.CreateText("Letter", cap.transform, true, s.keyCapFontSize, s.textDark, TextAnchor.MiddleCenter);
                text.text = letter;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                UiRoot.Stretch(text.rectTransform, 0f);
                text.rectTransform.anchoredPosition = inset;
                width = Mathf.Max(width, text.preferredWidth + 2f * s.keyCapSpacing + s.cardShadowInset * 2f);
            }
            else
            {
                Image arrow = ui.CreateIcon("Arrow", cap.transform, ui.ArrowIcon, s.textDark, s.keyIconSize, false);
                arrow.rectTransform.anchoredPosition = inset;
                arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            cap.rectTransform.sizeDelta = new Vector2(width, s.keyCapSize);
            UiRoot.Size(cap, width, s.keyCapSize);
        }

        /// <summary>Sets the level shown (1-based world and level-in-world numbers; par -1 when unknown).</summary>
        public void SetLevel(int world, int levelNumber, int par)
        {
            _world = world;
            _levelNumber = levelNumber;
            _parValue = par;
            Refresh();
        }

        /// <summary>Updates the move count; cheap when unchanged (call every frame).</summary>
        public void SetMoves(int moves)
        {
            if (moves == _moves) return;
            _moves = moves;
            _steps.text = Localization.Format("hud.steps", _moves);
        }

        /// <summary>
        /// Updates the evil-eye bead line: <paramref name="carried"/> beads held, or -1 to hide the line in levels
        /// without beads. Cheap when unchanged (call every frame).
        /// </summary>
        public void SetBeads(int carried)
        {
            if (carried == _beadCount) return;
            _beadCount = carried;
            _beads.gameObject.SetActive(_beadCount >= 0);
            if (_beadCount >= 0) _beads.text = Localization.Format("hud.beads", _beadCount);
        }

        /// <inheritdoc />
        public override void Refresh()
        {
            _level.text = Localization.Format("hud.level_format", _world, _levelNumber);
            _steps.text = Localization.Format("hud.steps", _moves);
            _par.text = _parValue >= 0 ? Localization.Format("hud.par", _parValue) : Localization.Get("hud.par_none");
            _par.gameObject.SetActive(_parValue >= 0); // no par line where par is hidden (World 1) or unknown
            _beads.gameObject.SetActive(_beadCount >= 0);
            if (_beadCount >= 0) _beads.text = Localization.Format("hud.beads", _beadCount);
            _keyMove.text = Localization.Get("hud.key_move");
            _keyUndo.text = Localization.Get("hud.key_undo");
            _keyRestart.text = Localization.Get("hud.key_restart");
            _keyMenu.text = Localization.Get("hud.key_menu");
        }
    }
}
