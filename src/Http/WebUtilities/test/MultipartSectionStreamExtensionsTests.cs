// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities;

public class MultipartSectionStreamExtensionsTests
{
    public static TheoryData<string, int> Utf8Lengths => new()
    {
        { "a", 0 },
        { "a", 1 },
        { "a", 16 },
        { "a", 4095 },
        { "a", 4096 },
        { "a", 4097 },
        { "a", 64 * 1024 - 1 },
        { "a", 64 * 1024 },
        { "a", 64 * 1024 + 1 },
        { "a", 200_000 },
        // Multi-byte characters that straddle the buffer boundaries.
        { "\u00e9", 2047 },
        { "\u00e9", 2049 },
        { "\u20ac", 1365 },
        { "\u20ac", 1366 },
        { "\U0001D11E", 1024 },
        { "\U0001D11E", 16 * 1024 + 1 },
    };

    [Theory]
    [MemberData(nameof(Utf8Lengths))]
    public async Task ReadAsStringAsync_Utf8_MatchesStreamReader(string unit, int repeat)
    {
        var text = string.Concat(Enumerable.Repeat(unit, repeat));
        var bytes = Encoding.UTF8.GetBytes(text);

        Assert.Equal(text, await ReadAsStringAsync(bytes, contentType: null, readSizes: null));
        Assert.Equal(text, await ReadAsStringAsync(bytes, contentType: "text/plain; charset=utf-8", readSizes: null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(5000)]
    public async Task ReadAsStringAsync_ShortReads_MatchesStreamReader(int length)
    {
        var text = string.Concat(Enumerable.Repeat("a\u00e9\u20ac", length));
        var bytes = Encoding.UTF8.GetBytes(text);

        Assert.Equal(text, await ReadAsStringAsync(bytes, contentType: null, readSizes: [1]));
        Assert.Equal(text, await ReadAsStringAsync(bytes, contentType: null, readSizes: [7, 1000]));
    }

    public static TheoryData<byte[]> SpecialPayloads => new()
    {
        // Byte order marks are detected and removed.
        new byte[] { 0xEF, 0xBB, 0xBF, (byte)'h', (byte)'i' },
        new byte[] { 0xFF, 0xFE, (byte)'h', 0, (byte)'i', 0 },
        new byte[] { 0xFE, 0xFF, 0, (byte)'h', 0, (byte)'i' },
        new byte[] { 0xFF, 0xFE, 0, 0, (byte)'h', 0, 0, 0 },
        new byte[] { 0, 0, 0xFE, 0xFF, 0, 0, 0, (byte)'h' },
        // Partial byte order marks and lead bytes.
        new byte[] { 0xEF },
        new byte[] { 0xEF, 0xBB },
        new byte[] { 0xFE },
        new byte[] { 0, (byte)'h' },
        // Invalid UTF-8.
        new byte[] { (byte)'a', 0xC3 },
        new byte[] { (byte)'a', 0xFF, (byte)'b' },
        new byte[] { 0xC3, 0x28, 0xA0, 0xA1 },
        // Byte order marks after the first byte.
        new byte[] { (byte)'a', 0xFF, 0xFE, (byte)'h', 0 },
        new byte[] { (byte)'a', 0xFE, 0xFF, 0, (byte)'h' },
        new byte[] { (byte)'a', 0xEF, 0xBB, 0xBF, (byte)'h' },
        new byte[] { (byte)'a', (byte)'b', 0xFF, 0xFE, (byte)'h', 0 },
        // Byte order mark followed by a large value.
        new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Enumerable.Repeat((byte)'x', 70_000)).ToArray(),
    };

    [Theory]
    [MemberData(nameof(SpecialPayloads))]
    public async Task ReadAsStringAsync_SpecialPayloads_MatchesStreamReader(byte[] bytes)
    {
        foreach (var contentType in new[] { null, "text/plain; charset=utf-8" })
        {
            foreach (var readSizes in new int[]?[] { null, [1], [1, 1024], [2, 1024], [3, 1024], [1, 2, 4096] })
            {
                // The result must match what StreamReader produces when reading the same stream, including
                // its read-size dependent byte order mark detection.
                var expected = ReadWithStreamReader(CreateBody(bytes, readSizes), Encoding.UTF8);

                Assert.Equal(expected, await ReadAsStringAsync(bytes, contentType, readSizes));
            }
        }
    }

    [Fact]
    public async Task ReadAsStringAsync_Utf8ByteOrderMark_IsRemoved()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF, (byte)'h', (byte)'i' };

        Assert.Equal("hi", await ReadAsStringAsync(bytes, contentType: null, readSizes: null));
    }

    [Theory]
    [InlineData("text/plain; charset=iso-8859-1", "caf\u00e9", 28591)]
    [InlineData("text/plain; charset=utf-16", "caf\u00e9", 1200)]
    public async Task ReadAsStringAsync_UsesContentTypeCharset(string contentType, string text, int codePage)
    {
        var bytes = Encoding.GetEncoding(codePage).GetBytes(text);

        Assert.Equal(text, await ReadAsStringAsync(bytes, contentType, readSizes: null));
    }

    [Fact]
    public async Task ReadAsStringAsync_NullBody_Throws()
    {
        var section = new MultipartSection { Body = null! };

        await Assert.ThrowsAsync<ArgumentException>(async () => await section.ReadAsStringAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("text/plain; charset=iso-8859-1")]
    public async Task ReadAsStringAsync_BufferedSection_CanceledToken_MatchesStreamReader(string contentType)
    {
        const string Boundary = "9051914041544843365972754266";
        var body = "--" + Boundary + "\r\n" +
            "Content-Disposition: form-data; name=\"text\"\r\n" +
            "Content-Type: " + contentType + "\r\n" +
            "\r\n" +
            "text default\r\n" +
            "--" + Boundary + "--\r\n";
        var reader = new MultipartReader(Boundary, new MemoryStream(Encoding.UTF8.GetBytes(body)));
        var section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // StreamReader.ReadToEndAsync only observes the token through the underlying stream reads, and buffered
        // section bodies complete without checking it. The UTF-8 fast path must behave the same as the
        // StreamReader path used for other charsets.
        Assert.Equal("text default", await section.ReadAsStringAsync(cts.Token));
    }

    private static async Task<string> ReadAsStringAsync(byte[] bytes, string? contentType, int[]? readSizes)
    {
        var section = new MultipartSection
        {
            Body = CreateBody(bytes, readSizes),
            Headers = contentType is null ? new() : new() { ["Content-Type"] = new StringValues(contentType) },
        };

        return await section.ReadAsStringAsync(CancellationToken.None);
    }

    private static Stream CreateBody(byte[] bytes, int[]? readSizes)
    {
        Stream body = new NonSeekableReadStream(bytes);
        return readSizes is null ? body : new ChunkedReadStream(body, readSizes);
    }

    private static string ReadWithStreamReader(Stream stream, Encoding encoding)
    {
        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024);
        return reader.ReadToEnd();
    }

    // Limits each read to the next size in the list, repeating the last size.
    private sealed class ChunkedReadStream(Stream inner, int[] readSizes) : Stream
    {
        private int _readCount;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, NextCount(count));
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => inner.ReadAsync(buffer, offset, NextCount(count), cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => inner.ReadAsync(buffer.Slice(0, NextCount(buffer.Length)), cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private int NextCount(int count) => Math.Min(count, readSizes[Math.Min(_readCount++, readSizes.Length - 1)]);
    }
}
