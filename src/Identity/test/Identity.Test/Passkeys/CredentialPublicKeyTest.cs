// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Formats.Cbor;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.Identity.Test;

public class CredentialPublicKeyTest
{
    // COSE algorithm identifiers.
    // See https://www.iana.org/assignments/cose/cose.xhtml#algorithms
    private const int AlgES256 = -7;
    private const int AlgES384 = -35;
    private const int AlgES512 = -36;
    private const int AlgES256K = -47;

    // COSE elliptic curve identifiers.
    // See https://www.iana.org/assignments/cose/cose.xhtml#elliptic-curves
    private const int CrvP256 = 1;
    private const int CrvP384 = 2;
    private const int CrvP521 = 3;
    private const int CrvP256K = 8;

    [Theory]
    [InlineData(AlgES256, CrvP256)]
    [InlineData(AlgES384, CrvP384)]
    [InlineData(AlgES512, CrvP521)]
    public void Decode_Succeeds_WhenAlgAndCrvMatch(int algId, int crv)
    {
        // Arrange
        var bytes = EncodeEcPublicKeyCbor(algId, crv);

        // Act
        var credentialPublicKey = CredentialPublicKey.Decode(bytes);

        // Assert
        Assert.Equal((COSEAlgorithmIdentifier)algId, credentialPublicKey.Alg);
    }

    [Theory]
    // ES256 (P-256) paired with a curve belonging to a different key size.
    [InlineData(AlgES256, CrvP384)]
    [InlineData(AlgES256, CrvP521)]
    // ES384 (P-384) paired with a mismatched curve.
    [InlineData(AlgES384, CrvP256)]
    [InlineData(AlgES384, CrvP521)]
    // ES512 (P-521) paired with a mismatched curve.
    [InlineData(AlgES512, CrvP256)]
    [InlineData(AlgES512, CrvP384)]
    public void Decode_Throws_WhenAlgAndCrvAreMismatched(int algId, int crv)
    {
        // Arrange
        var bytes = EncodeEcPublicKeyCbor(algId, crv);

        // Act & Assert
        var exception = Assert.Throws<PasskeyException>(() => CredentialPublicKey.Decode(bytes));

        Assert.IsType<CborContentException>(exception.InnerException);
        Assert.Contains("algorithm", exception.InnerException!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Decode_Throws_ForES256K()
    {
        // Arrange
        // ES256K (secp256k1 / "P256K") is defined in the COSEAlgorithmIdentifier
        // and COSEEllipticCurve enums but isn't in IsSupportedAlgorithm, and
        // P256K isn't one of the curves IsValidKtyCrvCombination accepts for
        // kty=EC2. This is rejected by the existing kty+crv check, before the
        // new alg+crv check ever runs; the underlying key material below is
        // generated on P-256 purely so the CBOR writer has valid X/Y
        // coordinates, since P256K isn't a curve .NET's ECDsa can create.
        var bytes = EncodeEcPublicKeyCbor(AlgES256K, CrvP256K, keyMaterialCrv: CrvP256);

        // Act & Assert
        var exception = Assert.Throws<PasskeyException>(() => CredentialPublicKey.Decode(bytes));

        Assert.IsType<CborContentException>(exception.InnerException);
        Assert.Contains("key type", exception.InnerException!.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Encodes a CTAP2-canonical COSE EC2 public key with the given (possibly
    /// invalid) alg/crv combination. The same helper backs both the valid and
    /// mismatched theories so both exercise the identical decode path; only
    /// the alg/crv values written into the CBOR differ.
    /// </summary>
    /// <param name="keyMaterialCrv">
    /// The curve to actually generate EC key material on. Defaults to
    /// <paramref name="crv"/>. Only needs to differ from <paramref name="crv"/>
    /// when <paramref name="crv"/> isn't a curve .NET's <see cref="ECDsa"/>
    /// can create (e.g. P256K), and the test only cares about the encoded
    /// crv label being rejected, not about real key material for that curve.
    /// </param>
    private static byte[] EncodeEcPublicKeyCbor(int algId, int crv, int? keyMaterialCrv = null)
    {
        using var ecdsa = ECDsa.Create(MapCrvToECCurve(keyMaterialCrv ?? crv));
        var parameters = ecdsa.ExportParameters(includePrivateParameters: false);

        // COSE key map labels.
        // See https://www.iana.org/assignments/cose/cose.xhtml#key-common-parameters
        const int LabelKeyType = 1;
        const int LabelAlg = 3;
        const int LabelCrv = -1;
        const int LabelX = -2;
        const int LabelY = -3;
        const int KeyTypeEC2 = 2;

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(5); // kty, alg, crv, x, y

        writer.WriteInt32(LabelKeyType);
        writer.WriteInt32(KeyTypeEC2);

        writer.WriteInt32(LabelAlg);
        writer.WriteInt32(algId);

        writer.WriteInt32(LabelCrv);
        writer.WriteInt32(crv);

        writer.WriteInt32(LabelX);
        writer.WriteByteString(parameters.Q.X!);

        writer.WriteInt32(LabelY);
        writer.WriteByteString(parameters.Q.Y!);

        writer.WriteEndMap();
        return writer.Encode();
    }

    private static ECCurve MapCrvToECCurve(int crv) => crv switch
    {
        CrvP256 => ECCurve.NamedCurves.nistP256,
        CrvP384 => ECCurve.NamedCurves.nistP384,
        CrvP521 => ECCurve.NamedCurves.nistP521,
        _ => throw new NotSupportedException($"Curve {crv} not supported in this test helper."),
    };
}
