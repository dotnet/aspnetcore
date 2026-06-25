// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.InternalTesting;

public static class TestResources
{
    private static readonly string _baseDir = Path.Combine(Directory.GetCurrentDirectory(), "shared", "TestCertificates");
    private static readonly object _generatedCertificateLock = new();
    private static readonly HashSet<string> _generatedCertificatePaths = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> _generatedCertificateNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "aspnetdevcert.pfx",
        "eku.client.pfx",
        "eku.code_signing.pfx",
        "eku.multiple_usages.pfx",
        "eku.server.pfx",
        "no_extensions.pfx",
        "testCert.pfx",
    };

    public static string TestCertificatePath => GetCertPath("testCert.pfx");

    public static string GetCertPath(string name)
    {
        var path = Path.Combine(_baseDir, name);
        EnsureGeneratedCertificate(name, path);
        return path;
    }

    private const int MutexTimeout = 120 * 1000;
    private static readonly Mutex importPfxMutex = OperatingSystem.IsWindows() ?
        new Mutex(initiallyOwned: false, "Global\\KestrelTests.Certificates.LoadPfxCertificate") :
        null;

    public static X509Certificate2 GetTestCertificate(string certName = "testCert.pfx")
    {
        // On Windows, applications should not import PFX files in parallel to avoid a known system-level
        // race condition bug in native code which can cause crashes/corruption of the certificate state.
        if (importPfxMutex != null && !importPfxMutex.WaitOne(MutexTimeout))
        {
            throw new InvalidOperationException("Cannot acquire the global certificate mutex.");
        }

        try
        {
            return new X509Certificate2(GetCertPath(certName), TestCertificateFactory.TestCertificatePassword);
        }
        finally
        {
            importPfxMutex?.ReleaseMutex();
        }
    }

    public static X509Certificate2 GetTestCertificate(string certName, string password)
    {
        return new X509Certificate2(GetCertPath(certName), password);
    }

    public static X509Certificate2 GetTestCertificateWithKey(string certName, string keyName)
    {
        var cert = X509Certificate2.CreateFromPemFile(GetCertPath(certName), GetCertPath(keyName));
        if (OperatingSystem.IsWindows())
        {
            using (cert)
            {
                return new X509Certificate2(cert.Export(X509ContentType.Pkcs12));
            }
        }
        return cert;
    }

    public static X509Certificate2Collection GetTestChain(string certName = "leaf.com.crt")
    {
        // On Windows, applications should not import PFX files in parallel to avoid a known system-level
        // race condition bug in native code which can cause crashes/corruption of the certificate state.
        if (importPfxMutex != null && !importPfxMutex.WaitOne(MutexTimeout))
        {
            throw new InvalidOperationException("Cannot acquire the global certificate mutex.");
        }

        try
        {
            var fullChain = new X509Certificate2Collection();
            fullChain.ImportFromPemFile(GetCertPath("leaf.com.crt"));
            return fullChain;
        }
        finally
        {
            importPfxMutex?.ReleaseMutex();
        }
    }

    private static void EnsureGeneratedCertificate(string name, string path)
    {
        var fileName = Path.GetFileName(name);
        if (!_generatedCertificateNames.Contains(fileName))
        {
            return;
        }

        lock (_generatedCertificateLock)
        {
            if (_generatedCertificatePaths.Contains(path))
            {
                return;
            }

            var directory = Path.GetDirectoryName(path) ??
                throw new InvalidOperationException($"Cannot determine certificate directory for '{path}'.");
            Directory.CreateDirectory(directory);

            using var certificate = CreateGeneratedCertificate(fileName);
            var tempPath = Path.Combine(directory, Path.GetRandomFileName());
            try
            {
                File.WriteAllBytes(tempPath, certificate.Export(X509ContentType.Pfx, TestCertificateFactory.TestCertificatePassword));
                File.Move(tempPath, path, overwrite: true);
                _generatedCertificatePaths.Add(path);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
    }

    private static X509Certificate2 CreateGeneratedCertificate(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "aspnetdevcert.pfx" => TestCertificateFactory.CreateRsaCertificate(
                "CN=localhost",
                [TestCertificateFactory.ServerAuthentication],
                includeSubjectAlternativeName: true,
                includeAspNetHttpsExtension: true,
                includeKeyUsage: true,
                keyUsageCritical: true,
                enhancedKeyUsageCritical: true,
                subjectAlternativeNameCritical: true,
                configureSubjectAlternativeNames: TestCertificateFactory.ConfigureAspNetHttpsSubjectAlternativeNames),
            "eku.client.pfx" => TestCertificateFactory.CreateRsaCertificate(
                "CN=testcertonly",
                [TestCertificateFactory.ClientAuthentication],
                includeSubjectAlternativeName: false,
                includeAspNetHttpsExtension: false,
                includeKeyUsage: true,
                keyUsageCritical: false),
            "eku.code_signing.pfx" => TestCertificateFactory.CreateRsaCertificate(
                "CN=testcertonly",
                [TestCertificateFactory.CodeSigning],
                includeSubjectAlternativeName: false,
                includeAspNetHttpsExtension: false,
                includeKeyUsage: true,
                keyUsageCritical: false),
            "eku.multiple_usages.pfx" => TestCertificateFactory.CreateRsaCertificate(
                "CN=testcertonly",
                [
                    TestCertificateFactory.ServerAuthentication,
                    TestCertificateFactory.ClientAuthentication,
                ],
                includeSubjectAlternativeName: false,
                includeAspNetHttpsExtension: false,
                includeKeyUsage: true,
                keyUsageCritical: false),
            "eku.server.pfx" => TestCertificateFactory.CreateRsaCertificate(
                "CN=testcertonly",
                [TestCertificateFactory.ServerAuthentication],
                includeSubjectAlternativeName: false,
                includeAspNetHttpsExtension: false,
                includeKeyUsage: true,
                keyUsageCritical: false),
            "no_extensions.pfx" => TestCertificateFactory.CreateRsaCertificate(
                "CN=testcertonly",
                [],
                includeSubjectAlternativeName: false,
                includeAspNetHttpsExtension: false,
                includeKeyUsage: false),
            "testcert.pfx" => TestCertificateFactory.CreateRsaCertificate(
                "CN=localhost",
                [TestCertificateFactory.ServerAuthentication],
                includeSubjectAlternativeName: true,
                includeAspNetHttpsExtension: false,
                includeKeyUsage: true,
                keyUsageCritical: false),
            _ => throw new InvalidOperationException($"Unknown generated certificate '{name}'."),
        };
    }
}
