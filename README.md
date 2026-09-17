# Distributed Trust Architecture for AI (DTAI)

This repository contains a .NET 8 demo implementation of DTAI-style local model protection and an optional test-only attestation/release flow. The code here is intentionally educational and demo-only: it is not a production multi-authority KMS architecture.

## Repository layout

- `tools/Dtai.Agent` — the executable demo app. It can generate fixtures, encrypt a safetensors file with a random DEK, and decrypt the resulting demo container.
- `tools/Dtai.Agent.Tests` — unit tests for the attestation and release workflow.
- `samples/demo` — generated public demo fixtures, including toy model data, a random DEK, and RSA key material.
- `Dtai.sln` — solution with both .NET projects.

## What the current implementation does

The executable supports three commands:

- `generate [output-directory]` — creates demo fixtures in a target directory
- `encrypt -Model <model> -DEK <dek-file>` — encrypts a model file and wraps the DEK with the bundled RSA keys
- `decrypt -EncryptedDEK <encrypted-dek.txt> -EncryptedModel <e-model-file>` — decrypts the bundle and verifies the model container before writing outputs

The demo model container format is intentionally simple and authenticated:

- magic: `DTAIEMOD`
- version: `1`
- algorithm: `1` (AES-256-GCM)
- nonce length: `12`
- tag length: `16`
- ciphertext length: 8-byte little-endian field
- then: nonce, tag, and ciphertext

The code validates the container header and tag before writing plaintext output.

## Demo-only warning

The files under `samples/demo` are intentionally public and must never be used for real models. The shared PFX password is also public, and the included key pairs are for demonstration only. They are not a real independent-authority deployment and do not enforce multi-cloud trust separation.

## Build

Requires the .NET 8 SDK or newer.

```bash
dotnet build DTAI.sln
```

## Quick start

Generate a fresh set of demo fixtures:

```bash
dotnet run --project tools/Dtai.Agent -- generate samples/demo
```

Or run the app from the project directory:

```bash
cd tools/Dtai.Agent
dotnet run -- generate .
```

Create the encrypted demo artifacts from the generated files:

```bash
cd samples/demo
DTAI.exe encrypt -Model model001.safetensors -DEK dek-plain.txt
```

Decrypt the results:

```bash
DTAI.exe decrypt -EncryptedDEK encrypted-dek.txt -EncryptedModel e-model001.safetensors
```

When running the app without arguments, it enters interactive mode.

## Optional attestation path

The repository also contains a test-only attestation leg that can run against Azure Attestation and Azure Key Vault when local evidence files are present.

The attestation flow is not enabled by default. It is controlled by files under `tools/Dtai.Agent/Attestation`:

- `Attestation/Evidence/` — contains sample evidence files such as TDX quote data
- `Attestation/attestation.settings.local.json` — local git-ignored override for attestation config

Example settings:

```json
{
  "AttestationType": "SevSnpVm",
  "KeyVaultName": "<your-premium-vault>",
  "KeyName": "<your-exportable-key>"
}
```

You can also set environment variables such as:

- `DTAI_ATTESTATION_TYPE`
- `DTAI_ATTESTATION_KEYVAULTNAME`
- `DTAI_ATTESTATION_KEYNAME`

The attestation workflow is illustrative; local PFX decryption still performs the actual demo decryption. A missing or invalid attestation configuration will not block the demo flow, but it can emit warnings if the attestation/release step fails.

## Running the attestation demo

```bash
cd tools/Dtai.Agent
dotnet run -- generate .
dotnet run -- encrypt -Model model001.safetensors -DEK dek-plain.txt
dotnet run -- decrypt -EncryptedDEK encrypted-dek.txt -EncryptedModel e-model001.safetensors
```

This path is meant for local validation of the Azure attestation and secure key release pattern; it is not production-grade trust enforcement.

## Tests

```bash
dotnet test tools/Dtai.Agent.Tests
```

## Security and usage notes

- Do not use the bundled fixtures for real model protection.
- The public RSA keys and private PFX files are intentionally included as demo artifacts only.
- The demo simulates a local encryption/decryption flow and optional evidence verification; it is not a replacement for a hardened attestation architecture.
