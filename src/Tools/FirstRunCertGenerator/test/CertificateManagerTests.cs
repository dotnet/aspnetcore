// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Certificates.Generation.Tests;

public class CertificateManagerTests : IClassFixture<CertFixture>
{
    private readonly CertFixture _fixture;
    private CertificateManager _manager => _fixture.Manager;

    public CertificateManagerTests(ITestOutputHelper output, CertFixture fixture)
    {
        _fixture = fixture;
        Output = output;
    }

    private const string TestCertificateSubject = "CN=aspnet.test";

    public ITestOutputHelper Output { get; }

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720", Queues = "All.OSX")]
    public void EnsureCreateHttpsCertificate_CreatesACertificate_WhenThereAreNoHttpsCertificates()
    {
        try
        {
            // Arrange
            _fixture.CleanupCertificates();

            const string CertificateName = nameof(EnsureCreateHttpsCertificate_CreatesACertificate_WhenThereAreNoHttpsCertificates) + ".cer";

            // Act
            var now = DateTimeOffset.UtcNow;
            now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
            var result = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), CertificateName, trust: false, isInteractive: false);

            // Assert
            Assert.Equal(EnsureCertificateResult.Succeeded, result);
            Assert.True(File.Exists(CertificateName));

            var exportedCertificate = new X509Certificate2(File.ReadAllBytes(CertificateName));
            Assert.NotNull(exportedCertificate);
            Assert.False(exportedCertificate.HasPrivateKey);

            var httpsCertificates = _manager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: false);
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
                    e.Oid.Value == CertificateManager.AspNetHttpsOid &&
                    e.RawData[0] == _manager.AspNetHttpsCertificateVersion);

            Assert.Equal(httpsCertificate.GetCertHashString(), exportedCertificate.GetCertHashString());

        }
        catch (Exception e)
        {
            Output.WriteLine(e.Message);
            ListCertificates();
            throw;
        }
    }

    private void ListCertificates()
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
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720", Queues = "All.OSX")]
    public void EnsureCreateHttpsCertificate_DoesNotCreateACertificate_WhenThereIsAnExistingHttpsCertificates()
    {
        // Arrange
        const string CertificateName = nameof(EnsureCreateHttpsCertificate_DoesNotCreateACertificate_WhenThereIsAnExistingHttpsCertificates) + ".pfx";
        var certificatePassword = Guid.NewGuid().ToString();

        _fixture.CleanupCertificates();

        var now = DateTimeOffset.UtcNow;
        now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
        var creation = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, isInteractive: false);
        Output.WriteLine(creation.ToString());
        ListCertificates();

        var httpsCertificate = _manager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: false).Single(c => c.Subject == TestCertificateSubject);
        // Act
        var result = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), CertificateName, trust: false, includePrivateKey: true, password: certificatePassword, isInteractive: false);

        // Assert
        Assert.Equal(EnsureCertificateResult.ValidCertificatePresent, result);
        Assert.True(File.Exists(CertificateName));

        var exportedCertificate = new X509Certificate2(File.ReadAllBytes(CertificateName), certificatePassword);
        Assert.NotNull(exportedCertificate);
        Assert.True(exportedCertificate.HasPrivateKey);

        Assert.Equal(httpsCertificate.GetCertHashString(), exportedCertificate.GetCertHashString());
    }

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720", Queues = "All.OSX")]
    public void EnsureCreateHttpsCertificate_CanExportTheCertInPemFormat()
    {
        // Arrange
        var message = "plaintext";
        const string CertificateName = nameof(EnsureCreateHttpsCertificate_DoesNotCreateACertificate_WhenThereIsAnExistingHttpsCertificates) + ".pem";
        var certificatePassword = Guid.NewGuid().ToString();

        _fixture.CleanupCertificates();

        var now = DateTimeOffset.UtcNow;
        now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
        var creation = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, isInteractive: false);
        Output.WriteLine(creation.ToString());
        ListCertificates();

        var httpsCertificate = _manager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: false).Single(c => c.Subject == TestCertificateSubject);

        // Act
        var result = _manager
            .EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), CertificateName, trust: false, includePrivateKey: true, password: certificatePassword, keyExportFormat: CertificateKeyExportFormat.Pem, isInteractive: false);

        // Assert
        Assert.Equal(EnsureCertificateResult.ValidCertificatePresent, result);
        Assert.True(File.Exists(CertificateName));

        var exportedCertificate = X509Certificate2.CreateFromEncryptedPemFile(CertificateName, certificatePassword, Path.ChangeExtension(CertificateName, "key"));
        Assert.NotNull(exportedCertificate);
        Assert.True(exportedCertificate.HasPrivateKey);

        Assert.Equal("plaintext", Encoding.ASCII.GetString(exportedCertificate.GetRSAPrivateKey().Decrypt(exportedCertificate.GetRSAPrivateKey().Encrypt(Encoding.ASCII.GetBytes(message), RSAEncryptionPadding.OaepSHA256), RSAEncryptionPadding.OaepSHA256)));
        Assert.Equal(httpsCertificate.GetCertHashString(), exportedCertificate.GetCertHashString());
    }

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720", Queues = "All.OSX")]
    public void EnsureCreateHttpsCertificate_CanExportTheCertInPemFormat_WithoutKey()
    {
        // Arrange
        const string CertificateName = nameof(EnsureCreateHttpsCertificate_DoesNotCreateACertificate_WhenThereIsAnExistingHttpsCertificates) + ".pem";

        _fixture.CleanupCertificates();

        var now = DateTimeOffset.UtcNow;
        now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
        var creation = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, isInteractive: false);
        Output.WriteLine(creation.ToString());
        ListCertificates();

        var httpsCertificate = _manager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: false).Single(c => c.Subject == TestCertificateSubject);

        // Act
        var result = _manager
            .EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), CertificateName, trust: false, includePrivateKey: false, password: null, keyExportFormat: CertificateKeyExportFormat.Pem, isInteractive: false);

        // Assert
        Assert.Equal(EnsureCertificateResult.ValidCertificatePresent, result);
        Assert.True(File.Exists(CertificateName));

        var exportedCertificate = new X509Certificate2(CertificateName);
        Assert.NotNull(exportedCertificate);
        Assert.False(exportedCertificate.HasPrivateKey);
    }

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720", Queues = "All.OSX")]
    public void EnsureCreateHttpsCertificate_CanImport_ExportedPfx()
    {
        // Arrange
        const string CertificateName = nameof(EnsureCreateHttpsCertificate_DoesNotCreateACertificate_WhenThereIsAnExistingHttpsCertificates) + ".pfx";
        var certificatePassword = Guid.NewGuid().ToString();

        _fixture.CleanupCertificates();

        var now = DateTimeOffset.UtcNow;
        now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
        var creation = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, isInteractive: false);
        Output.WriteLine(creation.ToString());
        ListCertificates();

        var httpsCertificate = _manager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: false).Single(c => c.Subject == TestCertificateSubject);

        _manager
            .EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), CertificateName, trust: false, includePrivateKey: true, password: certificatePassword, isInteractive: false);

        _manager.CleanupHttpsCertificates();

        // Act
        var result = _manager.ImportCertificate(CertificateName, certificatePassword);

        // Assert
        Assert.Equal(ImportCertificateResult.Succeeded, result);
        var importedCertificate = Assert.Single(_manager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: false));

        Assert.Equal(httpsCertificate.GetCertHashString(), importedCertificate.GetCertHashString());
    }

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720", Queues = "All.OSX")]
    public void EnsureCreateHttpsCertificate_CanImport_ExportedPfx_FailsIfThereAreCertificatesPresent()
    {
        // Arrange
        const string CertificateName = nameof(EnsureCreateHttpsCertificate_DoesNotCreateACertificate_WhenThereIsAnExistingHttpsCertificates) + ".pfx";
        var certificatePassword = Guid.NewGuid().ToString();

        _fixture.CleanupCertificates();

        var now = DateTimeOffset.UtcNow;
        now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
        var creation = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, isInteractive: false);
        Output.WriteLine(creation.ToString());
        ListCertificates();

        var httpsCertificate = _manager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: false).Single(c => c.Subject == TestCertificateSubject);

        _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), CertificateName, trust: false, includePrivateKey: true, password: certificatePassword, isInteractive: false);

        // Act
        var result = _manager.ImportCertificate(CertificateName, certificatePassword);

        // Assert
        Assert.Equal(ImportCertificateResult.ExistingCertificatesPresent, result);
    }

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720", Queues = "All.OSX")]
    public void EnsureCreateHttpsCertificate_CanExportTheCertInPemFormat_WithoutPassword()
    {
        // Arrange
        var message = "plaintext";
        const string CertificateName = nameof(EnsureCreateHttpsCertificate_DoesNotCreateACertificate_WhenThereIsAnExistingHttpsCertificates) + ".pem";
        _fixture.CleanupCertificates();

        var now = DateTimeOffset.UtcNow;
        now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
        var creation = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, isInteractive: false);
        Output.WriteLine(creation.ToString());
        ListCertificates();

        var httpsCertificate = _manager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: false).Single(c => c.Subject == TestCertificateSubject);
        // Act
        var result = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), CertificateName, trust: false, includePrivateKey: true, password: null, keyExportFormat: CertificateKeyExportFormat.Pem, isInteractive: false);

        // Assert
        Assert.Equal(EnsureCertificateResult.ValidCertificatePresent, result);
        Assert.True(File.Exists(CertificateName));

        var exportedCertificate = X509Certificate2.CreateFromPemFile(CertificateName, Path.ChangeExtension(CertificateName, "key"));
        Assert.NotNull(exportedCertificate);
        Assert.True(exportedCertificate.HasPrivateKey);

        Assert.Equal("plaintext", Encoding.ASCII.GetString(exportedCertificate.GetRSAPrivateKey().Decrypt(exportedCertificate.GetRSAPrivateKey().Encrypt(Encoding.ASCII.GetBytes(message), RSAEncryptionPadding.OaepSHA256), RSAEncryptionPadding.OaepSHA256)));
        Assert.Equal(httpsCertificate.GetCertHashString(), exportedCertificate.GetCertHashString());
    }

    [Fact]
    public void EnsureCreateHttpsCertificate_ReturnsExpiredCertificateIfVersionIsIncorrect()
    {
        _fixture.CleanupCertificates();

        var now = DateTimeOffset.UtcNow;
        now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
        var creation = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, isInteractive: false);
        Output.WriteLine(creation.ToString());
        ListCertificates();

        _manager.AspNetHttpsCertificateVersion = 2;

        var httpsCertificateList = _manager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: true);
        Assert.Empty(httpsCertificateList);
    }

    [Fact]
    public void EnsureCreateHttpsCertificate_ReturnsExpiredCertificateForEmptyVersionField()
    {
        _fixture.CleanupCertificates();

        var now = DateTimeOffset.UtcNow;
        now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
        _manager.AspNetHttpsCertificateVersion = 0;
        var creation = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, isInteractive: false);
        Output.WriteLine(creation.ToString());
        ListCertificates();

        _manager.AspNetHttpsCertificateVersion = 1;

        var httpsCertificateList = _manager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: true);
        Assert.Empty(httpsCertificateList);
    }

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720", Queues = "All.OSX")]
    public void EnsureCreateHttpsCertificate_ReturnsValidIfVersionIsZero()
    {
        _fixture.CleanupCertificates();

        var now = DateTimeOffset.UtcNow;
        now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
        _manager.AspNetHttpsCertificateVersion = 0;
        var creation = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, isInteractive: false);
        Output.WriteLine(creation.ToString());
        ListCertificates();

        var httpsCertificateList = _manager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: true);
        Assert.NotEmpty(httpsCertificateList);
    }

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720", Queues = "All.OSX")]
    public void EnsureCreateHttpsCertificate_ReturnValidIfCertIsNewer()
    {
        _fixture.CleanupCertificates();

        var now = DateTimeOffset.UtcNow;
        now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
        _manager.AspNetHttpsCertificateVersion = 2;
        var creation = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, isInteractive: false);
        Output.WriteLine(creation.ToString());
        ListCertificates();

        _manager.AspNetHttpsCertificateVersion = 1;
        var httpsCertificateList = _manager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: true);
        Assert.NotEmpty(httpsCertificateList);
    }

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720", Queues = "All.OSX")]
    public void ListCertificates_AlwaysReturnsTheCertificate_WithHighestVersion()
    {
        _fixture.CleanupCertificates();

        var now = DateTimeOffset.UtcNow;
        now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
        _manager.AspNetHttpsCertificateVersion = 1;
        var creation = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, isInteractive: false);
        Output.WriteLine(creation.ToString());
        ListCertificates();

        _manager.AspNetHttpsCertificateVersion = 2;
        creation = _manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), path: null, trust: false, isInteractive: false);
        Output.WriteLine(creation.ToString());
        ListCertificates();

        _manager.AspNetHttpsCertificateVersion = 1;
        var httpsCertificateList = _manager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: true);
        Assert.Equal(2, httpsCertificateList.Count);

        var firstCertificate = httpsCertificateList[0];
        var secondCertificate = httpsCertificateList[1];

        Assert.Contains(
            firstCertificate.Extensions.OfType<X509Extension>(),
            e => e.Critical == false &&
                e.Oid.Value == CertificateManager.AspNetHttpsOid &&
                e.RawData[0] == 2);

        Assert.Contains(
            secondCertificate.Extensions.OfType<X509Extension>(),
            e => e.Critical == false &&
                e.Oid.Value == CertificateManager.AspNetHttpsOid &&
                e.RawData[0] == 1);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows, SkipReason = "UnixFileMode is not supported on Windows.")]
    [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "https://github.com/dotnet/aspnetcore/issues/6720")]
    public void EnsureCreateHttpsCertificate_CreatesFilesWithUserOnlyUnixFileMode()
    {
        _fixture.CleanupCertificates();

        const string CertificateName = nameof(EnsureCreateHttpsCertificate_CreatesFilesWithUserOnlyUnixFileMode) + ".pem";
        const string KeyName = nameof(EnsureCreateHttpsCertificate_CreatesFilesWithUserOnlyUnixFileMode) + ".key";

        var certificatePassword = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);

        var result = _manager
            .EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), CertificateName, trust: false, includePrivateKey: true, password: certificatePassword, keyExportFormat: CertificateKeyExportFormat.Pem, isInteractive: false);

        Assert.Equal(EnsureCertificateResult.Succeeded, result);

        Assert.True(File.Exists(CertificateName));
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(CertificateName));

        Assert.True(File.Exists(KeyName));
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(KeyName));
    }
}

public class CertFixture : IDisposable
{
    public const string TestCertificateSubject = "CN=aspnet.test";

    public CertFixture()
    {
        Manager = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
        new WindowsCertificateManager(TestCertificateSubject, 1) :
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
            new MacOSCertificateManager(TestCertificateSubject, 1) as CertificateManager :
            new UnixCertificateManager(TestCertificateSubject, 1);

        CleanupCertificates();
    }

    internal CertificateManager Manager { get; set; }

    public void Dispose() => CleanupCertificates();

    internal void CleanupCertificates()
    {
        Manager.RemoveAllCertificates(StoreName.My, StoreLocation.CurrentUser);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Manager.RemoveAllCertificates(StoreName.Root, StoreLocation.CurrentUser);
        }
    }
}
