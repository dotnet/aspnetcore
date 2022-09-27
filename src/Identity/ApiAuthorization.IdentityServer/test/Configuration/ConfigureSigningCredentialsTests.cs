// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer;

public class ConfigureSigningCredentialsTests
{
    // We need to cast the underlying int value of the EphemeralKeySet to X509KeyStorageFlags
    // due to the fact that is not part of .NET Standard. This value is only used with non-windows
    // platforms (all .NET Core) for which the value is defined on the underlying platform.
    private const X509KeyStorageFlags UnsafeEphemeralKeySet = (X509KeyStorageFlags)32;
    private static readonly X509KeyStorageFlags DefaultFlags = OperatingSystem.IsLinux() ?
        UnsafeEphemeralKeySet : (OperatingSystem.IsMacOS() ? X509KeyStorageFlags.PersistKeySet :
        X509KeyStorageFlags.DefaultKeySet);

    [ConditionalFact]
    [FrameworkSkipCondition(RuntimeFrameworks.CLR)]
    public void Configure_NoOpsWhenConfigurationIsEmpty()
    {
        var expectedKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "./testkey.json");
        try
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                }).Build();

            var configureSigningCredentials = new ConfigureSigningCredentials(
                configuration,
                new TestLogger<ConfigureSigningCredentials>());

            var options = new ApiAuthorizationOptions();

            // Act
            configureSigningCredentials.Configure(options);

            // Assert
            Assert.NotNull(options);
            Assert.False(File.Exists(expectedKeyPath));
            Assert.Null(options.SigningCredential);
        }
        finally
        {
            if (File.Exists(expectedKeyPath))
            {
                File.Delete(expectedKeyPath);
            }
        }
    }

    [ConditionalFact]
    [FrameworkSkipCondition(RuntimeFrameworks.CLR)]
    public void Configure_AddsDevelopmentKeyFromConfiguration()
    {
        var expectedKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "./testkey.json");
        try
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    ["Type"] = "Development",
                    ["FilePath"] = "testkey.json"
                }).Build();

            var configureSigningCredentials = new ConfigureSigningCredentials(
                configuration,
                new TestLogger<ConfigureSigningCredentials>());

            var options = new ApiAuthorizationOptions();

            // Act
            configureSigningCredentials.Configure(options);

            // Assert
            Assert.NotNull(options);
            Assert.True(File.Exists(expectedKeyPath));
            Assert.NotNull(options.SigningCredential);
            Assert.Equal("Development", options.SigningCredential.Kid);
            Assert.IsType<RsaSecurityKey>(options.SigningCredential.Key);
        }
        finally
        {
            if (File.Exists(expectedKeyPath))
            {
                File.Delete(expectedKeyPath);
            }
        }
    }

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720", Queues = "All.OSX")]
    public void Configure_LoadsPfxCertificateCredentialFromConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["Type"] = "File",
                ["FilePath"] = "test.pfx",
                ["Password"] = "aspnetcore"
            }).Build();

        var configureSigningCredentials = new ConfigureSigningCredentials(
            configuration,
            new TestLogger<ConfigureSigningCredentials>());

        var options = new ApiAuthorizationOptions();

        // Act
        configureSigningCredentials.Configure(options);

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.SigningCredential);
        var key = Assert.IsType<X509SecurityKey>(options.SigningCredential.Key);
        Assert.NotNull(key.Certificate);
        Assert.Equal("AC8FDF4BD4C10841BD24DC88D983225D10B43BB2", key.Certificate.Thumbprint);
    }

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720", Queues = "All.OSX")]
    public void Configure_LoadsCertificateStoreCertificateCredentialFromConfiguration()
    {
        try
        {
            // Arrange
            var x509Certificate = new X509Certificate2("test.pfx", "aspnetcore", DefaultFlags);
            SetupTestCertificate(x509Certificate);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    ["Type"] = "Store",
                    ["StoreLocation"] = "CurrentUser",
                    ["StoreName"] = "My",
                    ["Name"] = "CN=Test"
                }).Build();

            var configureSigningCredentials = new ConfigureSigningCredentials(
                configuration,
                new TestLogger<ConfigureSigningCredentials>());

            var options = new ApiAuthorizationOptions();

            // Act
            configureSigningCredentials.Configure(options);

            // Assert
            Assert.NotNull(options);
            Assert.NotNull(options.SigningCredential);
            var key = Assert.IsType<X509SecurityKey>(options.SigningCredential.Key);
            Assert.NotNull(key.Certificate);
            Assert.Equal("AC8FDF4BD4C10841BD24DC88D983225D10B43BB2", key.Certificate.Thumbprint);
        }
        finally
        {
            CleanupTestCertificate();
        }
    }

    private static void CleanupTestCertificate()
    {
        using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
        {
            store.Open(OpenFlags.ReadWrite);
            var certificates = store
                .Certificates
                .Find(X509FindType.FindByThumbprint, "1646CFBEE354788D7116DF86EFC35C0075A9C05D", validOnly: false);

            foreach (var certificate in certificates)
            {
                store.Certificates.Remove(certificate);
            }
            foreach (var certificate in certificates)
            {
                certificate.Dispose();
            }

            store.Close();
        }
    }

    private static void SetupTestCertificate(X509Certificate2 x509Certificate)
    {
        using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
        {
            store.Open(OpenFlags.ReadWrite);
            var certificates = store
                .Certificates
                .Find(X509FindType.FindByThumbprint, "AC8FDF4BD4C10841BD24DC88D983225D10B43BB2", validOnly: false);
            if (certificates.Count == 0)
            {
                store.Add(x509Certificate);
            }
            foreach (var certificate in certificates)
            {
                certificate.Dispose();
            }
            store.Close();
        }
    }
}
