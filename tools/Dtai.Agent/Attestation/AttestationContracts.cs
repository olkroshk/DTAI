namespace Dtai.Agent.Attestation;

public sealed record TeeEvidence(string Value)
{
    public string Value { get; } = RequireValue(Value, nameof(Value));

    private static string RequireValue(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value cannot be empty.", parameterName) : value;
}

public sealed record AttestationToken(string Value)
{
    public string Value { get; } = RequireValue(Value, nameof(Value));

    private static string RequireValue(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value cannot be empty.", parameterName) : value;
}

public sealed record SignedKeyRelease(string Value)
{
    public string Value { get; } = RequireValue(Value, nameof(Value));

    private static string RequireValue(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value cannot be empty.", parameterName) : value;
}

public sealed record KeyRetrievalResult(SignedKeyRelease K1, SignedKeyRelease K2);

public interface ITeeEvidenceProvider
{
    TeeEvidence FetchEvidence(string recipientJwk);
}

public interface IMaaAttestationService
{
    AttestationToken Attest(TeeEvidence evidence);
}

public interface IKeyVaultKeyProvider
{
    SignedKeyRelease ReleaseK1(AttestationToken token);
}

public interface IItaAttestationService
{
    AttestationToken Attest(TeeEvidence evidence);
}

public interface IHashicorpKeyProvider
{
    SignedKeyRelease ReleaseK2(AttestationToken token);
}
