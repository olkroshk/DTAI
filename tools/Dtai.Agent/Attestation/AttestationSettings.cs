using System.Text.Json;

namespace Dtai.Agent.Attestation;

public sealed record AttestationSettings(string? KeyVaultName, string? KeyName)
{
    public bool UseDemoMode
    {
        get
        {
            var hasVault = !string.IsNullOrWhiteSpace(KeyVaultName);
            var hasKey = !string.IsNullOrWhiteSpace(KeyName);
            if (hasVault != hasKey)
            {
                throw new InvalidDataException("KeyVaultName and KeyName must either both be configured or both be empty.");
            }

            return !hasVault;
        }
    }

    public static AttestationSettings Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            return new AttestationSettings(null, null);
        }

        return JsonSerializer.Deserialize<AttestationSettings>(File.ReadAllText(path), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidDataException("Attestation settings must contain a JSON object.");
    }
}