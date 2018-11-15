// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class RequestTargetProcessingTests : LoggedTest
    {
        [Fact]
        public async Task RequestPathIsNotNormalized()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async context =>
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
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 11",
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

            using (var server = new TestServer(async context =>
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
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 11",
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

            using (var server = new TestServer(async context =>
            {
                Assert.Equal(requestTarget, context.Features.Get<IHttpRequestFeature>().RawTarget);
                Assert.Empty(context.Request.Path.Value);
                Assert.Empty(context.Request.PathBase.Value);
                Assert.Empty(context.Request.QueryString.Value);

                context.Response.Headers["Content-Length"] = new[] { "11" };
                await context.Response.WriteAsync("Hello World");
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
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                }
            }
        }
    }
}
