using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace LuckyStep
{
    /// <summary>
    /// Level select (Story 006): one row per world (its name, then a square button per level). Unlocked levels are gold
    /// with their number; locked ones are greyed cream with a lock and cannot be pressed; completed levels carry a tick
    /// badge and Golden pawn levels a gold pawn badge. A world with no levels yet shows only its name. Each world row
    /// ends with its completed count ("4/6"); the legend shows Golden pawns earned out of those possible and the
    /// figure's current base (progression spec 2026-11-12). Back returns to the menu. Built once from the catalog;
    /// <see cref="Refresh"/> re-reads progress and text.
    /// </summary>
    /// <example><code>var select = new LevelSelect(ui, catalog, progress, () => rank); select.LevelChosen += StartLevel;</code></example>
    public sealed class LevelSelect : UiScreen
    {
        private sealed class Slot
        {
            public TextAsset Level;
            public Button Button;
            public Image Face;
            public Text Number;
            public Image Lock;
            public Image Tick;
            public Image Pawn;
        }

        private readonly LevelCatalog _catalog;
        private readonly Progress _progress;
        private readonly List<Slot> _slots = new List<Slot>();
        private readonly List<Text> _worldLabels = new List<Text>();
        private readonly List<Text> _worldCounts = new List<Text>();
        private readonly Func<int> _rank;
        private readonly Image _rankIcon;
        private readonly Text _rankText;
        private readonly Text _title;
        private readonly Text _legend;
        private readonly Text _back;
        private readonly Button _backButton;

        /// <summary>An unlocked level button was pressed.</summary>
        public event Action<TextAsset> LevelChosen;

        /// <summary>Back pressed.</summary>
        public event Action BackClicked;

        /// <summary>
        /// Builds the screen (hidden) for every world and level in <paramref name="catalog"/>; <paramref name="rank"/>
        /// returns the figure rank shown in the legend.
        /// </summary>
        public LevelSelect(UiRoot ui, LevelCatalog catalog, Progress progress, Func<int> rank) : base(ui, "LevelSelect", ui.Style.dimColor)
        {
            _catalog = catalog;
            _progress = progress;
            _rank = rank;
            UiStyle s = ui.Style;
            RectTransform card = ui.CreateStack("Card", Root, s.selectWidth, true, s.cardPadding, s.spacing);
            _title = ui.CreateText("Title", card, true, s.headingSize, s.goldText, TextAnchor.MiddleCenter);
            ui.CreateDivider(card);

            for (int w = 0; w < catalog.Worlds.Count; w++)
            {
                RectTransform row = ui.CreateRow("World" + (w + 1), card, s.smallSpacing, TextAnchor.MiddleLeft);
                UiRoot.Size(row, -1f, s.levelButtonSize);
                Text label = ui.CreateText("Name", row, false, s.bodySize, s.textDark, TextAnchor.MiddleLeft);
                UiRoot.Size(label, s.worldLabelWidth, -1f);
                _worldLabels.Add(label);
                int number = 0;
                foreach (TextAsset level in catalog.Worlds[w].levels)
                {
                    if (level != null) _slots.Add(CreateSlot(row, level, ++number));
                }
                // Completed count: a numeral pair, not a translatable string.
                Text count = ui.CreateText("Count", row, true, s.bodySize, s.goldText, TextAnchor.MiddleRight);
                UiRoot.Size(count, s.worldCountWidth, -1f);
                _worldCounts.Add(count);
            }

            RectTransform legend = ui.CreateRow("Legend", card, s.smallSpacing, TextAnchor.MiddleCenter);
            ui.CreateIcon("Pawn", legend, ui.PawnIcon, s.gold, s.legendIconSize, true);
            _legend = ui.CreateText("Text", legend, false, s.captionSize, s.textDark, TextAnchor.MiddleLeft);
            UiRoot.Size(ui.CreateRect("Gap", legend), s.spacing * 2f, -1f);
            _rankIcon = ui.CreateIcon("Base", legend, ui.BaseIcon, Color.white, s.rankIconSize, true);
            _rankText = ui.CreateText("Rank", legend, true, s.captionSize, s.textDark, TextAnchor.MiddleLeft);

            RectTransform footer = ui.CreateRow("Footer", card, s.spacing, TextAnchor.MiddleCenter);
            _backButton = ui.CreateButton("Back", footer, false, s.smallButtonSize, out _back);
            _backButton.onClick.AddListener(() => BackClicked?.Invoke());
        }

        /// <summary>The first playable, not completed level (else the first playable one, else Back).</summary>
        protected override GameObject DefaultSelection
        {
            get
            {
                Slot firstOpen = null;
                foreach (Slot slot in _slots)
                {
                    if (!slot.Button.interactable) continue;
                    if (!_progress.IsCompleted(slot.Level.name)) return slot.Button.gameObject;
                    if (firstOpen == null) firstOpen = slot;
                }
                return firstOpen != null ? firstOpen.Button.gameObject : _backButton.gameObject;
            }
        }

        /// <inheritdoc />
        public override void Refresh()
        {
            _title.text = Localization.Get("levelselect.title");
            for (int w = 0; w < _worldLabels.Count; w++)
            {
                string name = Localization.Get("world." + (w + 1) + ".name");
                _worldLabels[w].text = Localization.Format("levelselect.world_format", w + 1, name);
            }
            for (int w = 0; w < _worldCounts.Count; w++)
            {
                int done = 0;
                int total = 0;
                foreach (TextAsset level in _catalog.Worlds[w].levels)
                {
                    if (level == null) continue;
                    total++;
                    if (_progress.IsCompleted(level.name)) done++;
                }
                _worldCounts[w].text = total > 0 ? done.ToString(CultureInfo.InvariantCulture) + "/" + total.ToString(CultureInfo.InvariantCulture) : string.Empty;
            }
            List<string> ids = _catalog.LevelIds;
            int pawns = 0;
            int pawnsPossible = 0;
            foreach (string id in ids)
            {
                if (_catalog.Par(id) < 0) continue; // worlds that hide par give no Golden pawn
                pawnsPossible++;
                if (_progress.HasGoldenPawn(id)) pawns++;
            }
            List<List<string>> worldIds = _catalog.WorldLevelIds;
            foreach (Slot slot in _slots)
            {
                string id = slot.Level.name;
                bool unlocked = _progress.IsUnlocked(id, worldIds);
                slot.Button.interactable = unlocked;
                slot.Face.sprite = unlocked ? Ui.LevelButton : Ui.LevelButtonLocked;
                slot.Number.gameObject.SetActive(unlocked);
                slot.Lock.gameObject.SetActive(!unlocked);
                slot.Tick.gameObject.SetActive(_progress.IsCompleted(id));
                slot.Pawn.gameObject.SetActive(_progress.HasGoldenPawn(id));
            }
            _legend.text = Localization.Format("levelselect.legend_golden_pawn", pawns, pawnsPossible);
            int rank = _rank != null ? _rank() : 0;
            _rankIcon.color = Ui.Style.RankColor(rank);
            _rankText.text = Localization.Format("levelselect.rank", Localization.Get("rank." + Mathf.Clamp(rank, 0, 5)));
            _back.text = Localization.Get("common.back");
        }

        private Slot CreateSlot(Transform row, TextAsset level, int number)
        {
            UiStyle s = Ui.Style;
            var slot = new Slot { Level = level };
            slot.Face = Ui.CreateImage("Level" + number, row, Ui.LevelButton, Color.white);
            slot.Face.raycastTarget = true;
            UiRoot.Size(slot.Face, s.levelButtonSize, s.levelButtonSize);
            slot.Button = slot.Face.gameObject.AddComponent<Button>();
            slot.Button.onClick.AddListener(Ui.NotifyButtonClicked);
            slot.Button.targetGraphic = slot.Face;
            slot.Button.colors = Ui.ButtonColors();
            slot.Button.onClick.AddListener(() => LevelChosen?.Invoke(level));

            // The level number is a numeral, not a translatable string.
            slot.Number = Ui.CreateText("Number", slot.Face.transform, true, s.headingSize, s.textDark, TextAnchor.MiddleCenter);
            UiRoot.Stretch(slot.Number.rectTransform, 0f);
            slot.Number.rectTransform.offsetMin = new Vector2(0f, s.buttonLabelLift);
            slot.Number.text = number.ToString(CultureInfo.InvariantCulture);

            slot.Lock = Ui.CreateIcon("Lock", slot.Face.transform, Ui.LockIcon, s.lockColor, s.lockIconSize, false);
            UiRoot.Pin(slot.Lock.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, s.buttonLabelLift * 0.5f));
            slot.Tick = Ui.CreateIcon("Tick", slot.Face.transform, Ui.TickIcon, Color.white, s.badgeSize, false);
            UiRoot.Pin(slot.Tick.rectTransform, new Vector2(1f, 1f), new Vector2(s.badgeOffset, s.badgeOffset));
            slot.Pawn = Ui.CreateIcon("Pawn", slot.Face.transform, Ui.PawnIcon, s.gold, s.badgeSize, true);
            UiRoot.Pin(slot.Pawn.rectTransform, new Vector2(0f, 1f), new Vector2(-s.badgeOffset, s.badgeOffset));
            return slot;
        }
    }
}
