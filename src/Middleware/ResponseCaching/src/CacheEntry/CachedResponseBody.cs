// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.ResponseCaching
{
    internal class CachedResponseBody
    {
        public CachedResponseBody(List<byte[]> segments, long length)
        {
            Segments = segments;
            Length = length;
        }

        public List<byte[]> Segments { get; }

        public long Length { get; }

        public async Task CopyToAsync(PipeWriter destination, CancellationToken cancellationToken)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            foreach (var segment in Segments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Copy(segment, destination);

                await destination.FlushAsync();
            }
        }

        private static void Copy(byte[] segment, PipeWriter destination)
        {
            var span = destination.GetSpan(segment.Length);

            segment.CopyTo(span);
            destination.Advance(segment.Length);
        }
    }
}
