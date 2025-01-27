// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
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
        HashAlgorithm prf = new HMACSHA512(kdk);

        // kdk is passed just to have a shared implementation for different framework versions
        DeriveKeys(kdk, label, contextHeader, contextData, operationSubkey, validationSubkey, prf);
    }
#endif

#if NET10_0_OR_GREATER
    public static void DeriveKeys(ReadOnlySpan<byte> kdk, ReadOnlySpan<byte> label, ReadOnlySpan<byte> contextHeader, ReadOnlySpan<byte> contextData, Span<byte> operationSubkey, Span<byte> validationSubkey)
        => DeriveKeys(kdk, label, contextHeader, contextData, operationSubkey, validationSubkey, prf: null);
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

        byte[]? prfOutput = null;

        // See SP800-108, Sec. 5.1 for the format of the input to the PRF routine.
        var prfInputLength = checked(sizeof(uint) /* [i]_2 */ + label.Length + 1 /* 0x00 */ + (contextHeader.Length + contextData.Length) + sizeof(uint) /* [K]_2 */);

#if NET10_0_OR_GREATER
        byte[]? prfInputArray = null;
        Span<byte> prfInput = prfInputLength <= 128
            ? stackalloc byte[prfInputLength]
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

            int prfOutputSizeInBytes =
#if NET10_0_OR_GREATER
            HMACSHA512.HashSizeInBytes;
#else
            prf.GetDigestSizeInBytes();
#endif
            for (uint i = 1; outputCount > 0; i++)
            {
                // Copy [i]_2 to prfInput since it mutates with each iteration
                prfInput[0] = (byte)(i >> 24);
                prfInput[1] = (byte)(i >> 16);
                prfInput[2] = (byte)(i >> 8);
                prfInput[3] = (byte)(i);

#if NET10_0_OR_GREATER
                // not using stackalloc here, because we are in a loop
                // and potentially can exhaust the stack memory: https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2014
                prfOutput = new byte[prfOutputSizeInBytes];
                HMACSHA512.TryHashData(kdk, prfInput, prfOutput, out _);
#else
                prfOutput = prf.ComputeHash(prfInputArray);
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
                    prfOutput.AsSpan(0, bytesToWrite).CopyTo(destination);
                    operationSubKeyIndex += bytesToWrite;
                }
                if (operationSubKeyIndex == operationSubkey.Length && leftOverBytes != 0) // we have filled the operationSubKey. It's time for the validationSubKey
                {
                    var destination = validationSubkey.Slice(validationSubKeyIndex, leftOverBytes);
                    prfOutput.AsSpan(bytesToWrite, leftOverBytes).CopyTo(destination);
                    validationSubKeyIndex += leftOverBytes;
                }

                outputCount -= numBytesToCopyThisIteration;
            }
        }
        finally
        {
            if (prf is IDisposable disposablePrf)
            {
                disposablePrf.Dispose();
            }

#if NET10_0_OR_GREATER
            if (prfOutput is not null)
            {
                Array.Clear(prfOutput, 0, prfOutput.Length); // contains key material, so delete it
            }

            if (prfInputArray is not null)
            {
                Array.Clear(prfInputArray, 0, prfInputArray.Length); // contains key material, so delete it
            }
            else
            {
                // to be extra careful - clear the stackalloc memory
                prfInput.Clear();
            }
#else
            Array.Clear(prfInputArray, 0, prfInputArray.Length); // contains key material, so delete it
            Array.Clear(prfOutput, 0, prfOutput.Length); // contains key material, so delete it
#endif
        }
    }
}
