// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Certificates.Generation.Tests
{
    public class CertificateManagerTests : IClassFixture<CertFixture>
    {
        private CertFixture _fixture;
        private CertificateManager _manager => _fixture.Manager;

        public CertificateManagerTests(ITestOutputHelper output, CertFixture fixture)
        {
            _fixture = fixture;
            Output = output;
        }

        public const string TestCertificateSubject = "CN=aspnet.test";

        public ITestOutputHelper Output { get; }

        [ConditionalFact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6721")]
        public void EnsureCreateHttpsCertificate_CreatesACertificate_WhenThereAreNoHttpsCertificates()
        {
            try
            {
                // Arrange
                _fixture.CleanupCertificates();

                const string CertificateName = nameof(EnsureCreateHttpsCertificate_CreatesACertificate_WhenThereAreNoHttpsCertificates) + ".cer";

                    // Act
                DateTimeOffset now = DateTimeOffset.UtcNow;
                now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
                var result = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), CertificateName, trust: false, subject: TestCertificateSubject, isInteractive: false);

                // Assert
                Assert.Equal(EnsureCertificateResult.Succeeded, result.ResultCode);
                Assert.True(File.Exists(CertificateName));

                var exportedCertificate = new X509Certificate2(File.ReadAllBytes(CertificateName));
                Assert.NotNull(exportedCertificate);
                Assert.False(exportedCertificate.HasPrivateKey);

                var httpsCertificates = CertificateManager.ListCertificates(CertificatePurpose.HTTPS, StoreName.My, StoreLocation.CurrentUser, isValid: false);
                var httpsCertificate = Assert.Single(httpsCertificates, c => c.Subject == TestCertificateSubject);
                Assert.True(httpsCertificate.HasPrivateKey);
                Assert.Equal(TestCertificateSubject, httpsCertificate.Subject);
                Assert.Equal(TestCertificateSubject, httpsCertificate.Issuer);
                Assert.Equal("sha256RSA", httpsCertificate.SignatureAlgorithm.FriendlyName);
                Assert.Equal("1.2.840.113549.1.1.11", httpsCertificate.SignatureAlgorithm.Value);

                Assert.Equal(now.LocalDateTime, httpsCertificate.NotBefore);
                Assert.Equal(now.AddYears(1).LocalDateTime, httpsCertificate.NotAfter);
                Assert.Contains(
                    httpsCertificate.Extensions.OfType<X509Extension>(),
                    e => e is X509BasicConstraintsExtension basicConstraints &&
                        basicConstraints.Critical == true &&
                        basicConstraints.CertificateAuthority == false &&
                        basicConstraints.HasPathLengthConstraint == false &&
                        basicConstraints.PathLengthConstraint == 0);

                Assert.Contains(
                    httpsCertificate.Extensions.OfType<X509Extension>(),
                    e => e is X509KeyUsageExtension keyUsage &&
                        keyUsage.Critical == true &&
                        keyUsage.KeyUsages == (X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature));

                Assert.Contains(
                    httpsCertificate.Extensions.OfType<X509Extension>(),
                    e => e is X509EnhancedKeyUsageExtension enhancedKeyUsage &&
                        enhancedKeyUsage.Critical == true &&
                        enhancedKeyUsage.EnhancedKeyUsages.OfType<Oid>().Single() is Oid keyUsage &&
                        keyUsage.Value == "1.3.6.1.5.5.7.3.1");

                // Subject alternative name
                Assert.Contains(
                    httpsCertificate.Extensions.OfType<X509Extension>(),
                    e => e.Critical == true &&
                        e.Oid.Value == "2.5.29.17");

                // ASP.NET HTTPS Development certificate extension
                Assert.Contains(
                    httpsCertificate.Extensions.OfType<X509Extension>(),
                    e => e.Critical == false &&
                        e.Oid.Value == "1.3.6.1.4.1.311.84.1.1" &&
                        e.RawData[0] == CertificateManager.AspNetHttpsCertificateVersion);

                Assert.Equal(httpsCertificate.GetCertHashString(), exportedCertificate.GetCertHashString());

            }
            catch (Exception e)
            {
                Output.WriteLine(e.Message);
                ListCertificates(Output);
                throw;
            }
        }

        private void ListCertificates(ITestOutputHelper output)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                var certificates = store.Certificates;
                foreach (var certificate in certificates)
                {
                    Output.WriteLine($"Certificate: '{Convert.ToBase64String(certificate.Export(X509ContentType.Cert))}'.");
                    certificate.Dispose();
                }

                store.Close();
            }
        }

        [ConditionalFact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6721")]
        public void EnsureCreateHttpsCertificate_DoesNotCreateACertificate_WhenThereIsAnExistingHttpsCertificates()
        {
            // Arrange
            const string CertificateName = nameof(EnsureCreateHttpsCertificate_DoesNotCreateACertificate_WhenThereIsAnExistingHttpsCertificates) + ".pfx";
            var certificatePassword = Guid.NewGuid().ToString();

            _fixture.CleanupCertificates();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
            _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, subject: TestCertificateSubject, isInteractive: false);

            var httpsCertificate = CertificateManager.ListCertificates(CertificatePurpose.HTTPS, StoreName.My, StoreLocation.CurrentUser, isValid: false).Single(c => c.Subject == TestCertificateSubject);

            // Act
            var result = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), CertificateName, trust: false, includePrivateKey: true, password: certificatePassword, subject: TestCertificateSubject, isInteractive: false);

            // Assert
            Assert.Equal(EnsureCertificateResult.ValidCertificatePresent, result.ResultCode);
            Assert.True(File.Exists(CertificateName));

            var exportedCertificate = new X509Certificate2(File.ReadAllBytes(CertificateName), certificatePassword);
            Assert.NotNull(exportedCertificate);
            Assert.True(exportedCertificate.HasPrivateKey);


            Assert.Equal(httpsCertificate.GetCertHashString(), exportedCertificate.GetCertHashString());
        }

        [ConditionalFact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6721")]
        public void EnsureCreateHttpsCertificate_ReturnsExpiredCertificateIfVersionIsIncorrect()
        {
            _fixture.CleanupCertificates();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
            _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, subject: TestCertificateSubject, isInteractive: false);

            CertificateManager.AspNetHttpsCertificateVersion = 2;

            var httpsCertificateList = CertificateManager.ListCertificates(CertificatePurpose.HTTPS, StoreName.My, StoreLocation.CurrentUser, isValid: true);
            Assert.Empty(httpsCertificateList);
        }

        [ConditionalFact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6721")]
        public void EnsureCreateHttpsCertificate_ReturnsExpiredCertificateForEmptyVersionField()
        {
            _fixture.CleanupCertificates();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
            CertificateManager.AspNetHttpsCertificateVersion = 0;
            _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, subject: TestCertificateSubject, isInteractive: false);

            CertificateManager.AspNetHttpsCertificateVersion = 1;

            var httpsCertificateList = CertificateManager.ListCertificates(CertificatePurpose.HTTPS, StoreName.My, StoreLocation.CurrentUser, isValid: true);
            Assert.Empty(httpsCertificateList);
        }

        [ConditionalFact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6721")]
        public void EnsureCreateHttpsCertificate_ReturnsValidIfVersionIsZero()
        {
            _fixture.CleanupCertificates();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
            CertificateManager.AspNetHttpsCertificateVersion = 0;
            _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, subject: TestCertificateSubject, isInteractive: false);

            var httpsCertificateList = CertificateManager.ListCertificates(CertificatePurpose.HTTPS, StoreName.My, StoreLocation.CurrentUser, isValid: true);
            Assert.NotEmpty(httpsCertificateList);
        }

        [ConditionalFact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6721")]
        public void EnsureCreateHttpsCertificate_ReturnValidIfCertIsNewer()
        {
            _fixture.CleanupCertificates();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
            CertificateManager.AspNetHttpsCertificateVersion = 2;
            _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, subject: TestCertificateSubject, isInteractive: false);

            CertificateManager.AspNetHttpsCertificateVersion = 1;
            var httpsCertificateList = CertificateManager.ListCertificates(CertificatePurpose.HTTPS, StoreName.My, StoreLocation.CurrentUser, isValid: true);
            Assert.NotEmpty(httpsCertificateList);
        }

        [Fact(Skip = "Requires user interaction")]
        public void EnsureAspNetCoreHttpsDevelopmentCertificate_ReturnsCorrectResult_WhenUserCancelsTrustStepOnWindows()
        {
            _fixture.CleanupCertificates();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
            var trustFailed = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: true, subject: TestCertificateSubject, isInteractive: false);

            Assert.Equal(EnsureCertificateResult.UserCancelledTrustStep, trustFailed.ResultCode);
        }

        [Fact(Skip = "Requires user interaction")]
        public void EnsureAspNetCoreHttpsDevelopmentCertificate_CanRemoveCertificates()
        {
            _fixture.CleanupCertificates();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
            _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: true, subject: TestCertificateSubject, isInteractive: false);

            _manager.CleanupHttpsCertificates(TestCertificateSubject);

            Assert.Empty(CertificateManager.ListCertificates(CertificatePurpose.HTTPS, StoreName.My, StoreLocation.CurrentUser, isValid: false).Where(c => c.Subject == TestCertificateSubject));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Empty(CertificateManager.ListCertificates(CertificatePurpose.HTTPS, StoreName.Root, StoreLocation.CurrentUser, isValid: false).Where(c => c.Subject == TestCertificateSubject));
            }
        }
    }

    public class CertFixture : IDisposable
    {
        public const string TestCertificateSubject = "CN=aspnet.test";

        public CertFixture()
        {
            Manager = new CertificateManager();

            CleanupCertificates();
        }

        internal CertificateManager Manager { get; set; }

        public void Dispose()
        {
            CleanupCertificates();
        }

        internal void CleanupCertificates()
        {
            Manager.RemoveAllCertificates(CertificatePurpose.HTTPS, StoreName.My, StoreLocation.CurrentUser, TestCertificateSubject);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Manager.RemoveAllCertificates(CertificatePurpose.HTTPS, StoreName.Root, StoreLocation.CurrentUser, TestCertificateSubject);
            }
        }
    }
}
