// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class MaxRequestLineSizeTests
    {
        [Theory]
        [InlineData("GET / HTTP/1.1\r\n", 16)]
        [InlineData("GET / HTTP/1.1\r\n", 17)]
        [InlineData("GET / HTTP/1.1\r\n", 137)]
        [InlineData("POST /abc/de HTTP/1.1\r\n", 23)]
        [InlineData("POST /abc/de HTTP/1.1\r\n", 24)]
        [InlineData("POST /abc/de HTTP/1.1\r\n", 287)]
        [InlineData("PUT /abc/de?f=ghi HTTP/1.1\r\n", 28)]
        [InlineData("PUT /abc/de?f=ghi HTTP/1.1\r\n", 29)]
        [InlineData("PUT /abc/de?f=ghi HTTP/1.1\r\n", 589)]
        [InlineData("DELETE /a%20b%20c/d%20e?f=ghi HTTP/1.1\r\n", 40)]
        [InlineData("DELETE /a%20b%20c/d%20e?f=ghi HTTP/1.1\r\n", 41)]
        [InlineData("DELETE /a%20b%20c/d%20e?f=ghi HTTP/1.1\r\n", 1027)]
        public async Task ServerAcceptsRequestLineWithinLimit(string requestLine, int limit)
        {
            var maxRequestLineSize = limit;

            using (var host = BuildWebHost(options =>
            {
                options.Limits.MaxRequestLineSize = maxRequestLineSize;
            }))
            {
                host.Start();

                using (var connection = new TestConnection(host.GetPort()))
                {
                    await connection.SendEnd($"{requestLine}\r\n");
                    await connection.Receive($"HTTP/1.1 200 OK\r\n");
                }
            }
        }

        [Theory]
        [InlineData("GET / HTTP/1.1\r\n")]
        [InlineData("POST /abc/de HTTP/1.1\r\n")]
        [InlineData("PUT /abc/de?f=ghi HTTP/1.1\r\n")]
        [InlineData("DELETE /a%20b%20c/d%20e?f=ghi HTTP/1.1\r\n")]
        public async Task ServerRejectsRequestLineExceedingLimit(string requestLine)
        {
            using (var host = BuildWebHost(options =>
            {
                options.Limits.MaxRequestLineSize = requestLine.Length - 1; // stop short of the '\n'
            }))
            {
                host.Start();

                using (var connection = new TestConnection(host.GetPort()))
                {
                    await connection.SendAllTryEnd($"{requestLine}\r\n");
                    await connection.Receive($"HTTP/1.1 400 Bad Request\r\n");
                }
            }
        }

        private IWebHost BuildWebHost(Action<KestrelServerOptions> options)
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
