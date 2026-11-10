using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace LuckyStep
{
    /// <summary>UI languages. English is the default.</summary>
    public enum Language
    {
        /// <summary>Assets/Resources/Localization/strings_en.json</summary>
        English,

        /// <summary>Assets/Resources/Localization/strings_tr.json</summary>
        Turkish,
    }

    /// <summary>
    /// One language's strings, parsed from JSON of the form { "entries": [ { "key": "menu.play", "value": "Play" } ] }.
    /// Pure lookup, no Unity state, so it is unit-testable.
    /// </summary>
    /// <example>
    /// <code>
    /// LocalizationTable table = LocalizationTable.Parse(jsonText);
    /// string play = table.Get("menu.play");   // "#menu.play#" if the key is missing
    /// </code>
    /// </example>
    public sealed class LocalizationTable
    {
        [Serializable]
        private sealed class FileData
        {
            public List<Entry> entries = new List<Entry>();
        }

        [Serializable]
        private sealed class Entry
        {
            public string key;
            public string value;
        }

        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        /// <summary>Every key in the table.</summary>
        public IEnumerable<string> Keys => _values.Keys;

        /// <summary>
        /// Parses a table. Null, empty or malformed JSON gives an empty table (logged); a duplicate key keeps the last
        /// value (logged).
        /// </summary>
        public static LocalizationTable Parse(string json)
        {
            var table = new LocalizationTable();
            if (string.IsNullOrEmpty(json)) return table;
            FileData data;
            try
            {
                data = JsonUtility.FromJson<FileData>(json);
            }
            catch (ArgumentException e)
            {
                Debug.LogError("Localization table unreadable: " + e.Message);
                return table;
            }
            if (data == null || data.entries == null) return table;
            foreach (Entry entry in data.entries)
            {
                if (string.IsNullOrEmpty(entry.key)) continue;
                if (table._values.ContainsKey(entry.key)) Debug.LogWarning("Localization: duplicate key " + entry.key);
                table._values[entry.key] = entry.value ?? string.Empty;
            }
            return table;
        }

        /// <summary>True if the table has <paramref name="key"/>.</summary>
        public bool Contains(string key) => _values.ContainsKey(key);

        /// <summary>The string for <paramref name="key"/>, or "#key#" when missing (visible in game, never a crash).</summary>
        public string Get(string key) => _values.TryGetValue(key, out string value) ? value : "#" + key + "#";
    }

    /// <summary>
    /// All UI text goes through here (Story 006). Loads the current language's table from
    /// Resources/Localization on first use; the language is saved in PlayerPrefs. Screens re-read their text when
    /// <see cref="LanguageChanged"/> fires.
    /// Note: strings are written in sentence case with Turkish casing (i / İ, ı / I) for the Fredoka UI font; do not
    /// call ToUpper() or ToLower() on localized text (culture-invariant casing breaks Turkish i).
    /// </summary>
    /// <example>
    /// <code>
    /// label.text = Localization.Get("menu.play");
    /// steps.text = Localization.Format("hud.steps", state.Counter);
    /// Localization.LanguageChanged += Refresh;
    /// Localization.SetLanguage(Language.Turkish);
    /// </code>
    /// </example>
    public static class Localization
    {
        private const string LanguagePrefKey = "LuckyStep.Language";

        private static LocalizationTable _table;
        private static Language _language;

        /// <summary>Raised after <see cref="SetLanguage"/> switches to a different language.</summary>
        public static event Action LanguageChanged;

        /// <summary>The current language (the saved one, English if none was saved).</summary>
        public static Language Current
        {
            get
            {
                EnsureLoaded();
                return _language;
            }
        }

        /// <summary>The string for <paramref name="key"/> in the current language, or "#key#" when missing.</summary>
        public static string Get(string key)
        {
            EnsureLoaded();
            return _table.Get(key);
        }

        /// <summary>
        /// The string for <paramref name="key"/> with {0}, {1} ... filled from <paramref name="args"/> (invariant
        /// culture, so numbers never get locale separators).
        /// </summary>
        public static string Format(string key, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(key), args);
        }

        /// <summary>Switches language, saves it and raises <see cref="LanguageChanged"/> (only if it changed).</summary>
        public static void SetLanguage(Language language)
        {
            EnsureLoaded();
            if (language == _language) return;
            _language = language;
            _table = LoadTable(language);
            PlayerPrefs.SetInt(LanguagePrefKey, (int)language);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
        }

        /// <summary>Resources path of a language's table, e.g. "Localization/strings_tr".</summary>
        public static string ResourcePath(Language language)
        {
            return language == Language.Turkish ? "Localization/strings_tr" : "Localization/strings_en";
        }

        private static void EnsureLoaded()
        {
            if (_table != null) return;
            int saved = PlayerPrefs.GetInt(LanguagePrefKey, (int)Language.English);
            _language = Enum.IsDefined(typeof(Language), saved) ? (Language)saved : Language.English;
            _table = LoadTable(_language);
        }

        private static LocalizationTable LoadTable(Language language)
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath(language));
            if (asset == null) Debug.LogError("Localization table missing: Resources/" + ResourcePath(language));
            return LocalizationTable.Parse(asset != null ? asset.text : null);
        }

        // Static state survives entering Play Mode when domain reload is disabled; start clean each time.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _table = null;
            LanguageChanged = null;
        }
    }
}
