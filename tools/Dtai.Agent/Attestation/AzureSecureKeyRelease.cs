using System.Net.Http.Json;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;

namespace Dtai.Agent.Attestation;

public enum MaaAttestationType
{
    TdxVm,
    SevSnpVm,
}

public sealed class MaaAttestationService : IMaaAttestationService
{
    private readonly HttpClient httpClient;
    private readonly Uri attestUri;

    public MaaAttestationService(HttpClient httpClient, Uri endpoint, MaaAttestationType attestationType)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(endpoint);
        attestUri = new Uri(endpoint, $"/attest/{attestationType}?api-version=2025-06-01");
    }

    public AttestationToken Attest(TeeEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        using var content = new StringContent(evidence.Value, System.Text.Encoding.UTF8, "application/json");
        using var response = httpClient.PostAsync(attestUri, content).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(response.Content.ReadAsStream());
        if (!body.RootElement.TryGetProperty("token", out var token))
        {
            throw new InvalidDataException("MAA response did not contain a token.");
        }

        return new AttestationToken(token.GetString() ?? string.Empty);
    }
}

public sealed class KeyVaultKeyProvider : IKeyVaultKeyProvider
{
    private readonly Func<ReleaseKeyOptions, string> releaseKey;
    private readonly string keyName;

    public KeyVaultKeyProvider(Uri vaultUri, string keyName, TokenCredential? credential = null)
    {
        ArgumentNullException.ThrowIfNull(vaultUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyName);
        var client = new KeyClient(vaultUri, credential ?? new DefaultAzureCredential());
        this.keyName = keyName;
        releaseKey = options => client.ReleaseKey(options).Value.Value;
    }

    public KeyVaultKeyProvider(string keyName, Func<ReleaseKeyOptions, string> releaseKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyName);
        this.releaseKey = releaseKey ?? throw new ArgumentNullException(nameof(releaseKey));
        this.keyName = keyName;
    }

    public SignedKeyRelease ReleaseK1(AttestationToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        var options = new ReleaseKeyOptions(keyName, token.Value);
        return new SignedKeyRelease(releaseKey(options));
    }
}