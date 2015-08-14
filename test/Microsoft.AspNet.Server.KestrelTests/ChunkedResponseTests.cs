// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class ChunkedResponseTests
    {
        [Fact]
        public async Task ResponsesAreChunkedAutomatically()
        {
            using (var server = new TestServer(async frame =>
            {
                frame.ResponseHeaders.Clear();
                await frame.ResponseBody.WriteAsync(Encoding.ASCII.GetBytes("Hello "), 0, 6);
                await frame.ResponseBody.WriteAsync(Encoding.ASCII.GetBytes("World!"), 0, 6);
            }))
            {
                using (var connection = new TestConnection())
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Transfer-Encoding: chunked",
                        "",
                        "6",
                        "Hello ",
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ZeroLengthWritesAreIgnored()
        {
            using (var server = new TestServer(async frame =>
            {
                frame.ResponseHeaders.Clear();
                await frame.ResponseBody.WriteAsync(Encoding.ASCII.GetBytes("Hello "), 0, 6);
                await frame.ResponseBody.WriteAsync(new byte[0], 0, 0);
                await frame.ResponseBody.WriteAsync(Encoding.ASCII.GetBytes("World!"), 0, 6);
            }))
            {
                using (var connection = new TestConnection())
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Transfer-Encoding: chunked",
                        "",
                        "6",
                        "Hello ",
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }
    }
}
