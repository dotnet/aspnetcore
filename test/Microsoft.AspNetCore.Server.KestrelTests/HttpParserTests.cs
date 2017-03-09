// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class HttpParserTests
    {
        // Returns true when all headers parsed
        // Return false otherwise

        [Theory]
        [MemberData(nameof(RequestLineValidData))]
        public void ParsesRequestLine(
            string requestLine,
            string expectedMethod,
            string expectedRawTarget,
            string expectedRawPath,
            string expectedDecodedPath,
            string expectedQueryString,
            string expectedVersion)
        {
            var parser = CreateParser(Mock.Of<IKestrelTrace>());
            var buffer = ReadableBuffer.Create(Encoding.ASCII.GetBytes(requestLine));

            string parsedMethod = null;
            string parsedVersion = null;
            string parsedRawTarget = null;
            string parsedRawPath = null;
            string parsedQuery = null;
            var requestLineHandler = new Mock<IHttpRequestLineHandler>();
            requestLineHandler
                .Setup(handler => handler.OnStartLine(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<HttpVersion>(),
                    It.IsAny<Span<byte>>(),
                    It.IsAny<Span<byte>>(),
                    It.IsAny<Span<byte>>(),
                    It.IsAny<Span<byte>>(),
                    It.IsAny<bool>()))
                .Callback<HttpMethod, HttpVersion, Span<byte>, Span<byte>, Span<byte>, Span<byte>, bool>((method, version, target, path, query, customMethod, pathEncoded) =>
                {
                    parsedMethod = method != HttpMethod.Custom ? HttpUtilities.MethodToString(method) : customMethod.GetAsciiStringNonNullCharacters();
                    parsedVersion = HttpUtilities.VersionToString(version);
                    parsedRawTarget = target.GetAsciiStringNonNullCharacters();
                    parsedRawPath = path.GetAsciiStringNonNullCharacters();
                    parsedQuery = query.GetAsciiStringNonNullCharacters();
                    pathEncoded = false;
                });

            Assert.True(parser.ParseRequestLine(requestLineHandler.Object, buffer, out var consumed, out var examined));

            Assert.Equal(parsedMethod, expectedMethod);
            Assert.Equal(parsedVersion, expectedVersion);
            Assert.Equal(parsedRawTarget, expectedRawTarget);
            Assert.Equal(parsedRawPath, expectedRawPath);
            Assert.Equal(parsedVersion, expectedVersion);
            Assert.Equal(buffer.End, consumed);
            Assert.Equal(buffer.End, examined);
        }

        [Theory]
        [MemberData(nameof(RequestLineIncompleteData))]
        public void ParseRequestLineReturnsFalseWhenGivenIncompleteRequestLines(string requestLine)
        {
            var parser = CreateParser(Mock.Of<IKestrelTrace>());
            var buffer = ReadableBuffer.Create(Encoding.ASCII.GetBytes(requestLine));

            Assert.False(parser.ParseRequestLine(Mock.Of<IHttpRequestLineHandler>(), buffer, out var consumed, out var examined));
        }

        [Theory]
        [MemberData(nameof(RequestLineIncompleteData))]
        public void ParseRequestLineDoesNotConsumeIncompleteRequestLine(string requestLine)
        {
            var parser = CreateParser(Mock.Of<IKestrelTrace>());
            var buffer = ReadableBuffer.Create(Encoding.ASCII.GetBytes(requestLine));

            Assert.False(parser.ParseRequestLine(Mock.Of<IHttpRequestLineHandler>(), buffer, out var consumed, out var examined));

            Assert.Equal(buffer.Start, consumed);
            Assert.Equal(buffer.End, examined);
        }

        [Theory]
        [MemberData(nameof(RequestLineInvalidData))]
        public void ParseRequestLineThrowsOnInvalidRequestLine(string requestLine)
        {
            var mockTrace = new Mock<IKestrelTrace>();
            mockTrace
                .Setup(trace => trace.IsEnabled(LogLevel.Information))
                .Returns(true);

            var parser = CreateParser(mockTrace.Object);
            var buffer = ReadableBuffer.Create(Encoding.ASCII.GetBytes(requestLine));

            var exception = Assert.Throws<BadHttpRequestException>(() =>
                parser.ParseRequestLine(Mock.Of<IHttpRequestLineHandler>(), buffer, out var consumed, out var examined));

            Assert.Equal($"Invalid request line: '{requestLine.Replace("\r", "\\x0D").Replace("\n", "\\x0A")}'", exception.Message);
            Assert.Equal(StatusCodes.Status400BadRequest, (exception as BadHttpRequestException).StatusCode);
        }

        [Theory]
        [MemberData(nameof(UnrecognizedHttpVersionData))]
        public void ParseRequestLineThrowsOnUnrecognizedHttpVersion(string httpVersion)
        {
            var requestLine = $"GET / {httpVersion}\r\n";

            var mockTrace = new Mock<IKestrelTrace>();
            mockTrace
                .Setup(trace => trace.IsEnabled(LogLevel.Information))
                .Returns(true);

            var parser = CreateParser(mockTrace.Object);
            var buffer = ReadableBuffer.Create(Encoding.ASCII.GetBytes(requestLine));

            var exception = Assert.Throws<BadHttpRequestException>(() =>
                parser.ParseRequestLine(Mock.Of<IHttpRequestLineHandler>(), buffer, out var consumed, out var examined));

            Assert.Equal($"Unrecognized HTTP version: {httpVersion}", exception.Message);
            Assert.Equal(StatusCodes.Status505HttpVersionNotsupported, (exception as BadHttpRequestException).StatusCode);
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
            var parser = CreateParser(Mock.Of<IKestrelTrace>());

            var buffer = ReadableBuffer.Create(Encoding.ASCII.GetBytes(rawHeaders));
            Assert.False(parser.ParseHeaders(Mock.Of<IHttpHeadersHandler>(), buffer, out var consumed, out var examined, out var consumedBytes));
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
            var parser = CreateParser(Mock.Of<IKestrelTrace>());

            var buffer = ReadableBuffer.Create(Encoding.ASCII.GetBytes(rawHeaders));
            parser.ParseHeaders(Mock.Of<IHttpHeadersHandler>(), buffer, out var consumed, out var examined, out var consumedBytes);

            Assert.Equal(buffer.Start, consumed);
            Assert.Equal(buffer.End, examined);
            Assert.Equal(0, consumedBytes);
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

            VerifyRawHeaders(rawHeaders, expectedHeaderNames, expectedHeaderValues);
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
            var parser = CreateParser(Mock.Of<IKestrelTrace>());

            const string headerLine = "Header: value\r\n\r";
            var buffer1 = ReadableBuffer.Create(Encoding.ASCII.GetBytes(headerLine));
            Assert.False(parser.ParseHeaders(Mock.Of<IHttpHeadersHandler>(), buffer1, out var consumed, out var examined, out var consumedBytes));

            Assert.Equal(buffer1.Move(buffer1.Start, headerLine.Length - 1), consumed);
            Assert.Equal(buffer1.End, examined);
            Assert.Equal(headerLine.Length - 1, consumedBytes);

            var buffer2 = ReadableBuffer.Create(Encoding.ASCII.GetBytes("\r\n"));
            Assert.True(parser.ParseHeaders(Mock.Of<IHttpHeadersHandler>(), buffer2, out consumed, out examined, out consumedBytes));

            Assert.Equal(buffer2.End, consumed);
            Assert.Equal(buffer2.End, examined);
            Assert.Equal(2, consumedBytes);
        }

        [Theory]
        [MemberData(nameof(RequestHeaderInvalidData))]
        public void ParseHeadersThrowsOnInvalidRequestHeaders(string rawHeaders, string expectedExceptionMessage)
        {
            var mockTrace = new Mock<IKestrelTrace>();
            mockTrace
                .Setup(trace => trace.IsEnabled(LogLevel.Information))
                .Returns(true);

            var parser = CreateParser(mockTrace.Object);
            var buffer = ReadableBuffer.Create(Encoding.ASCII.GetBytes(rawHeaders));

            var exception = Assert.Throws<BadHttpRequestException>(() =>
                parser.ParseHeaders(Mock.Of<IHttpHeadersHandler>(), buffer, out var consumed, out var examined, out var consumedBytes));

            Assert.Equal(expectedExceptionMessage, exception.Message);
            Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        }

        private void VerifyHeader(
            string headerName,
            string rawHeaderValue,
            string expectedHeaderValue)
        {
            var parser = CreateParser(Mock.Of<IKestrelTrace>());
            var buffer = ReadableBuffer.Create(Encoding.ASCII.GetBytes($"{headerName}:{rawHeaderValue}\r\n"));

            string parsedHeaderName = "unexpected";
            string parsedHeaderValue = "unexpected";
            var headersHandler = new Mock<IHttpHeadersHandler>();
            headersHandler
                .Setup(handler => handler.OnHeader(It.IsAny<Span<byte>>(), It.IsAny<Span<byte>>()))
                .Callback<Span<byte>, Span<byte>>((name, value) =>
                {
                    parsedHeaderName = name.GetAsciiStringNonNullCharacters();
                    parsedHeaderValue = value.GetAsciiStringNonNullCharacters();
                });

            parser.ParseHeaders(headersHandler.Object, buffer, out var consumed, out var examined, out var consumedBytes);

            Assert.Equal(headerName, parsedHeaderName);
            Assert.Equal(expectedHeaderValue, parsedHeaderValue);
            Assert.Equal(buffer.End, consumed);
            Assert.Equal(buffer.End, examined);
        }

        private void VerifyRawHeaders(string rawHeaders, IEnumerable<string> expectedHeaderNames, IEnumerable<string> expectedHeaderValues)
        {
            Assert.True(expectedHeaderNames.Count() == expectedHeaderValues.Count(), $"{nameof(expectedHeaderNames)} and {nameof(expectedHeaderValues)} sizes must match");

            var parser = CreateParser(Mock.Of<IKestrelTrace>());
            var buffer = ReadableBuffer.Create(Encoding.ASCII.GetBytes(rawHeaders));

            var parsedHeaders = new List<Tuple<string, string>>();
            var headersHandler = new Mock<IHttpHeadersHandler>();
            headersHandler
                .Setup(handler => handler.OnHeader(It.IsAny<Span<byte>>(), It.IsAny<Span<byte>>()))
                .Callback<Span<byte>, Span<byte>>((name, value) =>
                {
                    parsedHeaders.Add(Tuple.Create(name.GetAsciiStringNonNullCharacters(), value.GetAsciiStringNonNullCharacters()));
                });

            parser.ParseHeaders(headersHandler.Object, buffer, out var consumed, out var examined, out var consumedBytes);

            Assert.Equal(expectedHeaderNames.Count(), parsedHeaders.Count);
            Assert.Equal(expectedHeaderNames, parsedHeaders.Select(t => t.Item1));
            Assert.Equal(expectedHeaderValues, parsedHeaders.Select(t => t.Item2));
            Assert.Equal(buffer.End, consumed);
            Assert.Equal(buffer.End, examined);
        }

        private IHttpParser CreateParser(IKestrelTrace log) => new KestrelHttpParser(log);

        public static IEnumerable<string[]> RequestLineValidData => HttpParsingData.RequestLineValidData;

        public static IEnumerable<object[]> RequestLineIncompleteData => HttpParsingData.RequestLineIncompleteData.Select(requestLine => new[] { requestLine });

        public static IEnumerable<object[]> RequestLineInvalidData => HttpParsingData.RequestLineInvalidData.Select(requestLine => new[] { requestLine });

        public static TheoryData<string> UnrecognizedHttpVersionData => HttpParsingData.UnrecognizedHttpVersionData;

        public static IEnumerable<object[]> RequestHeaderInvalidData => HttpParsingData.RequestHeaderInvalidData;
    }
}
