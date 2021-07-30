// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO
{
    public static class StreamFillBufferExtensions
    {
        public static async Task<byte[]> ReadUntilEndAsync(this Stream stream, byte[] buffer = null, CancellationToken cancellationToken = default)
        {
            buffer ??= new byte[1024];
            var data = new List<byte>();
            var offset = 0;

            while (offset < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer, 0, buffer.Length - offset, cancellationToken);
                offset += read;

                if (read == 0)
                {
                    return data.ToArray();
                }

                data.AddRange(buffer.AsMemory(0, read).ToArray());
            }

            Assert.Equal(0, await stream.ReadAsync(new byte[1], 0, 1, cancellationToken));

            return data.ToArray();
        }

        public static async Task<byte[]> ReadUntilLengthAsync(this Stream stream, int length, byte[] buffer = null, CancellationToken cancellationToken = default)
        {
            buffer ??= new byte[1024];
            var data = new List<byte>();
            var offset = 0;

            while (offset < length)
            {
                var read = await stream.ReadAsync(buffer, 0, Math.Min(length - offset, buffer.Length), cancellationToken);
                offset += read;

                Assert.NotEqual(0, read);

                data.AddRange(buffer.AsMemory(0, read).ToArray());
            }

            return data.ToArray();
        }

        public static async Task<byte[]> ReadAtLeastLengthAsync(this Stream stream, int length, byte[] buffer = null, CancellationToken cancellationToken = default)
        {
            buffer ??= new byte[1024];
            var data = new List<byte>();
            var offset = 0;

            while (offset < length)
            {
                var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                offset += read;

                Assert.NotEqual(0, read);

                data.AddRange(buffer.AsMemory(0, read).ToArray());
            }

            return data.ToArray();
        }
    }
}
