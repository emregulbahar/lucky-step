using UnityEngine;
using UnityEngine.UI;

namespace LuckyStep
{
    /// <summary>
    /// World story card (Story 006): the board dimmed further, a centred cream card with the world title, a gold rule,
    /// the story text and "press any key". ScreenFlow dismisses it on any key, click or gamepad South/Start.
    /// </summary>
    /// <example><code>story.SetStory("story.world1"); story.Show();</code></example>
    public sealed class StoryCard : UiScreen
    {
        private readonly Text _title;
        private readonly Text _body;
        private readonly Text _prompt;
        private string _storyKey = string.Empty;

        /// <summary>Builds the card (hidden).</summary>
        public StoryCard(UiRoot ui) : base(ui, "StoryCard", ui.Style.storyDimColor)
        {
            UiStyle s = ui.Style;
            RectTransform card = ui.CreateStack("Card", Root, s.storyWidth, true, s.storyPadding, s.spacing);
            _title = ui.CreateText("Title", card, true, s.titleSize, s.goldText, TextAnchor.MiddleCenter);
            ui.CreateDivider(card);
            _body = ui.CreateText("Body", card, false, s.bodySize, s.textDark, TextAnchor.MiddleCenter);
            _prompt = ui.CreateText("Prompt", card, false, s.captionSize, s.lockColor, TextAnchor.MiddleCenter);
        }

        /// <summary>Localization key prefix of the story to show: title is key + ".title", body key + ".body".</summary>
        public void SetStory(string storyKey)
        {
            _storyKey = storyKey;
        }

        /// <inheritdoc />
        public override void Refresh()
        {
            _title.text = Localization.Get(_storyKey + ".title");
            _body.text = Localization.Get(_storyKey + ".body");
            _prompt.text = Localization.Get("common.press_any_key");
        }
    }
}
