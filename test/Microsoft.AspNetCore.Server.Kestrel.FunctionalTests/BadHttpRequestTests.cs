// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class BadHttpRequestTests
    {
        [Theory]
        [MemberData(nameof(InvalidRequestLineData))]
        public async Task TestInvalidRequestLines(string request)
        {
            using (var server = new TestServer(context => TaskCache.CompletedTask))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAll(request);
                    await ReceiveBadRequestResponse(connection, "400 Bad Request", server.Context.DateHeaderValue);
                }
            }
        }

        [Theory]
        [MemberData(nameof(UnrecognizedHttpVersionData))]
        public async Task TestInvalidRequestLinesWithUnrecognizedVersion(string httpVersion)
        {
            using (var server = new TestServer(context => TaskCache.CompletedTask))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAll($"GET / {httpVersion}\r\n");
                    await ReceiveBadRequestResponse(connection, "505 HTTP Version Not Supported", server.Context.DateHeaderValue);
                }
            }
        }

        [Theory]
        [MemberData(nameof(InvalidRequestHeaderData))]
        public async Task TestInvalidHeaders(string rawHeaders)
        {
            using (var server = new TestServer(context => TaskCache.CompletedTask))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAll($"GET / HTTP/1.1\r\n{rawHeaders}");
                    await ReceiveBadRequestResponse(connection, "400 Bad Request", server.Context.DateHeaderValue);
                }
            }
        }

        [Fact]
        public async Task BadRequestWhenHeaderNameContainsNonASCIICharacters()
        {
            using (var server = new TestServer(context => { return Task.FromResult(0); }))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAll(
                        "GET / HTTP/1.1",
                        "H\u00eb\u00e4d\u00ebr: value",
                        "",
                        "");
                    await ReceiveBadRequestResponse(connection, "400 Bad Request", server.Context.DateHeaderValue);
                }
            }
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        public async Task BadRequestIfMethodRequiresLengthButNoContentLengthOrTransferEncodingInRequest(string method)
        {
            using (var server = new TestServer(context => { return Task.FromResult(0); }))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send($"{method} / HTTP/1.1\r\n\r\n");
                    await ReceiveBadRequestResponse(connection, "411 Length Required", server.Context.DateHeaderValue);
                }
            }
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        public async Task BadRequestIfMethodRequiresLengthButNoContentLengthInHttp10Request(string method)
        {
            using (var server = new TestServer(context => { return Task.FromResult(0); }))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send($"{method} / HTTP/1.0\r\n\r\n");
                    await ReceiveBadRequestResponse(connection, "400 Bad Request", server.Context.DateHeaderValue);
                }
            }
        }

        [Theory]
        [InlineData("NaN")]
        [InlineData("-1")]
        public async Task BadRequestIfContentLengthInvalid(string contentLength)
        {
            using (var server = new TestServer(context => { return Task.FromResult(0); }))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAll($"GET / HTTP/1.1\r\nContent-Length: {contentLength}\r\n\r\n");
                    await ReceiveBadRequestResponse(connection, "400 Bad Request", server.Context.DateHeaderValue);
                }
            }
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

        public static IEnumerable<object> InvalidRequestLineData => HttpParsingData.InvalidRequestLineData.Select(data => new[] { data[0] });

        public static TheoryData<string> UnrecognizedHttpVersionData => HttpParsingData.UnrecognizedHttpVersionData;

        public static IEnumerable<object[]> InvalidRequestHeaderData => HttpParsingData.InvalidRequestHeaderData.Select(data => new[] { data[0] });
    }
}
