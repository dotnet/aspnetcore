// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class HttpParserTests
    {
        private static IKestrelTrace _nullTrace = Mock.Of<IKestrelTrace>();

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
            var parser = CreateParser(_nullTrace);
            var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(requestLine));
            var requestHandler = new RequestHandler();

            Assert.True(parser.ParseRequestLine(requestHandler, buffer, out var consumed, out var examined));

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
            var parser = CreateParser(_nullTrace);
            var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(requestLine));
            var requestHandler = new RequestHandler();

            Assert.False(parser.ParseRequestLine(requestHandler, buffer, out var consumed, out var examined));
        }

        [Theory]
        [MemberData(nameof(RequestLineIncompleteData))]
        public void ParseRequestLineDoesNotConsumeIncompleteRequestLine(string requestLine)
        {
            var parser = CreateParser(_nullTrace);
            var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(requestLine));
            var requestHandler = new RequestHandler();

            Assert.False(parser.ParseRequestLine(requestHandler, buffer, out var consumed, out var examined));

            Assert.Equal(buffer.Start, consumed);
            Assert.True(buffer.Slice(examined).IsEmpty);
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
            var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(requestLine));
            var requestHandler = new RequestHandler();

            var exception = Assert.Throws<BadHttpRequestException>(() =>
                parser.ParseRequestLine(requestHandler, buffer, out var consumed, out var examined));

            Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestLine_Detail(requestLine.EscapeNonPrintable()), exception.Message);
            Assert.Equal(StatusCodes.Status400BadRequest, (exception as BadHttpRequestException).StatusCode);
        }

        [Theory]
        [MemberData(nameof(MethodWithNonTokenCharData))]
        public void ParseRequestLineThrowsOnNonTokenCharsInCustomMethod(string method)
        {
            var requestLine = $"{method} / HTTP/1.1\r\n";

            var mockTrace = new Mock<IKestrelTrace>();
            mockTrace
                .Setup(trace => trace.IsEnabled(LogLevel.Information))
                .Returns(true);

            var parser = CreateParser(mockTrace.Object);
            var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(requestLine));
            var requestHandler = new RequestHandler();

            var exception = Assert.Throws<BadHttpRequestException>(() =>
                parser.ParseRequestLine(requestHandler, buffer, out var consumed, out var examined));

            Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestLine_Detail(method.EscapeNonPrintable() + @" / HTTP/1.1\x0D\x0A"), exception.Message);
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
            var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(requestLine));
            var requestHandler = new RequestHandler();

            var exception = Assert.Throws<BadHttpRequestException>(() =>
                parser.ParseRequestLine(requestHandler, buffer, out var consumed, out var examined));

            Assert.Equal(CoreStrings.FormatBadRequest_UnrecognizedHTTPVersion(httpVersion), exception.Message);
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
            var parser = CreateParser(_nullTrace);

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
            var parser = CreateParser(_nullTrace);

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
            var parser = CreateParser(_nullTrace);

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
            var mockTrace = new Mock<IKestrelTrace>();
            mockTrace
                .Setup(trace => trace.IsEnabled(LogLevel.Information))
                .Returns(true);

            var parser = CreateParser(mockTrace.Object);
            var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(rawHeaders));
            var requestHandler = new RequestHandler();

            var exception = Assert.Throws<BadHttpRequestException>(() =>
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
            var mockTrace = new Mock<IKestrelTrace>();
            mockTrace
                .Setup(trace => trace.IsEnabled(LogLevel.Information))
                .Returns(false);

            var parser = CreateParser(mockTrace.Object);

            // Invalid request line
            var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes("GET % HTTP/1.1\r\n"));
            var requestHandler = new RequestHandler();

            var exception = Assert.Throws<BadHttpRequestException>(() =>
                parser.ParseRequestLine(requestHandler, buffer, out var consumed, out var examined));

            Assert.Equal("Invalid request line: ''", exception.Message);
            Assert.Equal(StatusCodes.Status400BadRequest, (exception as BadHttpRequestException).StatusCode);

            // Unrecognized HTTP version
            buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes("GET / HTTP/1.2\r\n"));

            exception = Assert.Throws<BadHttpRequestException>(() =>
                parser.ParseRequestLine(requestHandler, buffer, out var consumed, out var examined));

            Assert.Equal(CoreStrings.FormatBadRequest_UnrecognizedHTTPVersion(string.Empty), exception.Message);
            Assert.Equal(StatusCodes.Status505HttpVersionNotsupported, (exception as BadHttpRequestException).StatusCode);

            // Invalid request header
            buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes("Header: value\n\r\n"));

            exception = Assert.Throws<BadHttpRequestException>(() =>
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
            var parser = CreateParser(_nullTrace);
            var buffer = ReadOnlySequenceFactory.CreateSegments(
                Encoding.ASCII.GetBytes("GET "),
                Encoding.ASCII.GetBytes("/"));

            var requestHandler = new RequestHandler();
            var result = parser.ParseRequestLine(requestHandler, buffer, out var consumed, out var examined);

            Assert.False(result);
            Assert.Equal(buffer.Start, consumed);
            Assert.Equal(buffer.End, examined);
        }

        [Fact]
        public void ParseRequestLineTlsOverHttp()
        {
            var parser = CreateParser(_nullTrace);
            var buffer = ReadOnlySequenceFactory.CreateSegments(new byte[] { 0x16, 0x03, 0x01, 0x02, 0x00, 0x01, 0x00, 0xfc, 0x03, 0x03, 0x03, 0xca, 0xe0, 0xfd, 0x0a });

            var requestHandler = new RequestHandler();

            var badHttpRequestException = Assert.Throws<BadHttpRequestException>(() =>
            {
                parser.ParseRequestLine(requestHandler, buffer, out var consumed, out var examined);
            });

            Assert.Equal(badHttpRequestException.StatusCode, StatusCodes.Status400BadRequest);
            Assert.Equal(RequestRejectionReason.TlsOverHttpError, badHttpRequestException.Reason);
        }

        [Fact]
        public void ParseHeadersWithGratuitouslySplitBuffers()
        {
            var parser = CreateParser(_nullTrace);
            var buffer = BytePerSegmentTestSequenceFactory.Instance.CreateWithContent("Host:\r\nConnection: keep-alive\r\n\r\n");

            var requestHandler = new RequestHandler();
            var reader = new SequenceReader<byte>(buffer);
            var result = parser.ParseHeaders(requestHandler, ref reader);

            Assert.True(result);
        }

        [Fact]
        public void ParseHeadersWithGratuitouslySplitBuffers2()
        {
            var parser = CreateParser(_nullTrace);
            var buffer = BytePerSegmentTestSequenceFactory.Instance.CreateWithContent("A:B\r\nB: C\r\n\r\n");

            var requestHandler = new RequestHandler();
            var reader = new SequenceReader<byte>(buffer);
            var result = parser.ParseHeaders(requestHandler, ref reader);

            Assert.True(result);
        }

        private void VerifyHeader(
            string headerName,
            string rawHeaderValue,
            string expectedHeaderValue)
        {
            var parser = CreateParser(_nullTrace);
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

        private void VerifyRawHeaders(string rawHeaders, IEnumerable<string> expectedHeaderNames, IEnumerable<string> expectedHeaderValues)
        {
            Assert.True(expectedHeaderNames.Count() == expectedHeaderValues.Count(), $"{nameof(expectedHeaderNames)} and {nameof(expectedHeaderValues)} sizes must match");

            var parser = CreateParser(_nullTrace);
            var buffer = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(rawHeaders));

            var requestHandler = new RequestHandler();
            var reader = new SequenceReader<byte>(buffer);
            Assert.True(parser.ParseHeaders(requestHandler,ref reader));

            var parsedHeaders = requestHandler.Headers.ToArray();

            Assert.Equal(expectedHeaderNames.Count(), parsedHeaders.Length);
            Assert.Equal(expectedHeaderNames, parsedHeaders.Select(t => t.Key));
            Assert.Equal(expectedHeaderValues, parsedHeaders.Select(t => t.Value));
            Assert.True(buffer.Slice(reader.Position).IsEmpty);
        }

        private IHttpParser<RequestHandler> CreateParser(IKestrelTrace log) => new HttpParser<RequestHandler>(log.IsEnabled(LogLevel.Information));

        public static IEnumerable<object[]> RequestLineValidData => HttpParsingData.RequestLineValidData;

        public static IEnumerable<object[]> RequestLineIncompleteData => HttpParsingData.RequestLineIncompleteData.Select(requestLine => new[] { requestLine });

        public static IEnumerable<object[]> RequestLineInvalidData => HttpParsingData.RequestLineInvalidData.Select(requestLine => new[] { requestLine });

        public static IEnumerable<object[]> MethodWithNonTokenCharData => HttpParsingData.MethodWithNonTokenCharData.Select(method => new[] { method });

        public static TheoryData<string> UnrecognizedHttpVersionData => HttpParsingData.UnrecognizedHttpVersionData;

        public static IEnumerable<object[]> RequestHeaderInvalidData => HttpParsingData.RequestHeaderInvalidData;

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
}
