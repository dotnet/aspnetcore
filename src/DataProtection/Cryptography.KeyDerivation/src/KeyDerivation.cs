// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Cryptography.KeyDerivation.PBKDF2;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Cryptography.KeyDerivation;

/// <summary>
/// Provides algorithms for performing key derivation.
/// </summary>
public static class KeyDerivation
{
    /// <summary>
    /// Performs key derivation using the PBKDF2 algorithm.
    /// </summary>
    /// <param name="password">The password from which to derive the key.</param>
    /// <param name="salt">The salt to be used during the key derivation process.</param>
    /// <param name="prf">The pseudo-random function to be used in the key derivation process.</param>
    /// <param name="iterationCount">The number of iterations of the pseudo-random function to apply
    /// during the key derivation process.</param>
    /// <param name="numBytesRequested">The desired length (in bytes) of the derived key.</param>
    /// <returns>The derived key.</returns>
    /// <remarks>
    /// The PBKDF2 algorithm is specified in RFC 2898.
    /// </remarks>
    public static byte[] Pbkdf2(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested)
    {
        ArgumentNullThrowHelper.ThrowIfNull(password);
        ArgumentNullThrowHelper.ThrowIfNull(salt);

        // parameter checking
        if (prf < KeyDerivationPrf.HMACSHA1 || prf > KeyDerivationPrf.HMACSHA512)
        {
            throw new ArgumentOutOfRangeException(nameof(prf));
        }
        ArgumentOutOfRangeThrowHelper.ThrowIfNegativeOrZero(iterationCount);
        ArgumentOutOfRangeThrowHelper.ThrowIfNegativeOrZero(numBytesRequested);

        return Pbkdf2Util.Pbkdf2Provider.DeriveKey(password, salt, prf, iterationCount, numBytesRequested);
    }
}
