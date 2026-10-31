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
public class CrossVersionCompatibility_ManagedAuthenticatedEncryptor_Tests : IClassFixture<NuGetEncryptorFactory>
{
    private readonly NuGetEncryptorFactory _factory;

    public CrossVersionCompatibility_ManagedAuthenticatedEncryptor_Tests(NuGetEncryptorFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("net9.0")]
    [InlineData("net462")]
    public void CurrentCode_CanDecrypt_PayloadFromReleasedNuGet(string targetFramework)
    {
        var nugetEncryptor = _factory.CreateEncryptor(targetFramework);
        using var currentEncryptor = CreateCurrentEncryptor();
        var plaintext = new ArraySegment<byte>("cross-version-test-payload"u8.ToArray());
        var aad = new ArraySegment<byte>("cross-version-aad"u8.ToArray());

        var ciphertext = nugetEncryptor.Encrypt(plaintext, aad);
        var decrypted = currentEncryptor.Decrypt(new ArraySegment<byte>(ciphertext), aad);

        Assert.Equal(plaintext.ToArray(), decrypted);
    }

    [Theory]
    [InlineData("net9.0")]
    [InlineData("net462")]
    public void ReleasedNuGet_CanDecrypt_PayloadFromCurrentCode(string targetFramework)
    {
        var nugetEncryptor = _factory.CreateEncryptor(targetFramework);
        using var currentEncryptor = CreateCurrentEncryptor();
        var plaintext = new ArraySegment<byte>("cross-version-test-payload"u8.ToArray());
        var aad = new ArraySegment<byte>("cross-version-aad"u8.ToArray());

        var ciphertext = currentEncryptor.Encrypt(plaintext, aad);
        var decrypted = nugetEncryptor.Decrypt(new ArraySegment<byte>(ciphertext), aad);

        Assert.Equal(plaintext.ToArray(), decrypted);
    }

    [Theory]
    [InlineData("net9.0")]
    [InlineData("net462")]
    public void ReleasedNuGet_SelfTest_RoundTrips(string targetFramework)
    {
        var nugetEncryptor = _factory.CreateEncryptor(targetFramework);
        var plaintext = new ArraySegment<byte>("self-test-roundtrip"u8.ToArray());
        var aad = new ArraySegment<byte>("self-test-aad"u8.ToArray());

        var ciphertext = nugetEncryptor.Encrypt(plaintext, aad);
        var decrypted = nugetEncryptor.Decrypt(new ArraySegment<byte>(ciphertext), aad);

        Assert.Equal(plaintext.ToArray(), decrypted);
    }

    [Fact]
    public void CurrentNetCore_CanDecrypt_PayloadFromCurrentNetFx()
    {
        // Verifies current source: #if NET path can decrypt what #else path encrypted.
        // Loads the source-built net462 DLL via ALC to get the #else code path.
        var netFxEncryptor = _factory.CreateSourceBuiltNetFxEncryptor();
        using var netCoreEncryptor = CreateCurrentEncryptor();
        var plaintext = new ArraySegment<byte>("cross-tfm-current-code"u8.ToArray());
        var aad = new ArraySegment<byte>("cross-tfm-aad"u8.ToArray());

        var ciphertext = netFxEncryptor.Encrypt(plaintext, aad);
        var decrypted = netCoreEncryptor.Decrypt(new ArraySegment<byte>(ciphertext), aad);

        Assert.Equal(plaintext.ToArray(), decrypted);
    }

    [Fact]
    public void CurrentNetFx_CanDecrypt_PayloadFromCurrentNetCore()
    {
        var netFxEncryptor = _factory.CreateSourceBuiltNetFxEncryptor();
        using var netCoreEncryptor = CreateCurrentEncryptor();
        var plaintext = new ArraySegment<byte>("cross-tfm-current-code"u8.ToArray());
        var aad = new ArraySegment<byte>("cross-tfm-aad"u8.ToArray());

        var ciphertext = netCoreEncryptor.Encrypt(plaintext, aad);
        var decrypted = netFxEncryptor.Decrypt(new ArraySegment<byte>(ciphertext), aad);

        Assert.Equal(plaintext.ToArray(), decrypted);
    }

    [Fact]
    public void CurrentNetFx_SelfTest_RoundTrips()
    {
        // Verifies the source-built #else path can roundtrip on its own.
        var netFxEncryptor = _factory.CreateSourceBuiltNetFxEncryptor();
        var plaintext = new ArraySegment<byte>("netfx-self-test"u8.ToArray());
        var aad = new ArraySegment<byte>("netfx-self-test-aad"u8.ToArray());

        var ciphertext = netFxEncryptor.Encrypt(plaintext, aad);
        var decrypted = netFxEncryptor.Decrypt(new ArraySegment<byte>(ciphertext), aad);

        Assert.Equal(plaintext.ToArray(), decrypted);
    }

    /// <summary>
    /// Creates a ManagedAuthenticatedEncryptor from current source code (net11, #if NET path).
    /// </summary>
    private static ManagedAuthenticatedEncryptor CreateCurrentEncryptor()
    {
        return new ManagedAuthenticatedEncryptor(
            new Secret(new byte[512 / 8]),
            symmetricAlgorithmFactory: Aes.Create,
            symmetricAlgorithmKeySizeInBytes: 256 / 8,
            validationAlgorithmFactory: () => new HMACSHA256());
    }
}
