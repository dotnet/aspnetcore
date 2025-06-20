// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Cbor;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.Identity;

internal sealed class CredentialPublicKey
{
    private readonly COSEKeyType _type;
    private readonly COSEAlgorithmIdentifier _alg;
    private readonly ReadOnlyMemory<byte> _bytes;
    private readonly RSA? _rsa;
    private readonly ECDsa? _ecdsa;

    public COSEAlgorithmIdentifier Alg => _alg;

    private CredentialPublicKey(ReadOnlyMemory<byte> bytes)
    {
        var reader = Ctap2CborReader.Create(bytes);

        reader.ReadCoseKeyLabel((int)COSEKeyParameter.KeyType);
        _type = (COSEKeyType)reader.ReadInt32();
        _alg = ParseCoseKeyCommonParameters(reader);

        switch (_type)
        {
            case COSEKeyType.EC2:
            case COSEKeyType.OKP:
                _ecdsa = ParseECDsa(_type, reader);
                break;
            case COSEKeyType.RSA:
                _rsa = ParseRSA(reader);
                break;
            default:
                throw new InvalidOperationException($"Unsupported key type '{_type}'.");
        }

        var keyLength = bytes.Length - reader.BytesRemaining;
        _bytes = bytes[..keyLength];
    }

    public static CredentialPublicKey Decode(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            return new CredentialPublicKey(bytes);
        }
        catch (PasskeyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw PasskeyException.InvalidCredentialPublicKey(ex);
        }
    }

    public static CredentialPublicKey Decode(ReadOnlyMemory<byte> bytes, out int bytesRead)
    {
        var key = Decode(bytes);
        bytesRead = key._bytes.Length;
        return key;
    }

    public bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
    {
        return _type switch
        {
            COSEKeyType.EC2 => _ecdsa!.VerifyData(data, signature, HashAlgFromCOSEAlg(_alg), DSASignatureFormat.Rfc3279DerSequence),
            COSEKeyType.RSA => _rsa!.VerifyData(data, signature, HashAlgFromCOSEAlg(_alg), GetRSASignaturePadding()),
            _ => throw new InvalidOperationException($"Missing or unknown kty {_type}"),
        };
    }

    private static COSEAlgorithmIdentifier ParseCoseKeyCommonParameters(Ctap2CborReader reader)
    {
        reader.ReadCoseKeyLabel((int)COSEKeyParameter.Alg);
        var alg = (COSEAlgorithmIdentifier)reader.ReadInt32();

        if (reader.TryReadCoseKeyLabel((int)COSEKeyParameter.KeyOps))
        {
            // No-op, simply tolerate potential key_ops labels
            reader.SkipValue();
        }

        return alg;
    }

    private static RSA ParseRSA(Ctap2CborReader reader)
    {
        var rsaParams = new RSAParameters();

        reader.ReadCoseKeyLabel((int)COSEKeyParameter.N);
        rsaParams.Modulus = reader.ReadByteString();

        if (!reader.TryReadCoseKeyLabel((int)COSEKeyParameter.E))
        {
            throw new CborContentException("The COSE key encodes a private key.");
        }
        rsaParams.Exponent = reader.ReadByteString();

        reader.ReadEndMap();

        return RSA.Create(rsaParams);
    }

    private static ECDsa ParseECDsa(COSEKeyType kty, Ctap2CborReader reader)
    {
        var ecParams = new ECParameters();

        reader.ReadCoseKeyLabel((int)COSEKeyParameter.Crv);
        var crv = (COSEEllipticCurve)reader.ReadInt32();

        if (IsValidKtyCrvCombination(kty, crv))
        {
            ecParams.Curve = MapCoseCrvToECCurve(crv);
        }

        reader.ReadCoseKeyLabel((int)COSEKeyParameter.X);
        ecParams.Q.X = reader.ReadByteString();

        reader.ReadCoseKeyLabel((int)COSEKeyParameter.Y);
        ecParams.Q.Y = reader.ReadByteString();

        if (reader.TryReadCoseKeyLabel((int)COSEKeyParameter.D))
        {
            throw new CborContentException("The COSE key encodes a private key.");
        }

        reader.ReadEndMap();

        return ECDsa.Create(ecParams);

        static ECCurve MapCoseCrvToECCurve(COSEEllipticCurve crv)
        {
            return crv switch
            {
                COSEEllipticCurve.P256 => ECCurve.NamedCurves.nistP256,
                COSEEllipticCurve.P384 => ECCurve.NamedCurves.nistP384,
                COSEEllipticCurve.P521 => ECCurve.NamedCurves.nistP521,
                COSEEllipticCurve.X25519 or
                COSEEllipticCurve.X448 or
                COSEEllipticCurve.Ed25519 or
                COSEEllipticCurve.Ed448 => throw new NotSupportedException("OKP type curves not supported."),
                _ => throw new CborContentException($"Unrecognized COSE crv value {crv}"),
            };
        }

        static bool IsValidKtyCrvCombination(COSEKeyType kty, COSEEllipticCurve crv)
        {
            return (kty, crv) switch
            {
                (COSEKeyType.EC2, COSEEllipticCurve.P256 or COSEEllipticCurve.P384 or COSEEllipticCurve.P521) => true,
                (COSEKeyType.OKP, COSEEllipticCurve.X25519 or COSEEllipticCurve.X448 or COSEEllipticCurve.Ed25519 or COSEEllipticCurve.Ed448) => true,
                _ => false,
            };
        }
    }

    private RSASignaturePadding GetRSASignaturePadding()
    {
        if (_type != COSEKeyType.RSA)
        {
            throw new InvalidOperationException($"Cannot get RSA signature padding for key type {_type}.");
        }

        // https://www.iana.org/assignments/cose/cose.xhtml#algorithms
        return _alg switch
        {
            COSEAlgorithmIdentifier.PS256 or
            COSEAlgorithmIdentifier.PS384 or
            COSEAlgorithmIdentifier.PS512
            => RSASignaturePadding.Pss,

            COSEAlgorithmIdentifier.RS1 or
            COSEAlgorithmIdentifier.RS256 or
            COSEAlgorithmIdentifier.RS384 or
            COSEAlgorithmIdentifier.RS512
            => RSASignaturePadding.Pkcs1,

            _ => throw new InvalidOperationException($"Missing or unknown alg {_alg}"),
        };
    }

    private static HashAlgorithmName HashAlgFromCOSEAlg(COSEAlgorithmIdentifier alg)
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
            (COSEAlgorithmIdentifier)4 => HashAlgorithmName.SHA1,
            (COSEAlgorithmIdentifier)11 => HashAlgorithmName.SHA256,
            (COSEAlgorithmIdentifier)12 => HashAlgorithmName.SHA384,
            (COSEAlgorithmIdentifier)13 => HashAlgorithmName.SHA512,
            COSEAlgorithmIdentifier.EdDSA => HashAlgorithmName.SHA512,
            _ => throw new InvalidOperationException("Invalid COSE algorithm value."),
        };
    }

    public ReadOnlyMemory<byte> AsMemory() => _bytes;

    public byte[] ToArray() => _bytes.ToArray();

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
