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
    public void FragmentedKnownAndUnknownValuesSurviveBodyReadsOnlyHttp11IsLazy()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.Server.Kestrel.Experimental.LazyRequestHeaders", "true");
        using var remoteHandle = RemoteExecutor.Invoke(static async () =>
        {
            foreach (var version in new[] { "HTTP/1.0", "HTTP/1.1" })
            {
                foreach (var length in new[] { 255, 256, 257 })
                {
                    var value = new string('a', length);
                    await using var server = new TestServer(async context =>
                    {
                        var headers = (HttpRequestHeaders)context.Request.Headers;
                        Assert.True(headers.LazyRequestHeaders);
                        Assert.Equal(version == "HTTP/1.1" ? 4 : 0, headers.LazyValueCount);
                        await context.Request.Body.CopyToAsync(Stream.Null);
                        Assert.Equal(value, context.Request.Headers.Accept);
                        Assert.Equal(value, context.Request.Headers.AcceptLanguage);
                        Assert.Equal(value, context.Request.Headers["X-Example"]);
                        Assert.Equal(value, context.Request.Headers["X-Other"]);
                        Assert.Equal(0, headers.LazyValueCount);
                        context.Response.ContentLength = 1;
                        await context.Response.WriteAsync("x");
                    }, new TestServiceContext { Scheduler = PipeScheduler.Inline });
                    using var connection = server.CreateConnection();
                    var request = Encoding.ASCII.GetBytes(
                        $"POST / {version}\r\nHost: localhost\r\nConnection: close\r\n" +
                        $"Accept: {value}\r\nAccept-Language: {value}\r\nX-Example: {value}\r\nX-Other: {value}\r\n" +
                        "Content-Length: 4\r\n\r\nbody");
                    for (var offset = 0; offset < request.Length; offset += 32)
                    {
                        await connection.Stream.WriteAsync(request.AsMemory(offset, Math.Min(32, request.Length - offset)));
                    }
                    var response = await new StreamReader(connection.Stream).ReadToEndAsync().DefaultTimeout();
                    Assert.StartsWith("HTTP/1.1 200 OK\r\n", response);
                    Assert.EndsWith("\r\n\r\nx", response);
                }
            }
        }, options);
    }

    [Fact]
    public void InvalidHostAndContentLengthAreRejectedWithSwitchOn()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.Server.Kestrel.Experimental.LazyRequestHeaders", "true");
        using var remoteHandle = RemoteExecutor.Invoke(static async () =>
        {
            await using var server = new TestServer(_ => throw new InvalidOperationException("Malformed request reached application."),
                new TestServiceContext());
            foreach (var headers in new[]
            {
                "Accept: unread",
                "Host: bad host",
                "Host: localhost\r\nContent-Length: +1",
                "Host: localhost\r\nContent-Length: 9223372036854775808",
                "Host: localhost\r\nContent-Length: 1\r\nContent-Length: 1",
            })
            {
                using var connection = server.CreateConnection();
                await connection.Stream.WriteAsync(Encoding.ASCII.GetBytes(
                    $"GET / HTTP/1.1\r\nConnection: close\r\n{headers}\r\n\r\n"));
                var response = await new StreamReader(connection.Stream).ReadToEndAsync().DefaultTimeout();
                Assert.StartsWith("HTTP/1.1 400 Bad Request\r\n", response);
            }
        }, options);
    }

    [Fact]
    public void Http2HeadersRemainCompatibleWithLazySwitchEnabled()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.Server.Kestrel.Experimental.LazyRequestHeaders", "true");

        using var remoteHandle = RemoteExecutor.Invoke(static async () =>
        {
            var values = new[] { "unread", "changed", null, "last" };
            var requestIndex = 0;
            await using var server = new TestServer(async context =>
            {
                Assert.True(((HttpRequestHeaders)context.Request.Headers).LazyRequestHeaders);
                Assert.Equal(0, ((HttpRequestHeaders)context.Request.Headers).LazyValueCount);
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
                        Assert.Equal(values[index], context.Request.Headers.Accept.ToString());
                        Assert.Equal(values[index], context.Request.Headers["X-Example"].ToString());
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
                    request.Headers.TryAddWithoutValidation("Accept", value);
                    request.Headers.TryAddWithoutValidation("X-Example", value);
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
    public void QPackHeadersRemainCompatibleWithLazySwitchEnabled()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.Server.Kestrel.Experimental.LazyRequestHeaders", "true");

        using var remoteHandle = RemoteExecutor.Invoke(static async () =>
        {
            var testContext = new TestServiceContext { Scheduler = PipeScheduler.Inline };
            var http3 = new Http3InMemory(testContext, testContext.FakeTimeProvider,
                Mock.Of<ITimeoutHandler>(), testContext.LoggerFactory);
            IHeaderDictionary previousHeaders = null;
            var requestIndex = 0;
            await http3.InitializeConnectionAsync(async context =>
            {
                Assert.True(((HttpRequestHeaders)context.Request.Headers).LazyRequestHeaders);
                Assert.Equal(0, ((HttpRequestHeaders)context.Request.Headers).LazyValueCount);
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
                        Assert.Equal("changed", context.Request.Headers.Accept);
                        Assert.Equal("changed", context.Request.Headers["X-Example"]);
                        break;
                    case 2:
                        Assert.False(context.Request.Headers.ContainsKey("User-Agent"));
                        break;
                    case 3:
                        Assert.Equal(new[] { "first", "second" }, context.Request.Headers.UserAgent.ToArray());
                        Assert.Equal(new[] { "first", "second" }, context.Request.Headers.Accept.ToArray());
                        Assert.Equal(new[] { "first", "second" }, context.Request.Headers["X-Example"].ToArray());
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
                headers.AddRange(values.Select(value => new KeyValuePair<string, string>("accept", value)));
                headers.AddRange(values.Select(value => new KeyValuePair<string, string>("x-example", value)));
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
    public void LazyHeadersSurviveBodyReadsAndKeepAliveReuse()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.Server.Kestrel.Experimental.LazyRequestHeaders", "true");

        using var remoteHandle = RemoteExecutor.Invoke(static async () =>
        {
            var testContext = new TestServiceContext();
            var request = 0;
            await using var server = new TestServer(async context =>
            {
                Assert.True(((HttpRequestHeaders)context.Request.Headers).LazyRequestHeaders);
                await context.Request.Body.CopyToAsync(Stream.Null);

                switch (request++)
                {
                    case 0:
                        // Leave the first value unread until this request's storage is reset.
                        break;
                    case 1:
                        Assert.Equal("changed", context.Request.Headers.UserAgent);
                        Assert.Equal("changed-accept", context.Request.Headers.Accept);
                        Assert.Equal("changed-unknown", context.Request.Headers["X-Example"]);
                        Assert.Equal("changed-other", context.Request.Headers["X-Other"]);
                        break;
                    case 2:
                        Assert.False(context.Request.Headers.ContainsKey("User-Agent"));
                        Assert.False(context.Request.Headers.ContainsKey("Accept"));
                        Assert.False(context.Request.Headers.ContainsKey("X-Example"));
                        break;
                    case 3:
                        Assert.Equal(new[] { "first", "second" }, context.Request.Headers.UserAgent.ToArray());
                        Assert.Equal(new[] { "first", "second" }, context.Request.Headers["X-Example"].ToArray());
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected request.");
                }

                context.Response.ContentLength = 1;
                await context.Response.WriteAsync("x");
            }, testContext);

            using var connection = server.CreateConnection();
            foreach (var userAgent in new[]
            {
                "User-Agent: unread\r\nAccept: unread-accept\r\nX-Example: unread-unknown\r\nX-Other: unread-other",
                "uSeR-aGeNt: changed\r\nAccept: changed-accept\r\nX-Example: changed-unknown\r\nX-Other: changed-other",
                "X-Other: value",
                "User-Agent: first\r\nUser-Agent: second\r\nX-Example: first\r\nx-example: second",
            })
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
