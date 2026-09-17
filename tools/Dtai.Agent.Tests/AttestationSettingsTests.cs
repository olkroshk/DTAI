using Dtai.Agent.Attestation;

namespace Dtai.Agent.Tests;

[TestClass]
public sealed class AttestationSettingsTests
{
    [TestMethod]
    public void MissingSettingsUseDemoMode()
    {
        var settings = AttestationSettings.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        Assert.IsTrue(settings.UseDemoMode);
    }

    [TestMethod]
    public void EmptyKeyVaultSettingsUseDemoMode()
    {
        var settings = new AttestationSettings("", " ");

        Assert.IsTrue(settings.UseDemoMode);
    }

    [TestMethod]
    public void CompleteKeyVaultSettingsDisableDemoMode()
    {
        var settings = new AttestationSettings("dtai-vault", "dtai-key");

        Assert.IsFalse(settings.UseDemoMode);
    }

    [TestMethod]
    [DataRow("dtai-vault", null)]
    [DataRow(null, "dtai-key")]
    public void PartialKeyVaultSettingsAreRejected(string? vaultName, string? keyName)
    {
        var settings = new AttestationSettings(vaultName, keyName);

        Assert.ThrowsExactly<InvalidDataException>(() => _ = settings.UseDemoMode);
    }
}