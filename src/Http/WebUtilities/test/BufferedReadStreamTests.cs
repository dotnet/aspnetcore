// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.WebUtilities;

public class BufferedReadStreamTests
{
    [Fact]
    public async Task ReadLineAsync_LineWithinSingleBuffer_Succeeds()
    {
        var stream = MakeStream("hello world\r\n", bufferSize: 4096);

        var line = await stream.ReadLineAsync(lengthLimit: 100, CancellationToken.None);

        Assert.Equal("hello world", line);
    }

    [Fact]
    public async Task ReadLineAsync_LineSpanningMultipleBuffersWithinLimit_Succeeds()
    {
        var content = new string('a', 100);
        var stream = MakeStream(content + "\r\n", bufferSize: 16);

        var line = await stream.ReadLineAsync(lengthLimit: 1000, CancellationToken.None);

        Assert.Equal(content, line);
    }

    [Fact]
    public async Task ReadLineAsync_LineSpanningMultipleBuffersExceedingLimit_Throws()
    {
        // The line is larger than both the buffer size and the length limit, so it spans
        // several internal buffers before the limit is reached.
        var stream = MakeStream(new string('a', 100) + "\r\n", bufferSize: 16);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => stream.ReadLineAsync(lengthLimit: 40, CancellationToken.None));
        Assert.Equal("Line length limit 40 exceeded.", exception.Message);
    }

    [Fact]
    public async Task ReadLineAsync_UnterminatedLineExceedingLimit_ThrowsInsteadOfAccumulating()
    {
        // No CRLF terminator, using the real default buffer (4 KiB) and header limit (16 KiB).
        // The limit must be enforced while reading rather than accumulating the whole payload.
        var stream = MakeStream(new string('a', 100_000), bufferSize: 1024 * 4);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => stream.ReadLineAsync(lengthLimit: 1024 * 16, CancellationToken.None));
        Assert.Equal("Line length limit 16384 exceeded.", exception.Message);
    }

    [Fact]
    public void ReadLine_LineSpanningMultipleBuffersExceedingLimit_Throws()
    {
        var stream = MakeStream(new string('a', 100) + "\r\n", bufferSize: 16);

        var exception = Assert.Throws<InvalidDataException>(() => stream.ReadLine(lengthLimit: 40));
        Assert.Equal("Line length limit 40 exceeded.", exception.Message);
    }

    [Fact]
    public void ReadLine_LineSpanningMultipleBuffersWithinLimit_Succeeds()
    {
        var content = new string('a', 100);
        var stream = MakeStream(content + "\r\n", bufferSize: 16);

        var line = stream.ReadLine(lengthLimit: 1000);

        Assert.Equal(content, line);
    }

    [Fact]
    public void Read_Span_DrainsBufferedDataBeforeReadingInner()
    {
        // The buffer is rented from the pool, so its actual size may exceed the requested size.
        // The content is long enough that some of it always remains in the inner stream.
        const string content = "0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz";
        var stream = MakeStream(content, bufferSize: 5);
        Assert.True(stream.EnsureBuffered(5));
        var buffered = stream.BufferedData.Count;
        Assert.InRange(buffered, 5, content.Length - 3);

        Span<byte> buffer = stackalloc byte[3];

        // A span smaller than the buffered data drains it partially.
        var read = stream.Read(buffer);
        Assert.Equal(3, read);
        Assert.Equal(content.Substring(0, 3), Encoding.UTF8.GetString(buffer.Slice(0, read)));
        Assert.Equal(buffered - 3, stream.BufferedData.Count);

        // Each read returns only buffered data, so the last one returns the remainder
        // rather than topping up from the inner stream.
        var consumed = read;
        while (stream.BufferedData.Count > 0)
        {
            var remaining = stream.BufferedData.Count;
            read = stream.Read(buffer);
            Assert.Equal(Math.Min(remaining, buffer.Length), read);
            Assert.Equal(content.Substring(consumed, read), Encoding.UTF8.GetString(buffer.Slice(0, read)));
            consumed += read;
        }
        Assert.Equal(buffered, consumed);

        // With the buffer drained, the read falls through to the inner stream.
        read = stream.Read(buffer);
        Assert.Equal(3, read);
        Assert.Equal(content.Substring(consumed, 3), Encoding.UTF8.GetString(buffer.Slice(0, read)));
        Assert.Equal(0, stream.BufferedData.Count);
    }

    [Fact]
    public void Read_Array_DrainsBufferedDataBeforeReadingInner()
    {
        const string content = "0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz";
        var stream = MakeStream(content, bufferSize: 5);
        Assert.True(stream.EnsureBuffered(5));
        var buffered = stream.BufferedData.Count;
        Assert.InRange(buffered, 5, content.Length - 3);

        var buffer = new byte[3];

        var read = stream.Read(buffer, 0, buffer.Length);
        Assert.Equal(3, read);
        Assert.Equal(content.Substring(0, 3), Encoding.UTF8.GetString(buffer, 0, read));
        Assert.Equal(buffered - 3, stream.BufferedData.Count);

        var consumed = read;
        while (stream.BufferedData.Count > 0)
        {
            var remaining = stream.BufferedData.Count;
            read = stream.Read(buffer, 0, buffer.Length);
            Assert.Equal(Math.Min(remaining, buffer.Length), read);
            Assert.Equal(content.Substring(consumed, read), Encoding.UTF8.GetString(buffer, 0, read));
            consumed += read;
        }
        Assert.Equal(buffered, consumed);

        read = stream.Read(buffer, 0, buffer.Length);
        Assert.Equal(3, read);
        Assert.Equal(content.Substring(consumed, 3), Encoding.UTF8.GetString(buffer, 0, read));
        Assert.Equal(0, stream.BufferedData.Count);
    }

    [Fact]
    public void Write_Span_WritesToInnerStream()
    {
        var inner = new MemoryStream();
        var stream = new BufferedReadStream(inner, bufferSize: 16);

        stream.Write("hello"u8);

        Assert.Equal("hello", Encoding.UTF8.GetString(inner.ToArray()));
    }

    private static BufferedReadStream MakeStream(string text, int bufferSize)
    {
        return new BufferedReadStream(new MemoryStream(Encoding.UTF8.GetBytes(text)), bufferSize);
    }
}
