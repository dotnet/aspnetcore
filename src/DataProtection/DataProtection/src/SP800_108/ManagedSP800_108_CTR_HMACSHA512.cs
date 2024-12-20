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
    public static void DeriveKeys(byte[] kdk, ArraySegment<byte> label, ArraySegment<byte> context, Func<byte[], HashAlgorithm> prfFactory, ArraySegment<byte> output)
    {
        // make copies so we can mutate these local vars
        var outputOffset = output.Offset;
        var outputCount = output.Count;

        using (var prf = prfFactory(kdk))
        {
            // See SP800-108, Sec. 5.1 for the format of the input to the PRF routine.
            var prfInput = new byte[checked(sizeof(uint) /* [i]_2 */ + label.Count + 1 /* 0x00 */ + context.Count + sizeof(uint) /* [K]_2 */)];

            // Copy [L]_2 to prfInput since it's stable over all iterations
            uint outputSizeInBits = (uint)checked((int)outputCount * 8);
            prfInput[prfInput.Length - 4] = (byte)(outputSizeInBits >> 24);
            prfInput[prfInput.Length - 3] = (byte)(outputSizeInBits >> 16);
            prfInput[prfInput.Length - 2] = (byte)(outputSizeInBits >> 8);
            prfInput[prfInput.Length - 1] = (byte)(outputSizeInBits);

            // Copy label and context to prfInput since they're stable over all iterations
            Buffer.BlockCopy(label.Array!, label.Offset, prfInput, sizeof(uint), label.Count);
            Buffer.BlockCopy(context.Array!, context.Offset, prfInput, sizeof(int) + label.Count + 1, context.Count);

            var prfOutputSizeInBytes = prf.GetDigestSizeInBytes();
            for (uint i = 1; outputCount > 0; i++)
            {
                // Copy [i]_2 to prfInput since it mutates with each iteration
                prfInput[0] = (byte)(i >> 24);
                prfInput[1] = (byte)(i >> 16);
                prfInput[2] = (byte)(i >> 8);
                prfInput[3] = (byte)(i);

                // Run the PRF and copy the results to the output buffer
                var prfOutput = prf.ComputeHash(prfInput);
                CryptoUtil.Assert(prfOutputSizeInBytes == prfOutput.Length, "prfOutputSizeInBytes == prfOutput.Length");
                var numBytesToCopyThisIteration = Math.Min(prfOutputSizeInBytes, outputCount);
                Buffer.BlockCopy(prfOutput, 0, output.Array!, outputOffset, numBytesToCopyThisIteration);
                Array.Clear(prfOutput, 0, prfOutput.Length); // contains key material, so delete it

                // adjust offsets
                outputOffset += numBytesToCopyThisIteration;
                outputCount -= numBytesToCopyThisIteration;
            }
        }
    }

#if NET10_0_OR_GREATER
    public static void DeriveKeysHMACSHA512(
        ReadOnlySpan<byte> kdk,
        ReadOnlySpan<byte> label,
        ReadOnlySpan<byte> contextHeader,
        ReadOnlySpan<byte> contextData,
        Span<byte> operationSubKey,
        Span<byte> validationSubKey)
    {
        var operationSubKeyIndex = 0;
        var validationSubKeyIndex = 0;
        var outputCount = operationSubKey.Length + validationSubKey.Length;

        byte[]? prfOutput = null;

        // See SP800-108, Sec. 5.1 for the format of the input to the PRF routine.
        var prfInputLength = checked(sizeof(uint) /* [i]_2 */ + label.Length + 1 /* 0x00 */ + (contextHeader.Length + contextData.Length) + sizeof(uint) /* [K]_2 */);

        byte[]? prfInputLease = null;
        Span<byte> prfInput = prfInputLength <= 128
            ? stackalloc byte[prfInputLength]
            : (prfInputLease = DataProtectionPool.Rent(prfInputLength)).AsSpan(0, prfInputLength);

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

                // Run the PRF and copy the results to the output buffer
                // not using stackalloc here, because we are in a loop
                // and potentially can exhaust the stack memory: https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2014
                prfOutput = DataProtectionPool.Rent(HMACSHA512.HashSizeInBytes);
                HMACSHA512.TryHashData(kdk, prfInput, prfOutput, out _);

                CryptoUtil.Assert(HMACSHA512.HashSizeInBytes == prfOutput.Length, "prfOutputSizeInBytes == prfOutput.Length");
                var numBytesToCopyThisIteration = Math.Min(HMACSHA512.HashSizeInBytes, outputCount);

                // we need to write into the operationSubkey
                // but it may be the case that we need to split the output
                // so lets count how many bytes we can write into the operationSubKey
                var bytesToWrite = Math.Min(numBytesToCopyThisIteration, operationSubKey.Length - operationSubKeyIndex);
                var leftOverBytes = numBytesToCopyThisIteration - bytesToWrite;
                if (operationSubKeyIndex < operationSubKey.Length) // meaning we need to write to operationSubKey
                {
                    var destination = operationSubKey.Slice(operationSubKeyIndex, bytesToWrite);
                    prfOutput.AsSpan(0, bytesToWrite).CopyTo(destination);
                    operationSubKeyIndex += bytesToWrite;
                }
                if (operationSubKeyIndex == operationSubKey.Length && leftOverBytes != 0) // we have filled the operationSubKey. It's time for the validationSubKey
                {
                    var destination = validationSubKey.Slice(validationSubKeyIndex, leftOverBytes);
                    prfOutput.AsSpan(bytesToWrite, leftOverBytes).CopyTo(destination);
                    validationSubKeyIndex += leftOverBytes;
                }

                outputCount -= numBytesToCopyThisIteration;
            }
        }
        finally
        {
            if (prfOutput is not null)
            {
                DataProtectionPool.Return(prfOutput, clearArray: true); // contains key material, so delete it
            }

            if (prfInputLease is not null)
            {
                DataProtectionPool.Return(prfInputLease, clearArray: true); // contains key material, so delete it
            }
            else
            {
                // to be extra careful - clear the stackalloc memory
                prfInput.Clear();
            }
        }
    }
#endif

    public static void DeriveKeys(
        byte[] kdk,
        ReadOnlySpan<byte> label,
        ReadOnlySpan<byte> contextHeader,
        ReadOnlySpan<byte> contextData,
        Func<byte[], HashAlgorithm> prfFactory,
        Span<byte> operationSubKey,
        Span<byte> validationSubKey)
    {
        var operationSubKeyIndex = 0;
        var validationSubKeyIndex = 0;
        var outputCount = operationSubKey.Length + validationSubKey.Length;

        using (var prf = prfFactory(kdk))
        {
            byte[]? prfOutput = null;

            // See SP800-108, Sec. 5.1 for the format of the input to the PRF routine.
            var prfInputLength = checked(sizeof(uint) /* [i]_2 */ + label.Length + 1 /* 0x00 */ + (contextHeader.Length + contextData.Length) + sizeof(uint) /* [K]_2 */);

#if NET10_0_OR_GREATER
            byte[]? prfInputLease = null;
            Span<byte> prfInput = prfInputLength <= 128
                ? stackalloc byte[prfInputLength]
                : (prfInputLease = DataProtectionPool.Rent(prfInputLength)).AsSpan(0, prfInputLength);
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

                var prfOutputSizeInBytes = prf.GetDigestSizeInBytes();
                for (uint i = 1; outputCount > 0; i++)
                {
                    // Copy [i]_2 to prfInput since it mutates with each iteration
                    prfInput[0] = (byte)(i >> 24);
                    prfInput[1] = (byte)(i >> 16);
                    prfInput[2] = (byte)(i >> 8);
                    prfInput[3] = (byte)(i);

                    // Run the PRF and copy the results to the output buffer
#if NET10_0_OR_GREATER
                    // not using stackalloc here, because we are in a loop
                    // and potentially can exhaust the stack memory: https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2014
                    prfOutput = DataProtectionPool.Rent(prfOutputSizeInBytes);
                    prf.TryComputeHash(prfInput, prfOutput, out _);
#else
                    prfOutput = prf.ComputeHash(prfInputArray);
#endif

                    CryptoUtil.Assert(prfOutputSizeInBytes == prfOutput.Length, "prfOutputSizeInBytes == prfOutput.Length");
                    var numBytesToCopyThisIteration = Math.Min(prfOutputSizeInBytes, outputCount);

                    // we need to write into the operationSubkey
                    // but it may be the case that we need to split the output
                    // so lets count how many bytes we can write into the operationSubKey
                    var bytesToWrite = Math.Min(numBytesToCopyThisIteration, operationSubKey.Length - operationSubKeyIndex);
                    var leftOverBytes = numBytesToCopyThisIteration - bytesToWrite;
                    if (operationSubKeyIndex < operationSubKey.Length) // meaning we need to write to operationSubKey
                    {
                        var destination = operationSubKey.Slice(operationSubKeyIndex, bytesToWrite);
                        prfOutput.AsSpan(0, bytesToWrite).CopyTo(destination);
                        operationSubKeyIndex += bytesToWrite;
                    }
                    if (operationSubKeyIndex == operationSubKey.Length && leftOverBytes != 0) // we have filled the operationSubKey. It's time for the validationSubKey
                    {
                        var destination = validationSubKey.Slice(validationSubKeyIndex, leftOverBytes);
                        prfOutput.AsSpan(bytesToWrite, leftOverBytes).CopyTo(destination);
                        validationSubKeyIndex += leftOverBytes;
                    }

                    outputCount -= numBytesToCopyThisIteration;
                }
            }
            finally
            {
#if NET10_0_OR_GREATER
                if (prfOutput is not null)
                {
                    DataProtectionPool.Return(prfOutput, clearArray: true); // contains key material, so delete it
                }
                
                if (prfInputLease is not null)
                {
                    DataProtectionPool.Return(prfInputLease, clearArray: true); // contains key material, so delete it
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

    /// <remarks>
    /// Probably, you would want to use similar method <see cref="DeriveKeys(byte[], ReadOnlySpan{byte}, ReadOnlySpan{byte}, ReadOnlySpan{byte}, Func{byte[], HashAlgorithm}, Span{byte}, Span{byte})"/>.
    /// It is more efficient allowing to skip an allocation of `combinedContext` and writing directly into passed Spans
    /// </remarks>
    public static void DeriveKeysWithContextHeader(byte[] kdk, ArraySegment<byte> label, byte[] contextHeader, ArraySegment<byte> context, Func<byte[], HashAlgorithm> prfFactory, ArraySegment<byte> output)
    {
        var combinedContext = new byte[checked(contextHeader.Length + context.Count)];
        Buffer.BlockCopy(contextHeader, 0, combinedContext, 0, contextHeader.Length);
        Buffer.BlockCopy(context.Array!, context.Offset, combinedContext, contextHeader.Length, context.Count);
        DeriveKeys(kdk, label, new ArraySegment<byte>(combinedContext), prfFactory, output);
    }
}
