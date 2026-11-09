using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LuckyStep
{
    /// <summary>
    /// Base of the runtime-built screens (Story 006): a full-screen root under the UI canvas with an optional dim
    /// overlay (the board stays visible behind it), a fade-in on show, a default focus for keyboard/gamepad and a
    /// hook that re-reads localized text.
    /// </summary>
    /// <example><code>menu.Show(); /* each frame */ menu.Tick(Time.unscaledDeltaTime); /* later */ menu.Hide();</code></example>
    public abstract class UiScreen
    {
        private readonly CanvasGroup _group;

        /// <summary>Builds the root; <paramref name="dim"/> null means no overlay (HUD).</summary>
        protected UiScreen(UiRoot ui, string name, Color? dim)
        {
            Ui = ui;
            Root = ui.CreateRect(name, ui.CanvasTransform);
            UiRoot.Stretch(Root, 0f);
            _group = Root.gameObject.AddComponent<CanvasGroup>();
            if (dim.HasValue)
            {
                Image overlay = ui.CreateImage("Dim", Root, null, dim.Value);
                UiRoot.Stretch(overlay.rectTransform, 0f);
                overlay.raycastTarget = true;
            }
            Root.gameObject.SetActive(false);
        }

        /// <summary>Factory and style.</summary>
        protected UiRoot Ui { get; }

        /// <summary>Full-screen root of this screen.</summary>
        public RectTransform Root { get; }

        /// <summary>True while shown.</summary>
        public bool IsVisible => Root.gameObject.activeSelf;

        /// <summary>The control that gets focus when shown (null clears focus).</summary>
        protected virtual GameObject DefaultSelection => null;

        /// <summary>Shows the screen, refreshes its text and focuses <see cref="DefaultSelection"/>.</summary>
        public void Show()
        {
            Root.gameObject.SetActive(true);
            _group.alpha = Ui.Style.fadeInSeconds > 0f ? 0f : 1f;
            Refresh();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(DefaultSelection);
        }

        /// <summary>Hides the screen.</summary>
        public void Hide() => Root.gameObject.SetActive(false);

        /// <summary>Advances the fade-in (unscaled seconds).</summary>
        public void Tick(float deltaTime)
        {
            if (_group.alpha >= 1f) return;
            float fade = Ui.Style.fadeInSeconds;
            _group.alpha = fade > 0f ? Mathf.MoveTowards(_group.alpha, 1f, deltaTime / fade) : 1f;
        }

        /// <summary>Restores focus after a mouse click cleared it, so keyboard/gamepad navigation keeps working.</summary>
        public void EnsureSelection()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return;
            GameObject current = eventSystem.currentSelectedGameObject;
            if (current != null && current.activeInHierarchy) return;
            eventSystem.SetSelectedGameObject(DefaultSelection);
        }

        /// <summary>Re-reads all text in the current language.</summary>
        public abstract void Refresh();
    }
}
