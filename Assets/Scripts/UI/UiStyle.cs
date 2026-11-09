using System;
using UnityEngine;

namespace LuckyStep
{
    /// <summary>
    /// Story 006 UI tuning data: canvas scaling, type scale, the 8 px spacing grid, sizes, the warm board-game palette
    /// (cream panels, dark brown text, gold accents) and timings. Serialized on <see cref="UiRoot"/>, so every value is
    /// editable in the Inspector; pixel values are at the 1920x1080 reference resolution.
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

        /// <summary>Card widths in px.</summary>
        [Header("Sizes (px)")]
        public float menuWidth = 640f;
        public float selectWidth = 1440f;
        public float storyWidth = 1200f;
        public float winWidth = 1040f;

        /// <summary>Full-width menu buttons (at least 88 px tall at 1080p).</summary>
        public Vector2 buttonSize = new Vector2(480f, 96f);

        /// <summary>Buttons that sit side by side (win panel, Back).</summary>
        public Vector2 smallButtonSize = new Vector2(288f, 96f);

        public float levelButtonSize = 112f;
        public float worldLabelWidth = 640f;
        public float badgeSize = 48f;
        public float badgeOffset = 12f;
        public float lockIconSize = 56f;
        public float legendIconSize = 40f;
        public float rewardIconSize = 64f;
        public float dividerHeight = 8f;
        public float buttonLabelPadding = 16f;

        /// <summary>Lifts button labels off the darker depth band at the bottom of the Kenney button sprites.</summary>
        public float buttonLabelLift = 12f;

        /// <summary>Scales 9-slice corners down (128 px source sprites drawn at about 96 px).</summary>
        public float slicedPixelsPerUnitMultiplier = 1.333f;

        public Vector2 outlineDistance = new Vector2(2f, -2f);
        public Vector2 cardShadowDistance = new Vector2(0f, -8f);

        /// <summary>Tint on the cream panel sprite (white keeps the sprite's own cream).</summary>
        [Header("Colours")]
        public Color panelColor = Color.white;
        public Color textDark = new Color(0.24f, 0.13f, 0.06f);
        public Color textLight = new Color(1f, 0.96f, 0.88f);
        public Color gold = new Color(0.98f, 0.76f, 0.20f);

        /// <summary>Darker gold for large text on cream panels (keeps contrast).</summary>
        public Color goldText = new Color(0.62f, 0.38f, 0.04f);

        public Color dimColor = new Color(0.07f, 0.04f, 0.02f, 0.6f);
        public Color storyDimColor = new Color(0.05f, 0.03f, 0.02f, 0.82f);
        public Color hudPlateColor = new Color(0.22f, 0.12f, 0.06f, 0.82f);
        public Color outlineColor = new Color(0.12f, 0.06f, 0.02f, 0.9f);
        public Color shadowColor = new Color(0f, 0f, 0f, 0.35f);
        public Color lockColor = new Color(0.36f, 0.24f, 0.16f);
        public Color focusTint = new Color(1f, 0.84f, 0.6f);
        public Color pressedTint = new Color(0.82f, 0.68f, 0.5f);
        public Color disabledTint = new Color(0.74f, 0.7f, 0.66f);

        /// <summary>Wait after the winning move before the win panel opens, so the win animation plays.</summary>
        [Header("Timing (seconds, unscaled)")]
        public float winPanelDelay = 1.0f;

        /// <summary>Screen fade-in; 0 turns it off (reduced motion).</summary>
        public float fadeInSeconds = 0.2f;

        public float buttonFadeSeconds = 0.08f;
    }
}
