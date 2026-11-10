using LuckyStep;
using NUnit.Framework;

/// <summary>EditMode tests for <see cref="VolumeSettings"/> (audio settings spec, 2026-11-10). In-memory only (persist false).</summary>
public sealed class VolumeSettingsTests
{
    [Test]
    public void NewSettings_InMemory_UseDefaults()
    {
        // Arrange / Act
        var volume = new VolumeSettings(persist: false);

        // Assert
        Assert.AreEqual(VolumeSettings.DefaultMusic, volume.Music);
        Assert.AreEqual(VolumeSettings.DefaultEffects, volume.Effects);
    }

    [Test]
    public void ChangeMusic_OneStepDown_LowersByStep()
    {
        // Arrange
        var volume = new VolumeSettings(persist: false);

        // Act
        volume.ChangeMusic(-1);

        // Assert
        Assert.AreEqual(VolumeSettings.DefaultMusic - VolumeSettings.Step, volume.Music);
        Assert.AreEqual(VolumeSettings.DefaultEffects, volume.Effects);
    }

    [Test]
    public void ChangeEffects_PastLimits_ClampsTo0And100()
    {
        // Arrange
        var volume = new VolumeSettings(persist: false);

        // Act
        volume.ChangeEffects(5);
        int high = volume.Effects;
        volume.ChangeEffects(-20);

        // Assert
        Assert.AreEqual(100, high);
        Assert.AreEqual(0, volume.Effects);
        Assert.AreEqual(0f, volume.EffectsFactor);
    }

    [Test]
    public void Changed_RaisedOnlyWhenAValueChanges()
    {
        // Arrange: effects start at 100, so +1 changes nothing.
        var volume = new VolumeSettings(persist: false);
        int raised = 0;
        volume.Changed += () => raised++;

        // Act
        volume.ChangeEffects(1);
        volume.ChangeMusic(1);

        // Assert
        Assert.AreEqual(1, raised);
    }
}
