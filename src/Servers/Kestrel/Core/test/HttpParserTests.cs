// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class HttpParserTests : LoggedTest
{
    private static readonly KestrelTrace _nullTrace = new KestrelTrace(NullLoggerFactory.Instance);
    private KestrelTrace CreateEnabledTrace() => new KestrelTrace(LoggerFactory);

    [Theory]
    [MemberData(nameof(RequestLineValidData))]
    public void ParsesRequestLine(
        string requestLine,
        string expectedMethod,
        string expectedRawTarget,
        string expectedRawPath,
            // This warns that theory methods should use all of their parameters,
            // but this method is using a shared data collection with Http1ConnectionTests.TakeStartLineSetsHttpProtocolProperties and others.
#pragma warning disable xUnit1026
            string expectedDecodedPath,
        string expectedQueryString,
#pragma warning restore xUnit1026
            string expectedVersion)
    {
        var parser = CreateParser(_nullTrace, false);
        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(requestLine));
        var requestHandler = new RequestHandler();

        Assert.True(ParseRequestLine(parser, requestHandler, buffer, out var consumed, out var examined));

        Assert.Equal(requestHandler.Method, expectedMethod);
        Assert.Equal(requestHandler.Version, expectedVersion);
        Assert.Equal(requestHandler.RawTarget, expectedRawTarget);
        Assert.Equal(requestHandler.RawPath, expectedRawPath);
        Assert.Equal(requestHandler.Version, expectedVersion);
        Assert.True(buffer.Slice(consumed).IsEmpty);
        Assert.True(buffer.Slice(examined).IsEmpty);
    }

    [Theory]
    [MemberData(nameof(RequestLineIncompleteData))]
    public void ParseRequestLineReturnsFalseWhenGivenIncompleteRequestLines(string requestLine)
    {
        var parser = CreateParser(_nullTrace, false);
        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(requestLine));
        var requestHandler = new RequestHandler();

        Assert.False(ParseRequestLine(parser, requestHandler, buffer, out var consumed, out var examined));
    }

    [Theory]
    [MemberData(nameof(RequestLineIncompleteData))]
    public void ParseRequestLineDoesNotConsumeIncompleteRequestLine(string requestLine)
    {
        var parser = CreateParser(_nullTrace, false);
        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(requestLine));
        var requestHandler = new RequestHandler();

        Assert.False(ParseRequestLine(parser, requestHandler, buffer, out var consumed, out var examined));

        Assert.Equal(buffer.Start, consumed);
        Assert.True(buffer.Slice(examined).IsEmpty);
    }

    [Theory]
    [MemberData(nameof(RequestLineInvalidData))]
    public void ParseRequestLineThrowsOnInvalidRequestLine(string requestLine)
    {
        var parser = CreateParser(CreateEnabledTrace());
        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(requestLine));
        var requestHandler = new RequestHandler();

#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Assert.Throws<BadHttpRequestException>(() =>
#pragma warning restore CS0618 // Type or member is obsolete
        ParseRequestLine(parser, requestHandler, buffer, out var consumed, out var examined));

        Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestLine_Detail(requestLine[..^1].EscapeNonPrintable()), exception.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Theory]
    [MemberData(nameof(RequestLineInvalidDataLineFeedTerminator))]
    public void ParseRequestSucceedsOnInvalidRequestLineLineFeedTerminator(string requestLine)
    {
        var parser = CreateParser(CreateEnabledTrace(), disableHttp1LineFeedTerminators: false);
        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(requestLine));
        var requestHandler = new RequestHandler();

        Assert.True(ParseRequestLine(parser, requestHandler, buffer, out var consumed, out var examined));
    }

    [Theory]
    [MemberData(nameof(RequestLineInvalidDataLineFeedTerminator))]
    public void ParseRequestLineThrowsOnInvalidRequestLineLineFeedTerminator(string requestLine)
    {
        var parser = CreateParser(CreateEnabledTrace());
        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(requestLine));
        var requestHandler = new RequestHandler();

#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Assert.Throws<BadHttpRequestException>(() =>
#pragma warning restore CS0618 // Type or member is obsolete
            ParseRequestLine(parser, requestHandler, buffer, out var consumed, out var examined));

        Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestLine_Detail(requestLine[..^1].EscapeNonPrintable()), exception.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Theory]
    [MemberData(nameof(MethodWithNonTokenCharData))]
    public void ParseRequestLineThrowsOnNonTokenCharsInCustomMethod(string method)
    {
        var requestLine = $"{method} / HTTP/1.1\r\n";

        var parser = CreateParser(CreateEnabledTrace(), false);
        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(requestLine));
        var requestHandler = new RequestHandler();

#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Assert.Throws<BadHttpRequestException>(() =>
#pragma warning restore CS0618 // Type or member is obsolete
            ParseRequestLine(parser, requestHandler, buffer, out var consumed, out var examined));

        Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestLine_Detail(method.EscapeNonPrintable() + @" / HTTP/1.1\x0D"), exception.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Theory]
    [MemberData(nameof(UnrecognizedHttpVersionData))]
    public void ParseRequestLineThrowsOnUnrecognizedHttpVersion(string httpVersion)
    {
        var requestLine = $"GET / {httpVersion}\r\n";

        var parser = CreateParser(CreateEnabledTrace(), false);
        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(requestLine));
        var requestHandler = new RequestHandler();

#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Assert.Throws<BadHttpRequestException>(() =>
#pragma warning restore CS0618 // Type or member is obsolete
            ParseRequestLine(parser, requestHandler, buffer, out var consumed, out var examined));

        Assert.Equal(CoreStrings.FormatBadRequest_UnrecognizedHTTPVersion(httpVersion), exception.Message);
        Assert.Equal(StatusCodes.Status505HttpVersionNotsupported, exception.StatusCode);
    }

    [Fact]
    public void StartOfPathNotFound()
    {
        var requestLine = $"GET \n";

        var parser = CreateParser(CreateEnabledTrace(), false);
        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(requestLine));
        var requestHandler = new RequestHandler();

#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Assert.Throws<BadHttpRequestException>(() =>
#pragma warning restore CS0618 // Type or member is obsolete
            ParseRequestLine(parser, requestHandler, buffer, out var consumed, out var examined));

        Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestLine_Detail("GET "), exception.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("H")]
    [InlineData("He")]
    [InlineData("Hea")]
    [InlineData("Head")]
    [InlineData("Heade")]
    [InlineData("Header")]
    [InlineData("Header:")]
    [InlineData("Header: ")]
    [InlineData("Header: v")]
    [InlineData("Header: va")]
    [InlineData("Header: val")]
    [InlineData("Header: valu")]
    [InlineData("Header: value")]
    [InlineData("Header: value\r")]
    [InlineData("Header: value\r\n")]
    [InlineData("Header: value\r\n\r")]
    [InlineData("Header-1: value1\r\nH")]
    [InlineData("Header-1: value1\r\nHe")]
    [InlineData("Header-1: value1\r\nHea")]
    [InlineData("Header-1: value1\r\nHead")]
    [InlineData("Header-1: value1\r\nHeade")]
    [InlineData("Header-1: value1\r\nHeader")]
    [InlineData("Header-1: value1\r\nHeader-")]
    [InlineData("Header-1: value1\r\nHeader-2")]
    [InlineData("Header-1: value1\r\nHeader-2:")]
    [InlineData("Header-1: value1\r\nHeader-2: ")]
    [InlineData("Header-1: value1\r\nHeader-2: v")]
    [InlineData("Header-1: value1\r\nHeader-2: va")]
    [InlineData("Header-1: value1\r\nHeader-2: val")]
    [InlineData("Header-1: value1\r\nHeader-2: valu")]
    [InlineData("Header-1: value1\r\nHeader-2: value")]
    [InlineData("Header-1: value1\r\nHeader-2: value2")]
    [InlineData("Header-1: value1\r\nHeader-2: value2\r")]
    [InlineData("Header-1: value1\r\nHeader-2: value2\r\n")]
    [InlineData("Header-1: value1\r\nHeader-2: value2\r\n\r")]
    public void ParseHeadersReturnsFalseWhenGivenIncompleteHeaders(string rawHeaders)
    {
        var parser = CreateParser(_nullTrace, false);

        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(rawHeaders));
        var requestHandler = new RequestHandler();
        var reader = new SequenceReader<byte>(buffer);
        Assert.False(parser.ParseHeaders(requestHandler, ref reader));
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("H")]
    [InlineData("He")]
    [InlineData("Hea")]
    [InlineData("Head")]
    [InlineData("Heade")]
    [InlineData("Header")]
    [InlineData("Header:")]
    [InlineData("Header: ")]
    [InlineData("Header: v")]
    [InlineData("Header: va")]
    [InlineData("Header: val")]
    [InlineData("Header: valu")]
    [InlineData("Header: value")]
    [InlineData("Header: value\r")]
    public void ParseHeadersDoesNotConsumeIncompleteHeader(string rawHeaders)
    {
        var parser = CreateParser(_nullTrace, false);

        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(rawHeaders));
        var requestHandler = new RequestHandler();
        var reader = new SequenceReader<byte>(buffer);

        Assert.False(parser.ParseHeaders(requestHandler, ref reader));

        Assert.Equal(buffer.Length, buffer.Slice(reader.Consumed).Length);
        Assert.Equal(0, reader.Consumed);
    }

    [Fact]
    public void ParseHeadersCanReadHeaderValueWithoutLeadingWhitespace()
    {
        VerifyHeader("Header", "value", "value");
    }

    [Theory]
    [InlineData("Cookie: \r\n\r\n", "Cookie", "", null, null)]
    [InlineData("Cookie:\r\n\r\n", "Cookie", "", null, null)]
    [InlineData("Cookie: \r\nConnection: close\r\n\r\n", "Cookie", "", "Connection", "close")]
    [InlineData("Cookie:\r\nConnection: close\r\n\r\n", "Cookie", "", "Connection", "close")]
    [InlineData("Connection: close\r\nCookie: \r\n\r\n", "Connection", "close", "Cookie", "")]
    [InlineData("Connection: close\r\nCookie:\r\n\r\n", "Connection", "close", "Cookie", "")]
    [InlineData("a:b\r\n\r\n", "a", "b", null, null)]
    [InlineData("a: b\r\n\r\n", "a", "b", null, null)]
    public void ParseHeadersCanParseEmptyHeaderValues(
        string rawHeaders,
        string expectedHeaderName1,
        string expectedHeaderValue1,
        string expectedHeaderName2,
        string expectedHeaderValue2)
    {
        var expectedHeaderNames = expectedHeaderName2 == null
            ? new[] { expectedHeaderName1 }
            : new[] { expectedHeaderName1, expectedHeaderName2 };
        var expectedHeaderValues = expectedHeaderValue2 == null
            ? new[] { expectedHeaderValue1 }
            : new[] { expectedHeaderValue1, expectedHeaderValue2 };

        VerifyRawHeaders(rawHeaders, expectedHeaderNames, expectedHeaderValues, disableHttp1LineFeedTerminators: false);
    }

    [Theory]
    [InlineData("Cookie: \n\r\n", "Cookie", "", null, null)]
    [InlineData("Cookie:\n\r\n", "Cookie", "", null, null)]
    [InlineData("Cookie: \nConnection: close\r\n\r\n", "Cookie", "", "Connection", "close")]
    [InlineData("Cookie: \r\nConnection: close\n\r\n", "Cookie", "", "Connection", "close")]
    [InlineData("Cookie:\nConnection: close\r\n\r\n", "Cookie", "", "Connection", "close")]
    [InlineData("Cookie:\r\nConnection: close\n\r\n", "Cookie", "", "Connection", "close")]
    [InlineData("Connection: close\nCookie: \r\n\r\n", "Connection", "close", "Cookie", "")]
    [InlineData("Connection: close\r\nCookie: \n\r\n", "Connection", "close", "Cookie", "")]
    [InlineData("Connection: close\nCookie:\r\n\r\n", "Connection", "close", "Cookie", "")]
    [InlineData("Connection: close\r\nCookie:\n\r\n", "Connection", "close", "Cookie", "")]
    [InlineData("a:b\n\r\n", "a", "b", null, null)]
    [InlineData("a: b\n\r\n", "a", "b", null, null)]
    [InlineData("a:b\n\n", "a", "b", null, null)]
    [InlineData("a: b\n\n", "a", "b", null, null)]
    public void ParseHeadersCantParseSingleLineFeedWihtoutLineFeedTerminatorEnabled(
        string rawHeaders,
        string expectedHeaderName1,
        string expectedHeaderValue1,
        string expectedHeaderName2,
        string expectedHeaderValue2)
    {
        var expectedHeaderNames = expectedHeaderName2 == null
            ? new[] { expectedHeaderName1 }
            : new[] { expectedHeaderName1, expectedHeaderName2 };
        var expectedHeaderValues = expectedHeaderValue2 == null
            ? new[] { expectedHeaderValue1 }
            : new[] { expectedHeaderValue1, expectedHeaderValue2 };

#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Throws<BadHttpRequestException>(() => VerifyRawHeaders(rawHeaders, expectedHeaderNames, expectedHeaderValues, disableHttp1LineFeedTerminators: true));
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Theory]
    [InlineData("Cookie: \n\r\n", "Cookie", "", null, null)]
    [InlineData("Cookie:\n\r\n", "Cookie", "", null, null)]
    [InlineData("Cookie: \nConnection: close\r\n\r\n", "Cookie", "", "Connection", "close")]
    [InlineData("Cookie: \r\nConnection: close\n\r\n", "Cookie", "", "Connection", "close")]
    [InlineData("Cookie:\nConnection: close\r\n\r\n", "Cookie", "", "Connection", "close")]
    [InlineData("Cookie:\r\nConnection: close\n\r\n", "Cookie", "", "Connection", "close")]
    [InlineData("Connection: close\nCookie: \r\n\r\n", "Connection", "close", "Cookie", "")]
    [InlineData("Connection: close\r\nCookie: \n\r\n", "Connection", "close", "Cookie", "")]
    [InlineData("Connection: close\nCookie:\r\n\r\n", "Connection", "close", "Cookie", "")]
    [InlineData("Connection: close\r\nCookie:\n\r\n", "Connection", "close", "Cookie", "")]
    public void ParseHeadersCanParseSingleLineFeedWithLineFeedTerminatorEnabled(
        string rawHeaders,
        string expectedHeaderName1,
        string expectedHeaderValue1,
        string expectedHeaderName2,
        string expectedHeaderValue2)
    {
        var expectedHeaderNames = expectedHeaderName2 == null
            ? new[] { expectedHeaderName1 }
            : new[] { expectedHeaderName1, expectedHeaderName2 };
        var expectedHeaderValues = expectedHeaderValue2 == null
            ? new[] { expectedHeaderValue1 }
            : new[] { expectedHeaderValue1, expectedHeaderValue2 };

        VerifyRawHeaders(rawHeaders, expectedHeaderNames, expectedHeaderValues, disableHttp1LineFeedTerminators: false);
    }

    [Theory]
    [InlineData("a: b\r\n\n", "a", "b", null, null)]
    [InlineData("a: b\n\n", "a", "b", null, null)]
    [InlineData("a: b\nc: d\r\n\n", "a", "b", "c", "d")]
    [InlineData("a: b\nc: d\n\n", "a", "b", "c", "d")]
    public void ParseHeadersCantEndWithLineFeedTerminator(
        string rawHeaders,
        string expectedHeaderName1,
        string expectedHeaderValue1,
        string expectedHeaderName2,
        string expectedHeaderValue2)
    {
        var expectedHeaderNames = expectedHeaderName2 == null
            ? new[] { expectedHeaderName1 }
            : new[] { expectedHeaderName1, expectedHeaderName2 };
        var expectedHeaderValues = expectedHeaderValue2 == null
            ? new[] { expectedHeaderValue1 }
            : new[] { expectedHeaderValue1, expectedHeaderValue2 };

#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Throws<BadHttpRequestException>(() => VerifyRawHeaders(rawHeaders, expectedHeaderNames, expectedHeaderValues, disableHttp1LineFeedTerminators: true));
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Theory]
    [InlineData("a:b\n\r\n", "a", "b", null, null)]
    [InlineData("a: b\n\r\n", "a", "b", null, null)]
    [InlineData("a: b\nc: d\n\r\n", "a", "b", "c", "d")]
    [InlineData("a: b\nc: d\n\n", "a", "b", "c", "d")]
    [InlineData("a: b\n\n", "a", "b", null, null)]
    public void ParseHeadersCanEndAfterLineFeedTerminator(
                string rawHeaders,
                string expectedHeaderName1,
                string expectedHeaderValue1,
                string expectedHeaderName2,
                string expectedHeaderValue2)
    {
        var expectedHeaderNames = expectedHeaderName2 == null
            ? new[] { expectedHeaderName1 }
            : new[] { expectedHeaderName1, expectedHeaderName2 };
        var expectedHeaderValues = expectedHeaderValue2 == null
            ? new[] { expectedHeaderValue1 }
            : new[] { expectedHeaderValue1, expectedHeaderValue2 };

        VerifyRawHeaders(rawHeaders, expectedHeaderNames, expectedHeaderValues, disableHttp1LineFeedTerminators: false);
    }

    [Theory]
    [InlineData(" value")]
    [InlineData("  value")]
    [InlineData("\tvalue")]
    [InlineData(" \tvalue")]
    [InlineData("\t value")]
    [InlineData("\t\tvalue")]
    [InlineData("\t\t value")]
    [InlineData(" \t\tvalue")]
    [InlineData(" \t\t value")]
    [InlineData(" \t \t value")]
    public void ParseHeadersDoesNotIncludeLeadingWhitespaceInHeaderValue(string rawHeaderValue)
    {
        VerifyHeader("Header", rawHeaderValue, "value");
    }

    [Theory]
    [InlineData("value ")]
    [InlineData("value\t")]
    [InlineData("value \t")]
    [InlineData("value\t ")]
    [InlineData("value\t\t")]
    [InlineData("value\t\t ")]
    [InlineData("value \t\t")]
    [InlineData("value \t\t ")]
    [InlineData("value \t \t ")]
    public void ParseHeadersDoesNotIncludeTrailingWhitespaceInHeaderValue(string rawHeaderValue)
    {
        VerifyHeader("Header", rawHeaderValue, "value");
    }

    [Theory]
    [InlineData("one two three")]
    [InlineData("one  two  three")]
    [InlineData("one\ttwo\tthree")]
    [InlineData("one two\tthree")]
    [InlineData("one\ttwo three")]
    [InlineData("one \ttwo \tthree")]
    [InlineData("one\t two\t three")]
    [InlineData("one \ttwo\t three")]
    public void ParseHeadersPreservesWhitespaceWithinHeaderValue(string headerValue)
    {
        VerifyHeader("Header", headerValue, headerValue);
    }

    [Fact]
    public void ParseHeadersConsumesBytesCorrectlyAtEnd()
    {
        var parser = CreateParser(_nullTrace, false);

        const string headerLine = "Header: value\r\n\r";
        var buffer1 = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(headerLine));
        var requestHandler = new RequestHandler();
        var reader1 = new SequenceReader<byte>(buffer1);
        Assert.False(parser.ParseHeaders(requestHandler, ref reader1));

        Assert.Equal(buffer1.GetPosition(headerLine.Length - 1), reader1.Position);
        Assert.Equal(headerLine.Length - 1, reader1.Consumed);

        var buffer2 = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes("\r\n"));
        var reader2 = new SequenceReader<byte>(buffer2);
        Assert.True(parser.ParseHeaders(requestHandler, ref reader2));

        Assert.True(buffer2.Slice(reader2.Position).IsEmpty);
        Assert.Equal(2, reader2.Consumed);
    }

    [Theory]
    [MemberData(nameof(RequestHeaderInvalidData))]
    public void ParseHeadersThrowsOnInvalidRequestHeaders(string rawHeaders, string expectedExceptionMessage)
    {
        var parser = CreateParser(CreateEnabledTrace(), false);
        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(rawHeaders));
        var requestHandler = new RequestHandler();

#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Assert.Throws<BadHttpRequestException>(() =>
#pragma warning restore CS0618 // Type or member is obsolete
        {
            var reader = new SequenceReader<byte>(buffer);
            parser.ParseHeaders(requestHandler, ref reader);
        });

        Assert.Equal(expectedExceptionMessage, exception.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Theory]
    [MemberData(nameof(RequestHeaderInvalidDataLineFeedTerminator))]
    public void ParseHeadersThrowsOnInvalidRequestHeadersLineFeedTerminator(string rawHeaders, string expectedExceptionMessage)
    {
        var parser = CreateParser(CreateEnabledTrace(), true);
        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(rawHeaders));
        var requestHandler = new RequestHandler();

#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Assert.Throws<BadHttpRequestException>(() =>
#pragma warning restore CS0618 // Type or member is obsolete
        {
            var reader = new SequenceReader<byte>(buffer);
            parser.ParseHeaders(requestHandler, ref reader);
        });

        Assert.Equal(expectedExceptionMessage, exception.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public void ExceptionDetailNotIncludedWhenLogLevelInformationNotEnabled()
    {
        var parser = CreateParser(_nullTrace);

        // Invalid request line
        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes("GET % HTTP/1.1\r\n"));
        var requestHandler = new RequestHandler();

#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Assert.Throws<BadHttpRequestException>(() =>
#pragma warning restore CS0618 // Type or member is obsolete
            ParseRequestLine(parser, requestHandler, buffer, out var consumed, out var examined));

        Assert.Equal("Invalid request line: ''", exception.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);

        // Unrecognized HTTP version
        buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes("GET / HTTP/1.2\r\n"));

#pragma warning disable CS0618 // Type or member is obsolete
        exception = Assert.Throws<BadHttpRequestException>(() =>
#pragma warning restore CS0618 // Type or member is obsolete
            ParseRequestLine(parser, requestHandler, buffer, out var consumed, out var examined));

        Assert.Equal(CoreStrings.FormatBadRequest_UnrecognizedHTTPVersion(string.Empty), exception.Message);
        Assert.Equal(StatusCodes.Status505HttpVersionNotsupported, exception.StatusCode);

        // Invalid request header
        buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes("Header: value\n\r\n"));

#pragma warning disable CS0618 // Type or member is obsolete
        exception = Assert.Throws<BadHttpRequestException>(() =>
#pragma warning restore CS0618 // Type or member is obsolete
        {
            var reader = new SequenceReader<byte>(buffer);
            parser.ParseHeaders(requestHandler, ref reader);
        });

        Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(string.Empty), exception.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public void ParseRequestLineSplitBufferWithoutNewLineDoesNotUpdateConsumed()
    {
        var parser = CreateParser(_nullTrace, false);
        var buffer = ReadOnlySequenceFactory.CreateSegments(
            Encoding.ASCII.GetBytes("GET "),
            Encoding.ASCII.GetBytes("/"));

        var requestHandler = new RequestHandler();
        var result = ParseRequestLine(parser, requestHandler, buffer, out var consumed, out var examined);

        Assert.False(result);
        Assert.Equal(buffer.Start, consumed);
        Assert.Equal(buffer.End, examined);
    }

    [Fact]
    public void ParseRequestLineTlsOverHttp()
    {
        var parser = CreateParser(_nullTrace, false);
        var buffer = ReadOnlySequenceFactory.CreateSegments(new byte[] { 0x16, 0x03, 0x01, 0x02, 0x00, 0x01, 0x00, 0xfc, 0x03, 0x03, 0x03, 0xca, 0xe0, 0xfd, 0x0a });

        var requestHandler = new RequestHandler();

#pragma warning disable CS0618 // Type or member is obsolete
        var badHttpRequestException = Assert.Throws<BadHttpRequestException>(() =>
#pragma warning restore CS0618 // Type or member is obsolete
        {
            ParseRequestLine(parser, requestHandler, buffer, out var consumed, out var examined);
        });

        Assert.Equal(StatusCodes.Status400BadRequest, badHttpRequestException.StatusCode);
        Assert.Equal(RequestRejectionReason.TlsOverHttpError, badHttpRequestException.Reason);
    }

    [Theory]
    [MemberData(nameof(RequestHeaderInvalidData))]
    public void ParseHeadersThrowsOnInvalidRequestHeadersWithGratuitouslySplitBuffers(string rawHeaders, string expectedExceptionMessage)
    {
        var parser = CreateParser(CreateEnabledTrace(), false);
        var buffer = BytePerSegmentTestSequenceFactory.Instance.CreateWithContent(rawHeaders);
        var requestHandler = new RequestHandler();

#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Assert.Throws<BadHttpRequestException>(() =>
#pragma warning restore CS0618 // Type or member is obsolete
        {
            var reader = new SequenceReader<byte>(buffer);
            parser.ParseHeaders(requestHandler, ref reader);
        });

        Assert.Equal(expectedExceptionMessage, exception.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Theory]
    [MemberData(nameof(RequestHeaderInvalidDataLineFeedTerminator))]
    public void ParseHeadersThrowsOnInvalidRequestHeadersWithGratuitouslySplitBuffersLineFeedTerminator(string rawHeaders, string expectedExceptionMessage)
    {
        var parser = CreateParser(CreateEnabledTrace(), true);
        var buffer = BytePerSegmentTestSequenceFactory.Instance.CreateWithContent(rawHeaders);
        var requestHandler = new RequestHandler();

#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Assert.Throws<BadHttpRequestException>(() =>
#pragma warning restore CS0618 // Type or member is obsolete
        {
            var reader = new SequenceReader<byte>(buffer);
            parser.ParseHeaders(requestHandler, ref reader);
        });

        Assert.Equal(expectedExceptionMessage, exception.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Theory]
    [InlineData("Host:\r\nConnection: keep-alive\r\n\r\n")]
    [InlineData("A:B\r\nB: C\r\n\r\n")]
    public void ParseHeadersWithGratuitouslySplitBuffers(string headers)
    {
        var parser = CreateParser(_nullTrace, false);
        var buffer = BytePerSegmentTestSequenceFactory.Instance.CreateWithContent(headers);

        var requestHandler = new RequestHandler();
        var reader = new SequenceReader<byte>(buffer);
        var result = parser.ParseHeaders(requestHandler, ref reader);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Host: \r\nConnection: keep-alive\r")]
    public void ParseHeaderLineIncompleteDataWithGratuitouslySplitBuffers(string headers)
    {
        var parser = CreateParser(_nullTrace, false);
        var buffer = BytePerSegmentTestSequenceFactory.Instance.CreateWithContent(headers);

        var requestHandler = new RequestHandler();
        var reader = new SequenceReader<byte>(buffer);
        var result = parser.ParseHeaders(requestHandler, ref reader);

        Assert.False(result);
    }

    [Theory]
    [InlineData("Host: \r\nConnection: keep-alive\r")]
    public void ParseHeaderLineIncompleteData(string headers)
    {
        var parser = CreateParser(_nullTrace, false);
        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(headers));

        var requestHandler = new RequestHandler();
        var reader = new SequenceReader<byte>(buffer);
        var result = parser.ParseHeaders(requestHandler, ref reader);

        Assert.False(result);
    }

    [Theory]
    [InlineData("Host:\nConnection: keep-alive\r\n\r\n")]
    [InlineData("Host:\r\nConnection: keep-alive\n\r\n")]
    [InlineData("A:B\nB: C\r\n\r\n")]
    [InlineData("A:B\r\nB: C\n\r\n")]
    [InlineData("Host:\r\nConnection: keep-alive\n\n")]
    public void ParseHeadersWithGratuitouslySplitBuffersQuirkMode(string headers)
    {
        var parser = CreateParser(_nullTrace, disableHttp1LineFeedTerminators: false);
        var buffer = BytePerSegmentTestSequenceFactory.Instance.CreateWithContent(headers);

        var requestHandler = new RequestHandler();
        var reader = new SequenceReader<byte>(buffer);
        var result = parser.ParseHeaders(requestHandler, ref reader);

        Assert.True(result);
    }

    private bool ParseRequestLine(IHttpParser<RequestHandler> parser, RequestHandler requestHandler, ReadOnlySequence<byte> readableBuffer, out SequencePosition consumed, out SequencePosition examined)
    {
        var reader = new SequenceReader<byte>(readableBuffer);
        if (parser.ParseRequestLine(requestHandler, ref reader))
        {
            consumed = reader.Position;
            examined = reader.Position;
            return true;
        }
        else
        {
            consumed = reader.Position;
            examined = readableBuffer.End;
            return false;
        }
    }

    private void VerifyHeader(
        string headerName,
        string rawHeaderValue,
        string expectedHeaderValue)
    {
        var parser = CreateParser(_nullTrace, false);
        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"{headerName}:{rawHeaderValue}\r\n"));

        var requestHandler = new RequestHandler();
        var reader = new SequenceReader<byte>(buffer);
        Assert.False(parser.ParseHeaders(requestHandler, ref reader));

        var pairs = requestHandler.Headers.ToArray();
        Assert.Single(pairs);
        Assert.Equal(headerName, pairs[0].Key);
        Assert.Equal(expectedHeaderValue, pairs[0].Value);
        Assert.True(buffer.Slice(reader.Position).IsEmpty);
    }

    private void VerifyRawHeaders(string rawHeaders, IEnumerable<string> expectedHeaderNames, IEnumerable<string> expectedHeaderValues, bool disableHttp1LineFeedTerminators = true)
    {
        Assert.True(expectedHeaderNames.Count() == expectedHeaderValues.Count(), $"{nameof(expectedHeaderNames)} and {nameof(expectedHeaderValues)} sizes must match");

        var parser = CreateParser(_nullTrace, disableHttp1LineFeedTerminators);
        var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(rawHeaders));

        var requestHandler = new RequestHandler();
        var reader = new SequenceReader<byte>(buffer);
        Assert.True(parser.ParseHeaders(requestHandler, ref reader));

        var parsedHeaders = requestHandler.Headers.ToArray();

        Assert.Equal(expectedHeaderNames.Count(), parsedHeaders.Length);
        Assert.Equal(expectedHeaderNames, parsedHeaders.Select(t => t.Key));
        Assert.Equal(expectedHeaderValues, parsedHeaders.Select(t => t.Value));
        Assert.True(buffer.Slice(reader.Position).IsEmpty);
    }

    private IHttpParser<RequestHandler> CreateParser(KestrelTrace log, bool disableHttp1LineFeedTerminators = true) => new HttpParser<RequestHandler>(log.IsEnabled(LogLevel.Information), disableHttp1LineFeedTerminators);

    public static IEnumerable<object[]> RequestLineValidData => HttpParsingData.RequestLineValidData;

    public static IEnumerable<object[]> RequestLineIncompleteData => HttpParsingData.RequestLineIncompleteData.Select(requestLine => new[] { requestLine });

    public static IEnumerable<object[]> RequestLineInvalidDataLineFeedTerminator => HttpParsingData.RequestLineInvalidDataLineFeedTerminator.Select(requestLine => new[] { requestLine });

    public static IEnumerable<object[]> RequestLineInvalidData => HttpParsingData.RequestLineInvalidData.Select(requestLine => new[] { requestLine });

    public static IEnumerable<object[]> MethodWithNonTokenCharData => HttpParsingData.MethodWithNonTokenCharData.Select(method => new[] { method });

    public static TheoryData<string> UnrecognizedHttpVersionData => HttpParsingData.UnrecognizedHttpVersionData;

    public static IEnumerable<object[]> RequestHeaderInvalidData => HttpParsingData.RequestHeaderInvalidData;

    public static IEnumerable<object[]> RequestHeaderInvalidDataLineFeedTerminator => HttpParsingData.RequestHeaderInvalidDataLineFeedTerminator;

    private class RequestHandler : IHttpRequestLineHandler, IHttpHeadersHandler
    {
        public string Method { get; set; }

        public string Version { get; set; }

        public string RawTarget { get; set; }

        public string RawPath { get; set; }

        public string Query { get; set; }

        public bool PathEncoded { get; set; }

        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            Headers[name.GetAsciiStringNonNullCharacters()] = value.GetAsciiStringNonNullCharacters();
        }

        void IHttpHeadersHandler.OnHeadersComplete(bool endStream) { }

        public void OnStartLine(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded)
        {
            Method = method != HttpMethod.Custom ? HttpUtilities.MethodToString(method) : customMethod.GetAsciiStringNonNullCharacters();
            Version = HttpUtilities.VersionToString(version);
            RawTarget = target.GetAsciiStringNonNullCharacters();
            RawPath = path.GetAsciiStringNonNullCharacters();
            Query = query.GetAsciiStringNonNullCharacters();
            PathEncoded = pathEncoded;
        }

        public void OnStartLine(HttpVersionAndMethod versionAndMethod, TargetOffsetPathLength targetPath, Span<byte> startLine)
        {
            var method = versionAndMethod.Method;
            var version = versionAndMethod.Version;
            var customMethod = startLine[..versionAndMethod.MethodEnd];
            var targetStart = targetPath.Offset;
            var target = startLine[targetStart..];
            var path = target[..targetPath.Length];
            var query = target[targetPath.Length..];

            Method = method != HttpMethod.Custom ? HttpUtilities.MethodToString(method) : customMethod.GetAsciiStringNonNullCharacters();
            Version = HttpUtilities.VersionToString(version);
            RawTarget = target.GetAsciiStringNonNullCharacters();
            RawPath = path.GetAsciiStringNonNullCharacters();
            Query = query.GetAsciiStringNonNullCharacters();
            PathEncoded = targetPath.IsEncoded;
        }

        public void OnStaticIndexedHeader(int index)
        {
            throw new NotImplementedException();
        }

        public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
        {
            throw new NotImplementedException();
        }
    }

    // Doesn't put empty blocks in between every byte
    internal class BytePerSegmentTestSequenceFactory : ReadOnlySequenceFactory
    {
        public static ReadOnlySequenceFactory Instance { get; } = new HttpParserTests.BytePerSegmentTestSequenceFactory();

        public override ReadOnlySequence<byte> CreateOfSize(int size)
        {
            return CreateWithContent(new byte[size]);
        }

        public override ReadOnlySequence<byte> CreateWithContent(byte[] data)
        {
            var segments = new List<byte[]>();

            foreach (var b in data)
            {
                segments.Add(new[] { b });
            }

            return CreateSegments(segments.ToArray());
        }
    }
}
