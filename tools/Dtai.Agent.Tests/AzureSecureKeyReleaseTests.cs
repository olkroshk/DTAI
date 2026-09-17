using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Security.KeyVault.Keys;
using Dtai.Agent.Attestation;

namespace Dtai.Agent.Tests;

[TestClass]
public sealed class AzureSecureKeyReleaseTests
{
    [TestMethod]
    [DataRow(MaaAttestationType.TdxVm, "TdxVm")]
    [DataRow(MaaAttestationType.SevSnpVm, "SevSnpVm")]
    public void MaaAttestationServiceReturnsTokenFromTeeEndpoint(
        MaaAttestationType attestationType,
        string route)
    {
        var evidence = LoadCapturedEvidence(attestationType);
        var handler = new RecordingHandler(HttpStatusCode.OK, "{\"token\":\"signed-maa-jwt\"}");
        var service = new MaaAttestationService(
            new HttpClient(handler),
            new Uri("https://example.attest.azure.net"),
            attestationType);

        var token = service.Attest(evidence);

        Assert.AreEqual("signed-maa-jwt", token.Value);
        Assert.AreEqual($"https://example.attest.azure.net/attest/{route}?api-version=2025-06-01", handler.RequestUri?.AbsoluteUri);
        Assert.AreEqual(evidence.Value, handler.RequestBody);
        using var request = JsonDocument.Parse(handler.RequestBody!);
        if (attestationType == MaaAttestationType.TdxVm)
        {
            Assert.IsTrue(request.RootElement.TryGetProperty("quote", out _));
        }
        else
        {
            var encodedReport = request.RootElement.GetProperty("report").GetString()!;
            using var report = JsonDocument.Parse(DecodeBase64Url(encodedReport));
            Assert.IsTrue(report.RootElement.TryGetProperty("SnpReport", out _));
            Assert.IsTrue(report.RootElement.TryGetProperty("VcekCertChain", out _));
        }
    }

    [TestMethod]
    public void MaaAttestationServiceRejectsResponseWithoutToken()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "{}");
        var service = new MaaAttestationService(
            new HttpClient(handler),
            new Uri("https://example.attest.azure.net"),
            MaaAttestationType.TdxVm);

        Assert.ThrowsExactly<InvalidDataException>(() => service.Attest(new TeeEvidence("{}")));
    }

    [TestMethod]
    public void KeyVaultProviderPassesExactMaaTokenAndPreservesSignedRelease()
    {
        ReleaseKeyOptions? captured = null;
        var provider = new KeyVaultKeyProvider(
            "release-key",
            options =>
            {
                captured = options;
                return "header.payload.signature";
            });

        var result = provider.ReleaseK1(new AttestationToken("maa.header.signature"));

        Assert.IsNotNull(captured);
        Assert.AreEqual("release-key", captured.Name);
        Assert.AreEqual("maa.header.signature", captured.TargetAttestationToken);
        Assert.AreEqual("header.payload.signature", result.Value);
    }

    [TestMethod]
    public void ProtocolValuesRejectEmptyStrings()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new TeeEvidence(""));
        Assert.ThrowsExactly<ArgumentException>(() => new AttestationToken(" "));
        Assert.ThrowsExactly<ArgumentException>(() => new SignedKeyRelease(""));
    }

    private static TeeEvidence LoadCapturedEvidence(MaaAttestationType attestationType)
    {
        var directory = Path.Combine(Path.GetDirectoryName(typeof(AttestationSettings).Assembly.Location)!, "Attestation", "Evidence");
        return attestationType switch
        {
            MaaAttestationType.TdxVm => CapturedTeeEvidence.CreateTdx(
                File.ReadAllText(Path.Combine(directory, "tdx-quote.b64url")).Trim(),
                File.ReadAllText(Path.Combine(directory, "tdx-runtime-data.b64url")).Trim()),
            MaaAttestationType.SevSnpVm => CapturedTeeEvidence.CreateSevSnp(
                File.ReadAllText(Path.Combine(directory, "snp-report.b64url")).Trim(),
                File.ReadAllText(Path.Combine(directory, "vcek-cert-chain.b64url")).Trim(),
                File.ReadAllText(Path.Combine(directory, "runtime-data.json")).Trim()),
            _ => throw new ArgumentOutOfRangeException(nameof(attestationType)),
        };
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/').PadRight((value.Length + 3) / 4 * 4, '=');
        return Convert.FromBase64String(padded);
    }

    private sealed class RecordingHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        public string? RequestBody { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            RequestBody = request.Content?.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            });
        }
    }
}