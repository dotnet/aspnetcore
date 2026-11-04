// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography;

namespace Microsoft.AspNetCore.DataProtection.Managed;

internal static class HashAlgorithmExtensions
{
    public static int GetDigestSizeInBytes(this HashAlgorithm hashAlgorithm)
    {
        var hashSizeInBits = hashAlgorithm.HashSize;
        CryptoUtil.Assert(hashSizeInBits >= 0 && hashSizeInBits % 8 == 0, "hashSizeInBits >= 0 && hashSizeInBits % 8 == 0");
        return hashSizeInBits / 8;
    }

    public static HashAlgorithmType GetHashAlgorithmType(this KeyedHashAlgorithm type) => type switch
    {
        HMACSHA256 => HashAlgorithmType.HMACSHA256,
        HMACSHA512 => HashAlgorithmType.HMACSHA512,
        _ => HashAlgorithmType.Other,
    };
}
