// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

internal sealed class CredentialPublicKey
{
    private readonly CoseKeyType _type;
    private readonly COSEAlgorithmIdentifier _alg;
    private readonly ReadOnlyMemory<byte> _bytes;
    private readonly RSA? _rsa;

#if NETCOREAPP
    private readonly ECDsa? _ecdsa;
#endif

    public COSEAlgorithmIdentifier Alg => _alg;

    public CredentialPublicKey(ReadOnlyMemory<byte> bytes)
    {
        var reader = Ctap2CborReader.Create(bytes);

        reader.ReadCoseKeyLabel((int)CoseKeyParameter.KeyType);
        _type = (CoseKeyType)reader.ReadInt32();
        _alg = ParseCoseKeyCommonParameters(reader);

        switch (_type)
        {
#if NETCOREAPP
            case CoseKeyType.EC2:
            case CoseKeyType.OKP:
                _ecdsa = ParseECDsa(_type, reader);
                break;
#endif
            case CoseKeyType.RSA:
                _rsa = ParseRSA(reader);
                break;
            default:
                throw new InvalidOperationException($"Unsupported key type '{_type}'.");
        }

        var keyLength = bytes.Length - reader.BytesRemaining;
        _bytes = bytes.Slice(0, keyLength);
    }

    public bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
    {
        switch (_type)
        {
#if NETCOREAPP
            case CoseKeyType.EC2:
                return _ecdsa!.VerifyData(data, signature, HashAlgFromCOSEAlg(_alg), DSASignatureFormat.Rfc3279DerSequence);
#endif

            case CoseKeyType.RSA:
#if NETCOREAPP
                return _rsa!.VerifyData(data, signature, HashAlgFromCOSEAlg(_alg), Padding);
#else
                return _rsa!.VerifyData(data.ToArray(), signature.ToArray(), HashAlgFromCOSEAlg(_alg), Padding);
#endif
        }
        throw new InvalidOperationException($"Missing or unknown kty {_type}");
    }

    private static COSEAlgorithmIdentifier ParseCoseKeyCommonParameters(Ctap2CborReader reader)
    {
        reader.ReadCoseKeyLabel((int)CoseKeyParameter.Alg);
        var alg = (COSEAlgorithmIdentifier)reader.ReadInt32();

        if (reader.TryReadCoseKeyLabel((int)CoseKeyParameter.KeyOps))
        {
            // No-op, simply tolerate potential key_ops labels
            reader.SkipValue();
        }

        return alg;
    }

    private static RSA ParseRSA(Ctap2CborReader reader)
    {
        var rsaParams = new RSAParameters();

        reader.ReadCoseKeyLabel((int)CoseKeyParameter.N);
        rsaParams.Modulus = reader.ReadByteString();

        if (!reader.TryReadCoseKeyLabel((int)CoseKeyParameter.E))
        {
            throw new CborContentException("The COSE key encodes a private key.");
        }
        rsaParams.Exponent = reader.ReadByteString();

        reader.ReadEndMap();

#if NETCOREAPP
        return RSA.Create(rsaParams);
#else
        var rsa = RSA.Create();
        rsa.ImportParameters(rsaParams);
        return rsa;
#endif
    }

#if NETCOREAPP
    private static ECDsa ParseECDsa(CoseKeyType kty, Ctap2CborReader reader)
    {
        var ecParams = new ECParameters();

        reader.ReadCoseKeyLabel((int)CoseKeyParameter.Crv);
        var crv = (CoseEllipticCurve)reader.ReadInt32();

        if (IsValidKtyCrvCombination(kty, crv))
        {
            ecParams.Curve = MapCoseCrvToECCurve(crv);
        }

        reader.ReadCoseKeyLabel((int)CoseKeyParameter.X);
        ecParams.Q.X = reader.ReadByteString();

        reader.ReadCoseKeyLabel((int)CoseKeyParameter.Y);
        ecParams.Q.Y = reader.ReadByteString();

        if (reader.TryReadCoseKeyLabel((int)CoseKeyParameter.D))
        {
            throw new CborContentException("The COSE key encodes a private key.");
        }

        reader.ReadEndMap();

        return ECDsa.Create(ecParams);

        static ECCurve MapCoseCrvToECCurve(CoseEllipticCurve crv)
        {
            return crv switch
            {
                CoseEllipticCurve.P256 => ECCurve.NamedCurves.nistP256,
                CoseEllipticCurve.P384 => ECCurve.NamedCurves.nistP384,
                CoseEllipticCurve.P521 => ECCurve.NamedCurves.nistP521,
                CoseEllipticCurve.X25519 or
                CoseEllipticCurve.X448 or
                CoseEllipticCurve.Ed25519 or
                CoseEllipticCurve.Ed448 => throw new NotSupportedException("OKP type curves not supported."),
                _ => throw new CborContentException($"Unrecognized COSE crv value {crv}"),
            };
        }

        static bool IsValidKtyCrvCombination(CoseKeyType kty, CoseEllipticCurve crv)
        {
            return (kty, crv) switch
            {
                (CoseKeyType.EC2, CoseEllipticCurve.P256 or CoseEllipticCurve.P384 or CoseEllipticCurve.P521) => true,
                (CoseKeyType.OKP, CoseEllipticCurve.X25519 or CoseEllipticCurve.X448 or CoseEllipticCurve.Ed25519 or CoseEllipticCurve.Ed448) => true,
                _ => false,
            };
        }
    }
#endif

    internal RSASignaturePadding Padding
    {
        get
        {
            if (_type != CoseKeyType.RSA)
            {
                throw new InvalidOperationException($"Must be a RSA key. Was {_type}");
            }

            switch (_alg) // https://www.iana.org/assignments/cose/cose.xhtml#algorithms
            {
                case COSEAlgorithmIdentifier.PS256:
                case COSEAlgorithmIdentifier.PS384:
                case COSEAlgorithmIdentifier.PS512:
                    return RSASignaturePadding.Pss;

                case COSEAlgorithmIdentifier.RS1:
                case COSEAlgorithmIdentifier.RS256:
                case COSEAlgorithmIdentifier.RS384:
                case COSEAlgorithmIdentifier.RS512:
                    return RSASignaturePadding.Pkcs1;
                default:
                    throw new InvalidOperationException($"Missing or unknown alg {_alg}");
            }
        }
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

    public static CredentialPublicKey Decode(ReadOnlyMemory<byte> cpk, out int bytesRead)
    {
        var key = new CredentialPublicKey(cpk);
        bytesRead = key._bytes.Length;
        return key;
    }

    public ReadOnlyMemory<byte> AsMemory() => _bytes;

    public byte[] ToArray() => _bytes.ToArray();

    private enum CoseKeyType
    {
        OKP = 1,
        EC2 = 2,
        RSA = 3,
        Symmetric = 4
    }

    private enum CoseKeyParameter
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

    private enum CoseEllipticCurve
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
