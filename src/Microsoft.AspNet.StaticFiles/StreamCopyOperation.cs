// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.StaticFiles
{
    // FYI: In most cases the source will be a FileStream and the destination will be to the network.
    internal static class StreamCopyOperation
    {
        private const int DefaultBufferSize = 1024 * 16;

        internal static async Task CopyToAsync(Stream source, Stream destination, long? length, CancellationToken cancel)
        {
            long? bytesRemaining = length;
            byte[] buffer = new byte[DefaultBufferSize];

            Contract.Assert(source != null);
            Contract.Assert(destination != null);
            Contract.Assert(!bytesRemaining.HasValue || bytesRemaining.Value >= 0);
            Contract.Assert(buffer != null);

            while (true)
            {
                // The natural end of the range.
                if (bytesRemaining.HasValue && bytesRemaining.Value <= 0)
                {
                    return;
                }

                cancel.ThrowIfCancellationRequested();

                int readLength = buffer.Length;
                if (bytesRemaining.HasValue)
                {
                    readLength = (int)Math.Min(bytesRemaining.Value, (long)readLength);
                }
                int count = await source.ReadAsync(buffer, 0, readLength, cancel);

                if (bytesRemaining.HasValue)
                {
                    bytesRemaining -= count;
                }

                // End of the source stream.
                if (count == 0)
                {
                    return;
                }

                cancel.ThrowIfCancellationRequested();

                await destination.WriteAsync(buffer, 0, count, cancel);
            }
        }
    }
}