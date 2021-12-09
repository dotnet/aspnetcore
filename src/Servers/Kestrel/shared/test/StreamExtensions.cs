// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO;

public static class StreamExtensions
{
    /// <summary>
    /// Fill the buffer until the end of the stream.
    /// </summary>
    public static async Task<int> FillBufferUntilEndAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken = default)
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

    public static async Task<int> FillEntireBufferAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken = default)
    {
        var offset = 0;
        int read;

        do
        {
            read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken);
            offset += read;
        } while (read != 0 && offset < buffer.Length);

        Assert.True(offset >= buffer.Length);
        return offset;
    }

    public static async Task<byte[]> ReadAtLeastLengthAsync(this Stream stream, int length, int bufferLength = 1024, bool allowEmpty = false, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[bufferLength];
        var data = new List<byte>();
        var offset = 0;

        while (offset < length)
        {
            var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            offset += read;

            if (read == 0)
            {
                if (allowEmpty && offset == 0)
                {
                    return null;
                }

                throw new Exception("Stream read 0.");
            }

            data.AddRange(buffer.AsMemory(0, read).ToArray());
        }

        return data.ToArray();
    }

    public static async Task<byte[]> ReadUntilEndAsync(this Stream stream, int bufferLength = 1024, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[bufferLength];
        var data = new List<byte>();
        var offset = 0;

        while (true)
        {
            var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            offset += read;

            if (read == 0)
            {
                return data.ToArray();
            }

            data.AddRange(buffer.AsMemory(0, read).ToArray());
        }
    }
}
