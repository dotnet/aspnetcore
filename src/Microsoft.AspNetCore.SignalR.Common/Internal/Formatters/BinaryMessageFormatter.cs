// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Binary;
using System.Buffers;
using System.IO;

namespace Microsoft.AspNetCore.SignalR.Internal.Formatters
{
    public static class BinaryMessageFormatter
    {
        public static void WriteMessage(ReadOnlySpan<byte> payload, Stream output)
        {
            // TODO: Optimize for size - (e.g. use Varints)
            var length = sizeof(long);
            var buffer = ArrayPool<byte>.Shared.Rent(length + payload.Length);
            var bufferSpan = buffer.AsSpan();

            BufferWriter.WriteBigEndian<long>(bufferSpan, payload.Length);
            bufferSpan = bufferSpan.Slice(length);
            payload.CopyTo(bufferSpan);
            output.Write(buffer, 0, payload.Length + length);

            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}