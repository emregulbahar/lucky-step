using System.Collections.Generic;
using LuckyStep;
using NUnit.Framework;

/// <summary>EditMode tests for <see cref="Progress"/> (Story 006): completion, Golden pawn, unlock order, save round trip.</summary>
public sealed class ProgressTests
{
    private sealed class MemoryStore : IProgressStore
    {
        public string Json;
        public int SaveCount;

        public string Load() => Json;

        public void Save(string json)
        {
            Json = json;
            SaveCount++;
        }
    }

    // Two worlds: Level01-03, then Level04-05.
    private static readonly List<List<string>> Order = new List<List<string>>
    {
        new List<string> { "Level01", "Level02", "Level03" },
        new List<string> { "Level04", "Level05" },
    };

    [Test]
    public void NewProgress_EmptyStore_NothingCompletedNoPawns()
    {
        var progress = new Progress(new MemoryStore());

        Assert.IsFalse(progress.IsCompleted("Level01"));
        Assert.IsFalse(progress.HasGoldenPawn("Level01"));
        Assert.AreEqual(0, progress.GoldenPawnCount);
    }

    [Test]
    public void IsUnlocked_FirstLevel_AlwaysTrue()
    {
        var progress = new Progress(new MemoryStore());

        Assert.IsTrue(progress.IsUnlocked("Level01", Order));
        Assert.IsFalse(progress.IsUnlocked("Level02", Order));
    }

    [Test]
    public void IsUnlocked_AfterPreviousCompleted_True()
    {
        var progress = new Progress(new MemoryStore());

        progress.MarkCompleted("Level01", 10, -1);

        Assert.IsTrue(progress.IsUnlocked("Level02", Order));
        Assert.IsFalse(progress.IsUnlocked("Level03", Order));
    }

    [Test]
    public void IsUnlocked_UnknownLevel_False()
    {
        var progress = new Progress(new MemoryStore());

        Assert.IsFalse(progress.IsUnlocked("Level99", Order));
    }

    [Test]
    public void IsUnlocked_FirstLevelOfEveryWorld_OpenFromStart()
    {
        var progress = new Progress(new MemoryStore());

        Assert.IsTrue(progress.IsUnlocked("Level04", Order));
        Assert.IsFalse(progress.IsUnlocked("Level05", Order));
    }

    [Test]
    public void IsUnlocked_LastOfPreviousWorldDone_DoesNotOpenSecondOfNext()
    {
        var progress = new Progress(new MemoryStore());

        progress.MarkCompleted("Level03", 10, -1);

        Assert.IsFalse(progress.IsUnlocked("Level05", Order));
    }

    [Test]
    public void MarkCompleted_MovesEqualPar_EarnsGoldenPawn()
    {
        var progress = new Progress(new MemoryStore());

        bool pawn = progress.MarkCompleted("Level02", 7, 7);

        Assert.IsTrue(pawn);
        Assert.IsTrue(progress.HasGoldenPawn("Level02"));
        Assert.AreEqual(1, progress.GoldenPawnCount);
    }

    [Test]
    public void MarkCompleted_MovesAbovePar_CompletedWithoutPawn()
    {
        var progress = new Progress(new MemoryStore());

        bool pawn = progress.MarkCompleted("Level02", 8, 7);

        Assert.IsFalse(pawn);
        Assert.IsTrue(progress.IsCompleted("Level02"));
        Assert.IsFalse(progress.HasGoldenPawn("Level02"));
    }

    [Test]
    public void MarkCompleted_UnknownPar_NoPawn()
    {
        var progress = new Progress(new MemoryStore());

        Assert.IsFalse(progress.MarkCompleted("Level01", 0, -1));
        Assert.IsFalse(progress.HasGoldenPawn("Level01"));
    }

    [Test]
    public void MarkCompleted_WorseRunAfterPawn_KeepsPawn()
    {
        var progress = new Progress(new MemoryStore());

        progress.MarkCompleted("Level02", 7, 7);
        progress.MarkCompleted("Level02", 20, 7);

        Assert.IsTrue(progress.HasGoldenPawn("Level02"));
    }

    [Test]
    public void MarkCompleted_Always_SavesToStore()
    {
        var store = new MemoryStore();
        var progress = new Progress(store);

        progress.MarkCompleted("Level01", 5, -1);

        Assert.AreEqual(1, store.SaveCount);
        Assert.AreEqual(progress.ToJson(), store.Json);
    }

    [Test]
    public void NewProgress_StoreWithSavedJson_RestoresProgress()
    {
        var store = new MemoryStore();
        var first = new Progress(store);
        first.MarkCompleted("Level01", 3, -1);
        first.MarkCompleted("Level02", 7, 7);

        var second = new Progress(store);

        Assert.IsTrue(second.IsCompleted("Level01"));
        Assert.IsTrue(second.IsCompleted("Level02"));
        Assert.IsTrue(second.HasGoldenPawn("Level02"));
        Assert.IsFalse(second.HasGoldenPawn("Level01"));
        Assert.AreEqual(first.ToJson(), second.ToJson());
    }

    [Test]
    public void NewProgress_MalformedJson_StartsEmpty()
    {
        var progress = new Progress(new MemoryStore { Json = "{not json" });

        Assert.IsFalse(progress.IsCompleted("Level01"));
        Assert.AreEqual(0, progress.GoldenPawnCount);
    }

    private static readonly List<List<string>> Worlds = new List<List<string>>
    {
        new List<string> { "Level01", "Level02" },
        new List<string> { "Level03", "Level04" },
        new List<string>(),
    };

    [Test]
    public void CompletedWorlds_NothingDone_Zero()
    {
        var progress = new Progress(new MemoryStore());

        Assert.AreEqual(0, progress.CompletedWorlds(Worlds));
    }

    [Test]
    public void CompletedWorlds_PartlyDoneWorld_NotCounted()
    {
        var progress = new Progress(new MemoryStore());

        progress.MarkCompleted("Level01", 9, -1);
        progress.MarkCompleted("Level03", 9, -1);

        Assert.AreEqual(0, progress.CompletedWorlds(Worlds));
    }

    [Test]
    public void CompletedWorlds_LaterWorldFirst_CountsAnyOrder()
    {
        var progress = new Progress(new MemoryStore());

        progress.MarkCompleted("Level03", 9, -1);
        progress.MarkCompleted("Level04", 9, -1);
        Assert.AreEqual(1, progress.CompletedWorlds(Worlds));

        progress.MarkCompleted("Level01", 9, -1);
        progress.MarkCompleted("Level02", 9, -1);
        Assert.AreEqual(2, progress.CompletedWorlds(Worlds), "an empty world never counts");
    }
}
