using Dtai.Agent.Attestation;

namespace Dtai.Agent.Tests;

[TestClass]
public sealed class AttestationWorkflowTests
{
    [TestMethod]
    public void RunBindsRecipientAndAttestsBeforeEachRelease()
    {
        var calls = new List<string>();
        var workflow = new AttestationWorkflow(
            new EvidenceProvider(calls),
            new MaaService(calls),
            new KeyVaultProvider(calls),
            new ItaService(calls),
            new SecondaryProvider(calls));

        var result = workflow.Run("recipient-jwk", _ => { });

        CollectionAssert.AreEqual(
            new[] { "evidence:recipient-jwk", "maa", "key-vault:maa-token", "ita", "secondary:ita-token" },
            calls);
        Assert.AreEqual("azure-signed-release", result.K1.Value);
        Assert.AreEqual("secondary-signed-release", result.K2.Value);
    }

    [TestMethod]
    public void RunRejectsMissingRecipientBeforeCollectingEvidence()
    {
        var calls = new List<string>();
        var workflow = new AttestationWorkflow(
            new EvidenceProvider(calls),
            new MaaService(calls),
            new KeyVaultProvider(calls),
            new ItaService(calls),
            new SecondaryProvider(calls));

        Assert.ThrowsExactly<ArgumentException>(() => workflow.Run(" ", _ => { }));
        Assert.AreEqual(0, calls.Count);
    }

    private sealed class EvidenceProvider(List<string> calls) : ITeeEvidenceProvider
    {
        public TeeEvidence FetchEvidence(string recipientJwk)
        {
            calls.Add($"evidence:{recipientJwk}");
            return new TeeEvidence("evidence");
        }
    }

    private sealed class MaaService(List<string> calls) : IMaaAttestationService
    {
        public AttestationToken Attest(TeeEvidence evidence)
        {
            calls.Add("maa");
            return new AttestationToken("maa-token");
        }
    }

    private sealed class KeyVaultProvider(List<string> calls) : IKeyVaultKeyProvider
    {
        public SignedKeyRelease ReleaseK1(AttestationToken token)
        {
            calls.Add($"key-vault:{token.Value}");
            return new SignedKeyRelease("azure-signed-release");
        }
    }

    private sealed class ItaService(List<string> calls) : IItaAttestationService
    {
        public AttestationToken Attest(TeeEvidence evidence)
        {
            calls.Add("ita");
            return new AttestationToken("ita-token");
        }
    }

    private sealed class SecondaryProvider(List<string> calls) : IHashicorpKeyProvider
    {
        public SignedKeyRelease ReleaseK2(AttestationToken token)
        {
            calls.Add($"secondary:{token.Value}");
            return new SignedKeyRelease("secondary-signed-release");
        }
    }
}