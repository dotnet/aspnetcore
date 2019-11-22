// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class RequestHeaderLimitsTests : LoggedTest
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(0, 1337)]
        [InlineData(1, 0)]
        [InlineData(1, 1)]
        [InlineData(1, 1337)]
        [InlineData(5, 0)]
        [InlineData(5, 1)]
        [InlineData(5, 1337)]
        public async Task ServerAcceptsRequestWithHeaderTotalSizeWithinLimit(int headerCount, int extraLimit)
        {
            var headers = MakeHeaders(headerCount);

            await using (var server = CreateServer(maxRequestHeadersTotalSize: headers.Length + extraLimit))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send($"GET / HTTP/1.1\r\n{headers}\r\n");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "c",
                        "hello, world",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(0, 1337)]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(1, 1337)]
        [InlineData(5, 5)]
        [InlineData(5, 6)]
        [InlineData(5, 1337)]
        public async Task ServerAcceptsRequestWithHeaderCountWithinLimit(int headerCount, int maxHeaderCount)
        {
            var headers = MakeHeaders(headerCount);

            await using (var server = CreateServer(maxRequestHeaderCount: maxHeaderCount))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send($"GET / HTTP/1.1\r\n{headers}\r\n");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "c",
                        "hello, world",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public async Task ServerRejectsRequestWithHeaderTotalSizeOverLimit(int headerCount)
        {
            var headers = MakeHeaders(headerCount);

            await using (var server = CreateServer(maxRequestHeadersTotalSize: headers.Length - 1))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAll($"GET / HTTP/1.1\r\n{headers}\r\n");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 431 Request Header Fields Too Large",
                        "Connection: close",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [InlineData(2, 1)]
        [InlineData(5, 1)]
        [InlineData(5, 4)]
        public async Task ServerRejectsRequestWithHeaderCountOverLimit(int headerCount, int maxHeaderCount)
        {
            var headers = MakeHeaders(headerCount);

            await using (var server = CreateServer(maxRequestHeaderCount: maxHeaderCount))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAll($"GET / HTTP/1.1\r\n{headers}\r\n");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 431 Request Header Fields Too Large",
                        "Connection: close",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        private static string MakeHeaders(int count)
        {
            const string host = "Host:\r\n";
            if (count <= 1) return host;

            return string.Join("", new[] { host }
                .Concat(Enumerable
                .Range(0, count -1)
                .Select(i => $"Header-{i}: value{i}\r\n")));
        }

        private TestServer CreateServer(int? maxRequestHeaderCount = null, int? maxRequestHeadersTotalSize = null)
        {
            var options = new KestrelServerOptions { AddServerHeader = false };

            if (maxRequestHeaderCount.HasValue)
            {
                options.Limits.MaxRequestHeaderCount = maxRequestHeaderCount.Value;
            }

            if (maxRequestHeadersTotalSize.HasValue)
            {
                options.Limits.MaxRequestHeadersTotalSize = maxRequestHeadersTotalSize.Value;
            }

            return new TestServer(async httpContext => await httpContext.Response.WriteAsync("hello, world"), new TestServiceContext(LoggerFactory)
            {
                ServerOptions = options
            });
        }
    }
}
