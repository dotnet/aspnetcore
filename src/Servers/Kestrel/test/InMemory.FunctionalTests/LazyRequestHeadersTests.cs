// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.DotNet.RemoteExecutor;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class LazyRequestHeadersTests
{
    [Fact]
    public void LazyUserAgentSurvivesHttp2BodyReads()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.Server.Kestrel.Experimental.LazyUserAgent", "true");

        using var remoteHandle = RemoteExecutor.Invoke(static async () =>
        {
            var values = new[] { "unread", "changed", null, "last" };
            var requestIndex = 0;
            await using var server = new TestServer(async context =>
            {
                Assert.True(((HttpRequestHeaders)context.Request.Headers).LazyUserAgent);
                await context.Request.Body.CopyToAsync(Stream.Null);
                var index = requestIndex++;
                if (index != 0)
                {
                    if (values[index] is null)
                    {
                        Assert.False(context.Request.Headers.ContainsKey("User-Agent"));
                    }
                    else
                    {
                        Assert.Equal(values[index], context.Request.Headers.UserAgent.ToString());
                    }
                }
                context.Response.ContentLength = 1;
                await context.Response.WriteAsync("x");
            }, new TestServiceContext(), listenOptions => listenOptions.Protocols = HttpProtocols.Http2);

            using var connection = server.CreateConnection();
            var connectionCount = 0;
            using var handler = new SocketsHttpHandler
            {
                ConnectCallback = (_, _) =>
                {
                    Assert.Equal(0, connectionCount++);
                    return ValueTask.FromResult<Stream>(connection.Stream);
                },
            };
            using var client = new HttpClient(handler);
            foreach (var value in values)
            {
                using var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "http://localhost/")
                {
                    Version = new Version(2, 0),
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                    Content = new StringContent("body"),
                };
                if (value is not null)
                {
                    request.Headers.TryAddWithoutValidation("User-Agent", value);
                }
                using var response = await client.SendAsync(request).DefaultTimeout();
                Assert.Equal(new Version(2, 0), response.Version);
                Assert.Equal("x", await response.Content.ReadAsStringAsync());
            }
            Assert.Equal(4, requestIndex);
            Assert.Equal(1, connectionCount);
        }, options);
    }

    [Fact]
    public void LazyUserAgentSurvivesQPackDecodingAndStreamReuse()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.Server.Kestrel.Experimental.LazyUserAgent", "true");

        using var remoteHandle = RemoteExecutor.Invoke(static async () =>
        {
            var testContext = new TestServiceContext { Scheduler = PipeScheduler.Inline };
            var http3 = new Http3InMemory(testContext, testContext.FakeTimeProvider,
                Mock.Of<ITimeoutHandler>(), testContext.LoggerFactory);
            IHeaderDictionary previousHeaders = null;
            var requestIndex = 0;
            await http3.InitializeConnectionAsync(async context =>
            {
                Assert.True(((HttpRequestHeaders)context.Request.Headers).LazyUserAgent);
                if (previousHeaders is not null)
                {
                    Assert.Same(previousHeaders, context.Request.Headers);
                }
                previousHeaders = context.Request.Headers;
                await context.Request.Body.CopyToAsync(Stream.Null);
                switch (requestIndex++)
                {
                    case 0:
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
            });
            await http3.CreateControlStream();

            foreach (var values in new[] { new[] { "unread" }, new[] { "changed" }, Array.Empty<string>(), new[] { "first", "second" } })
            {
                var headers = new List<KeyValuePair<string, string>>
                {
                    new(":method", "POST"),
                    new(":path", "/"),
                    new(":scheme", "http"),
                    new(":authority", "localhost"),
                };
                headers.AddRange(values.Select(value => new KeyValuePair<string, string>("user-agent", value)));
                var request = await http3.CreateRequestStream(headers);
                await request.SendDataAsync("body"u8.ToArray(), endStream: true);
                var responseHeaders = await request.ExpectHeadersAsync();
                Assert.Equal("200", responseHeaders[":status"]);
                Assert.Equal("x", Encoding.ASCII.GetString((await request.ExpectDataAsync()).Span));
                await request.ExpectReceiveEndOfStream();
                await request.OnDisposedTask.DefaultTimeout();
            }
            Assert.Equal(4, requestIndex);
            http3.CloseServerGracefully();
            await http3.WaitForConnectionErrorAsync<Exception>(ignoreNonGoAwayFrames: true,
                expectedLastStreamId: null, expectedErrorCode: Http3ErrorCode.NoError);
            http3._memoryPool.Dispose();
        }, options);
    }

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
