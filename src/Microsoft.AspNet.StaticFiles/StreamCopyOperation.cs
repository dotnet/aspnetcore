// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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