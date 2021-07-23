// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO
{
    public static class StreamFillBufferExtensions
    {
        public static async Task<int> ReadUntilEndAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken = default)
        {
            var offset = 0;

            while (offset < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken);
                offset += read;

                if (read == 0)
                {
                    return offset;
                }
            }

            Assert.Equal(0, await stream.ReadAsync(new byte[1], 0, 1, cancellationToken));

            return offset;
        }

        public static async Task ReadUntilLengthAsync(this Stream stream, byte[] buffer, int length, CancellationToken cancellationToken = default)
        {
            var offset = 0;

            while (offset < length)
            {
                var read = await stream.ReadAsync(buffer, offset, length - offset, cancellationToken);
                offset += read;

                Assert.NotEqual(0, read);
            }
        }
    }
}
