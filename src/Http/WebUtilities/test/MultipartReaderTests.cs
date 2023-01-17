// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable warnings

using System.Text;

namespace Microsoft.AspNetCore.WebUtilities;

public class MultipartReaderTests
{
    private const string Boundary = "9051914041544843365972754266";
    private const string BoundaryWithQuotes = @"""9051914041544843365972754266""";
    // Note that CRLF (\r\n) is required. You can't use multi-line C# strings here because the line breaks on Linux are just LF.
    private const string OnePartBody =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266--\r\n";
    private const string OnePartBodyTwoHeaders =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"Custom-header: custom-value\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266--\r\n";
    private const string OnePartBodyWithTrailingWhitespace =
"--9051914041544843365972754266             \r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266--\r\n";
    // It's non-compliant but common to leave off the last CRLF.
    private const string OnePartBodyWithoutFinalCRLF =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266--";
    private const string TwoPartBody =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"file1\"; filename=\"a.txt\"\r\n" +
"Content-Type: text/plain\r\n" +
"\r\n" +
"Content of a.txt.\r\n" +
"\r\n" +
"--9051914041544843365972754266--\r\n";
    private const string TwoPartBodyWithUnicodeFileName =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"file1\"; filename=\"a色.txt\"\r\n" +
"Content-Type: text/plain\r\n" +
"\r\n" +
"Content of a.txt.\r\n" +
"\r\n" +
"--9051914041544843365972754266--\r\n";
    private const string ThreePartBody =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"file1\"; filename=\"a.txt\"\r\n" +
"Content-Type: text/plain\r\n" +
"\r\n" +
"Content of a.txt.\r\n" +
"\r\n" +
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"file2\"; filename=\"a.html\"\r\n" +
"Content-Type: text/html\r\n" +
"\r\n" +
"<!DOCTYPE html><title>Content of a.html.</title>\r\n" +
"\r\n" +
"--9051914041544843365972754266--\r\n";

    private const string TwoPartBodyIncompleteBuffer =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"file1\"; filename=\"a.txt\"\r\n" +
"Content-Type: text/plain\r\n" +
"\r\n" +
"Content of a.txt.\r\n" +
"\r\n" +
"--9051914041544843365";

    private static MemoryStream MakeStream(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }

    private static string GetString(byte[] buffer, int count)
    {
        return Encoding.ASCII.GetString(buffer, 0, count);
    }

    [Fact]
    public async Task MultipartReader_ReadSinglePartBody_Success()
    {
        var stream = MakeStream(OnePartBody);
        var reader = new MultipartReader(Boundary, stream);

        var section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);
        Assert.Single(section.Headers);
        Assert.Equal("form-data; name=\"text\"", section.Headers["Content-Disposition"][0]);
        var buffer = new MemoryStream();
        await section.Body.CopyToAsync(buffer);
        Assert.Equal("text default", Encoding.ASCII.GetString(buffer.ToArray()));

        Assert.Null(await reader.ReadNextSectionAsync());
    }

    [Fact]
    public async Task MultipartReader_HeaderCountExceeded_Throws()
    {
        var stream = MakeStream(OnePartBodyTwoHeaders);
        var reader = new MultipartReader(Boundary, stream)
        {
            HeadersCountLimit = 1,
        };

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => reader.ReadNextSectionAsync());
        Assert.Equal("Multipart headers count limit 1 exceeded.", exception.Message);
    }

    [Fact]
    public async Task MultipartReader_HeadersLengthExceeded_Throws()
    {
        var stream = MakeStream(OnePartBodyTwoHeaders);
        var reader = new MultipartReader(Boundary, stream)
        {
            HeadersLengthLimit = 60,
        };

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => reader.ReadNextSectionAsync());
        Assert.Equal("Line length limit 17 exceeded.", exception.Message);
    }

    [Fact]
    public async Task MultipartReader_ReadSinglePartBodyWithTrailingWhitespace_Success()
    {
        var stream = MakeStream(OnePartBodyWithTrailingWhitespace);
        var reader = new MultipartReader(Boundary, stream);

        var section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);
        Assert.Single(section.Headers);
        Assert.Equal("form-data; name=\"text\"", section.Headers["Content-Disposition"][0]);
        var buffer = new MemoryStream();
        await section.Body.CopyToAsync(buffer);
        Assert.Equal("text default", Encoding.ASCII.GetString(buffer.ToArray()));

        Assert.Null(await reader.ReadNextSectionAsync());
    }

    [Fact]
    public async Task MultipartReader_ReadSinglePartBodyWithoutLastCRLF_Success()
    {
        var stream = MakeStream(OnePartBodyWithoutFinalCRLF);
        var reader = new MultipartReader(Boundary, stream);

        var section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);
        Assert.Single(section.Headers);
        Assert.Equal("form-data; name=\"text\"", section.Headers["Content-Disposition"][0]);
        var buffer = new MemoryStream();
        await section.Body.CopyToAsync(buffer);
        Assert.Equal("text default", Encoding.ASCII.GetString(buffer.ToArray()));

        Assert.Null(await reader.ReadNextSectionAsync());
    }

    [Fact]
    public async Task MultipartReader_ReadTwoPartBody_Success()
    {
        var stream = MakeStream(TwoPartBody);
        var reader = new MultipartReader(Boundary, stream);

        var section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);
        Assert.Single(section.Headers);
        Assert.Equal("form-data; name=\"text\"", section.Headers["Content-Disposition"][0]);
        var buffer = new MemoryStream();
        await section.Body.CopyToAsync(buffer);
        Assert.Equal("text default", Encoding.ASCII.GetString(buffer.ToArray()));

        section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);
        Assert.Equal(2, section.Headers.Count);
        Assert.Equal("form-data; name=\"file1\"; filename=\"a.txt\"", section.Headers["Content-Disposition"][0]);
        Assert.Equal("text/plain", section.Headers["Content-Type"][0]);
        buffer = new MemoryStream();
        await section.Body.CopyToAsync(buffer);
        Assert.Equal("Content of a.txt.\r\n", Encoding.ASCII.GetString(buffer.ToArray()));

        Assert.Null(await reader.ReadNextSectionAsync());
    }

    [Fact]
    public async Task MultipartReader_ReadTwoPartBodyWithUnicodeFileName_Success()
    {
        var stream = MakeStream(TwoPartBodyWithUnicodeFileName);
        var reader = new MultipartReader(Boundary, stream);

        var section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);
        Assert.Single(section.Headers);
        Assert.Equal("form-data; name=\"text\"", section.Headers["Content-Disposition"][0]);
        var buffer = new MemoryStream();
        await section.Body.CopyToAsync(buffer);
        Assert.Equal("text default", Encoding.ASCII.GetString(buffer.ToArray()));

        section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);
        Assert.Equal(2, section.Headers.Count);
        Assert.Equal("form-data; name=\"file1\"; filename=\"a色.txt\"", section.Headers["Content-Disposition"][0]);
        Assert.Equal("text/plain", section.Headers["Content-Type"][0]);
        buffer = new MemoryStream();
        await section.Body.CopyToAsync(buffer);
        Assert.Equal("Content of a.txt.\r\n", Encoding.ASCII.GetString(buffer.ToArray()));

        Assert.Null(await reader.ReadNextSectionAsync());
    }

    [Fact]
    public async Task MultipartReader_ThreePartBody_Success()
    {
        var stream = MakeStream(ThreePartBody);
        var reader = new MultipartReader(Boundary, stream);

        var section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);
        Assert.Single(section.Headers);
        Assert.Equal("form-data; name=\"text\"", section.Headers["Content-Disposition"][0]);
        var buffer = new MemoryStream();
        await section.Body.CopyToAsync(buffer);
        Assert.Equal("text default", Encoding.ASCII.GetString(buffer.ToArray()));

        section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);
        Assert.Equal(2, section.Headers.Count);
        Assert.Equal("form-data; name=\"file1\"; filename=\"a.txt\"", section.Headers["Content-Disposition"][0]);
        Assert.Equal("text/plain", section.Headers["Content-Type"][0]);
        buffer = new MemoryStream();
        await section.Body.CopyToAsync(buffer);
        Assert.Equal("Content of a.txt.\r\n", Encoding.ASCII.GetString(buffer.ToArray()));

        section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);
        Assert.Equal(2, section.Headers.Count);
        Assert.Equal("form-data; name=\"file2\"; filename=\"a.html\"", section.Headers["Content-Disposition"][0]);
        Assert.Equal("text/html", section.Headers["Content-Type"][0]);
        buffer = new MemoryStream();
        await section.Body.CopyToAsync(buffer);
        Assert.Equal("<!DOCTYPE html><title>Content of a.html.</title>\r\n", Encoding.ASCII.GetString(buffer.ToArray()));

        Assert.Null(await reader.ReadNextSectionAsync());
    }

    [Fact]
    public void MultipartReader_BufferSizeMustBeLargerThanBoundary_Throws()
    {
        var stream = MakeStream(ThreePartBody);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var reader = new MultipartReader(Boundary, stream, 5);
        });
    }

    [Fact]
    public async Task MultipartReader_TwoPartBodyIncompleteBuffer_TwoSectionsReadSuccessfullyThirdSectionThrows()
    {
        var stream = MakeStream(TwoPartBodyIncompleteBuffer);
        var reader = new MultipartReader(Boundary, stream);
        var buffer = new byte[128];

        //first section can be read successfully
        var section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);
        Assert.Single(section.Headers);
        Assert.Equal("form-data; name=\"text\"", section.Headers["Content-Disposition"][0]);
        var read = section.Body.Read(buffer, 0, buffer.Length);
        Assert.Equal("text default", GetString(buffer, read));

        //second section can be read successfully (even though the bottom boundary is truncated)
        section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);
        Assert.Equal(2, section.Headers.Count);
        Assert.Equal("form-data; name=\"file1\"; filename=\"a.txt\"", section.Headers["Content-Disposition"][0]);
        Assert.Equal("text/plain", section.Headers["Content-Type"][0]);
        read = section.Body.Read(buffer, 0, buffer.Length);
        Assert.Equal("Content of a.txt.\r\n", GetString(buffer, read));

        await Assert.ThrowsAsync<IOException>(async () =>
        {
            // we'll be unable to ensure enough bytes are buffered to even contain a final boundary
            section = await reader.ReadNextSectionAsync();
        });
    }

    [Fact]
    public async Task MultipartReader_ReadInvalidUtf8Header_ReplacementCharacters()
    {
        var body1 =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\" filename=\"a";

        var body2 =
".txt\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266--\r\n";
        var stream = new MemoryStream();
        var bytes = Encoding.UTF8.GetBytes(body1);
        stream.Write(bytes, 0, bytes.Length);

        // Write an invalid utf-8 segment in the middle
        stream.Write(new byte[] { 0xC1, 0x21 }, 0, 2);

        bytes = Encoding.UTF8.GetBytes(body2);
        stream.Write(bytes, 0, bytes.Length);
        stream.Seek(0, SeekOrigin.Begin);
        var reader = new MultipartReader(Boundary, stream);

        var section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);
        Assert.Single(section.Headers);
        Assert.Equal("form-data; name=\"text\" filename=\"a\uFFFD!.txt\"", section.Headers["Content-Disposition"][0]);
        var buffer = new MemoryStream();
        await section.Body.CopyToAsync(buffer);
        Assert.Equal("text default", Encoding.ASCII.GetString(buffer.ToArray()));

        Assert.Null(await reader.ReadNextSectionAsync());
    }

    [Fact]
    public async Task MultipartReader_ReadInvalidUtf8SurrogateHeader_ReplacementCharacters()
    {
        var body1 =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\" filename=\"a";

        var body2 =
".txt\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266--\r\n";
        var stream = new MemoryStream();
        var bytes = Encoding.UTF8.GetBytes(body1);
        stream.Write(bytes, 0, bytes.Length);

        // Write an invalid utf-8 segment in the middle
        stream.Write(new byte[] { 0xED, 0xA0, 85 }, 0, 3);

        bytes = Encoding.UTF8.GetBytes(body2);
        stream.Write(bytes, 0, bytes.Length);
        stream.Seek(0, SeekOrigin.Begin);
        var reader = new MultipartReader(Boundary, stream);

        var section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);
        Assert.Single(section.Headers);
        Assert.Equal("form-data; name=\"text\" filename=\"a\uFFFD\uFFFDU.txt\"", section.Headers["Content-Disposition"][0]);
        var buffer = new MemoryStream();
        await section.Body.CopyToAsync(buffer);
        Assert.Equal("text default", Encoding.ASCII.GetString(buffer.ToArray()));

        Assert.Null(await reader.ReadNextSectionAsync());
    }

    // MultiPartReader should strip any quotes from the boundary passed in instead of throwing an exception
    [Fact]
    public async Task MultipartReader_StripQuotesFromBoundary()
    {
        var stream = MakeStream(OnePartBody);
        var reader = new MultipartReader(BoundaryWithQuotes, stream);

        var section = await reader.ReadNextSectionAsync();
        Assert.NotNull(section);
    }
}
