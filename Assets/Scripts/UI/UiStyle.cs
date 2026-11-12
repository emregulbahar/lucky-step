using System;
using UnityEngine;

namespace LuckyStep
{
    /// <summary>
    /// Story 006 UI tuning data: canvas scaling, type scale, the 8 px spacing grid, sizes, the "Printed Board" palette
    /// (cream cards, ink text and borders, gold accents; see design/game-brief.md) and timings. Serialized on
    /// <see cref="UiRoot"/>, so every value is editable in the Inspector; pixel values are at the 1920x1080 reference
    /// resolution.
    /// </summary>
    /// <example><code>text.fontSize = ui.Style.bodySize;</code></example>
    [Serializable]
    public sealed class UiStyle
    {
        /// <summary>CanvasScaler reference resolution.</summary>
        [Header("Canvas")]
        public Vector2 referenceResolution = new Vector2(1920f, 1080f);

        /// <summary>CanvasScaler width/height match (0.5 = balanced).</summary>
        [Range(0f, 1f)] public float matchWidthOrHeight = 0.5f;

        /// <summary>Type scale in px: title, heading, body, caption; line spacing multiplier.</summary>
        [Header("Type scale (px)")]
        public int titleSize = 72;
        public int headingSize = 44;
        public int bodySize = 32;
        public int captionSize = 24;
        public float lineSpacing = 1.1f;

        /// <summary>Paddings and gaps, all multiples of the 8 px grid.</summary>
        [Header("Spacing (multiples of 8 px)")]
        public int cardPadding = 48;
        public int storyPadding = 64;
        public int spacing = 24;
        public int smallSpacing = 16;
        public int hudPadding = 24;
        public int hudSpacing = 8;
        public float screenMargin = 32f;
        /// <summary>Height (and minimum width) of a HUD key cap.</summary>
        public float keyCapSize = 48f;
        /// <summary>Arrow icon size inside an arrow-key cap.</summary>
        public float keyIconSize = 22f;
        /// <summary>Font size of the letter on a key cap.</summary>
        public int keyCapFontSize = 22;
        /// <summary>Gap between key caps in one row, and between the caps and the label.</summary>
        public int keyCapSpacing = 6;
        /// <summary>Gap between the rows of the key legend.</summary>
        public int keyRowSpacing = 8;

        /// <summary>Card widths in px.</summary>
        [Header("Sizes (px)")]
        public float menuWidth = 640f;
        /// <summary>Settings card width, and the widths of a row's name and value columns.</summary>
        public float settingsWidth = 720f;
        /// <summary>Width of the HUD tutorial hint card (top centre, between the level card and the key legend).</summary>
        public float hintWidth = 1060f;
        public float settingsLabelWidth = 220f;
        public float settingsValueWidth = 120f;
        public float selectWidth = 1440f;
        public float storyWidth = 1200f;
        public float winWidth = 1040f;

        /// <summary>Full-width menu buttons (at least 88 px tall at 1080p).</summary>
        public Vector2 buttonSize = new Vector2(480f, 96f);

        /// <summary>Buttons that sit side by side (win panel, Back).</summary>
        public Vector2 smallButtonSize = new Vector2(288f, 96f);

        public float levelButtonSize = 96f;
        public float worldLabelWidth = 520f;
        public float badgeSize = 48f;
        public float badgeOffset = 12f;
        public float lockIconSize = 56f;
        public float legendIconSize = 40f;
        public float rewardIconSize = 64f;
        public float dividerHeight = 8f;
        public float buttonLabelPadding = 16f;

        /// <summary>Lifts button labels to the centre of the face above the 4 px ink shadow baked into the button sprites.</summary>
        public float buttonLabelLift = 4f;

        /// <summary>Scales 9-slice corners down (the card and button sprites are drawn at 2x).</summary>
        public float slicedPixelsPerUnitMultiplier = 2f;

        public Vector2 outlineDistance = new Vector2(2f, -2f);

        /// <summary>Extra uGUI Shadow on cards; zero because the hard ink shadow is baked into the card sprite.</summary>
        public Vector2 cardShadowDistance = Vector2.zero;

        /// <summary>Width of the ink shadow baked into the card sprite; added to card padding on the right and bottom.</summary>
        public int cardShadowInset = 4;

        /// <summary>Hard ink shadow under the outlined main menu title (matches the 4 px card shadow).</summary>
        public Vector2 titleShadowDistance = new Vector2(4f, -4f);

        /// <summary>Tint on the cream panel sprite (white keeps the sprite's own cream #FFF6E0).</summary>
        [Header("Colours")]
        public Color panelColor = Color.white;

        /// <summary>Ink #1C1A24: text on cream and gold.</summary>
        public Color textDark = new Color(0.110f, 0.102f, 0.141f);

        /// <summary>Gold #FFC21A: rules, the pawn icon and the menu title.</summary>
        public Color gold = new Color(1f, 0.761f, 0.102f);

        /// <summary>Darker gold for text on cream cards (about 4.6:1 contrast).</summary>
        public Color goldText = new Color(0.604f, 0.392f, 0f);
        /// <summary>Cobalt of the evil-eye bead, for the HUD bead count.</summary>
        public Color beadText = new Color(0.12f, 0.31f, 0.75f);

        /// <summary>Warm dark floor colour #201712 (beyond the card table), translucent, behind menus.</summary>
        public Color dimColor = new Color(0.125f, 0.090f, 0.071f, 0.6f);
        public Color storyDimColor = new Color(0.09f, 0.065f, 0.05f, 0.82f);

        /// <summary>Tint on the HUD cards (white keeps the sprite's own cream).</summary>
        public Color hudPlateColor = Color.white;

        /// <summary>Ink outline and hard shadow on text and icons.</summary>
        public Color outlineColor = new Color(0.110f, 0.102f, 0.141f);
        public Color shadowColor = new Color(0.110f, 0.102f, 0.141f);

        /// <summary>Muted ink: lock icon and secondary captions on cream.</summary>
        public Color lockColor = new Color(0.353f, 0.337f, 0.4f);
        public Color focusTint = new Color(1f, 0.84f, 0.6f);
        public Color pressedTint = new Color(0.82f, 0.68f, 0.5f);
        public Color disabledTint = new Color(0.74f, 0.7f, 0.66f);

        /// <summary>Wait after the winning move before the win panel opens, so the win animation plays.</summary>
        [Header("Timing (seconds, unscaled)")]
        public float winPanelDelay = 1.0f;

        /// <summary>Screen fade-in; 0 turns it off (reduced motion).</summary>
        public float fadeInSeconds = 0.2f;

        public float buttonFadeSeconds = 0.08f;

        /// <summary>Golden pawn moment on the win panel: wait after the panel opens (lets the fade-in finish).</summary>
        [Header("Golden pawn moment (win panel)")]
        public float goldenDelaySeconds = 0.2f;

        /// <summary>Length of the scale punch (overshoot, then settle); 0 turns the punch and sparkles off (reduced motion).</summary>
        public float goldenPopSeconds = 0.35f;

        /// <summary>Scale the golden row starts from.</summary>
        public float goldenStartScale = 0.2f;

        /// <summary>Overshoot scale reached at <see cref="goldenPeakFraction"/> of the punch; it then settles to 1.</summary>
        public float goldenPeakScale = 1.25f;

        /// <summary>Fraction (0..1) of the punch spent growing to the peak.</summary>
        [Range(0.1f, 0.9f)] public float goldenPeakFraction = 0.6f;

        /// <summary>Number of gold sparks in the burst.</summary>
        public int sparkCount = 10;

        /// <summary>Spark square size in px (drawn as a 45 degree diamond).</summary>
        public float sparkSize = 28f;

        /// <summary>Distance in px a spark travels outward from the banner edge (sparks start on the banner's outline).</summary>
        public float sparkRadius = 96f;

        /// <summary>Seconds the burst lasts (spark flies out, shrinks and fades).</summary>
        public float sparkSeconds = 0.55f;

        /// <summary>Spark colour (bright gold, alpha is the starting opacity).</summary>
        public Color sparkColor = new Color(1f, 0.72f, 0.05f, 1f);

        /// <summary>
        /// Tint on the cream panel sprite behind the Golden pawn row (same ink rim and shadow as the HUD plates); this
        /// value turns the cream face #FFF6E0 into gold #FFC21A.
        /// </summary>
        public Color goldenBannerColor = new Color(1f, 0.789f, 0.116f);

        /// <summary>Pawn icon colour on the gold banner (pale gold; its ink outline and hard shadow separate it from the banner).</summary>
        public Color goldenIconColor = new Color(1f, 0.93f, 0.62f);

        /// <summary>Pawn icon size on the banner (about 1.6x <see cref="rewardIconSize"/>, on the 8 px grid).</summary>
        public float goldenIconSize = 104f;

        /// <summary>Banner padding in px, left/right and top/bottom (the card shadow inset is added on the right and bottom).</summary>
        public int goldenBannerPaddingX = 40;
        public int goldenBannerPaddingY = 8;
    }
}
