using LuckyStep;
using NUnit.Framework;
using System.IO;
using UnityEngine;

/// <summary>
/// EditMode tests for the Printed Board score track (Story 005): atlas cell layout of the four printed faces,
/// which face each count gets, and where the track ends.
/// </summary>
public sealed class TrackAtlasTests
{
    private static readonly Color32 Gold = new Color32(255, 194, 26, 255);
    private static readonly Color32 Ink = new Color32(28, 26, 36, 255);
    private static readonly Color32 Paper = new Color32(255, 255, 255, 255);
    private const string AtlasPath = "Assets/Art/Generated/TrackAtlas.png";

    [Test]
    public void CellUV_SameNumberDifferentFaces_StackedOneBlockApart()
    {
        Vector4 plain = TrackAtlas.CellUV(7, TrackAtlas.Face.Plain);
        Vector4 lucky = TrackAtlas.CellUV(7, TrackAtlas.Face.Lucky);
        float block = 4f * 128f / TrackAtlas.Height;

        Assert.AreEqual(plain.x, lucky.x, 1e-6f);
        Assert.AreEqual(plain.z, lucky.z, 1e-6f);
        Assert.AreEqual(plain.y + 2f * block, lucky.y, 1e-6f);
    }

    [Test]
    public void CellUV_AllFacesAndNumbers_InsideTheTexture()
    {
        foreach (TrackAtlas.Face face in System.Enum.GetValues(typeof(TrackAtlas.Face)))
        {
            for (int n = 0; n <= TrackAtlas.MaxNumber; n++)
            {
                Vector4 uv = TrackAtlas.CellUV(n, face);
                Assert.That(uv.x, Is.GreaterThanOrEqualTo(0f).And.LessThan(uv.z));
                Assert.That(uv.y, Is.GreaterThanOrEqualTo(0f).And.LessThan(uv.w));
                Assert.That(uv.z, Is.LessThanOrEqualTo(1f + 1e-6f));
                Assert.That(uv.w, Is.LessThanOrEqualTo(1f + 1e-6f));
            }
        }
    }

    [Test]
    public void AtlasPng_LuckyAndDeadlyCells_HaveGoldAndInkBackgrounds()
    {
        // Contract between tools/textures/make_track_atlas.py and the TrackAtlas layout (reads the generated asset).
        var atlas = new Texture2D(2, 2);
        try
        {
            Assert.IsTrue(atlas.LoadImage(File.ReadAllBytes(AtlasPath)), "Could not decode " + AtlasPath);
            Assert.AreEqual(TrackAtlas.Width, atlas.width);
            Assert.AreEqual(TrackAtlas.Height, atlas.height);
            // Bottom-left pixel of a cell is background (glyphs keep a margin).
            Assert.AreEqual(Gold, Corner(atlas, 7, TrackAtlas.Face.Lucky));
            Assert.AreEqual(Ink, Corner(atlas, 13, TrackAtlas.Face.Deadly));
            Assert.AreEqual(Paper, Corner(atlas, 12, TrackAtlas.Face.Plain));
            Assert.AreEqual(Paper, Corner(atlas, 0, TrackAtlas.Face.Start));
        }
        finally
        {
            Object.DestroyImmediate(atlas);
        }
    }

    [Test]
    public void TrackFace_DefaultRules_StartLuckyDeadlyPlain()
    {
        RuleSet rules = RuleSet.Default;

        Assert.AreEqual(TrackAtlas.Face.Start, BoardView.TrackFace(rules, 0, 0));
        Assert.AreEqual(TrackAtlas.Face.Lucky, BoardView.TrackFace(rules, 7, 0));
        Assert.AreEqual(TrackAtlas.Face.Deadly, BoardView.TrackFace(rules, 13, 0));
        Assert.AreEqual(TrackAtlas.Face.Plain, BoardView.TrackFace(rules, 12, 0));
    }

    [Test]
    public void TrackFace_World4StartsAtFive_StartOnFiveDeadlyBeatsLucky()
    {
        RuleSet rules = RuleSet.World4;

        Assert.AreEqual(TrackAtlas.Face.Start, BoardView.TrackFace(rules, 5, 5));
        Assert.AreEqual(TrackAtlas.Face.Lucky, BoardView.TrackFace(rules, 8, 5));
        Assert.AreEqual(TrackAtlas.Face.Deadly, BoardView.TrackFace(rules, 24, 5)); // multiple of 8 but contains a 4
    }

    [Test]
    public void TrackEnd_DefaultRules_EndsAtThirteen()
    {
        Assert.AreEqual(13, BoardView.TrackEnd(RuleSet.Default, 30));
    }

    private static Color32 Corner(Texture2D atlas, int n, TrackAtlas.Face face)
    {
        Vector4 uv = TrackAtlas.CellUV(n, face);
        return atlas.GetPixel(Mathf.RoundToInt(uv.x * atlas.width), Mathf.RoundToInt(uv.y * atlas.height));
    }
}
