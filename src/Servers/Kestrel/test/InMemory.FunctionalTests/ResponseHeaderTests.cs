// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class ResponseHeaderTests : TestApplicationErrorLoggerLoggedTest
    {
        [Fact]
        public async Task ResponseHeaders_WithNonAscii_Throws()
        {
            await using var server = new TestServer(context =>
            {
                Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("Custom你好Name", "Custom Value"));
                Assert.Throws<InvalidOperationException>(() => context.Response.ContentType = "Custom 你好 Type");
                Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("CustomName", "Custom 你好 Value"));
                context.Response.ContentLength = 11;
                return context.Response.WriteAsync("Hello World");
            }, new TestServiceContext(LoggerFactory));
            using var connection = server.CreateConnection();
            await connection.Send(
                "GET / HTTP/1.1",
                "Host:",
                "",
                "");

            await connection.Receive(
                $"HTTP/1.1 200 OK",
                $"Date: {server.Context.DateHeaderValue}",
                "Content-Length: 11",
                "",
                "Hello World");
        }

        [Fact]
        public async Task ResponseHeaders_WithNonAsciiWithCustomEncoding_Works()
        {
            var testContext = new TestServiceContext(LoggerFactory);
            testContext.ServerOptions.ResponseHeaderEncodingSelector = _ => Encoding.UTF8;

            await using var server = new TestServer(context =>
            {
                Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("Custom你好Name", "Custom Value"));
                context.Response.ContentType = "Custom 你好 Type";
                context.Response.Headers.Append("CustomName", "Custom 你好 Value");
                context.Response.ContentLength = 11;
                return context.Response.WriteAsync("Hello World");
            }, testContext);
            
            using var connection = server.CreateConnection(Encoding.UTF8);
            await connection.Send(
                "GET / HTTP/1.1",
                "Host:",
                "",
                "");

            await connection.Receive(
                $"HTTP/1.1 200 OK",
                "Content-Type: Custom 你好 Type",
                $"Date: {server.Context.DateHeaderValue}",
                "Content-Length: 11",
                "CustomName: Custom 你好 Value",
                "",
                "Hello World");
        }
    }
}
