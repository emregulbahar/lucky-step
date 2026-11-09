using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LuckyStep;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for <see cref="LocalizationTable"/> and the shipped string files (Story 006): lookup, missing keys,
/// and English/Turkish tables kept in step (same keys, same {n} placeholders, Turkish all upper case for the caps-only font).
/// </summary>
public sealed class LocalizationTests
{
    private static readonly Regex Placeholder = new Regex(@"\{\d+\}");

    private static LocalizationTable LoadShipped(Language language)
    {
        TextAsset asset = Resources.Load<TextAsset>(Localization.ResourcePath(language));
        Assert.IsNotNull(asset, "missing Resources/" + Localization.ResourcePath(language));
        return LocalizationTable.Parse(asset.text);
    }

    [Test]
    public void Get_KnownKey_ReturnsValue()
    {
        LocalizationTable table = LocalizationTable.Parse("{\"entries\":[{\"key\":\"menu.play\",\"value\":\"Play\"}]}");

        Assert.AreEqual("Play", table.Get("menu.play"));
        Assert.IsTrue(table.Contains("menu.play"));
    }

    [Test]
    public void Get_MissingKey_ReturnsMarkedKey()
    {
        LocalizationTable table = LocalizationTable.Parse("{\"entries\":[]}");

        Assert.AreEqual("#menu.play#", table.Get("menu.play"));
        Assert.IsFalse(table.Contains("menu.play"));
    }

    [Test]
    public void Parse_EmptyText_EmptyTable()
    {
        Assert.AreEqual(0, LocalizationTable.Parse(string.Empty).Keys.Count());
        Assert.AreEqual(0, LocalizationTable.Parse(null).Keys.Count());
    }

    [Test]
    public void ShippedTables_EnglishAndTurkish_HaveSameKeys()
    {
        var en = new HashSet<string>(LoadShipped(Language.English).Keys);
        var tr = new HashSet<string>(LoadShipped(Language.Turkish).Keys);

        CollectionAssert.IsNotEmpty(en);
        CollectionAssert.AreEquivalent(en, tr,
            "only in en: " + string.Join(", ", en.Except(tr)) + "; only in tr: " + string.Join(", ", tr.Except(en)));
    }

    [Test]
    public void ShippedTables_EveryKey_SamePlaceholders()
    {
        LocalizationTable en = LoadShipped(Language.English);
        LocalizationTable tr = LoadShipped(Language.Turkish);

        foreach (string key in en.Keys)
        {
            string[] enHoles = Placeholder.Matches(en.Get(key)).Cast<Match>().Select(m => m.Value).OrderBy(v => v).ToArray();
            string[] trHoles = Placeholder.Matches(tr.Get(key)).Cast<Match>().Select(m => m.Value).OrderBy(v => v).ToArray();
            CollectionAssert.AreEqual(enHoles, trHoles, "placeholders differ for " + key);
        }
    }

    [Test]
    public void ShippedTurkish_EveryValue_HasNoLowercaseLetters()
    {
        // Kenney Future is caps-only and its lowercase i has no dot, so Turkish must be written in upper case (İ / I).
        LocalizationTable tr = LoadShipped(Language.Turkish);

        foreach (string key in tr.Keys)
        {
            string value = tr.Get(key);
            Assert.IsFalse(value.Any(char.IsLower), "lowercase letter in tr " + key + ": " + value);
        }
    }

    [Test]
    public void ShippedTables_RequiredKeys_Present()
    {
        string[] required =
        {
            "menu.play", "menu.level_select", "menu.language", "hud.steps", "hud.par", "win.title", "win.next",
            "story.world1.title", "story.world1.body", "story.world4.title", "story.world4.body",
        };
        LocalizationTable en = LoadShipped(Language.English);

        foreach (string key in required) Assert.IsTrue(en.Contains(key), "missing en key " + key);
    }
}
