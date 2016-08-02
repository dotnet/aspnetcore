// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class BadHttpRequestTests
    {
        // Don't send more data than necessary to fail, otherwise the test throws trying to
        // send data after the server has already closed the connection. This would cause the
        // test to fail on Windows, due to a winsock limitation: after the error when trying
        // to write to the socket closed by the server, winsock disposes all resources used
        // by that socket. The test then fails when we try to read the expected response
        // from the server because, although it would have been buffered, it got discarded
        // by winsock on the send() error.
        // The solution for this is for the client to always try to receive before doing
        // any sends, that way it can detect that the connection has been closed by the server
        // and not try to send() on the closed connection, triggering the error that would cause
        // any buffered received data to be lost.
        // We do not deem necessary to mitigate this issue in TestConnection, since it would only
        // be ensuring that we have a properly implemented HTTP client that can handle the
        // winsock issue. There is nothing to be verified in Kestrel in this situation.
        [Theory]
        // Incomplete request lines
        [InlineData("G")]
        [InlineData("GE")]
        [InlineData("GET")]
        [InlineData("GET ")]
        [InlineData("GET /")]
        [InlineData("GET / ")]
        [InlineData("GET / H")]
        [InlineData("GET / HT")]
        [InlineData("GET / HTT")]
        [InlineData("GET / HTTP")]
        [InlineData("GET / HTTP/")]
        [InlineData("GET / HTTP/1")]
        [InlineData("GET / HTTP/1.")]
        [InlineData("GET / HTTP/1.1")]
        [InlineData("GET / HTTP/1.1\r")]
        [InlineData("GET / HTTP/1.0")]
        [InlineData("GET / HTTP/1.0\r")]
        // Missing method
        [InlineData(" ")]
        // Missing second space
        [InlineData("/ ")] // This fails trying to read the '/' because that's invalid for an HTTP method
        [InlineData("GET /\r\n")]
        // Missing target
        [InlineData("GET  ")]
        // Missing version
        [InlineData("GET / \r")]
        // Missing CR
        [InlineData("GET / \n")]
        // Unrecognized HTTP version
        [InlineData("GET / http/1.0\r")]
        [InlineData("GET / http/1.1\r")]
        [InlineData("GET / HTTP/1.1 \r")]
        [InlineData("GET / HTTP/1.1a\r")]
        [InlineData("GET / HTTP/1.0\n\r")]
        [InlineData("GET / HTTP/1.2\r")]
        [InlineData("GET / HTTP/3.0\r")]
        [InlineData("GET / H\r")]
        [InlineData("GET / HTTP/1.\r")]
        [InlineData("GET / hello\r")]
        [InlineData("GET / 8charact\r")]
        // Missing LF
        [InlineData("GET / HTTP/1.0\rA")]
        // Bad HTTP Methods (invalid according to RFC)
        [InlineData("( / HTTP/1.0\r\n")]
        [InlineData(") / HTTP/1.0\r\n")]
        [InlineData("< / HTTP/1.0\r\n")]
        [InlineData("> / HTTP/1.0\r\n")]
        [InlineData("@ / HTTP/1.0\r\n")]
        [InlineData(", / HTTP/1.0\r\n")]
        [InlineData("; / HTTP/1.0\r\n")]
        [InlineData(": / HTTP/1.0\r\n")]
        [InlineData("\\ / HTTP/1.0\r\n")]
        [InlineData("\" / HTTP/1.0\r\n")]
        [InlineData("/ / HTTP/1.0\r\n")]
        [InlineData("[ / HTTP/1.0\r\n")]
        [InlineData("] / HTTP/1.0\r\n")]
        [InlineData("? / HTTP/1.0\r\n")]
        [InlineData("= / HTTP/1.0\r\n")]
        [InlineData("{ / HTTP/1.0\r\n")]
        [InlineData("} / HTTP/1.0\r\n")]
        [InlineData("get@ / HTTP/1.0\r\n")]
        [InlineData("post= / HTTP/1.0\r\n")]
        public async Task TestBadRequestLines(string request)
        {
            using (var server = new TestServer(context => TaskUtilities.CompletedTask))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAllTryEnd(request);
                    await ReceiveBadRequestResponse(connection, server.Context.DateHeaderValue);
                }
            }
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("GET  ")]
        [InlineData("GET / HTTP/1.2\r")]
        [InlineData("GET / HTTP/1.0\rA")]
        // Bad HTTP Methods (invalid according to RFC)
        [InlineData("( ")]
        [InlineData(") ")]
        [InlineData("< ")]
        [InlineData("> ")]
        [InlineData("@ ")]
        [InlineData(", ")]
        [InlineData("; ")]
        [InlineData(": ")]
        [InlineData("\\ ")]
        [InlineData("\" ")]
        [InlineData("/ ")]
        [InlineData("[ ")]
        [InlineData("] ")]
        [InlineData("? ")]
        [InlineData("= ")]
        [InlineData("{ ")]
        [InlineData("} ")]
        [InlineData("get@ ")]
        [InlineData("post= ")]
        public async Task ServerClosesConnectionAsSoonAsBadRequestLineIsDetected(string request)
        {
            using (var server = new TestServer(context => TaskUtilities.CompletedTask))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAll(request);
                    await ReceiveBadRequestResponse(connection, server.Context.DateHeaderValue);
                }
            }
        }

        [Theory]
        // Missing final CRLF
        [InlineData("Header-1: value1\r\nHeader-2: value2\r\n")]
        // Leading whitespace
        [InlineData(" Header-1: value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("\tHeader-1: value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\n Header-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\n\tHeader-2: value2\r\n\r\n")]
        // Missing LF
        [InlineData("Header-1: value1\rHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2: value2\r\r\n")]
        // Line folding
        [InlineData("Header-1: multi\r\n line\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2: multi\r\n line\r\n\r\n")]
        // Missing ':'
        [InlineData("Header-1 value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2 value2\r\n\r\n")]
        // Whitespace in header name
        [InlineData("Header 1: value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader 2: value2\r\n\r\n")]
        [InlineData("Header-1 : value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1\t: value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2 : value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2\t: value2\r\n\r\n")]
        public async Task TestInvalidHeaders(string rawHeaders)
        {
            using (var server = new TestServer(context => TaskUtilities.CompletedTask))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAllTryEnd($"GET / HTTP/1.1\r\n{rawHeaders}");
                    await ReceiveBadRequestResponse(connection, server.Context.DateHeaderValue);
                }
            }
        }

        [Fact]
        public async Task BadRequestWhenNameHeaderNamesContainsNonASCIICharacters()
        {
            using (var server = new TestServer(context => { return Task.FromResult(0); }))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAllTryEnd(
                        "GET / HTTP/1.1",
                        "H\u00eb\u00e4d\u00ebr: value",
                        "",
                        "");
                    await ReceiveBadRequestResponse(connection, server.Context.DateHeaderValue);
                }
            }
        }

        [Theory]
        [InlineData("\0")]
        [InlineData("%00")]
        [InlineData("/\0")]
        [InlineData("/%00")]
        [InlineData("/\0\0")]
        [InlineData("/%00%00")]
        [InlineData("/%C8\0")]
        [InlineData("/%E8%00%84")]
        [InlineData("/%E8%85%00")]
        [InlineData("/%F3%00%82%86")]
        [InlineData("/%F3%85%00%82")]
        [InlineData("/%F3%85%82%00")]
        [InlineData("/%E8%85%00")]
        [InlineData("/%E8%01%00")]
        public async Task BadRequestIfPathContainsNullCharacters(string path)
        {
            using (var server = new TestServer(context => { return Task.FromResult(0); }))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAllTryEnd($"GET {path} HTTP/1.1\r\n");
                    await ReceiveBadRequestResponse(connection, server.Context.DateHeaderValue);
                }
            }
        }

        private async Task ReceiveBadRequestResponse(TestConnection connection, string expectedDateHeaderValue)
        {
            await connection.Receive(
                "HTTP/1.1 400 Bad Request",
                "");
            await connection.Receive(
                "Connection: close",
                "");
            await connection.ReceiveForcedEnd(
                $"Date: {expectedDateHeaderValue}",
                "Content-Length: 0",
                "",
                "");
        }
    }
}
