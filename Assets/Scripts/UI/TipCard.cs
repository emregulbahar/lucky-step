using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LuckyStep
{
    /// <summary>
    /// New-mechanic card (spec tutorial-guide-and-tips-addition-2026-11-13): over the dimmed board, a cream card headed
    /// "New!" with one row per mechanic (full-colour icon, name, one sentence; at most <see cref="MaxRows"/>) and a
    /// "Got it" button. Clicks and Submit are ignored for UiStyle.tipInputDelaySeconds after it opens, so the key that
    /// closed the story card cannot close this card in the same moment.
    /// </summary>
    /// <example><code>tips.SetMechanics(new[] { "spike", "flowerpot" }); tips.Show(); tips.Closed += StartPlaying;</code></example>
    public sealed class TipCard : UiScreen
    {
        /// <summary>Most mechanics one card lists.</summary>
        public const int MaxRows = 3;

        private sealed class Row
        {
            public GameObject Root;
            public Image Icon;
            public Text Title;
            public Text Body;
        }

        private readonly Text _header;
        private readonly Text _ok;
        private readonly Button _okButton;
        private readonly Row[] _rows = new Row[MaxRows];
        private readonly List<string> _ids = new List<string>();
        private float _shownSeconds;

        /// <summary>Got it pressed (after the input delay).</summary>
        public event Action Closed;

        /// <summary>Builds the card (hidden).</summary>
        public TipCard(UiRoot ui) : base(ui, "TipCard", ui.Style.storyDimColor)
        {
            UiStyle s = ui.Style;
            RectTransform card = ui.CreateStack("Card", Root, s.tipWidth, true, s.storyPadding, s.spacing);
            _header = ui.CreateText("Header", card, true, s.titleSize, s.goldText, TextAnchor.MiddleCenter);
            ui.CreateDivider(card);
            for (int i = 0; i < MaxRows; i++)
            {
                RectTransform row = ui.CreateRow("Row" + i, card, s.spacing, TextAnchor.MiddleLeft);
                var r = new Row { Root = row.gameObject };
                r.Icon = ui.CreateIcon("Icon", row, null, Color.white, s.tipIconSize, false);
                RectTransform text = ui.CreateStack("Text", row, 0f, false, 0, s.hudSpacing);
                UiRoot.Size(text, s.tipTextWidth, -1f);
                r.Title = ui.CreateText("Title", text, true, s.headingSize, s.textDark, TextAnchor.MiddleLeft);
                r.Body = ui.CreateText("Body", text, false, s.bodySize, s.textDark, TextAnchor.MiddleLeft);
                _rows[i] = r;
            }
            RectTransform buttons = ui.CreateRow("Buttons", card, s.spacing, TextAnchor.MiddleCenter);
            _okButton = ui.CreateButton("Ok", buttons, true, s.smallButtonSize, out _ok);
            _okButton.onClick.AddListener(OnOk);
        }

        /// <inheritdoc />
        protected override GameObject DefaultSelection => _okButton.gameObject;

        /// <summary>Sets the mechanics to list (LevelMechanics ids; the first <see cref="MaxRows"/> are shown).</summary>
        public void SetMechanics(IEnumerable<string> mechanicIds)
        {
            _ids.Clear();
            foreach (string id in mechanicIds)
            {
                if (_ids.Count < MaxRows) _ids.Add(id);
            }
            _shownSeconds = 0f;
        }

        /// <inheritdoc />
        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            _shownSeconds += deltaTime;
        }

        /// <inheritdoc />
        public override void Refresh()
        {
            _header.text = Localization.Get("tip.header");
            _ok.text = Localization.Get("tip.ok");
            for (int i = 0; i < MaxRows; i++)
            {
                Row row = _rows[i];
                bool used = i < _ids.Count;
                row.Root.SetActive(used);
                if (!used) continue;
                string id = _ids[i];
                row.Icon.sprite = Ui.TipIcon(id);
                row.Title.text = Localization.Get("tip." + id + ".title");
                row.Body.text = Localization.Get("tip." + id + ".body");
            }
        }

        private void OnOk()
        {
            if (_shownSeconds < Ui.Style.tipInputDelaySeconds) return;
            Closed?.Invoke();
        }
    }
}
