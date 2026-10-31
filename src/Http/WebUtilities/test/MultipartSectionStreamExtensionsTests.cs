// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.WebUtilities;

public class MultipartSectionStreamExtensionsTests
{
    private const string Boundary = "9051914041544843365972754266";

    public static TheoryData<string, byte[], string?> SectionValues
    {
        get
        {
            var data = new TheoryData<string, byte[], string?>
            {
                { "Empty", Array.Empty<byte>(), null },
                { "Ascii", Encoding.UTF8.GetBytes("hello world"), null },
                { "NonAscii", Encoding.UTF8.GetBytes("h\u00e9llo \u6fc0\u5149 \U0001F600"), null },
                { "Utf8Bom", [0xEF, 0xBB, 0xBF, .. "hello"u8], null },
                { "Utf8BomOnly", [0xEF, 0xBB, 0xBF], null },
                { "PartialUtf8Bom", [0xEF, 0xBB, 0x41], null },
                { "Utf16LEBom", [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("hi \u00e9")], null },
                { "Utf16BEBom", [.. Encoding.BigEndianUnicode.GetPreamble(), .. Encoding.BigEndianUnicode.GetBytes("hi \u00e9")], null },
                { "Utf32LEBom", [.. Encoding.UTF32.GetPreamble(), .. Encoding.UTF32.GetBytes("hi \u00e9")], null },
                { "Utf32BEBom", [0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0x00, 0x41], null },
                { "LeadingNul", [0x00, 0x41, 0x42], null },
                { "SingleFF", [0xFF], null },
                { "InvalidUtf8", [0x61, 0xC3, 0x28, 0x62], null },
                { "TruncatedUtf8", [0x61, 0xE2, 0x82], null },
                { "Utf16Charset", Encoding.Unicode.GetBytes("h\u00e9llo"), "text/plain; charset=utf-16" },
                { "Latin1Charset", Encoding.Latin1.GetBytes("h\u00e9llo"), "text/plain; charset=iso-8859-1" },
                { "Utf8Charset", Encoding.UTF8.GetBytes("h\u00e9llo"), "text/plain; charset=utf-8" },
            };

            foreach (var length in new[] { 1023, 1024, 1025, 2048, 5000 })
            {
                data.Add($"Ascii{length}", Encoding.UTF8.GetBytes(new string('a', length)), null);

                // Multi-byte characters straddle the 1024 byte boundary.
                data.Add($"NonAscii{length}", Encoding.UTF8.GetBytes(new string('a', length - 1) + "\u00e9\u6fc0"), null);
                data.Add($"Utf16Bom{length}", [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(new string('b', length))], null);
                data.Add($"TruncatedUtf8_{length}", [.. Encoding.UTF8.GetBytes(new string('c', length)), 0xE2, 0x82], null);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(SectionValues))]
    public async Task ReadAsStringAsync_MatchesStreamReader(string name, byte[] value, string? contentType)
    {
        Assert.NotNull(name);
        var expected = ReadWithStreamReader(value, contentType);

        foreach (var seekable in new[] { true, false })
        {
            var section = await ReadSingleSectionAsync(value, contentType, seekable);

            var result = await section.ReadAsStringAsync(CancellationToken.None);

            Assert.Equal(expected, result);
        }
    }

    [Theory]
    [MemberData(nameof(SectionValues))]
    public async Task ReadAsStringAsync_NonMultipartReaderBody_MatchesStreamReader(string name, byte[] value, string? contentType)
    {
        Assert.NotNull(name);
        var expected = ReadWithStreamReader(value, contentType);
        var section = new MultipartSection
        {
            Headers = contentType is null ? new() : new() { ["Content-Type"] = contentType },
            Body = new MemoryStream(value),
        };

        var result = await section.ReadAsStringAsync(CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ReadAsStringAsync_ReadsFollowingSections()
    {
        var body = Encoding.UTF8.GetBytes(
            $"--{Boundary}\r\nContent-Disposition: form-data; name=\"a\"\r\n\r\n{new string('a', 3000)}\r\n" +
            $"--{Boundary}\r\nContent-Disposition: form-data; name=\"b\"\r\n\r\nsecond\r\n" +
            $"--{Boundary}--\r\n");
        var reader = new MultipartReader(Boundary, new NonSeekableReadStream(body));

        var first = await reader.ReadNextSectionAsync();
        Assert.Equal(new string('a', 3000), await first!.ReadAsStringAsync(CancellationToken.None));
        var second = await reader.ReadNextSectionAsync();
        Assert.Equal("second", await second!.ReadAsStringAsync(CancellationToken.None));
        Assert.Null(await reader.ReadNextSectionAsync());
    }

    private static string ReadWithStreamReader(byte[] value, string? contentType)
    {
        // The pre-existing implementation of ReadAsStringAsync.
        var encoding = contentType is null ? null : Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType).Encoding;
        if (encoding is null || encoding.CodePage == 65000)
        {
            encoding = Encoding.UTF8;
        }

        using var reader = new StreamReader(new MemoryStream(value), encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024);
        return reader.ReadToEnd();
    }

    private static async Task<MultipartSection> ReadSingleSectionAsync(byte[] value, string? contentType, bool seekable)
    {
        var body = new MemoryStream();
        body.Write(Encoding.UTF8.GetBytes($"--{Boundary}\r\nContent-Disposition: form-data; name=\"value\"\r\n"));
        if (contentType is not null)
        {
            body.Write(Encoding.UTF8.GetBytes($"Content-Type: {contentType}\r\n"));
        }
        body.Write("\r\n"u8);
        body.Write(value);
        body.Write(Encoding.UTF8.GetBytes($"\r\n--{Boundary}--\r\n"));
        body.Position = 0;

        var reader = new MultipartReader(Boundary, seekable ? body : new NonSeekableReadStream(body));
        var section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);
        return section;
    }
}
