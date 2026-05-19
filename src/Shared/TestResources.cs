// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.InternalTesting;

public static class TestResources
{
    private static readonly string _baseDir = Path.Combine(Directory.GetCurrentDirectory(), "shared", "TestCertificates");

    public static string TestCertificatePath { get; } = Path.Combine(_baseDir, "testCert.pfx");
    public static string GetCertPath(string name) => Path.Combine(_baseDir, name);

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
            return new X509Certificate2(GetCertPath(certName), "testPassword");
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
}
