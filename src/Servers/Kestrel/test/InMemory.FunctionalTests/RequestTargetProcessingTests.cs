// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class RequestTargetProcessingTests : LoggedTest
{
    [Fact]
    public async Task RequestPathIsNotNormalized()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async context =>
        {
            Assert.Equal("/\u0041\u030A/B/\u0041\u030A", context.Request.Path.Value);

            context.Response.Headers.ContentLength = 11;
            await context.Response.WriteAsync("Hello World");
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "GET /%41%CC%8A/A/../B/%41%CC%8A HTTP/1.1",
                    "Host:",
                    "",
                    "");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 11",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World");
            }
        }
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/.")]
    [InlineData("/..")]
    [InlineData("/./.")]
    [InlineData("/./..")]
    [InlineData("/../.")]
    [InlineData("/../..")]
    [InlineData("/path")]
    [InlineData("/path?foo=1&bar=2")]
    [InlineData("/hello%20world")]
    [InlineData("/hello%20world?foo=1&bar=2")]
    [InlineData("/base/path")]
    [InlineData("/base/path?foo=1&bar=2")]
    [InlineData("/base/hello%20world")]
    [InlineData("/base/hello%20world?foo=1&bar=2")]
    public async Task RequestFeatureContainsRawTarget(string requestTarget)
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async context =>
        {
            Assert.Equal(requestTarget, context.Features.Get<IHttpRequestFeature>().RawTarget);

            context.Response.Headers["Content-Length"] = new[] { "11" };
            await context.Response.WriteAsync("Hello World");
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    $"GET {requestTarget} HTTP/1.1",
                    "Host:",
                    "",
                    "");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 11",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World");
            }
        }
    }

    [Theory]
    [InlineData(HttpMethod.Options, "*")]
    [InlineData(HttpMethod.Connect, "host")]
    public async Task NonPathRequestTargetSetInRawTarget(HttpMethod method, string requestTarget)
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async context =>
        {
            Assert.Equal(requestTarget, context.Features.Get<IHttpRequestFeature>().RawTarget);
            Assert.Empty(context.Request.Path.Value);
            Assert.Empty(context.Request.PathBase.Value);
            Assert.Empty(context.Request.QueryString.Value);

            await context.Response.CompleteAsync();
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                var host = method == HttpMethod.Connect
                    ? requestTarget
                    : string.Empty;

                await connection.Send(
                    $"{HttpUtilities.MethodToString(method)} {requestTarget} HTTP/1.1",
                    $"Host: {host}",
                    "",
                    "");
            }
        }
    }
}
