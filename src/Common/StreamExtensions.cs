// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    internal static class StreamExtensions
    {
        public static async Task WriteAsync(this Stream stream, ReadOnlyBuffer<byte> buffer, CancellationToken cancellationToken = default)
        {
            // REVIEW: Should we special case IsSingleSegment here?
            foreach (var segment in buffer)
            {
#if NETCOREAPP2_1
                await stream.WriteAsync(segment, cancellationToken);
#else
                var isArray = MemoryMarshal.TryGetArray(segment, out var arraySegment);
                // We're using the managed memory pool which is backed by managed buffers
                Debug.Assert(isArray);
                await stream.WriteAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count, cancellationToken);
#endif
            }
        }
    }
}
