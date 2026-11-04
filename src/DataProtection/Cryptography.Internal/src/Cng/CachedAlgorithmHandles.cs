// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Cryptography.SafeHandles;

namespace Microsoft.AspNetCore.Cryptography.Cng;

/// <summary>
/// Provides cached CNG algorithm provider instances, as calling BCryptOpenAlgorithmProvider is expensive.
/// Callers should use caution never to dispose of the algorithm provider instances returned by this type.
/// </summary>
internal static class CachedAlgorithmHandles
{
    private static CachedAlgorithmInfo _aesCbc = new CachedAlgorithmInfo(() => GetAesAlgorithm(chainingMode: Constants.BCRYPT_CHAIN_MODE_CBC));
    private static CachedAlgorithmInfo _aesGcm = new CachedAlgorithmInfo(() => GetAesAlgorithm(chainingMode: Constants.BCRYPT_CHAIN_MODE_GCM));
    private static CachedAlgorithmInfo _hmacSha1 = new CachedAlgorithmInfo(() => GetHmacAlgorithm(algorithm: Constants.BCRYPT_SHA1_ALGORITHM));
    private static CachedAlgorithmInfo _hmacSha256 = new CachedAlgorithmInfo(() => GetHmacAlgorithm(algorithm: Constants.BCRYPT_SHA256_ALGORITHM));
    private static CachedAlgorithmInfo _hmacSha512 = new CachedAlgorithmInfo(() => GetHmacAlgorithm(algorithm: Constants.BCRYPT_SHA512_ALGORITHM));
    private static CachedAlgorithmInfo _pbkdf2 = new CachedAlgorithmInfo(GetPbkdf2Algorithm);
    private static CachedAlgorithmInfo _sha1 = new CachedAlgorithmInfo(() => GetHashAlgorithm(algorithm: Constants.BCRYPT_SHA1_ALGORITHM));
    private static CachedAlgorithmInfo _sha256 = new CachedAlgorithmInfo(() => GetHashAlgorithm(algorithm: Constants.BCRYPT_SHA256_ALGORITHM));
    private static CachedAlgorithmInfo _sha512 = new CachedAlgorithmInfo(() => GetHashAlgorithm(algorithm: Constants.BCRYPT_SHA512_ALGORITHM));
    private static CachedAlgorithmInfo _sp800_108_ctr_hmac = new CachedAlgorithmInfo(GetSP800_108_CTR_HMACAlgorithm);

    public static BCryptAlgorithmHandle AES_CBC => CachedAlgorithmInfo.GetAlgorithmHandle(ref _aesCbc);

    public static BCryptAlgorithmHandle AES_GCM => CachedAlgorithmInfo.GetAlgorithmHandle(ref _aesGcm);

    public static BCryptAlgorithmHandle HMAC_SHA1 => CachedAlgorithmInfo.GetAlgorithmHandle(ref _hmacSha1);

    public static BCryptAlgorithmHandle HMAC_SHA256 => CachedAlgorithmInfo.GetAlgorithmHandle(ref _hmacSha256);

    public static BCryptAlgorithmHandle HMAC_SHA512 => CachedAlgorithmInfo.GetAlgorithmHandle(ref _hmacSha512);

    // Only available on Win8+.
    public static BCryptAlgorithmHandle PBKDF2 => CachedAlgorithmInfo.GetAlgorithmHandle(ref _pbkdf2);

    public static BCryptAlgorithmHandle SHA1 => CachedAlgorithmInfo.GetAlgorithmHandle(ref _sha1);

    public static BCryptAlgorithmHandle SHA256 => CachedAlgorithmInfo.GetAlgorithmHandle(ref _sha256);

    public static BCryptAlgorithmHandle SHA512 => CachedAlgorithmInfo.GetAlgorithmHandle(ref _sha512);

    // Only available on Win8+.
    public static BCryptAlgorithmHandle SP800_108_CTR_HMAC => CachedAlgorithmInfo.GetAlgorithmHandle(ref _sp800_108_ctr_hmac);

    private static BCryptAlgorithmHandle GetAesAlgorithm(string chainingMode)
    {
        var algHandle = BCryptAlgorithmHandle.OpenAlgorithmHandle(Constants.BCRYPT_AES_ALGORITHM);
        algHandle.SetChainingMode(chainingMode);
        return algHandle;
    }

    private static BCryptAlgorithmHandle GetHashAlgorithm(string algorithm)
    {
        return BCryptAlgorithmHandle.OpenAlgorithmHandle(algorithm, hmac: false);
    }

    private static BCryptAlgorithmHandle GetHmacAlgorithm(string algorithm)
    {
        return BCryptAlgorithmHandle.OpenAlgorithmHandle(algorithm, hmac: true);
    }

    private static BCryptAlgorithmHandle GetPbkdf2Algorithm()
    {
        return BCryptAlgorithmHandle.OpenAlgorithmHandle(Constants.BCRYPT_PBKDF2_ALGORITHM, implementation: Constants.MS_PRIMITIVE_PROVIDER);
    }

    private static BCryptAlgorithmHandle GetSP800_108_CTR_HMACAlgorithm()
    {
        return BCryptAlgorithmHandle.OpenAlgorithmHandle(Constants.BCRYPT_SP800108_CTR_HMAC_ALGORITHM, implementation: Constants.MS_PRIMITIVE_PROVIDER);
    }

    // Warning: mutable struct!
    private struct CachedAlgorithmInfo
    {
        private WeakReference<BCryptAlgorithmHandle>? _algorithmHandle;
        private readonly Func<BCryptAlgorithmHandle> _factory;

        public CachedAlgorithmInfo(Func<BCryptAlgorithmHandle> factory)
        {
            _algorithmHandle = null;
            _factory = factory;
        }

        public static BCryptAlgorithmHandle GetAlgorithmHandle(ref CachedAlgorithmInfo cachedAlgorithmInfo)
        {
            return WeakReferenceHelpers.GetSharedInstance(ref cachedAlgorithmInfo._algorithmHandle, cachedAlgorithmInfo._factory);
        }
    }
}
