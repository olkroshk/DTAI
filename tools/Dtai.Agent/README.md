# DTAI Agent

`Dtai.Agent` is the command-line application. It currently contains a runnable
local fixture demo and incomplete client boundaries for Microsoft Azure
Attestation (MAA) and Azure Key Vault Secure Key Release (SKR).

## Current status

| Path | Status |
| --- | --- |
| Local fixture encryption/decryption | Implemented and runnable on any .NET 8 host |
| Key Vault selection | Missing configuration falls back to demo mode; partial configuration is rejected |
| MAA TDX request and Key Vault `ReleaseKey` call | Implemented as client boundaries and covered by unit tests, but not wired into decrypt |
| TDX evidence harvesting | Not implemented; files under `Attestation/Evidence` are local captures |
| SEV-SNP evidence harvesting and MAA request | Not implemented |
| Native Azure `key_hsm` unwrap inside the TEE | Not implemented |
| Secondary authority and HKDF derivation | Placeholder interfaces only |

There is currently no end-to-end local TDX or SEV-SNP run. A configured Key
Vault does not silently fall back to local private keys: decrypt stops until
live TEE evidence harvesting and native `key_hsm` unwrapping are available.

## Configuration

The agent reads `Attestation/attestation.settings.local.json` from its output
directory, or `attestation.settings.local.json` from the current directory:

```json
{
  "KeyVaultName": "",
  "KeyName": ""
}
```

Leave both values empty, or omit the file, to use the local fixture demo.
Provide both values to select Azure SKR. Supplying only one value is an error.

## Build and test

From the repository root:

```shell
dotnet build Dtai.sln
dotnet test Dtai.sln
dotnet run --project tools/Dtai.Agent -- --help
```

## Local fixture demo

The fixture demo does not harvest or attest TEE evidence. It uses deliberately
public local RSA keys and the password `dtai-demo-only`; never use these files
for real models.

Run from `samples/demo` so the local K1/K2 files resolve from the current
directory:

```shell
dotnet run --project ../../tools/Dtai.Agent -- encrypt -Model model001.safetensors -DEK dek-plain.txt
dotnet run --project ../../tools/Dtai.Agent -- decrypt -EncryptedDEK encrypted-dek.txt -EncryptedModel e-model001.safetensors
```

When Key Vault settings are empty, decrypt reports that it is falling back to
demo mode, decrypts the DEK with the local PFX files, authenticates the model
with AES-256-GCM, and writes `result-dek.txt` and
`result-model001.safetensors`.

Generate or overwrite local fixtures with:

```shell
dotnet run --project ../../tools/Dtai.Agent -- generate .
```

## TEE evidence

The checked-in files under `Attestation/Evidence` are captured inputs, not a
live evidence-harvesting implementation. A real run must create an ephemeral
recipient key before quote generation and bind that exact public JWK into the
TEE report data. Reusing a captured quote with a newly generated recipient key
will fail MAA binding validation.

- TDX: the MAA HTTP client targets `/attest/TdxVm`, but no TDX quote harvester
  is connected to the CLI.
- SEV-SNP: report and VCEK capture files exist locally, but no SEV-SNP MAA
  request or evidence harvester is connected to the CLI.
