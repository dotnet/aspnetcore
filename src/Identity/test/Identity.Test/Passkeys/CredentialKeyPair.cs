// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Formats.Cbor;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.Identity.Test;

internal sealed class CredentialKeyPair
{
    private readonly RSA? _rsa;
    private readonly ECDsa? _ecdsa;
    private readonly COSEAlgorithmIdentifier _alg;
    private readonly COSEKeyType _keyType;
    private readonly COSEEllipticCurve _curve;

    private CredentialKeyPair(RSA rsa, COSEAlgorithmIdentifier alg)
    {
        _rsa = rsa;
        _alg = alg;
        _keyType = COSEKeyType.RSA;
    }

    private CredentialKeyPair(ECDsa ecdsa, COSEAlgorithmIdentifier alg, COSEEllipticCurve curve)
    {
        _ecdsa = ecdsa;
        _alg = alg;
        _keyType = COSEKeyType.EC2;
        _curve = curve;
    }

    public static CredentialKeyPair Generate(COSEAlgorithmIdentifier alg)
    {
        return alg switch
        {
            COSEAlgorithmIdentifier.RS1 or
            COSEAlgorithmIdentifier.RS256 or
            COSEAlgorithmIdentifier.RS384 or
            COSEAlgorithmIdentifier.RS512 or
            COSEAlgorithmIdentifier.PS256 or
            COSEAlgorithmIdentifier.PS384 or
            COSEAlgorithmIdentifier.PS512 => GenerateRsaKeyPair(alg),

            COSEAlgorithmIdentifier.ES256 => GenerateEcKeyPair(alg, ECCurve.NamedCurves.nistP256, COSEEllipticCurve.P256),
            COSEAlgorithmIdentifier.ES384 => GenerateEcKeyPair(alg, ECCurve.NamedCurves.nistP384, COSEEllipticCurve.P384),
            COSEAlgorithmIdentifier.ES512 => GenerateEcKeyPair(alg, ECCurve.NamedCurves.nistP521, COSEEllipticCurve.P521),
            COSEAlgorithmIdentifier.ES256K => GenerateEcKeyPair(alg, ECCurve.CreateFromFriendlyName("secP256k1"), COSEEllipticCurve.P256K),

            _ => throw new NotSupportedException($"Algorithm {alg} is not supported for key pair generation")
        };
    }

    public ReadOnlyMemory<byte> SignData(ReadOnlySpan<byte> data)
    {
        return _keyType switch
        {
            COSEKeyType.RSA => SignRsaData(data),
            COSEKeyType.EC2 => SignEcData(data),
            _ => throw new InvalidOperationException($"Unsupported key type {_keyType}")
        };
    }

    private byte[] SignRsaData(ReadOnlySpan<byte> data)
    {
        if (_rsa is null)
        {
            throw new InvalidOperationException("RSA key is not available for signing");
        }

        var hashAlgorithm = GetHashAlgorithmFromCoseAlg(_alg);
        var padding = GetRsaPaddingFromCoseAlg(_alg);

        return _rsa.SignData(data.ToArray(), hashAlgorithm, padding);
    }

    private byte[] SignEcData(ReadOnlySpan<byte> data)
    {
        if (_ecdsa is null)
        {
            throw new InvalidOperationException("ECDSA key is not available for signing");
        }

        var hashAlgorithm = GetHashAlgorithmFromCoseAlg(_alg);
        return _ecdsa.SignData(data.ToArray(), hashAlgorithm, DSASignatureFormat.Rfc3279DerSequence);
    }

    private static CredentialKeyPair GenerateRsaKeyPair(COSEAlgorithmIdentifier alg)
    {
        const int KeySize = 2048;
        var rsa = RSA.Create(KeySize);
        return new CredentialKeyPair(rsa, alg);
    }

    private static CredentialKeyPair GenerateEcKeyPair(COSEAlgorithmIdentifier alg, ECCurve curve, COSEEllipticCurve coseCurve)
    {
        var ecdsa = ECDsa.Create(curve);
        return new CredentialKeyPair(ecdsa, alg, coseCurve);
    }

    public ReadOnlyMemory<byte> EncodePublicKeyCbor()
        => _keyType switch
        {
            COSEKeyType.RSA => EncodeCoseRsaPublicKey(_rsa!, _alg),
            COSEKeyType.EC2 => EncodeCoseEcPublicKey(_ecdsa!, _alg, _curve),
            _ => throw new InvalidOperationException($"Unsupported key type {_keyType}")
        };

    private static byte[] EncodeCoseRsaPublicKey(RSA rsa, COSEAlgorithmIdentifier alg)
    {
        var parameters = rsa.ExportParameters(false);

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4); // kty, alg, n, e

        writer.WriteInt32((int)COSEKeyParameter.KeyType);
        writer.WriteInt32((int)COSEKeyType.RSA);

        writer.WriteInt32((int)COSEKeyParameter.Alg);
        writer.WriteInt32((int)alg);

        writer.WriteInt32((int)COSEKeyParameter.N);
        writer.WriteByteString(parameters.Modulus!);

        writer.WriteInt32((int)COSEKeyParameter.E);
        writer.WriteByteString(parameters.Exponent!);

        writer.WriteEndMap();
        return writer.Encode();
    }

    private static byte[] EncodeCoseEcPublicKey(ECDsa ecdsa, COSEAlgorithmIdentifier alg, COSEEllipticCurve curve)
    {
        var parameters = ecdsa.ExportParameters(false);

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(5); // kty, alg, crv, x, y

        writer.WriteInt32((int)COSEKeyParameter.KeyType);
        writer.WriteInt32((int)COSEKeyType.EC2);

        writer.WriteInt32((int)COSEKeyParameter.Alg);
        writer.WriteInt32((int)alg);

        writer.WriteInt32((int)COSEKeyParameter.Crv);
        writer.WriteInt32((int)curve);

        writer.WriteInt32((int)COSEKeyParameter.X);
        writer.WriteByteString(parameters.Q.X!);

        writer.WriteInt32((int)COSEKeyParameter.Y);
        writer.WriteByteString(parameters.Q.Y!);

        writer.WriteEndMap();
        return writer.Encode();
    }

    private static HashAlgorithmName GetHashAlgorithmFromCoseAlg(COSEAlgorithmIdentifier alg)
    {
        return alg switch
        {
            COSEAlgorithmIdentifier.RS1 => HashAlgorithmName.SHA1,
            COSEAlgorithmIdentifier.ES256 => HashAlgorithmName.SHA256,
            COSEAlgorithmIdentifier.ES384 => HashAlgorithmName.SHA384,
            COSEAlgorithmIdentifier.ES512 => HashAlgorithmName.SHA512,
            COSEAlgorithmIdentifier.PS256 => HashAlgorithmName.SHA256,
            COSEAlgorithmIdentifier.PS384 => HashAlgorithmName.SHA384,
            COSEAlgorithmIdentifier.PS512 => HashAlgorithmName.SHA512,
            COSEAlgorithmIdentifier.RS256 => HashAlgorithmName.SHA256,
            COSEAlgorithmIdentifier.RS384 => HashAlgorithmName.SHA384,
            COSEAlgorithmIdentifier.RS512 => HashAlgorithmName.SHA512,
            COSEAlgorithmIdentifier.ES256K => HashAlgorithmName.SHA256,
            _ => throw new InvalidOperationException($"Unsupported algorithm: {alg}")
        };
    }

    private static RSASignaturePadding GetRsaPaddingFromCoseAlg(COSEAlgorithmIdentifier alg)
    {
        return alg switch
        {
            COSEAlgorithmIdentifier.PS256 or
            COSEAlgorithmIdentifier.PS384 or
            COSEAlgorithmIdentifier.PS512 => RSASignaturePadding.Pss,

            COSEAlgorithmIdentifier.RS1 or
            COSEAlgorithmIdentifier.RS256 or
            COSEAlgorithmIdentifier.RS384 or
            COSEAlgorithmIdentifier.RS512 => RSASignaturePadding.Pkcs1,

            _ => throw new InvalidOperationException($"Unsupported RSA algorithm: {alg}")
        };
    }

    private enum COSEKeyType
    {
        OKP = 1,
        EC2 = 2,
        RSA = 3,
        Symmetric = 4
    }

    private enum COSEKeyParameter
    {
        Crv = -1,
        K = -1,
        X = -2,
        Y = -3,
        D = -4,
        N = -1,
        E = -2,
        KeyType = 1,
        KeyId = 2,
        Alg = 3,
        KeyOps = 4,
        BaseIV = 5
    }

    private enum COSEEllipticCurve
    {
        Reserved = 0,
        P256 = 1,
        P384 = 2,
        P521 = 3,
        X25519 = 4,
        X448 = 5,
        Ed25519 = 6,
        Ed448 = 7,
        P256K = 8,
    }
}
