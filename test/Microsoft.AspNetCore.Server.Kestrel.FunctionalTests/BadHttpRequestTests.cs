// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class BadHttpRequestTests
    {
        [Theory]
        [MemberData(nameof(InvalidRequestLineData))]
        public Task TestInvalidRequestLines(string request, string expectedExceptionMessage)
        {
            return TestBadRequest(
                request,
                "400 Bad Request",
                expectedExceptionMessage);
        }

        [Theory]
        [MemberData(nameof(UnrecognizedHttpVersionData))]
        public Task TestInvalidRequestLinesWithUnrecognizedVersion(string httpVersion)
        {
            return TestBadRequest(
                $"GET / {httpVersion}\r\n",
                "505 HTTP Version Not Supported",
                $"Unrecognized HTTP version: {httpVersion}");
        }

        [Theory]
        [MemberData(nameof(InvalidRequestHeaderData))]
        public Task TestInvalidHeaders(string rawHeaders, string expectedExceptionMessage)
        {
            return TestBadRequest(
                $"GET / HTTP/1.1\r\n{rawHeaders}",
                "400 Bad Request",
                expectedExceptionMessage);
        }

        [Theory]
        [InlineData("Hea\0der: value", "Invalid characters in header name.")]
        [InlineData("Header: va\0lue", "Malformed request: invalid headers.")]
        [InlineData("Head\x80r: value", "Invalid characters in header name.")]
        [InlineData("Header: valu\x80", "Malformed request: invalid headers.")]
        public Task BadRequestWhenHeaderNameContainsNonASCIIOrNullCharacters(string header, string expectedExceptionMessage)
        {
            return TestBadRequest(
                $"GET / HTTP/1.1\r\n{header}\r\n\r\n",
                "400 Bad Request",
                expectedExceptionMessage);
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        public Task BadRequestIfMethodRequiresLengthButNoContentLengthOrTransferEncodingInRequest(string method)
        {
            return TestBadRequest(
                $"{method} / HTTP/1.1\r\n\r\n",
                "411 Length Required",
                $"{method} request contains no Content-Length or Transfer-Encoding header");
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        public Task BadRequestIfMethodRequiresLengthButNoContentLengthInHttp10Request(string method)
        {
            return TestBadRequest(
                $"{method} / HTTP/1.0\r\n\r\n",
                "400 Bad Request",
                $"{method} request contains no Content-Length header");
        }

        [Theory]
        [InlineData("NaN")]
        [InlineData("-1")]
        public Task BadRequestIfContentLengthInvalid(string contentLength)
        {
            return TestBadRequest(
                $"POST / HTTP/1.1\r\nContent-Length: {contentLength}\r\n\r\n",
                "400 Bad Request",
                $"Invalid content length: {contentLength}");
        }

        private async Task TestBadRequest(string request, string expectedResponseStatusCode, string expectedExceptionMessage)
        {
            BadHttpRequestException loggedException = null;
            var mockKestrelTrace = new Mock<IKestrelTrace>();
            mockKestrelTrace
                .Setup(trace => trace.IsEnabled(LogLevel.Information))
                .Returns(true);
            mockKestrelTrace
                .Setup(trace => trace.ConnectionBadRequest(It.IsAny<string>(), It.IsAny<BadHttpRequestException>()))
                .Callback<string, BadHttpRequestException>((connectionId, exception) => loggedException = exception);

            using (var server = new TestServer(context => TaskCache.CompletedTask, new TestServiceContext { Log = mockKestrelTrace.Object }))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAll(request);
                    await ReceiveBadRequestResponse(connection, expectedResponseStatusCode, server.Context.DateHeaderValue);
                }
            }

            mockKestrelTrace.Verify(trace => trace.ConnectionBadRequest(It.IsAny<string>(), It.IsAny<BadHttpRequestException>()));
            Assert.Equal(expectedExceptionMessage, loggedException.Message);
        }

        private async Task ReceiveBadRequestResponse(TestConnection connection, string expectedResponseStatusCode, string expectedDateHeaderValue)
        {
            await connection.ReceiveForcedEnd(
                $"HTTP/1.1 {expectedResponseStatusCode}",
                "Connection: close",
                $"Date: {expectedDateHeaderValue}",
                "Content-Length: 0",
                "",
                "");
        }

        public static IEnumerable<object> InvalidRequestLineData => HttpParsingData.InvalidRequestLineData
            .Select(requestLine => new object[]
            {
                requestLine,
                $"Invalid request line: {requestLine.Replace("\r", "<0x0D>").Replace("\n", "<0x0A>")}",
            })
            .Concat(HttpParsingData.EncodedNullCharInTargetRequestLines.Select(requestLine => new object[]
            {
                requestLine,
                "Invalid request line."
            }))
            .Concat(HttpParsingData.NullCharInTargetRequestLines.Select(requestLine => new object[]
            {
                requestLine,
                "Invalid request line."
            }));

        public static TheoryData<string> UnrecognizedHttpVersionData => HttpParsingData.UnrecognizedHttpVersionData;

        public static IEnumerable<object[]> InvalidRequestHeaderData => HttpParsingData.InvalidRequestHeaderData;
    }
}
