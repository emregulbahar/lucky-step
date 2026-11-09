using System;
using UnityEngine;
using UnityEngine.UI;

namespace LuckyStep
{
    /// <summary>
    /// Level complete panel (Story 006): title, steps against par, a gold pawn line when this run earned the Golden
    /// pawn, and Next / Retry / Level select. After the last level Next is hidden and the "all levels done" line shows.
    /// </summary>
    /// <example><code>win.Show(moves: 9, par: 9, goldenPawn: true, isLastLevel: false);</code></example>
    public sealed class WinPanel : UiScreen
    {
        private readonly Text _title;
        private readonly Text _score;
        private readonly GameObject _goldenRow;
        private readonly Text _golden;
        private readonly Text _lastLevel;
        private readonly Button _nextButton;
        private readonly Button _retryButton;
        private readonly Text _next;
        private readonly Text _retry;
        private readonly Text _levelSelect;
        private int _moves;
        private int _par = -1;
        private bool _isLastLevel;

        /// <summary>Next pressed (not shown after the last level).</summary>
        public event Action NextClicked;

        /// <summary>Retry pressed.</summary>
        public event Action RetryClicked;

        /// <summary>Level select pressed.</summary>
        public event Action LevelSelectClicked;

        /// <summary>Builds the panel (hidden).</summary>
        public WinPanel(UiRoot ui) : base(ui, "WinPanel", ui.Style.dimColor)
        {
            UiStyle s = ui.Style;
            RectTransform card = ui.CreateStack("Card", Root, s.winWidth, true, s.cardPadding, s.spacing);
            _title = ui.CreateText("Title", card, true, s.titleSize, s.goldText, TextAnchor.MiddleCenter);
            ui.CreateDivider(card);
            _score = ui.CreateText("Score", card, true, s.headingSize, s.textDark, TextAnchor.MiddleCenter);

            RectTransform golden = ui.CreateRow("Golden", card, s.smallSpacing, TextAnchor.MiddleCenter);
            ui.CreateIcon("Pawn", golden, ui.PawnIcon, s.gold, s.rewardIconSize, true);
            _golden = ui.CreateText("Text", golden, false, s.bodySize, s.textDark, TextAnchor.MiddleLeft);
            _goldenRow = golden.gameObject;

            _lastLevel = ui.CreateText("LastLevel", card, false, s.bodySize, s.textDark, TextAnchor.MiddleCenter);

            RectTransform buttons = ui.CreateRow("Buttons", card, s.spacing, TextAnchor.MiddleCenter);
            _nextButton = ui.CreateButton("Next", buttons, true, s.smallButtonSize, out _next);
            _nextButton.onClick.AddListener(() => NextClicked?.Invoke());
            _retryButton = ui.CreateButton("Retry", buttons, false, s.smallButtonSize, out _retry);
            _retryButton.onClick.AddListener(() => RetryClicked?.Invoke());
            ui.CreateButton("LevelSelect", buttons, false, s.smallButtonSize, out _levelSelect)
                .onClick.AddListener(() => LevelSelectClicked?.Invoke());
        }

        /// <inheritdoc />
        protected override GameObject DefaultSelection => _isLastLevel ? _retryButton.gameObject : _nextButton.gameObject;

        /// <summary>Shows the result: <paramref name="par"/> -1 means unknown (score without par, no pawn possible).</summary>
        public void Show(int moves, int par, bool goldenPawn, bool isLastLevel)
        {
            _moves = moves;
            _par = par;
            _isLastLevel = isLastLevel;
            _goldenRow.SetActive(goldenPawn);
            _lastLevel.gameObject.SetActive(isLastLevel);
            _nextButton.gameObject.SetActive(!isLastLevel);
            Show();
        }

        /// <inheritdoc />
        public override void Refresh()
        {
            _title.text = Localization.Get("win.title");
            _score.text = _par >= 0 ? Localization.Format("win.score", _moves, _par) : Localization.Format("win.score_no_par", _moves);
            _golden.text = Localization.Get("win.golden_pawn");
            _lastLevel.text = Localization.Get("win.last_level");
            _next.text = Localization.Get("win.next");
            _retry.text = Localization.Get("win.retry");
            _levelSelect.text = Localization.Get("win.level_select");
        }
    }
}
