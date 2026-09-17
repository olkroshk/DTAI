namespace Dtai.Agent.Attestation;

// These stubs perform no service calls. Their results are demo markers, not valid evidence, tokens, or keys.
public sealed class TeeEvidenceProvider : ITeeEvidenceProvider
{
    public TeeEvidence FetchEvidence(string recipientJwk)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientJwk);
        // TODO: Implement a separate provider that collects actual evidence from the CVM's TEE.
        return new TeeEvidence("demo-only-tee-evidence");
    }
}

public sealed class ItaAttestationService : IItaAttestationService
{
    public AttestationToken Attest(TeeEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        // TODO: Implement a separate service that submits evidence to ITA and validates the returned token.
        return new AttestationToken("demo-only-ita-token");
    }
}

public sealed class HashicorpKeyProvider : IHashicorpKeyProvider
{
    public SignedKeyRelease ReleaseK2(AttestationToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        // TODO: Implement a separate provider that requests authorized access to K2 using the ITA token.
        return new SignedKeyRelease("demo-only-k2-release");
    }
}
