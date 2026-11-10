using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LuckyStep
{
    /// <summary>
    /// Story 006 UI entry point. On Awake builds a Screen Space Overlay canvas (scaled from 1920x1080), an EventSystem
    /// with the Input System UI module if the scene has none, and the screens, then runs <see cref="ScreenFlow"/>.
    /// Also the small widget factory the screens share (legacy <see cref="Text"/> with the Turkish-patched Kenney fonts,
    /// 9-sliced card and button sprites). All references are set by BuildBoardScene; the UI itself is never saved in the scene.
    /// </summary>
    /// <example>
    /// <code>
    /// Button play = ui.CreateButton("Play", card, true, ui.Style.buttonSize, out Text label);
    /// label.text = Localization.Get("menu.play");
    /// </code>
    /// </example>
    public sealed class UiRoot : MonoBehaviour
    {
        [SerializeField] private PlayerInputController _controller;
        [SerializeField] private LevelCatalog _catalog;
        [SerializeField] private Font _titleFont;
        [SerializeField] private Font _bodyFont;
        [SerializeField] private Sprite _panel;
        [SerializeField] private Sprite _buttonPrimary;
        [SerializeField] private Sprite _buttonSecondary;
        [SerializeField] private Sprite _levelButton;
        [SerializeField] private Sprite _levelButtonLocked;
        [SerializeField] private Sprite _lockIcon;
        [SerializeField] private Sprite _tickIcon;
        [SerializeField] private Sprite _pawnIcon;
        [SerializeField] private Sprite _arrowIcon;
        [SerializeField] private UiStyle _style = new UiStyle();

        private ScreenFlow _flow;

        /// <summary>Raised when any UI button is pressed (sound hooks in here; the UI does not know about audio).</summary>
        public event System.Action ButtonClicked;

        /// <summary>The player's music and effects volume (saved, except in QA runs).</summary>
        public VolumeSettings Volume { get; private set; }

        /// <summary>True while a level is on screen (playing or its win panel); false on the menu, level select and story cards.</summary>
        public bool InGame => _flow != null && _flow.InGame;

        /// <summary>Raises <see cref="ButtonClicked"/>; for buttons a screen builds itself.</summary>
        public void NotifyButtonClicked() => ButtonClicked?.Invoke();

        /// <summary>Sizes, colours and timings.</summary>
        public UiStyle Style => _style;

        /// <summary>The canvas all screens are parented to.</summary>
        public Transform CanvasTransform { get; private set; }

        /// <summary>Unlocked level button face.</summary>
        public Sprite LevelButton => _levelButton;

        /// <summary>Locked level button face.</summary>
        public Sprite LevelButtonLocked => _levelButtonLocked;

        /// <summary>White lock icon (tinted).</summary>
        public Sprite LockIcon => _lockIcon;

        /// <summary>Completed tick icon.</summary>
        public Sprite TickIcon => _tickIcon;

        /// <summary>White pawn icon (tinted gold for the Golden pawn).</summary>
        public Sprite PawnIcon => _pawnIcon;

        /// <summary>White up arrow (HUD arrow-key caps; rotate for other directions).</summary>
        public Sprite ArrowIcon => _arrowIcon;

        private void Awake()
        {
            if (_controller == null || _catalog == null || _titleFont == null || _bodyFont == null)
            {
                Debug.LogError("UiRoot: controller, catalog and fonts must be assigned (run Lucky Step/Build Board Scene).", this);
                enabled = false;
                return;
            }
            BuildCanvas();
            IProgressStore store = QaArgs.IsQaRun ? new MemoryProgressStore() : (IProgressStore)new PlayerPrefsProgressStore();
            Volume = new VolumeSettings(!QaArgs.IsQaRun);
            // QA captures count frames; with runInBackground off (the player setting) an unfocused window stalls them.
            if (QaArgs.IsQaRun) Application.runInBackground = true;
            _flow = new ScreenFlow(this, _controller, _catalog, new Progress(store));
        }

        private void Start() => _flow.Begin();

        private void Update() => _flow.Tick(Time.unscaledDeltaTime);

        private void OnDestroy() => _flow?.Dispose();

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Snap every element to whole pixels: text laid out on a half pixel was sampled between rows and read blurry.
            canvas.pixelPerfect = true;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = _style.referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = _style.matchWidthOrHeight;
            canvasGo.AddComponent<GraphicRaycaster>();
            CanvasTransform = canvasGo.transform;

            if (FindAnyObjectByType<EventSystem>() != null) return;
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(transform, false);
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        /// <summary>New empty UI object under <paramref name="parent"/>.</summary>
        public RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Stretches <paramref name="rect"/> over its parent, inset by <paramref name="inset"/> px.</summary>
        public static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Pins <paramref name="rect"/> to a parent corner (0..1 per axis) at <paramref name="offset"/> px.</summary>
        public static void Pin(RectTransform rect, Vector2 corner, Vector2 offset)
        {
            rect.anchorMin = corner;
            rect.anchorMax = corner;
            rect.pivot = corner;
            rect.anchoredPosition = offset;
        }

        /// <summary>Fixes the layout size of <paramref name="target"/>; a negative value leaves that axis free.</summary>
        public static LayoutElement Size(Component target, float width, float height)
        {
            LayoutElement element = target.GetComponent<LayoutElement>();
            if (element == null) element = target.gameObject.AddComponent<LayoutElement>();
            if (width >= 0f) element.minWidth = element.preferredWidth = width;
            if (height >= 0f) element.minHeight = element.preferredHeight = height;
            return element;
        }

        /// <summary>Image; 9-sliced when the sprite has borders, aspect-preserved otherwise. Not a raycast target.</summary>
        public Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            Image image = CreateRect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = _style.slicedPixelsPerUnitMultiplier;
            }
            else
            {
                image.preserveAspect = true;
            }
            return image;
        }

        /// <summary>Square icon of <paramref name="size"/> px; <paramref name="outline"/> adds a dark rim for light backgrounds.</summary>
        public Image CreateIcon(string name, Transform parent, Sprite sprite, Color color, float size, bool outline)
        {
            Image icon = CreateImage(name, parent, sprite, color);
            icon.rectTransform.sizeDelta = new Vector2(size, size);
            Size(icon, size, size);
            if (outline) AddOutline(icon);
            return icon;
        }

        /// <summary>Legacy UI text (title or body font), wrapping, not a raycast target.</summary>
        public Text CreateText(string name, Transform parent, bool titleFont, int size, Color color, TextAnchor alignment)
        {
            Text text = CreateRect(name, parent).gameObject.AddComponent<Text>();
            text.font = titleFont ? _titleFont : _bodyFont;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.lineSpacing = _style.lineSpacing;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Adds the style's dark outline to a text or icon (contrast over the board).</summary>
        public void AddOutline(Graphic graphic)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = _style.outlineColor;
            outline.effectDistance = _style.outlineDistance;
        }

        /// <summary>Adds the style's hard ink offset shadow (no blur); add after <see cref="AddOutline"/> to shadow the outline too.</summary>
        public void AddHardShadow(Graphic graphic)
        {
            Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = _style.shadowColor;
            shadow.effectDistance = _style.titleShadowDistance;
        }

        /// <summary>Tint colours shared by every button (hover and gamepad focus look the same).</summary>
        public ColorBlock ButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = _style.focusTint;
            colors.selectedColor = _style.focusTint;
            colors.pressedColor = _style.pressedTint;
            colors.disabledColor = _style.disabledTint;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = _style.buttonFadeSeconds;
            return colors;
        }

        /// <summary>
        /// Gold (primary) or cream (secondary) button of <paramref name="size"/> with a centred label that shrinks to the
        /// caption size for long translations.
        /// </summary>
        public Button CreateButton(string name, Transform parent, bool primary, Vector2 size, out Text label)
        {
            Image face = CreateImage(name, parent, primary ? _buttonPrimary : _buttonSecondary, Color.white);
            face.raycastTarget = true;
            face.rectTransform.sizeDelta = size;
            Size(face, size.x, size.y);
            Button button = face.gameObject.AddComponent<Button>();
            button.onClick.AddListener(NotifyButtonClicked);
            button.targetGraphic = face;
            button.colors = ButtonColors();
            label = CreateText("Label", face.transform, false, _style.bodySize, _style.textDark, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, _style.buttonLabelPadding);
            label.rectTransform.offsetMin += new Vector2(0f, _style.buttonLabelLift);
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = _style.captionSize;
            label.resizeTextMaxSize = _style.bodySize;
            return button;
        }

        /// <summary>
        /// Vertical stack, optionally on the cream card with a drop shadow. At the top level it sizes itself to its
        /// content and is centred (<paramref name="width"/> 0 or less also fits the width); inside a layout group the
        /// parent sizes it.
        /// </summary>
        public RectTransform CreateStack(string name, Transform parent, float width, bool card, int padding, int spacing)
        {
            RectTransform rect;
            if (card)
            {
                Image background = CreateImage(name, parent, _panel, _style.panelColor);
                if (_style.cardShadowDistance != Vector2.zero)
                {
                    Shadow shadow = background.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = _style.shadowColor;
                    shadow.effectDistance = _style.cardShadowDistance;
                }
                rect = background.rectTransform;
            }
            else
            {
                rect = CreateRect(name, parent);
            }
            VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            int inset = card ? _style.cardShadowInset : 0; // keeps content centred inside the ink border
            layout.padding = new RectOffset(padding, padding + inset, padding, padding + inset);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            if (parent.GetComponent<LayoutGroup>() == null)
            {
                ContentSizeFitter fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                if (width > 0f) rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                else fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            return rect;
        }

        /// <summary>Horizontal row; children keep their preferred sizes and are aligned by <paramref name="alignment"/>.</summary>
        public RectTransform CreateRow(string name, Transform parent, int spacing, TextAnchor alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rect;
        }

        /// <summary>Thin gold rule across a card.</summary>
        public Image CreateDivider(Transform parent)
        {
            Image divider = CreateImage("Divider", parent, null, _style.gold);
            Size(divider, -1f, _style.dividerHeight);
            return divider;
        }
    }
}
