using System.Text;
using System.Text.Json;

namespace Dtai.Agent.Attestation;

public static class CapturedTeeEvidence
{
    public static TeeEvidence CreateTdx(string quote, string runtimeData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(quote);
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeData);
        return new TeeEvidence(JsonSerializer.Serialize(new
        {
            quote,
            runtimeData = new { data = runtimeData, dataType = "JSON" },
        }));
    }

    public static TeeEvidence CreateSevSnp(string report, string vcekCertChain, string runtimeDataJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(vcekCertChain);
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeDataJson);

        var reportEnvelope = JsonSerializer.Serialize(new
        {
            SnpReport = report,
            VcekCertChain = vcekCertChain,
        });
        return new TeeEvidence(JsonSerializer.Serialize(new
        {
            report = Base64UrlEncode(Encoding.UTF8.GetBytes(reportEnvelope)),
            runtimeData = new
            {
                data = Base64UrlEncode(Encoding.UTF8.GetBytes(runtimeDataJson)),
                dataType = "JSON",
            },
        }));
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}