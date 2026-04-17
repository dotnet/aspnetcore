// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection.Managed;

namespace Microsoft.AspNetCore.DataProtection.NuGetIntegrationTests;

/// <summary>
/// Cross-version compatibility tests that download a released NuGet package,
/// load the ManagedAuthenticatedEncryptor from it via reflection in an isolated
/// AssemblyLoadContext, and verify that payloads encrypted by the released version
/// can be decrypted by the current source code (and vice versa).
/// </summary>
public class CrossVersionCompatibility_ManagedAuthenticatedEncryptor_Tests : IAsyncLifetime
{
    // Pin to a known-good released version. Update this when a new patch is verified.
    private const string NuGetPackageVersion = "10.0.5";

    private readonly NuGetEncryptorFactory _factory = new(NuGetPackageVersion);

    private NuGetEncryptorWrapper _nugetEncryptor = null!;
    private ManagedAuthenticatedEncryptor _currentEncryptor = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();

        _nugetEncryptor = _factory.CreateEncryptor();
        _currentEncryptor = CreateCurrentEncryptor();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        _currentEncryptor?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public void CurrentCode_CanDecrypt_PayloadFromReleasedNuGet()
    {
        var plaintext = new ArraySegment<byte>("cross-version-test-payload"u8.ToArray());
        var aad = new ArraySegment<byte>("cross-version-aad"u8.ToArray());

        // Encrypt with released NuGet version, decrypt with current source code
        var ciphertext = _nugetEncryptor.Encrypt(plaintext, aad);
        var decrypted = _currentEncryptor.Decrypt(new ArraySegment<byte>(ciphertext), aad);

        Assert.Equal(plaintext.ToArray(), decrypted);
    }

    [Fact]
    public void ReleasedNuGet_CanDecrypt_PayloadFromCurrentCode()
    {
        var plaintext = new ArraySegment<byte>("cross-version-test-payload"u8.ToArray());
        var aad = new ArraySegment<byte>("cross-version-aad"u8.ToArray());

        // Encrypt with current source code, decrypt with released NuGet version
        var ciphertext = _currentEncryptor.Encrypt(plaintext, aad);
        var decrypted = _nugetEncryptor.Decrypt(new ArraySegment<byte>(ciphertext), aad);

        Assert.Equal(plaintext.ToArray(), decrypted);
    }

    [Fact]
    public void ReleasedNuGet_SelfTest_RoundTrips()
    {
        // verifies that NuGet is valid and can perofrm roundtrip well
        var plaintext = new ArraySegment<byte>("self-test-roundtrip"u8.ToArray());
        var aad = new ArraySegment<byte>("self-test-aad"u8.ToArray());

        var ciphertext = _nugetEncryptor.Encrypt(plaintext, aad);
        var decrypted = _nugetEncryptor.Decrypt(new ArraySegment<byte>(ciphertext), aad);

        Assert.Equal(plaintext.ToArray(), decrypted);
    }

    private static ManagedAuthenticatedEncryptor CreateCurrentEncryptor()
    {
        return new ManagedAuthenticatedEncryptor(
            new Secret(new byte[512 / 8]),
            symmetricAlgorithmFactory: Aes.Create,
            symmetricAlgorithmKeySizeInBytes: 256 / 8,
            validationAlgorithmFactory: () => new HMACSHA256());
    }
}
