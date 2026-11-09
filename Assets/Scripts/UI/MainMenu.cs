using System;
using UnityEngine;
using UnityEngine.UI;

namespace LuckyStep
{
    /// <summary>
    /// Main menu (Story 006): game title over the dimmed board, then a cream card with Play (continue), Level select,
    /// the EN/TR language toggle and Quit (hidden on WebGL, where a page cannot quit).
    /// </summary>
    /// <example><code>var menu = new MainMenu(ui); menu.PlayClicked += Continue; menu.Show();</code></example>
    public sealed class MainMenu : UiScreen
    {
        private readonly Text _title;
        private readonly Text _play;
        private readonly Text _levelSelect;
        private readonly Text _language;
        private readonly Text _quit;
        private readonly Button _playButton;

        /// <summary>Play: continue at the first unlocked, not completed level.</summary>
        public event Action PlayClicked;

        /// <summary>Level select pressed.</summary>
        public event Action LevelSelectClicked;

        /// <summary>Builds the menu (hidden).</summary>
        public MainMenu(UiRoot ui) : base(ui, "MainMenu", ui.Style.dimColor)
        {
            UiStyle s = ui.Style;
            RectTransform column = ui.CreateStack("Column", Root, s.menuWidth, false, 0, s.spacing * 2);
            _title = ui.CreateText("Title", column, true, s.titleSize, s.gold, TextAnchor.MiddleCenter);
            ui.AddOutline(_title);
            RectTransform card = ui.CreateStack("Card", column, 0f, true, s.cardPadding, s.spacing);

            _playButton = ui.CreateButton("Play", card, true, s.buttonSize, out _play);
            _playButton.onClick.AddListener(() => PlayClicked?.Invoke());
            ui.CreateButton("LevelSelect", card, false, s.buttonSize, out _levelSelect)
                .onClick.AddListener(() => LevelSelectClicked?.Invoke());
            ui.CreateButton("Language", card, false, s.buttonSize, out _language).onClick.AddListener(ToggleLanguage);
            Button quit = ui.CreateButton("Quit", card, false, s.buttonSize, out _quit);
            quit.onClick.AddListener(Application.Quit);
            quit.gameObject.SetActive(Application.platform != RuntimePlatform.WebGLPlayer);
        }

        /// <inheritdoc />
        protected override GameObject DefaultSelection => _playButton.gameObject;

        /// <inheritdoc />
        public override void Refresh()
        {
            _title.text = Localization.Get("game.title");
            _play.text = Localization.Get("menu.play");
            _levelSelect.text = Localization.Get("menu.level_select");
            string current = Localization.Current == Language.Turkish ? "lang.turkish" : "lang.english";
            _language.text = Localization.Format("menu.language", Localization.Get(current));
            _quit.text = Localization.Get("menu.quit");
        }

        private static void ToggleLanguage()
        {
            // LanguageChanged makes ScreenFlow refresh every screen.
            Localization.SetLanguage(Localization.Current == Language.English ? Language.Turkish : Language.English);
        }
    }
}
