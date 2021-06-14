// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

                await destination.FlushAsync(cancellationToken);
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
