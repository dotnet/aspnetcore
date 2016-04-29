// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class BadHttpRequestTests
    {
        [Theory]
        [InlineData("/ HTTP/1.1\r\n\r\n")]
        [InlineData(" / HTTP/1.1\r\n\r\n")]
        [InlineData("  / HTTP/1.1\r\n\r\n")]
        [InlineData("GET  / HTTP/1.1\r\n\r\n")]
        [InlineData("GET  /  HTTP/1.1\r\n\r\n")]
        [InlineData("GET  HTTP/1.1\r\n\r\n")]
        [InlineData("GET /")]
        [InlineData("GET / ")]
        [InlineData("GET / H")]
        [InlineData("GET / HTTP/1.")]
        [InlineData("GET /\r\n")]
        [InlineData("GET / \r\n")]
        [InlineData("GET / \n")]
        [InlineData("GET / http/1.0\r\n\r\n")]
        [InlineData("GET / http/1.1\r\n\r\n")]
        [InlineData("GET / HTTP/1.1 \r\n\r\n")]
        [InlineData("GET / HTTP/1.1a\r\n\r\n")]
        [InlineData("GET / HTTP/1.0\n\r\n")]
        [InlineData("GET / HTTP/3.0\r\n\r\n")]
        [InlineData("GET / H\r\n\r\n")]
        [InlineData("GET / HTTP/1.\r\n\r\n")]
        [InlineData("GET / hello\r\n\r\n")]
        [InlineData("GET / 8charact\r\n\r\n")]
        public async Task TestBadRequests(string request)
        {
            using (var server = new TestServer(context => { return Task.FromResult(0); }))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    var receiveTask = Task.Run(async () =>
                    {
                        await connection.Receive(
                            "HTTP/1.0 400 Bad Request",
                            "");
                        await connection.ReceiveStartsWith("Date: ");
                        await connection.ReceiveForcedEnd(
                            "Content-Length: 0",
                            "Server: Kestrel",
                            "",
                            "");
                    });

                    try
                    {
                        await connection.SendEnd(request).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // TestConnection.SendEnd will start throwing while sending characters
                        // in cases where the server rejects the request as soon as it
                        // determines the request line is malformed, even though there
                        // are more characters following.
                    }

                    await receiveTask;
                }
            }
        }
    }
}