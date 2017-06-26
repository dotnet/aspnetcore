// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Binary;
using System.Buffers;
using System.IO;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    public static class BinaryMessageFormatter
    {
        public static bool TryWriteMessage(ReadOnlySpan<byte> payload, Stream output)
        {
            // TODO: Optimize for size - (e.g. use Varints)
            var length = sizeof(long);
            var buffer = ArrayPool<byte>.Shared.Rent(length);
            BufferWriter.WriteBigEndian<long>(buffer, payload.Length);
            output.Write(buffer, 0, length);
            ArrayPool<byte>.Shared.Return(buffer);

            buffer = ArrayPool<byte>.Shared.Rent(payload.Length);
            payload.CopyTo(buffer);
            output.Write(buffer, 0, payload.Length);
            ArrayPool<byte>.Shared.Return(buffer);

            return true;
        }
    }
}