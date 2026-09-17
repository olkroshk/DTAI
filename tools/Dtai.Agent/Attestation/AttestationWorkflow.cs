namespace Dtai.Agent.Attestation;

public sealed class AttestationWorkflow
{
    private readonly ITeeEvidenceProvider evidenceProvider;
    private readonly IMaaAttestationService maaService;
    private readonly IKeyVaultKeyProvider keyVaultProvider;
    private readonly IItaAttestationService itaService;
    private readonly IHashicorpKeyProvider hashicorpProvider;

    public AttestationWorkflow(
        ITeeEvidenceProvider evidenceProvider,
        IMaaAttestationService maaService,
        IKeyVaultKeyProvider keyVaultProvider,
        IItaAttestationService itaService,
        IHashicorpKeyProvider hashicorpProvider)
    {
        this.evidenceProvider = evidenceProvider ?? throw new ArgumentNullException(nameof(evidenceProvider));
        this.maaService = maaService ?? throw new ArgumentNullException(nameof(maaService));
        this.keyVaultProvider = keyVaultProvider ?? throw new ArgumentNullException(nameof(keyVaultProvider));
        this.itaService = itaService ?? throw new ArgumentNullException(nameof(itaService));
        this.hashicorpProvider = hashicorpProvider ?? throw new ArgumentNullException(nameof(hashicorpProvider));
    }

    public KeyRetrievalResult Run(string recipientJwk, Action<string> reportProgress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientJwk);
        ArgumentNullException.ThrowIfNull(reportProgress);

        reportProgress("Fetch TEE evidence from the CVM");
        var evidence = evidenceProvider.FetchEvidence(recipientJwk);

        reportProgress("Submit the evidence to MAA to get a token");
        var maaToken = maaService.Attest(evidence);

        reportProgress("Submit the request for K1 to Azure KeyVault");
        var k1 = keyVaultProvider.ReleaseK1(maaToken);

        reportProgress("Submit the evidence to ITA attestation service");
        var itaToken = itaService.Attest(evidence);

        reportProgress("Submit the request to Hashicorp to get K2");
        var k2 = hashicorpProvider.ReleaseK2(itaToken);

        return new KeyRetrievalResult(k1, k2);
    }
}
