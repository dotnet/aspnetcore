// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class LazyRequestHeadersTests
{
    [Fact]
    public void LazyUserAgentSurvivesBodyReadsAndKeepAliveReuse()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.Server.Kestrel.Experimental.LazyUserAgent", "true");

        using var remoteHandle = RemoteExecutor.Invoke(static async () =>
        {
            var testContext = new TestServiceContext();
            var request = 0;
            await using var server = new TestServer(async context =>
            {
                Assert.True(((HttpRequestHeaders)context.Request.Headers).LazyUserAgent);
                await context.Request.Body.CopyToAsync(Stream.Null);

                switch (request++)
                {
                    case 0:
                        // Leave the first value unread until this request's storage is reset.
                        break;
                    case 1:
                        Assert.Equal("changed", context.Request.Headers.UserAgent);
                        break;
                    case 2:
                        Assert.False(context.Request.Headers.ContainsKey("User-Agent"));
                        break;
                    case 3:
                        Assert.Equal(new[] { "first", "second" }, context.Request.Headers.UserAgent.ToArray());
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected request.");
                }

                context.Response.ContentLength = 1;
                await context.Response.WriteAsync("x");
            }, testContext);

            using var connection = server.CreateConnection();
            foreach (var userAgent in new[] { "User-Agent: unread", "uSeR-aGeNt: changed", "X-Other: value", "User-Agent: first\r\nUser-Agent: second" })
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host: localhost",
                    userAgent,
                    "Content-Length: 4",
                    "",
                    "body");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 1",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "x");
            }
            Assert.Equal(4, request);
        }, options);
    }
}
