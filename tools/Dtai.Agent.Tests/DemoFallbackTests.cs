using System.Diagnostics;
using System.Security.Cryptography;
using Dtai.Agent.Attestation;

namespace Dtai.Agent.Tests;

[TestClass]
public sealed class DemoFallbackTests
{
    [TestMethod]
    public void MissingKeyVaultRunsOriginalDemoWorkflow()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"dtai-demo-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var generate = RunAgent(directory, "generate", directory);
            Assert.AreEqual(0, generate.ExitCode, generate.Error);

            var encrypt = RunAgent(directory, "encrypt", "-Model", "model001.safetensors", "-DEK", "dek-plain.txt");
            Assert.AreEqual(0, encrypt.ExitCode, encrypt.Error);

            var decrypt = RunAgent(
                directory,
                "decrypt",
                "-EncryptedDEK",
                "encrypted-dek.txt",
                "-EncryptedModel",
                "e-model001.safetensors");

            Assert.AreEqual(0, decrypt.ExitCode, decrypt.Error);
            StringAssert.Contains(decrypt.Output, "Falling back to demo-only mode");
            AssertFilesEqual(
                Path.Combine(directory, "model001.safetensors"),
                Path.Combine(directory, "result-model001.safetensors"));
            AssertFilesEqual(
                Path.Combine(directory, "dek-plain.txt"),
                Path.Combine(directory, "result-dek.txt"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static ProcessResult RunAgent(string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(typeof(AttestationSettings).Assembly.Location);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start DTAI.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, output, error);
    }

    private static void AssertFilesEqual(string expectedPath, string actualPath)
    {
        var expectedHash = SHA256.HashData(File.ReadAllBytes(expectedPath));
        var actualHash = SHA256.HashData(File.ReadAllBytes(actualPath));
        CollectionAssert.AreEqual(expectedHash, actualHash);
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}