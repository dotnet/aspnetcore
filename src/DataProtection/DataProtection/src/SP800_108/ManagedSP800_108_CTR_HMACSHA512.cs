// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.DataProtection.Managed;

namespace Microsoft.AspNetCore.DataProtection.SP800_108;

internal static class ManagedSP800_108_CTR_HMACSHA512
{
#if !NET10_0_OR_GREATER
    public static void DeriveKeys(
        byte[] kdk,
        ReadOnlySpan<byte> label,
        ReadOnlySpan<byte> contextHeader,
        ReadOnlySpan<byte> contextData,
        Span<byte> operationSubkey,
        Span<byte> validationSubkey)
    {
        // netFX and netStandard dont have API to NOT use HashAlgorithm
        using HashAlgorithm prf = new HMACSHA512(kdk);

        // kdk is passed just to have a shared implementation for different framework versions
        DeriveKeys(kdk, label, contextHeader, contextData, operationSubkey, validationSubkey, prf);
    }
#endif

#if NET10_0_OR_GREATER
    /// <summary>
    /// For FIPS enabled scenarios we have no other choice but to enforce a minimum key length of 14 bytes.
    /// It should not be on the hot-path, but only needed for one-time derivation of keys when e.g. creating context header.
    /// </summary>
    /// <remarks>
    /// NIST SP 800-131A Rev. 2:
    /// Keys less than 112 bits in length are disallowed for HMAC generation.
    /// The use of key lengths ≥ 112 bits is acceptable for HMAC generation.
    /// <see href="https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-131Ar2.pdf"/>
    /// </remarks>
    private const int FipsMinimumKeyLengthInBytes = 14;

    public static void DeriveKeys(ReadOnlySpan<byte> kdk, ReadOnlySpan<byte> label, ReadOnlySpan<byte> contextHeader, ReadOnlySpan<byte> contextData, Span<byte> operationSubkey, Span<byte> validationSubkey)
    {
        if (kdk.Length < FipsMinimumKeyLengthInBytes)
        {
            // When you provide a key to HMAC that is shorter than the algorithm's internal block size,
            // the HMAC algorithm internally pads the key with zeros to reach the block size.
            Span<byte> zeroExtended = stackalloc byte[FipsMinimumKeyLengthInBytes];
            zeroExtended.Clear();
            kdk.CopyTo(zeroExtended);

            DeriveKeys(zeroExtended, label, contextHeader, contextData, operationSubkey, validationSubkey, prf: null);
        }
        else
        {
            DeriveKeys(kdk, label, contextHeader, contextData, operationSubkey, validationSubkey, prf: null);
        }
    }
#endif

    /// <remarks>
    /// note: kdk will be used only if prf is null and only in later framework versions (10+)
    /// where static method on `HMACSHA512` exists which avoids allocations
    /// </remarks>
    private static void DeriveKeys(
        ReadOnlySpan<byte> kdk,
        ReadOnlySpan<byte> label,
        ReadOnlySpan<byte> contextHeader,
        ReadOnlySpan<byte> contextData,
        Span<byte> operationSubkey,
        Span<byte> validationSubkey,
        HashAlgorithm? prf = null)
    {
        var operationSubKeyIndex = 0;
        var validationSubKeyIndex = 0;
        var outputCount = operationSubkey.Length + validationSubkey.Length;

        int prfOutputSizeInBytes =
#if NET10_0_OR_GREATER
            HMACSHA512.HashSizeInBytes;
#else
            prf.GetDigestSizeInBytes();
#endif

#if NET10_0_OR_GREATER
        Span<byte> prfOutput = prfOutputSizeInBytes <= 128
            ? stackalloc byte[128].Slice(0, prfOutputSizeInBytes)
            : new byte[prfOutputSizeInBytes];
#endif

        // See SP800-108, Sec. 5.1 for the format of the input to the PRF routine.
        var prfInputLength = checked(sizeof(uint) /* [i]_2 */ + label.Length + 1 /* 0x00 */ + (contextHeader.Length + contextData.Length) + sizeof(uint) /* [K]_2 */);

#if NET10_0_OR_GREATER
        byte[]? prfInputArray = null;
        Span<byte> prfInput = prfInputLength <= 128
            ? stackalloc byte[128].Slice(0, prfInputLength)
            : (prfInputArray = new byte[prfInputLength]);
#else
        var prfInputArray = new byte[prfInputLength];
        var prfInput = prfInputArray.AsSpan();
#endif

        try
        {
            // Copy [L]_2 to prfInput since it's stable over all iterations
            uint outputSizeInBits = (uint)checked((int)outputCount * 8);
            prfInput[prfInput.Length - 4] = (byte)(outputSizeInBits >> 24);
            prfInput[prfInput.Length - 3] = (byte)(outputSizeInBits >> 16);
            prfInput[prfInput.Length - 2] = (byte)(outputSizeInBits >> 8);
            prfInput[prfInput.Length - 1] = (byte)(outputSizeInBits);

            // Copy label and context to prfInput since they're stable over all iterations
            label.CopyTo(prfInput.Slice(sizeof(uint)));
            contextHeader.CopyTo(prfInput.Slice(sizeof(uint) + label.Length + 1));
            contextData.CopyTo(prfInput.Slice(sizeof(uint) + label.Length + 1 + contextHeader.Length));

            for (uint i = 1; outputCount > 0; i++)
            {
                // Copy [i]_2 to prfInput since it mutates with each iteration
                prfInput[0] = (byte)(i >> 24);
                prfInput[1] = (byte)(i >> 16);
                prfInput[2] = (byte)(i >> 8);
                prfInput[3] = (byte)(i);

#if NET10_0_OR_GREATER
                if (prf is not null)
                {
                    var prfOutputArray = prf.ComputeHash(prfInputArray ?? prfInput.ToArray());
                    prfOutputArray.CopyTo(prfOutput);
                }
                else
                {
                    var success = HMACSHA512.TryHashData(kdk, prfInput, prfOutput, out var bytesWritten);
                    Debug.Assert(success);
                    Debug.Assert(bytesWritten == prfOutputSizeInBytes);
                }
#else
                var prfOutputArray = prf.ComputeHash(prfInputArray);
                var prfOutput = prfOutputArray.AsSpan();
#endif
                CryptoUtil.Assert(prfOutputSizeInBytes == prfOutput.Length, "prfOutputSizeInBytes == prfOutput.Length");
                var numBytesToCopyThisIteration = Math.Min(prfOutputSizeInBytes, outputCount);

                // we need to write into the operationSubkey
                // but it may be the case that we need to split the output
                // so lets count how many bytes we can write into the operationSubKey
                var bytesToWrite = Math.Min(numBytesToCopyThisIteration, operationSubkey.Length - operationSubKeyIndex);
                var leftOverBytes = numBytesToCopyThisIteration - bytesToWrite;
                if (operationSubKeyIndex < operationSubkey.Length) // meaning we need to write to operationSubKey
                {
                    var destination = operationSubkey.Slice(operationSubKeyIndex, bytesToWrite);
                    prfOutput.Slice(0, bytesToWrite).CopyTo(destination);
                    operationSubKeyIndex += bytesToWrite;
                }

                if (operationSubKeyIndex == operationSubkey.Length && leftOverBytes != 0) // we have filled the operationSubKey. It's time for the validationSubKey
                {
                    var destination = validationSubkey.Slice(validationSubKeyIndex, leftOverBytes);
                    prfOutput.Slice(bytesToWrite, leftOverBytes).CopyTo(destination);
                    validationSubKeyIndex += leftOverBytes;
                }

                outputCount -= numBytesToCopyThisIteration;
                prfOutput.Clear(); // contains key material, so delete it
            }
        }
        finally
        {
            prfInput.Clear();
        }
    }
}
