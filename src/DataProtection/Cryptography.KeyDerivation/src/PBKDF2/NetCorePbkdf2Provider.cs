// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Cryptography.KeyDerivation.PBKDF2;

/// <summary>
/// Implements Pbkdf2 using <see cref="Rfc2898DeriveBytes"/>.
/// </summary>
internal sealed class NetCorePbkdf2Provider : IPbkdf2Provider
{
    public byte[] DeriveKey(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested)
    {
        Debug.Assert(password != null);
        Debug.Assert(salt != null);
        Debug.Assert(iterationCount > 0);
        Debug.Assert(numBytesRequested > 0);

        HashAlgorithmName algorithmName;
        switch (prf)
        {
            case KeyDerivationPrf.HMACSHA1:
                algorithmName = HashAlgorithmName.SHA1;
                break;
            case KeyDerivationPrf.HMACSHA256:
                algorithmName = HashAlgorithmName.SHA256;
                break;
            case KeyDerivationPrf.HMACSHA512:
                algorithmName = HashAlgorithmName.SHA512;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(prf));
        }

        return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterationCount, algorithmName, numBytesRequested);
    }
}
#endif
