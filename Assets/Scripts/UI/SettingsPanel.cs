using System;
using UnityEngine;
using UnityEngine.UI;

namespace LuckyStep
{
    /// <summary>
    /// Settings card (design/quick-specs/audio-settings-addition-2026-11-10.md): a title, a Music row and an Effects
    /// row, each "[-] NN% [+]", and Back. Each press moves the value one <see cref="VolumeSettings.Step"/>; the
    /// value text updates at once. Display and input only: the values live in <see cref="VolumeSettings"/>.
    /// </summary>
    /// <example><code>var settings = new SettingsPanel(ui, ui.Volume); settings.BackClicked += () => Enter(State.Menu);</code></example>
    public sealed class SettingsPanel : UiScreen
    {
        private readonly VolumeSettings _volume;
        private readonly Text _title;
        private readonly Text _musicLabel;
        private readonly Text _musicValue;
        private readonly Text _effectsLabel;
        private readonly Text _effectsValue;
        private readonly Text _back;
        private readonly Button _firstButton;

        /// <summary>Back pressed.</summary>
        public event Action BackClicked;

        /// <summary>Builds the card (hidden).</summary>
        public SettingsPanel(UiRoot ui, VolumeSettings volume) : base(ui, "Settings", ui.Style.dimColor)
        {
            _volume = volume;
            UiStyle s = ui.Style;
            RectTransform card = ui.CreateStack("Card", Root, s.settingsWidth, true, s.cardPadding, s.spacing);
            _title = ui.CreateText("Title", card, true, s.headingSize, s.goldText, TextAnchor.MiddleCenter);
            _firstButton = Row(ui, card, "Music", out _musicLabel, out _musicValue, volume.ChangeMusic);
            Row(ui, card, "Effects", out _effectsLabel, out _effectsValue, volume.ChangeEffects);
            ui.CreateButton("Back", card, false, s.smallButtonSize, out _back).onClick.AddListener(() => BackClicked?.Invoke());
            volume.Changed += RefreshValues;
        }

        /// <inheritdoc />
        protected override GameObject DefaultSelection => _firstButton.gameObject;

        /// <summary>One row: the name, then [-] value [+]. Returns the [-] button.</summary>
        private static Button Row(UiRoot ui, Transform card, string name, out Text label, out Text value, Action<int> change)
        {
            UiStyle s = ui.Style;
            RectTransform row = ui.CreateRow(name, card, s.spacing, TextAnchor.MiddleCenter);
            label = ui.CreateText("Label", row, false, s.bodySize, s.textDark, TextAnchor.MiddleLeft);
            UiRoot.Size(label, s.settingsLabelWidth, -1f);
            Vector2 square = new Vector2(s.buttonSize.y, s.buttonSize.y);
            Button minus = ui.CreateButton("Minus", row, false, square, out Text minusText);
            minusText.text = "-";
            BigSign(minusText, s);
            minus.onClick.AddListener(() => change(-1));
            value = ui.CreateText("Value", row, true, s.bodySize, s.textDark, TextAnchor.MiddleCenter);
            UiRoot.Size(value, s.settingsValueWidth, -1f);
            Button plus = ui.CreateButton("Plus", row, false, square, out Text plusText);
            plusText.text = "+";
            BigSign(plusText, s);
            plus.onClick.AddListener(() => change(1));
            return minus;
        }

        // The +/- glyphs read small at body size: heading size, no best-fit shrink.
        private static void BigSign(Text sign, UiStyle s)
        {
            sign.resizeTextForBestFit = false;
            sign.fontSize = s.headingSize;
        }

        private void RefreshValues()
        {
            _musicValue.text = _volume.Music + "%";
            _effectsValue.text = _volume.Effects + "%";
        }

        /// <inheritdoc />
        public override void Refresh()
        {
            _title.text = Localization.Get("settings.title");
            _musicLabel.text = Localization.Get("settings.music");
            _effectsLabel.text = Localization.Get("settings.effects");
            _back.text = Localization.Get("common.back");
            RefreshValues();
        }
    }
}
