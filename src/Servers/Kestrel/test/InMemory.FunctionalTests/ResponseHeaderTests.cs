// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class ResponseHeaderTests : TestApplicationErrorLoggerLoggedTest
{
    [Fact]
    public async Task ResponseHeaders_WithNonAscii_Throws()
    {
        await using var server = new TestServer(context =>
        {
            Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("Custom你好Name", "Custom Value"));
            Assert.Throws<InvalidOperationException>(() => context.Response.ContentType = "Custom 你好 Type"); // Special cased
            Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Accept = "Custom 你好 Accept"); // Not special cased
            Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("CustomName", "Custom 你好 Value"));
            Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("CustomName", "Custom \r Value"));
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
            "Content-Length: 11",
            $"Date: {server.Context.DateHeaderValue}",
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
            Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("CustomName", "Custom \r Value"));
            context.Response.ContentType = "Custom 你好 Type";
            context.Response.Headers.Accept = "Custom 你好 Accept";
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
            "Content-Length: 11",
            "Content-Type: Custom 你好 Type",
            $"Date: {server.Context.DateHeaderValue}",
            "Accept: Custom 你好 Accept",
            "CustomName: Custom 你好 Value",
            "",
            "Hello World");
    }

    [Fact]
    public async Task ResponseHeaders_WithInvalidValuesAndCustomEncoder_AbortsConnection()
    {
        var testContext = new TestServiceContext(LoggerFactory);
        var encoding = Encoding.GetEncoding(Encoding.Latin1.CodePage, EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);
        testContext.ServerOptions.ResponseHeaderEncodingSelector = _ => encoding;

        await using var server = new TestServer(context =>
        {
            context.Response.Headers.Append("CustomName", "Custom 你好 Value");
            context.Response.ContentLength = 11;
            return context.Response.WriteAsync("Hello World");
        }, testContext);
        using var connection = server.CreateConnection();
        await connection.Send(
            "GET / HTTP/1.1",
            "Host:",
            "",
            "");

        await connection.ReceiveEnd();
    }

    [Fact]
    public async Task ResponseHeaders_NullEntriesAreIgnored()
    {
        var tag = "Warning";

        await using var server = new TestServer(context =>
        {
            Assert.Equal(0, context.Response.Headers[tag].Count);

            context.Response.Headers.Add(tag, new StringValues((string)null));

            Assert.Equal(0, context.Response.Headers[tag].Count);

            // this should not throw
            context.Response.Headers.Add(tag, new StringValues("Hello"));

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
            "Content-Length: 11",
            $"Date: {server.Context.DateHeaderValue}",
            $"{tag}: Hello",
            "",
            "Hello World");
    }
}
