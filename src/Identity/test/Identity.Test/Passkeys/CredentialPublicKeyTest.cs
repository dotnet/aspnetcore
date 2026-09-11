// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Cbor;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.Identity.Test;

public class CredentialPublicKeyTest
{
    [Theory]
    [InlineData(2048)]
    [InlineData(3072)]
    public void Decode_AcceptsValidRsaKey(int keySize)
    {
        using var rsa = RSA.Create(keySize);
        var parameters = rsa.ExportParameters(false);
        var encodedKey = EncodeRsaKey(parameters.Modulus!, parameters.Exponent!);

        var key = CredentialPublicKey.Decode(encodedKey);

        Assert.Equal(encodedKey, key.AsMemory());
    }

    [Fact]
    public void Decode_RejectsRsaModulusBelowMinimumKeySize()
    {
        using var rsa = RSA.Create(1024);
        var parameters = rsa.ExportParameters(false);
        var encodedKey = EncodeRsaKey(parameters.Modulus!, parameters.Exponent!);

        var exception = Assert.Throws<PasskeyException>(() => CredentialPublicKey.Decode(encodedKey));

        Assert.IsType<CborContentException>(exception.InnerException);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Decode_RejectsRsaParameterWithLeadingZero(bool addLeadingZeroToModulus)
    {
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(false);
        var modulus = parameters.Modulus!;
        var exponent = parameters.Exponent!;

        if (addLeadingZeroToModulus)
        {
            modulus = [0, .. modulus];
        }
        else
        {
            exponent = [0, .. exponent];
        }

        var exception = Assert.Throws<PasskeyException>(
            () => CredentialPublicKey.Decode(EncodeRsaKey(modulus, exponent)));

        Assert.IsType<CborContentException>(exception.InnerException);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void Decode_RejectsInvalidRsaExponent(byte exponent)
    {
        using var rsa = RSA.Create(2048);
        var modulus = rsa.ExportParameters(false).Modulus!;

        var exception = Assert.Throws<PasskeyException>(
            () => CredentialPublicKey.Decode(EncodeRsaKey(modulus, [exponent])));

        Assert.IsType<CborContentException>(exception.InnerException);
    }

    [Fact]
    public void Decode_RejectsRsaExponentNotLessThanModulus()
    {
        using var rsa = RSA.Create(2048);
        var modulus = rsa.ExportParameters(false).Modulus!;

        var exception = Assert.Throws<PasskeyException>(
            () => CredentialPublicKey.Decode(EncodeRsaKey(modulus, modulus)));

        Assert.IsType<CborContentException>(exception.InnerException);
    }

    private static byte[] EncodeRsaKey(byte[] modulus, byte[] exponent)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4);
        writer.WriteInt32(1);
        writer.WriteInt32(3);
        writer.WriteInt32(3);
        writer.WriteInt32((int)COSEAlgorithmIdentifier.RS256);
        writer.WriteInt32(-1);
        writer.WriteByteString(modulus);
        writer.WriteInt32(-2);
        writer.WriteByteString(exponent);
        writer.WriteEndMap();

        return writer.Encode();
    }
}
