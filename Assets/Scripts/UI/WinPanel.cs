using System;
using UnityEngine;
using UnityEngine.UI;

namespace LuckyStep
{
    /// <summary>
    /// Level complete panel (Story 006): title, steps against par, a gold banner with the pawn when this run earned the
    /// Golden pawn (punch and sparkle burst on show), and Next / Retry / Level select. After the last level Next is
    /// hidden and the "all levels done" line shows.
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
        private readonly RectTransform _bannerRect;
        private readonly RectTransform _sparkRoot;
        private readonly Image[] _sparks;
        private readonly Vector2[] _sparkDirs;
        private bool _goldenAnimating;
        private float _goldenTime;
        private bool _goldenStarted;

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

            // Golden pawn banner: a gold plate (the HUD plate sprite, tinted) centred in the row, sized to its content.
            RectTransform golden = ui.CreateRow("Golden", card, 0, TextAnchor.MiddleCenter);
            RectTransform banner = ui.CreateStack("Banner", golden, 0f, true, 0, 0);
            banner.GetComponent<Image>().color = s.goldenBannerColor;
            banner.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(
                s.goldenBannerPaddingX, s.goldenBannerPaddingX + s.cardShadowInset,
                s.goldenBannerPaddingY, s.goldenBannerPaddingY + s.cardShadowInset);
            RectTransform content = ui.CreateRow("Content", banner, s.smallSpacing, TextAnchor.MiddleCenter);
            Image pawn = ui.CreateIcon("Pawn", content, ui.PawnIcon, s.goldenIconColor, s.goldenIconSize, true);
            ui.AddHardShadow(pawn);
            _golden = ui.CreateText("Text", content, true, s.headingSize, s.textDark, TextAnchor.MiddleLeft);
            _goldenRow = golden.gameObject;
            _bannerRect = banner;

            // Sparks: pre-built once under the panel root after the card, so they draw over every card element; the
            // root follows the banner centre and the sparks are only moved/tinted while the burst runs.
            _sparkRoot = ui.CreateRect("Sparks", Root);
            int count = Mathf.Max(0, s.sparkCount);
            _sparks = new Image[count];
            _sparkDirs = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                Image spark = ui.CreateImage("Spark" + i, _sparkRoot, null, s.sparkColor);
                spark.rectTransform.sizeDelta = new Vector2(s.sparkSize, s.sparkSize);
                spark.gameObject.SetActive(false);
                float angle = (i + 0.5f) * Mathf.PI * 2f / count;
                _sparkDirs[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                _sparks[i] = spark;
            }

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
            // The moment needs a visible par: golden is only true when moves <= par and par is shown (par >= 0).
            _goldenAnimating = goldenPawn && par >= 0;
            _goldenStarted = false;
            _goldenTime = 0f;
            _bannerRect.localScale = _goldenAnimating && Ui.Style.goldenPopSeconds > 0f
                ? Vector3.one * Ui.Style.goldenStartScale : Vector3.one;
            for (int i = 0; i < _sparks.Length; i++) _sparks[i].gameObject.SetActive(false);
            _lastLevel.gameObject.SetActive(isLastLevel);
            _nextButton.gameObject.SetActive(!isLastLevel);
            Show();
        }

        /// <summary>Fade-in plus the Golden pawn punch and sparkle burst (unscaled seconds, no allocations).</summary>
        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!_goldenAnimating) return;
            UiStyle s = Ui.Style;
            _goldenTime += deltaTime;
            float t = _goldenTime - s.goldenDelaySeconds;
            if (t < 0f) return;

            if (!_goldenStarted)
            {
                _goldenStarted = true;
                Ui.NotifyGoldenPawnShown();
                if (s.goldenPopSeconds > 0f)
                {
                    for (int i = 0; i < _sparks.Length; i++) _sparks[i].gameObject.SetActive(true);
                }
            }

            if (s.goldenPopSeconds <= 0f)
            {
                _goldenAnimating = false;
                return;
            }

            // Punch: start -> peak (ease out), then peak -> 1 (smoothstep settle).
            float p = Mathf.Clamp01(t / s.goldenPopSeconds);
            float scale;
            if (p < s.goldenPeakFraction)
            {
                float u = p / s.goldenPeakFraction;
                u = 1f - (1f - u) * (1f - u);
                scale = Mathf.LerpUnclamped(s.goldenStartScale, s.goldenPeakScale, u);
            }
            else
            {
                float u = (p - s.goldenPeakFraction) / (1f - s.goldenPeakFraction);
                u = u * u * (3f - 2f * u);
                scale = Mathf.LerpUnclamped(s.goldenPeakScale, 1f, u);
            }
            _bannerRect.localScale = new Vector3(scale, scale, 1f);

            // Burst: sparks start on the banner's outline (an ellipse of its settled size), fly out (ease out), shrink
            // and fade.
            _sparkRoot.position = _bannerRect.position;
            Vector2 half = _bannerRect.rect.size * 0.5f;
            float sp = s.sparkSeconds > 0f ? Mathf.Clamp01(t / s.sparkSeconds) : 1f;
            float travel = 1f - (1f - sp) * (1f - sp);
            Color c = s.sparkColor;
            c.a *= 1f - sp;
            Vector3 sparkScale = Vector3.one * (1f - sp * 0.6f);
            for (int i = 0; i < _sparks.Length; i++)
            {
                RectTransform r = _sparks[i].rectTransform;
                Vector2 dir = _sparkDirs[i];
                r.anchoredPosition = new Vector2(dir.x * half.x, dir.y * half.y) + dir * (s.sparkRadius * travel);
                r.localRotation = Quaternion.Euler(0f, 0f, 45f);
                r.localScale = sparkScale;
                _sparks[i].color = c;
            }

            if (p >= 1f && sp >= 1f)
            {
                _goldenAnimating = false;
                _bannerRect.localScale = Vector3.one;
                for (int i = 0; i < _sparks.Length; i++) _sparks[i].gameObject.SetActive(false);
            }
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
