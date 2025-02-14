// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.Test.Shared;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection;

public class DataProtectionProviderTests
{
    [Fact]
    public void System_UsesProvidedDirectory()
    {
        WithUniqueTempDirectory(directory =>
        {
            // Step 1: directory should be completely empty
            directory.Create();
            Assert.Empty(directory.GetFiles());

            // Step 2: instantiate the system and round-trip a payload
            var protector = DataProtectionProvider.Create(directory).CreateProtector("purpose");
            Assert.Equal("payload", protector.Unprotect(protector.Protect("payload")));

            // Step 3: validate that there's now a single key in the directory and that it's not protected
            var allFiles = directory.GetFiles();
            Assert.Single(allFiles);
            Assert.StartsWith("key-", allFiles[0].Name, StringComparison.OrdinalIgnoreCase);
            string fileText = File.ReadAllText(allFiles[0].FullName);
            Assert.Contains("Warning: the key below is in an unencrypted form.", fileText, StringComparison.Ordinal);
            Assert.DoesNotContain("Windows DPAPI", fileText, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void System_NoKeysDirectoryProvided_UsesDefaultKeysDirectory()
    {
        var mock = new Mock<IDefaultKeyStorageDirectories>();
        var keysPath = Path.Combine(AppContext.BaseDirectory, Path.GetRandomFileName());
        mock.Setup(m => m.GetKeyStorageDirectory()).Returns(new DirectoryInfo(keysPath));

        // Step 1: Instantiate the system and round-trip a payload
        var provider = DataProtectionProvider.CreateProvider(
            keyDirectory: null,
            certificate: null,
            setupAction: builder =>
        {
            builder.SetApplicationName("TestApplication");
            builder.Services.AddSingleton<IKeyManager>(s =>
                new XmlKeyManager(
                    s.GetRequiredService<IOptions<KeyManagementOptions>>(),
                    s.GetRequiredService<IActivator>(),
                    NullLoggerFactory.Instance,
                    mock.Object));
        });

        var protector = provider.CreateProtector("Protector");
        Assert.Equal("payload", protector.Unprotect(protector.Protect("payload")));

        // Step 2: Validate that there's now a single key in the directory
        var newFileName = Assert.Single(Directory.GetFiles(keysPath));
        var file = new FileInfo(newFileName);
        Assert.StartsWith("key-", file.Name, StringComparison.OrdinalIgnoreCase);
        var fileText = File.ReadAllText(file.FullName);
        // On Windows, validate that it's protected using Windows DPAPI.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.DoesNotContain("Warning: the key below is in an unencrypted form.", fileText, StringComparison.Ordinal);
            Assert.Contains("This key is encrypted with Windows DPAPI.", fileText, StringComparison.Ordinal);
        }
        else
        {
            Assert.Contains("Warning: the key below is in an unencrypted form.", fileText, StringComparison.Ordinal);
        }
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyOnWindows]
    public void System_UsesProvidedDirectory_WithConfigurationCallback()
    {
        WithUniqueTempDirectory(directory =>
        {
            // Step 1: directory should be completely empty
            directory.Create();
            Assert.Empty(directory.GetFiles());

            // Step 2: instantiate the system and round-trip a payload
            var protector = DataProtectionProvider.Create(directory, configure =>
        {
            configure.ProtectKeysWithDpapi();
        }).CreateProtector("purpose");
            Assert.Equal("payload", protector.Unprotect(protector.Protect("payload")));

            // Step 3: validate that there's now a single key in the directory and that it's protected with DPAPI
            var allFiles = directory.GetFiles();
            Assert.Single(allFiles);
            Assert.StartsWith("key-", allFiles[0].Name, StringComparison.OrdinalIgnoreCase);
            string fileText = File.ReadAllText(allFiles[0].FullName);
            Assert.DoesNotContain("Warning: the key below is in an unencrypted form.", fileText, StringComparison.Ordinal);
            Assert.Contains("Windows DPAPI", fileText, StringComparison.Ordinal);
        });
    }

    [ConditionalFact]
    [X509StoreIsAvailable(StoreName.My, StoreLocation.CurrentUser)]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720", Queues = "All.OSX")]
    public void System_UsesProvidedDirectoryAndCertificate()
    {
        var filePath = Path.Combine(GetTestFilesPath(), "TestCert.pfx");
        using (var imported = new X509Certificate2(filePath, "password", X509KeyStorageFlags.Exportable))
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(imported);
                store.Close();
            }

            WithUniqueTempDirectory(directory =>
            {
                var certificateStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                certificateStore.Open(OpenFlags.ReadWrite);
                var certificate = certificateStore.Certificates.Find(X509FindType.FindBySubjectName, "TestCert", false)[0];
                Assert.True(certificate.HasPrivateKey, "Cert should have a private key");
                try
                {
                    // Step 1: directory should be completely empty
                    directory.Create();
                    Assert.Empty(directory.GetFiles());

                    // Step 2: instantiate the system and round-trip a payload
                    var protector = DataProtectionProvider.Create(directory, certificate).CreateProtector("purpose");
                    var data = protector.Protect("payload");

                    // add a cert without the private key to ensure the decryption will still fallback to the cert store
                    var certWithoutKey = new X509Certificate2(Path.Combine(GetTestFilesPath(), "TestCertWithoutPrivateKey.pfx"), "password");
                    var unprotector = DataProtectionProvider.Create(directory, o => o.UnprotectKeysWithAnyCertificate(certWithoutKey)).CreateProtector("purpose");
                    Assert.Equal("payload", unprotector.Unprotect(data));

                    // Step 3: validate that there's now a single key in the directory and that it's is protected using the certificate
                    var allFiles = directory.GetFiles();
                    Assert.Single(allFiles);
                    Assert.StartsWith("key-", allFiles[0].Name, StringComparison.OrdinalIgnoreCase);
                    string fileText = File.ReadAllText(allFiles[0].FullName);
                    Assert.DoesNotContain("Warning: the key below is in an unencrypted form.", fileText, StringComparison.Ordinal);
                    Assert.Contains("X509Certificate", fileText, StringComparison.Ordinal);
                }
                finally
                {
                    certificateStore.Remove(certificate);
                    certificateStore.Close();
                }
            });
        }
    }

    [ConditionalFact]
    [X509StoreIsAvailable(StoreName.My, StoreLocation.CurrentUser)]
    public void System_UsesProvidedCertificateNotFromStore()
    {
        using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
        {
            store.Open(OpenFlags.ReadWrite);
            var certWithoutKey = new X509Certificate2(Path.Combine(GetTestFilesPath(), "TestCert3WithoutPrivateKey.pfx"), "password3", X509KeyStorageFlags.Exportable);
            Assert.False(certWithoutKey.HasPrivateKey, "Cert should not have private key");
            store.Add(certWithoutKey);
            store.Close();
        }

        WithUniqueTempDirectory(directory =>
        {
            using (var certificateStore = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                certificateStore.Open(OpenFlags.ReadWrite);
                var certInStore = certificateStore.Certificates.Find(X509FindType.FindBySubjectName, "TestCert", false)[0];
                Assert.NotNull(certInStore);
                Assert.False(certInStore.HasPrivateKey, "Cert should not have private key");

                try
                {
                    var certWithKey = new X509Certificate2(Path.Combine(GetTestFilesPath(), "TestCert3.pfx"), "password3");

                    var protector = DataProtectionProvider.Create(directory, certWithKey).CreateProtector("purpose");
                    var data = protector.Protect("payload");

                    var keylessUnprotector = DataProtectionProvider.Create(directory).CreateProtector("purpose");
                    Assert.Throws<CryptographicException>(() => keylessUnprotector.Unprotect(data));

                    var unprotector = DataProtectionProvider.Create(directory, o => o.UnprotectKeysWithAnyCertificate(certInStore, certWithKey)).CreateProtector("purpose");
                    Assert.Equal("payload", unprotector.Unprotect(data));
                }
                finally
                {
                    certificateStore.Remove(certInStore);
                    certificateStore.Close();
                }
            }
        });
    }

    [Fact]
    public void System_UsesInMemoryCertificate()
    {
        var filePath = Path.Combine(GetTestFilesPath(), "TestCert2.pfx");
        var certificate = new X509Certificate2(filePath, "password");

        AssetStoreDoesNotContain(certificate);

        WithUniqueTempDirectory(directory =>
        {
            // Step 1: directory should be completely empty
            directory.Create();
            Assert.Empty(directory.GetFiles());

            // Step 2: instantiate the system and round-trip a payload
            var protector = DataProtectionProvider.Create(directory, certificate).CreateProtector("purpose");
            Assert.Equal("payload",
                protector.Unprotect(protector.Protect("payload")));

            // Step 3: validate that there's now a single key in the directory and that it's is protected using the certificate
            var allFiles = directory.GetFiles();
            Assert.Single(allFiles);
            Assert.StartsWith("key-", allFiles[0].Name, StringComparison.OrdinalIgnoreCase);
            string fileText = File.ReadAllText(allFiles[0].FullName);
            Assert.DoesNotContain("Warning: the key below is in an unencrypted form.", fileText, StringComparison.Ordinal);
            Assert.Contains("X509Certificate", fileText, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void System_UsesCertificate()
    {
        var filePath = Path.Combine(GetTestFilesPath(), "TestCert2.pfx");
        var certificate = new X509Certificate2(filePath, "password");

        AssetStoreDoesNotContain(certificate);

        WithUniqueTempDirectory(directory =>
        {
            // Step 1: directory should be completely empty
            directory.Create();
            Assert.Empty(directory.GetFiles());

            // Step 2: instantiate the system and round-trip a payload
            var protector = DataProtectionProvider.Create("Test", certificate).CreateProtector("purpose");
            Assert.Equal("payload",
                protector.Unprotect(protector.Protect("payload")));

            // Step 3: validate that there's no key in the directory
            Assert.Empty(directory.GetFiles());
        });
    }

    private static void AssetStoreDoesNotContain(X509Certificate2 certificate)
    {
        using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
        {
            try
            {
                store.Open(OpenFlags.ReadOnly);
            }
            catch
            {
                return;
            }

            // ensure this cert is not in the x509 store
            Assert.Empty(store.Certificates.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, false));
        }
    }

    [Fact]
    public void System_CanUnprotectWithCert()
    {
        var filePath = Path.Combine(GetTestFilesPath(), "TestCert2.pfx");
        var certificate = new X509Certificate2(filePath, "password");

        WithUniqueTempDirectory(directory =>
        {
            // Step 1: directory should be completely empty
            directory.Create();
            Assert.Empty(directory.GetFiles());

            // Step 2: instantiate the system and create some data
            var protector = DataProtectionProvider
            .Create(directory, certificate)
            .CreateProtector("purpose");

            var data = protector.Protect("payload");

            // Step 3: validate that there's now a single key in the directory and that it's is protected using the certificate
            var allFiles = directory.GetFiles();
            Assert.Single(allFiles);
            Assert.StartsWith("key-", allFiles[0].Name, StringComparison.OrdinalIgnoreCase);
            string fileText = File.ReadAllText(allFiles[0].FullName);
            Assert.DoesNotContain("Warning: the key below is in an unencrypted form.", fileText, StringComparison.Ordinal);
            Assert.Contains("X509Certificate", fileText, StringComparison.Ordinal);

            // Step 4: setup a second system and validate it can decrypt keys and unprotect data
            var unprotector = DataProtectionProvider.Create(directory,
            b => b.UnprotectKeysWithAnyCertificate(certificate));
            Assert.Equal("payload", unprotector.CreateProtector("purpose").Unprotect(data));
        });
    }

    /// <summary>
    /// Runs a test and cleans up the temp directory afterward.
    /// </summary>
    private static void WithUniqueTempDirectory(Action<DirectoryInfo> testCode)
    {
        string uniqueTempPath = Path.Combine(AppContext.BaseDirectory, Path.GetRandomFileName());
        var dirInfo = new DirectoryInfo(uniqueTempPath);
        try
        {
            testCode(dirInfo);
        }
        finally
        {
            // clean up when test is done
            if (dirInfo.Exists)
            {
                dirInfo.Delete(recursive: true);
            }
        }
    }

    private static string GetTestFilesPath()
        => Path.Combine(AppContext.BaseDirectory, "TestFiles");
}
