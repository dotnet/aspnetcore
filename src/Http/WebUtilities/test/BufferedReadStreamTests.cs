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
    public async Task ReadLineAsync_MultipleLinesWithinSingleBuffer_ReadsEachLineAndLeavesRemainder()
    {
        var stream = MakeStream("first\r\n\r\nthird\rwith\nlone\r\nremainder", bufferSize: 4096);

        Assert.Equal("first", await stream.ReadLineAsync(lengthLimit: 100, CancellationToken.None));
        Assert.Equal("", await stream.ReadLineAsync(lengthLimit: 100, CancellationToken.None));
        Assert.Equal("third\rwith\nlone", await stream.ReadLineAsync(lengthLimit: 100, CancellationToken.None));

        var remainder = new byte[100];
        var read = await stream.ReadAsync(remainder, 0, remainder.Length);
        Assert.Equal("remainder", Encoding.UTF8.GetString(remainder, 0, read));
    }

    [Fact]
    public void ReadLine_MultipleLinesWithinSingleBuffer_ReadsEachLineAndLeavesRemainder()
    {
        var stream = MakeStream("first\r\n\r\nthird\rwith\nlone\r\nremainder", bufferSize: 4096);

        Assert.Equal("first", stream.ReadLine(lengthLimit: 100));
        Assert.Equal("", stream.ReadLine(lengthLimit: 100));
        Assert.Equal("third\rwith\nlone", stream.ReadLine(lengthLimit: 100));
        Assert.Equal("remainder", stream.ReadLine(lengthLimit: 100));
        Assert.Equal("", stream.ReadLine(lengthLimit: 100));
    }

    [Theory]
    [InlineData("abc\r\n", 4, "abc")]
    [InlineData("\r\n", 1, "")]
    [InlineData("h\u00e9llo\r\n", 7, "h\u00e9llo")]
    public async Task ReadLineAsync_LineWithinSingleBufferAtLimit_Succeeds(string text, int lengthLimit, string expected)
    {
        var stream = MakeStream(text, bufferSize: 4096);

        Assert.Equal(expected, await stream.ReadLineAsync(lengthLimit, CancellationToken.None));
        Assert.Equal(expected, MakeStream(text, bufferSize: 4096).ReadLine(lengthLimit));
    }

    [Theory]
    [InlineData("abc\r\n", 3)]
    [InlineData("\r\n", 0)]
    [InlineData("h\u00e9llo\r\n", 6)]
    public async Task ReadLineAsync_LineWithinSingleBufferExceedingLimit_Throws(string text, int lengthLimit)
    {
        var stream = MakeStream(text, bufferSize: 4096);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => stream.ReadLineAsync(lengthLimit, CancellationToken.None));
        Assert.Equal($"Line length limit {lengthLimit} exceeded.", exception.Message);

        exception = Assert.Throws<InvalidDataException>(() => MakeStream(text, bufferSize: 4096).ReadLine(lengthLimit));
        Assert.Equal($"Line length limit {lengthLimit} exceeded.", exception.Message);
    }

    [Fact]
    public async Task ReadLineAsync_CrLfSplitAcrossBuffers_Succeeds()
    {
        // "abc\r" fills the first buffer and "\n" arrives in the next one.
        var stream = MakeStream("abc\r\nnext\r\n", bufferSize: 4);

        Assert.Equal("abc", await stream.ReadLineAsync(lengthLimit: 100, CancellationToken.None));
        Assert.Equal("next", await stream.ReadLineAsync(lengthLimit: 100, CancellationToken.None));
        Assert.Equal("", await stream.ReadLineAsync(lengthLimit: 100, CancellationToken.None));
    }

    private static BufferedReadStream MakeStream(string text, int bufferSize)
    {
        return new BufferedReadStream(new MemoryStream(Encoding.UTF8.GetBytes(text)), bufferSize);
    }
}
