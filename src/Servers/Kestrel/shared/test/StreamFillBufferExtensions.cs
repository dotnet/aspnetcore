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
            int read;

            do
            {
                read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken);
                offset += read;
            } while (read != 0 && offset < buffer.Length);

            if (read != 0)
            {
                Assert.Equal(0, await stream.ReadAsync(new byte[1], 0, 1, cancellationToken));
            }

            return offset;
        }

        public static async Task<int> ReadAtLeastLengthAsync(this Stream stream, byte[] buffer, int minLength, CancellationToken cancellationToken = default)
        {
            Assert.True(minLength <= buffer.Length);

            var offset = 0;
            int read;

            do
            {
                read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken);
                offset += read;
            } while (read != 0 && offset < minLength);

            Assert.True(offset >= minLength);
            return offset;
        }

        public static Task FillEntireBufferAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken = default) =>
            stream.ReadAtLeastLengthAsync(buffer, buffer.Length, cancellationToken);
    }
}
