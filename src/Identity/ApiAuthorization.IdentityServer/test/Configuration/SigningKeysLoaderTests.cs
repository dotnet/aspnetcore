// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration
{
    public class SigningKeysLoaderTests
    {
        // We need to cast the underlying int value of the EphemeralKeySet to X509KeyStorageFlags
        // due to the fact that is not part of .NET Standard. This value is only used with non-windows
        // platforms (all .NET Core) for which the value is defined on the underlying platform.
        private const X509KeyStorageFlags UnsafeEphemeralKeySet = (X509KeyStorageFlags)32;
        private static readonly X509KeyStorageFlags DefaultFlags = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ?
            UnsafeEphemeralKeySet : (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? X509KeyStorageFlags.PersistKeySet :
            X509KeyStorageFlags.DefaultKeySet);

        [Fact]
        public void LoadFromFile_ThrowsIfFileDoesNotExist()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => SigningKeysLoader.LoadFromFile("./nonexisting.pfx", "", DefaultFlags));
            Assert.Equal($"There was an error loading the certificate. The file './nonexisting.pfx' was not found.", exception.Message);
        }

        [Fact]
        public void LoadFromFile_ThrowsIfPasswordIsNull()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => SigningKeysLoader.LoadFromFile("test.pfx", null, DefaultFlags));
            Assert.Equal("There was an error loading the certificate. No password was provided.", exception.Message);
        }

        [Fact]
        public void LoadFromFile_ThrowsIfPasswordIsIncorrect()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => SigningKeysLoader.LoadFromFile("test.pfx", "incorrect", DefaultFlags));
            Assert.Equal(
                $"There was an error loading the certificate. Either the password is incorrect or the process does not have permisions to store the key in the Keyset '{DefaultFlags}'",
                exception.Message);
        }

        [Fact]
        public static void LoadFromStoreCert_ThrowsIfThereIsNoCertificateAvailable()
        {
            // Arrange
            var time = new DateTimeOffset(2018, 09, 25, 12, 0, 0, TimeSpan.Zero);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => SigningKeysLoader.LoadFromStoreCert("Invalid", "My", StoreLocation.CurrentUser, time));
            Assert.Equal("Couldn't find a valid certificate with subject 'Invalid' on the 'CurrentUser\\My'", exception.Message);
        }

        [ConditionalFact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720")]
        public static void LoadFromStoreCert_SkipsCertificatesNotYetValid()
        {
            try
            {
                SetupCertificates("./current.pfx", "./future.pfx");
                // Arrange
                var time = new DateTimeOffset(2018, 10, 29, 12, 0, 0, TimeSpan.Zero);

                // Act
                var certificate = SigningKeysLoader.LoadFromStoreCert("CN=SigningKeysLoaderTest", "My", StoreLocation.CurrentUser, time);

                // Assert
                Assert.NotNull(certificate);
                Assert.Equal("C54CD513088C23EC2AFD256874CC6C0F81EA9D5E", certificate.Thumbprint);
            }
            finally
            {
                CleanupCertificates();
            }
        }

        [ConditionalFact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720")]
        public static void LoadFromStoreCert_PrefersCertificatesCloserToExpirationDate()
        {
            try
            {
                SetupCertificates("./current.pfx", "./future.pfx");
                // Arrange
                var time = new DateTimeOffset(2020, 10, 29, 12, 0, 0, TimeSpan.Zero);

                // Act
                var certificate = SigningKeysLoader.LoadFromStoreCert("CN=SigningKeysLoaderTest", "My", StoreLocation.CurrentUser, time);

                // Assert
                Assert.NotNull(certificate);
                Assert.Equal("C54CD513088C23EC2AFD256874CC6C0F81EA9D5E", certificate.Thumbprint);
            }
            finally
            {
                CleanupCertificates();
            }
        }

        [ConditionalFact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720")]
        public static void LoadFromStoreCert_SkipsExpiredCertificates()
        {
            try
            {
                SetupCertificates("./expired.pfx", "./current.pfx", "./future.pfx");
                // Arrange
                var time = new DateTimeOffset(2024, 01, 01, 12, 0, 0, TimeSpan.Zero);

                // Act
                var certificate = SigningKeysLoader.LoadFromStoreCert("CN=SigningKeysLoaderTest", "My", StoreLocation.CurrentUser, time);

                // Assert
                Assert.NotNull(certificate);
                Assert.Equal("35840DD366107B89D2885A6B4F42CCBBAE6BA8E3", certificate.Thumbprint);
            }
            finally
            {
                CleanupCertificates();
            }
        }

        [Fact]
        public static void LoadDevelopment_ThrowsIfKeyDoesNotExist()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => SigningKeysLoader.LoadDevelopment("c:/inexistent.json", createIfMissing: false));
            Assert.Equal("Couldn't find the file 'c:/inexistent.json' and creation of a development key was not requested.", exception.Message);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR)]
        public static void LoadDevelopment_CreatesKeyIfItDoesNotExist()
        {
            // Arrange
            var path = "./tempkeyfolder/tempkey.json";
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            // Act
            var key = SigningKeysLoader.LoadDevelopment(path, createIfMissing: true);

            // Assert
            Assert.NotNull(key);
            Assert.True(File.Exists(path));
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR)]
        public static void LoadDevelopment_ReusesKeyIfExists()
        {
            // Arrange
            var path = "./tempkeyfolder/existing.json";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var existingKey = SigningKeysLoader.LoadDevelopment(path, createIfMissing: true);
            var existingParameters = existingKey.ExportParameters(includePrivateParameters: true);

            // Act
            var currentKey = SigningKeysLoader.LoadDevelopment(path, createIfMissing: true);
            var currentParameters = currentKey.ExportParameters(includePrivateParameters: true);

            // Assert
            Assert.NotNull(currentKey);
            Assert.Equal(existingParameters.P, currentParameters.P);
            Assert.Equal(existingParameters.Q, currentParameters.Q);
        }

        private static void CleanupCertificates()
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                store.RemoveRange(store.Certificates.Find(X509FindType.FindBySubjectName, "CN=SigningKeysLoaderTest", validOnly: false));
                store.Close();
            }
        }

        private static void SetupCertificates(params string[] certificateFiles)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                foreach (var certificate in certificateFiles)
                {
                    var cert = new X509Certificate2(certificate, "aspnetcore", DefaultFlags);
                    if (!(store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, validOnly: false).Count > 0))
                    {
                        store.Add(cert);
                    }
                }
                store.Close();
            }
        }
    }
}
