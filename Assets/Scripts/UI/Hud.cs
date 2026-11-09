using UnityEngine;
using UnityEngine.UI;

namespace LuckyStep
{
    /// <summary>
    /// In-level HUD (Story 006): a dark plate in the top-left with "WORLD 1 - LEVEL 3", the move count and par, and the
    /// key hints small in the top-right. Nothing is placed in the bottom of the screen, where the board's score track
    /// sits. Text only changes when a value changes (no per-frame allocation).
    /// </summary>
    /// <example><code>hud.SetLevel(1, 3, catalog.Par("Level03")); hud.SetMoves(controller.Moves);</code></example>
    public sealed class Hud : UiScreen
    {
        private readonly Text _level;
        private readonly Text _steps;
        private readonly Text _par;
        private readonly Text _keys;
        private int _world;
        private int _levelNumber;
        private int _parValue = -1;
        private int _moves;

        /// <summary>Builds the HUD (hidden).</summary>
        public Hud(UiRoot ui) : base(ui, "Hud", null)
        {
            UiStyle s = ui.Style;
            RectTransform info = ui.CreateStack("Info", Root, 0f, true, s.hudPadding, s.hudSpacing);
            info.GetComponent<Image>().color = s.hudPlateColor;
            info.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
            UiRoot.Pin(info, new Vector2(0f, 1f), new Vector2(s.screenMargin, -s.screenMargin));
            _level = ui.CreateText("Level", info, true, s.headingSize, s.textLight, TextAnchor.UpperLeft);
            _steps = ui.CreateText("Steps", info, false, s.bodySize, s.textLight, TextAnchor.UpperLeft);
            _par = ui.CreateText("Par", info, false, s.bodySize, s.gold, TextAnchor.UpperLeft);

            RectTransform keys = ui.CreateStack("Keys", Root, 0f, true, s.hudPadding / 2, 0);
            keys.GetComponent<Image>().color = s.hudPlateColor;
            UiRoot.Pin(keys, new Vector2(1f, 1f), new Vector2(-s.screenMargin, -s.screenMargin));
            _keys = ui.CreateText("Text", keys, false, s.captionSize, s.textLight, TextAnchor.MiddleRight);
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

        /// <inheritdoc />
        public override void Refresh()
        {
            _level.text = Localization.Format("hud.level_format", _world, _levelNumber);
            _steps.text = Localization.Format("hud.steps", _moves);
            _par.text = _parValue >= 0 ? Localization.Format("hud.par", _parValue) : Localization.Get("hud.par_none");
            _par.gameObject.SetActive(_parValue >= 0); // no par line where par is hidden (World 1) or unknown
            _keys.text = Localization.Get("hud.keys");
        }
    }
}
