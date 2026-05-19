// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Cryptography.Internal;

namespace Microsoft.AspNetCore.Cryptography.Cng;

// http://msdn.microsoft.com/en-us/library/windows/desktop/aa375525(v=vs.85).aspx
[StructLayout(LayoutKind.Sequential)]
internal struct BCRYPT_KEY_LENGTHS_STRUCT
{
    // MSDN says these fields represent the key length in bytes.
    // It's wrong: these key lengths are all actually in bits.
    internal uint dwMinLength;
    internal uint dwMaxLength;
    internal uint dwIncrement;

    public void EnsureValidKeyLength(uint keyLengthInBits)
    {
        if (!IsValidKeyLength(keyLengthInBits))
        {
            string message = Resources.FormatBCRYPT_KEY_LENGTHS_STRUCT_InvalidKeyLength(keyLengthInBits, dwMinLength, dwMaxLength, dwIncrement);
            throw new ArgumentOutOfRangeException(nameof(keyLengthInBits), message);
        }
        CryptoUtil.Assert(keyLengthInBits % 8 == 0, "keyLengthInBits % 8 == 0");
    }

    private bool IsValidKeyLength(uint keyLengthInBits)
    {
        // If the step size is zero, then the key length must be exactly the min or the max. Otherwise,
        // key length must be between min and max (inclusive) and a whole number of increments away from min.
        if (dwIncrement == 0)
        {
            return (keyLengthInBits == dwMinLength || keyLengthInBits == dwMaxLength);
        }
        else
        {
            return (dwMinLength <= keyLengthInBits)
                && (keyLengthInBits <= dwMaxLength)
                && ((keyLengthInBits - dwMinLength) % dwIncrement == 0);
        }
    }
}
