// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.Test.Shared;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection
{
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
            Assert.NotNull(FileSystemXmlRepository.DefaultKeyStorageDirectory);

            var keysPath = FileSystemXmlRepository.DefaultKeyStorageDirectory.FullName;
            var tempPath = FileSystemXmlRepository.DefaultKeyStorageDirectory.FullName + "Temp";

            try
            {
                // Step 1: Move the current contents, if any, to a temporary directory.
                if (Directory.Exists(keysPath))
                {
                    Directory.Move(keysPath, tempPath);
                }

                // Step 2: Instantiate the system and round-trip a payload
                var protector = DataProtectionProvider.Create("TestApplication").CreateProtector("purpose");
                Assert.Equal("payload", protector.Unprotect(protector.Protect("payload")));

                // Step 3: Validate that there's now a single key in the directory
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
            finally
            {
                if (Directory.Exists(keysPath))
                {
                    Directory.Delete(keysPath, recursive: true);
                }
                if (Directory.Exists(tempPath))
                {
                    Directory.Move(tempPath, keysPath);
                }
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

        [Fact]
        public void System_UsesProvidedDirectoryAndCertificate()
        {
            var filePath = Path.Combine(GetTestFilesPath(), "TestCert.pfx");
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(new X509Certificate2(filePath, "password", X509KeyStorageFlags.Exportable));
            store.Close();

            WithUniqueTempDirectory(directory =>
            {
                var certificateStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                certificateStore.Open(OpenFlags.ReadWrite);
                var certificate = certificateStore.Certificates.Find(X509FindType.FindBySubjectName, "TestCert", false)[0];

                try
                {
                    // Step 1: directory should be completely empty
                    directory.Create();
                    Assert.Empty(directory.GetFiles());

                    // Step 2: instantiate the system and round-trip a payload
                    var protector = DataProtectionProvider.Create(directory, certificate).CreateProtector("purpose");
                    Assert.Equal("payload", protector.Unprotect(protector.Protect("payload")));

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

        [Fact]
        public void System_UsesInMemoryCertificate()
        {
            var filePath = Path.Combine(GetTestFilesPath(), "TestCert2.pfx");
            var certificate = new X509Certificate2(filePath, "password");

            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                // ensure this cert is not in the x509 store
                Assert.Empty(store.Certificates.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, false));
            }

            WithUniqueTempDirectory(directory =>
            {
                // Step 1: directory should be completely empty
                directory.Create();
                Assert.Empty(directory.GetFiles());

                // Step 2: instantiate the system and round-trip a payload
                var protector = DataProtectionProvider.Create(directory, certificate).CreateProtector("purpose");
                Assert.Equal("payload", protector.Unprotect(protector.Protect("payload")));

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
            string uniqueTempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
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
}
