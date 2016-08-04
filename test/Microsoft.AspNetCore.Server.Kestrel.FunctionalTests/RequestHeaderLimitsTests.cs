// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class RequestHeaderLimitsTests
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

            using (var host = BuildWebHost(options =>
            {
                options.Limits.MaxRequestHeadersTotalSize = headers.Length + extraLimit;
            }))
            {
                host.Start();

                using (var connection = new TestConnection(host.GetPort()))
                {
                    await connection.SendEnd($"GET / HTTP/1.1\r\n{headers}\r\n");
                    await connection.Receive($"HTTP/1.1 200 OK\r\n");
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

            using (var host = BuildWebHost(options =>
            {
                options.Limits.MaxRequestHeaderCount = maxHeaderCount;
            }))
            {
                host.Start();

                using (var connection = new TestConnection(host.GetPort()))
                {
                    await connection.SendEnd($"GET / HTTP/1.1\r\n{headers}\r\n");
                    await connection.Receive($"HTTP/1.1 200 OK\r\n");
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public async Task ServerRejectsRequestWithHeaderTotalSizeOverLimit(int headerCount)
        {
            var headers = MakeHeaders(headerCount);

            using (var host = BuildWebHost(options =>
            {
                options.Limits.MaxRequestHeadersTotalSize = headers.Length - 1;
            }))
            {
                host.Start();

                using (var connection = new TestConnection(host.GetPort()))
                {
                    await connection.SendAllTryEnd($"GET / HTTP/1.1\r\n{headers}\r\n");
                    await connection.Receive($"HTTP/1.1 400 Bad Request\r\n");
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

            using (var host = BuildWebHost(options =>
            {
                options.Limits.MaxRequestHeaderCount = maxHeaderCount;
            }))
            {
                host.Start();

                using (var connection = new TestConnection(host.GetPort()))
                {
                    await connection.SendAllTryEnd($"GET / HTTP/1.1\r\n{headers}\r\n");
                    await connection.Receive($"HTTP/1.1 400 Bad Request\r\n");
                }
            }
        }

        private static string MakeHeaders(int count)
        {
            return string.Join("", Enumerable
                .Range(0, count)
                .Select(i => $"Header-{i}: value{i}\r\n"));
        }

        private static IWebHost BuildWebHost(Action<KestrelServerOptions> options)
        {
            var host = new WebHostBuilder()
                .UseKestrel(options)
                .UseUrls("http://127.0.0.1:0/")
                .Configure(app => app.Run(async context =>
                {
                    await context.Response.WriteAsync("hello, world");
                }))
                .Build();

            return host;
        }
    }
}