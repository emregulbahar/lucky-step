using UnityEngine;
using UnityEngine.UI;

namespace LuckyStep
{
    /// <summary>
    /// In-level HUD (Story 006): a cream card in the top-left with "WORLD 1 - LEVEL 3", the move count and par (and the
    /// evil-eye beads carried, in levels that have beads), and a
    /// key legend in the top-right: drawn key caps (arrow keys, Z, R, Esc) each followed by what the key does. Nothing is placed in the bottom of the screen, where the board's score track
    /// sits. Text only changes when a value changes (no per-frame allocation).
    /// In guided levels a speech bubble with a pointer follows the figure (<see cref="SetGuide"/>, <see cref="PlaceGuide"/>;
    /// spec tutorial-guide-and-tips-addition-2026-11-13). While the bubble overlaps the level card or the key legend
    /// (the figure stands high on a tall board), that card fades out so the bubble reads cleanly.
    /// </summary>
    /// <example><code>hud.SetLevel(1, 3, catalog.Par("Level03")); hud.SetMoves(controller.Moves);</code></example>
    public sealed class Hud : UiScreen
    {
        private readonly Text _level;
        private readonly Text _steps;
        private readonly Text _par;
        private readonly Text _beads;
        private readonly RectTransform _hintCard;
        private readonly Text _hint;
        private string _hintKey;
        private readonly RectTransform _guide;
        private readonly RectTransform _guideCard;
        private readonly Text _guideText;
        private readonly Text _keyMove;
        private readonly Text _keyUndo;
        private readonly Text _keyRestart;
        private readonly Text _keyMenu;
        private readonly RectTransform _info;
        private readonly RectTransform _keys;
        private readonly CanvasGroup _infoFade;
        private readonly CanvasGroup _keysFade;
        private readonly Vector3[] _corners = new Vector3[4];
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
            _info = info;
            _infoFade = info.gameObject.AddComponent<CanvasGroup>();
            info.GetComponent<Image>().color = s.hudPlateColor;
            info.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
            UiRoot.Pin(info, new Vector2(0f, 1f), new Vector2(s.screenMargin, -s.screenMargin));
            _level = ui.CreateText("Level", info, true, s.headingSize, s.textDark, TextAnchor.UpperLeft);
            _steps = ui.CreateText("Steps", info, false, s.bodySize, s.textDark, TextAnchor.UpperLeft);
            _par = ui.CreateText("Par", info, false, s.bodySize, s.goldText, TextAnchor.UpperLeft);
            _beads = ui.CreateText("Beads", info, false, s.bodySize, s.beadText, TextAnchor.UpperLeft);

            // Tutorial hint (design/quick-specs/tutorial-hints-addition-2026-11-10.md): top centre, between the level
            // card and the key legend; shown only in levels whose strings have hint.<LevelId>.
            _hintCard = ui.CreateStack("Hint", Root, s.hintWidth, true, s.hudPadding, s.hudSpacing);
            _hintCard.GetComponent<Image>().color = s.hudPlateColor;
            UiRoot.Pin(_hintCard, new Vector2(0.5f, 1f), new Vector2(0f, -s.screenMargin));
            _hint = ui.CreateText("Text", _hintCard, false, s.captionSize, s.textDark, TextAnchor.MiddleCenter);
            _hintCard.gameObject.SetActive(false);

            // Guide bubble: a zero-size root placed at the figure's head each frame; the ink pointer (a diamond, half
            // hidden behind the card) sits on the root and the card stands on top of it.
            _guide = ui.CreateRect("Guide", Root);
            _guide.anchorMin = _guide.anchorMax = new Vector2(0.5f, 0.5f);
            _guide.sizeDelta = Vector2.zero;
            Image pointer = ui.CreateImage("Pointer", _guide, null, s.outlineColor);
            pointer.rectTransform.sizeDelta = new Vector2(s.guidePointerSize, s.guidePointerSize);
            pointer.rectTransform.anchoredPosition = new Vector2(0f, s.guidePointerSize * 0.75f);
            pointer.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _guideCard = ui.CreateStack("Card", _guide, s.guideBubbleWidth, true, s.hudPadding, s.hudSpacing);
            _guideCard.GetComponent<Image>().color = s.hudPlateColor;
            _guideCard.anchorMin = _guideCard.anchorMax = new Vector2(0.5f, 0.5f);
            _guideCard.pivot = new Vector2(0.5f, 0f);
            _guideCard.anchoredPosition = new Vector2(0f, s.guidePointerSize);
            _guideText = ui.CreateText("Text", _guideCard, true, s.guideTextSize, s.textDark, TextAnchor.MiddleCenter);
            _guide.gameObject.SetActive(false);

            RectTransform keys = ui.CreateStack("Keys", Root, 0f, true, s.hudPadding / 2, s.keyRowSpacing);
            _keys = keys;
            _keysFade = keys.gameObject.AddComponent<CanvasGroup>();
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

        /// <summary>Shows the guide bubble with <paramref name="text"/> (already localized); null or empty hides it.</summary>
        public void SetGuide(string text)
        {
            bool show = !string.IsNullOrEmpty(text);
            _guide.gameObject.SetActive(show);
            if (show) _guideText.text = text;
            else _infoFade.alpha = _keysFade.alpha = 1f;
        }

        /// <summary>
        /// Moves the guide bubble so its pointer touches <paramref name="screenPoint"/> (pixels), kept inside the screen
        /// sideways. No allocation.
        /// </summary>
        public void PlaceGuide(Vector2 screenPoint)
        {
            if (!_guide.gameObject.activeSelf) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, screenPoint, null, out Vector2 local)) return;
            float half = Root.rect.width * 0.5f - Ui.Style.screenMargin - _guideCard.rect.width * 0.5f;
            float shift = half > 0f ? Mathf.Clamp(local.x, -half, half) - local.x : 0f;
            _guide.anchoredPosition = local;
            _guideCard.anchoredPosition = new Vector2(shift, Ui.Style.guidePointerSize);

            // The bubble card in Root space (pivot bottom centre), then fade whichever corner card it covers.
            Rect card = _guideCard.rect;
            Vector2 min = _guide.anchoredPosition + _guideCard.anchoredPosition - new Vector2(card.width * 0.5f, 0f)
                + Root.rect.center;
            var bubble = new Rect(min, card.size);
            float step = Time.unscaledDeltaTime * Ui.Style.guideFadeSpeed;
            Fade(_infoFade, Overlaps(_info, bubble), step);
            Fade(_keysFade, Overlaps(_keys, bubble), step);
        }

        private bool Overlaps(RectTransform target, Rect bubble)
        {
            target.GetWorldCorners(_corners);
            Vector2 a = Root.InverseTransformPoint(_corners[0]);
            Vector2 b = Root.InverseTransformPoint(_corners[2]);
            return bubble.Overlaps(Rect.MinMaxRect(a.x, a.y, b.x, b.y));
        }

        private static void Fade(CanvasGroup group, bool hidden, float step)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, hidden ? 0f : 1f, step);
        }

        /// <summary>Shows the tutorial hint of <paramref name="levelId"/> (key hint.LevelId), or hides it when there is none.</summary>
        public void SetHint(string levelId)
        {
            _hintKey = string.IsNullOrEmpty(levelId) ? null : "hint." + levelId;
            RefreshHint();
        }

        private void RefreshHint()
        {
            bool show = _hintKey != null && Localization.Has(_hintKey);
            _hintCard.gameObject.SetActive(show);
            if (show) _hint.text = Localization.Get(_hintKey);
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
            RefreshHint();
            _keyMove.text = Localization.Get("hud.key_move");
            _keyUndo.text = Localization.Get("hud.key_undo");
            _keyRestart.text = Localization.Get("hud.key_restart");
            _keyMenu.text = Localization.Get("hud.key_menu");
        }
    }
}
