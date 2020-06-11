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
        public static ValueTask WriteAsync(this Stream stream, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (buffer.IsSingleSegment)
            {
#if NETCOREAPP
                return stream.WriteAsync(buffer.First, cancellationToken);
#else
                var isArray = MemoryMarshal.TryGetArray(buffer.First, out var arraySegment);
                // We're using the managed memory pool which is backed by managed buffers
                Debug.Assert(isArray);
                return new ValueTask(stream.WriteAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count, cancellationToken));
#endif
            }

            return WriteMultiSegmentAsync(stream, buffer, cancellationToken);
        }

        private static async ValueTask WriteMultiSegmentAsync(Stream stream, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            var position = buffer.Start;
            while (buffer.TryGet(ref position, out var segment))
            {
#if NETCOREAPP
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
