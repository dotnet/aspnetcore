// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
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
        [InlineData("/ HTTP/1.1\r\n\r\n")]
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
        public async Task TestBadRequestLines(string request)
        {
            using (var server = new TestServer(context => { return Task.FromResult(0); }))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(request);
                    await ReceiveBadRequestResponse(connection);
                }
            }
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("GET  ")]
        [InlineData("GET / HTTP/1.2\r")]
        [InlineData("GET / HTTP/1.0\rA")]
        public async Task ServerClosesConnectionAsSoonAsBadRequestLineIsDetected(string request)
        {
            using (var server = new TestServer(context => { return Task.FromResult(0); }))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.Send(request);
                    await ReceiveBadRequestResponse(connection);
                }
            }
        }

        private async Task ReceiveBadRequestResponse(TestConnection connection)
        {
            await connection.Receive(
                "HTTP/1.1 400 Bad Request",
                "");
            await connection.Receive(
                "Connection: close",
                "");
            await connection.ReceiveStartsWith("Date: ");
            await connection.ReceiveEnd(
                "Content-Length: 0",
                "Server: Kestrel",
                "",
                "");
        }
    }
}