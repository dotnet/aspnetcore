// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebUtilities
{
    internal static class PipeReaderExtensions
    {

        private const int StackAllocThreshold = 128;

        public static async Task<string> ReadToEndAsync(this PipeReader pipeReader, Encoding streamEncoding, CancellationToken cancellationToken = default)
        {
            string GetDecodedString(ReadOnlySpan<byte> readOnlySpan)
            {
                if (readOnlySpan.Length == 0)
                {
                    return string.Empty;
                }
                else
                {
                    // We need to create a Span from a ReadOnlySpan. This cast is safe because the memory is still held by the pipe
                    // We will also create a string from it by the end of the function.
                    var span = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(readOnlySpan[0]), readOnlySpan.Length);
                    return streamEncoding.GetString(span);
                }
            }

            string GetDecodedStringFromReadOnlySequence(ref ReadOnlySequence<byte> sequence)
            {
                if (sequence.IsSingleSegment)
                {
                    var str = GetDecodedString(sequence.First.Span);
                    sequence = sequence.Slice(sequence.End);
                    return str;
                }

                if (sequence.Length < StackAllocThreshold)
                {
                    Span<byte> buffer = stackalloc byte[(int)sequence.Length];
                    sequence.CopyTo(buffer);
                    sequence = sequence.Slice(sequence.End);
                    return GetDecodedString(buffer);
                }
                else
                {
                    var byteArray = ArrayPool<byte>.Shared.Rent((int)sequence.Length);

                    try
                    {
                        Span<byte> buffer = byteArray.AsSpan(0, (int)sequence.Length);
                        sequence.CopyTo(buffer);
                        sequence = sequence.Slice(sequence.End);
                        return GetDecodedString(buffer);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(byteArray);
                    }
                }
            }

            var stringBuilder = new StringBuilder();
            ReadResult readResult;
            do
            {
                readResult = await pipeReader.ReadAsync(cancellationToken);
                if (readResult.IsCanceled)
                {
                    throw new OperationCanceledException("Read was canceled");
                }

                var sequence = readResult.Buffer;
                stringBuilder.Append(GetDecodedStringFromReadOnlySequence(ref sequence));
                pipeReader.AdvanceTo(sequence.Start);

            } while (!readResult.IsCompleted);

            return stringBuilder.ToString();
        }

        public static async Task DrainAsync(this PipeReader pipeReader, CancellationToken cancellationToken)
        {
            ReadResult readResult;
            do
            {
                readResult = await pipeReader.ReadAsync(cancellationToken);

                if (readResult.IsCanceled)
                {
                    throw new OperationCanceledException("Read was canceled");
                }

                pipeReader.AdvanceTo(readResult.Buffer.End);
            } while (!readResult.IsCompleted);
        }
    }
}
