// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Http;

public class HttpResponseWritingExtensionsTests
{
    [Fact]
    public async Task WritingText_WriteText()
    {
        HttpContext context = CreateRequest();
        await context.Response.WriteAsync("Hello World");

        Assert.Equal(11, context.Response.Body.Length);
    }

    [Fact]
    public async Task WritingText_MultipleWrites()
    {
        HttpContext context = CreateRequest();
        await context.Response.WriteAsync("Hello World");
        await context.Response.WriteAsync("Hello World");

        Assert.Equal(22, context.Response.Body.Length);
    }

    [Theory]
    [MemberData(nameof(Encodings))]
    public async Task WritingTextThatRequiresMultipleSegmentsWorks(Encoding encoding)
    {
        var outputStream = new MemoryStream();

        HttpContext context = new DefaultHttpContext();
        context.Response.Body = outputStream;

        var inputString = string.Concat(Enumerable.Repeat("昨日すき焼きを食べました", 1000));
        var expected = encoding.GetBytes(inputString);
        await context.Response.WriteAsync(inputString, encoding);

        outputStream.Position = 0;
        var actual = new byte[expected.Length];
        var length = outputStream.Read(actual);

        Assert.Equal(expected.Length, length);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(Encodings))]
    public async Task WritingTextWithPassedInEncodingWorks(Encoding encoding)
    {
        HttpContext context = CreateRequest();

        var inputString = "昨日すき焼きを食べました";
        var expected = encoding.GetBytes(inputString);
        await context.Response.WriteAsync(inputString, encoding);

        context.Response.Body.Position = 0;
        var actual = new byte[expected.Length * 2];
        var length = context.Response.Body.Read(actual);

        var actualShortened = new byte[length];
        Array.Copy(actual, actualShortened, length);

        Assert.Equal(expected.Length, length);
        Assert.Equal(expected, actualShortened);
    }

    public static TheoryData<Encoding> Encodings =>
        new TheoryData<Encoding>
        {
                        { Encoding.ASCII },
                        { Encoding.BigEndianUnicode },
                        { Encoding.Unicode },
                        { Encoding.UTF32 },
#pragma warning disable CS0618, SYSLIB0001 // Type or member is obsolete
                        { Encoding.UTF7 },
#pragma warning restore CS0618, SYSLIB0001 // Type or member is obsolete
                        { Encoding.UTF8 }
        };

    private HttpContext CreateRequest()
    {
        HttpContext context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }
}
