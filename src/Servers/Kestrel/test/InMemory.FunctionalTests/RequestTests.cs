// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class RequestTests : LoggedTest
    {
        [Fact]
        public async Task StreamsAreNotPersistedAcrossRequests()
        {
            var requestBodyPersisted = false;
            var responseBodyPersisted = false;

            using (var server = new TestServer(async context =>
            {
                if (context.Request.Body is MemoryStream)
                {
                    requestBodyPersisted = true;
                }

                if (context.Response.Body is MemoryStream)
                {
                    responseBodyPersisted = true;
                }

                context.Request.Body = new MemoryStream();
                context.Response.Body = new MemoryStream();

                await context.Response.WriteAsync("hello, world");
            }, new TestServiceContext(LoggerFactory)))
            {
                Assert.Equal(string.Empty, await server.HttpClientSlim.GetStringAsync($"http://localhost:{server.Port}/"));
                Assert.Equal(string.Empty, await server.HttpClientSlim.GetStringAsync($"http://localhost:{server.Port}/"));

                Assert.False(requestBodyPersisted);
                Assert.False(responseBodyPersisted);
            }
        }

        [Fact]
        public async Task RequestBodyReadAsyncCanBeCancelled()
        {
            var helloTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cts = new CancellationTokenSource();

            using (var server = new TestServer(async context =>
            {
                var buffer = new byte[1024];
                try
                {
                    await context.Request.Body.ReadUntilLengthAsync(buffer, 6, cts.Token).DefaultTimeout();

                    Assert.Equal("Hello ", Encoding.ASCII.GetString(buffer, 0, 6));

                    helloTcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    // This shouldn't fail
                    helloTcs.TrySetException(ex);
                }

                try
                {
                    await context.Request.Body.ReadAsync(buffer, 0, 1, cts.Token).DefaultTimeout();

                    context.Response.ContentLength = 12;
                    await context.Response.WriteAsync("Read success");
                }
                catch (OperationCanceledException)
                {
                    context.Response.ContentLength = 14;
                    await context.Response.WriteAsync("Read cancelled");
                }

            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Connection: keep-alive",
                        "Content-Length: 11",
                        "",
                        "");

                    await connection.Send("Hello ");

                    await helloTcs.Task;

                    // Cancel the body after hello is read
                    cts.Cancel();

                    await connection.Send("World");

                    await connection.Receive($"HTTP/1.1 200 OK",
                           $"Date: {server.Context.DateHeaderValue}",
                           "Content-Length: 14",
                           "",
                           "Read cancelled");
                }
            }
        }

        [Fact]
        public async Task CanUpgradeRequestWithConnectionKeepAliveUpgradeHeader()
        {
            var testContext = new TestServiceContext();
            var dataRead = false;

            using (var server = new TestServer(async context =>
            {
                var stream = await context.Features.Get<IHttpUpgradeFeature>().UpgradeAsync();
                var data = new byte[3];

                await stream.ReadUntilLengthAsync(data, 3).DefaultTimeout();

                dataRead = Encoding.ASCII.GetString(data, 0, 3) == "abc";
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:\r\nConnection: keep-alive, upgrade",
                        "",
                        "abc");

                    await connection.ReceiveEnd(
                        "HTTP/1.1 101 Switching Protocols",
                        "Connection: Upgrade",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "");
                }
            }

            Assert.True(dataRead);
        }

        [Theory]
        [InlineData("http://localhost/abs/path", "/abs/path", null)]
        [InlineData("https://localhost/abs/path", "/abs/path", null)] // handles mismatch scheme
        [InlineData("https://localhost:22/abs/path", "/abs/path", null)] // handles mismatched ports
        [InlineData("https://differenthost/abs/path", "/abs/path", null)] // handles mismatched hostname
        [InlineData("http://localhost/", "/", null)]
        [InlineData("http://root@contoso.com/path", "/path", null)]
        [InlineData("http://root:password@contoso.com/path", "/path", null)]
        [InlineData("https://localhost/", "/", null)]
        [InlineData("http://localhost", "/", null)]
        [InlineData("http://127.0.0.1/", "/", null)]
        [InlineData("http://[::1]/", "/", null)]
        [InlineData("http://[::1]:8080/", "/", null)]
        [InlineData("http://localhost?q=123&w=xyz", "/", "123")]
        [InlineData("http://localhost/?q=123&w=xyz", "/", "123")]
        [InlineData("http://localhost/path?q=123&w=xyz", "/path", "123")]
        [InlineData("http://localhost/path%20with%20space?q=abc%20123", "/path with space", "abc 123")]
        public async Task CanHandleRequestsWithUrlInAbsoluteForm(string requestUrl, string expectedPath, string queryValue)
        {
            var pathTcs = new TaskCompletionSource<PathString>(TaskCreationOptions.RunContinuationsAsynchronously);
            var rawTargetTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var queryTcs = new TaskCompletionSource<IQueryCollection>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(async context =>
            {
                pathTcs.TrySetResult(context.Request.Path);
                queryTcs.TrySetResult(context.Request.Query);
                rawTargetTcs.TrySetResult(context.Features.Get<IHttpRequestFeature>().RawTarget);
                await context.Response.WriteAsync("Done");
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    var requestTarget = new Uri(requestUrl, UriKind.Absolute);
                    var host = requestTarget.Authority;
                    if (requestTarget.IsDefaultPort)
                    {
                        host += ":" + requestTarget.Port;
                    }

                    await connection.Send(
                        $"GET {requestUrl} HTTP/1.1",
                        "Content-Length: 0",
                        $"Host: {host}",
                        "",
                        "");

                    await connection.Receive($"HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "4",
                        "Done");

                    await Task.WhenAll(pathTcs.Task, rawTargetTcs.Task, queryTcs.Task).DefaultTimeout();
                    Assert.Equal(new PathString(expectedPath), pathTcs.Task.Result);
                    Assert.Equal(requestUrl, rawTargetTcs.Task.Result);
                    if (queryValue == null)
                    {
                        Assert.False(queryTcs.Task.Result.ContainsKey("q"));
                    }
                    else
                    {
                        Assert.Equal(queryValue, queryTcs.Task.Result["q"]);
                    }
                }
            }
        }

        [Fact]
        public async Task AppCanSetTraceIdentifier()
        {
            const string knownId = "xyz123";
            using (var server = new TestServer(async context =>
            {
                context.TraceIdentifier = knownId;
                await context.Response.WriteAsync(context.TraceIdentifier);
            }, new TestServiceContext(LoggerFactory)))
            {
                var requestId = await server.HttpClientSlim.GetStringAsync($"http://localhost:{server.Port}/");
                Assert.Equal(knownId, requestId);
            }
        }

        [Fact]
        public async Task TraceIdentifierIsUnique()
        {
            const int identifierLength = 22;
            const int iterations = 10;

            using (var server = new TestServer(async context =>
            {
                Assert.Equal(identifierLength, Encoding.ASCII.GetByteCount(context.TraceIdentifier));
                context.Response.ContentLength = identifierLength;
                await context.Response.WriteAsync(context.TraceIdentifier);
            }, new TestServiceContext(LoggerFactory)))
            {
                var usedIds = new ConcurrentBag<string>();

                // requests on separate connections in parallel
                Parallel.For(0, iterations, async i =>
                {
                    var id = await server.HttpClientSlim.GetStringAsync($"http://localhost:{server.Port}/");
                    Assert.DoesNotContain(id, usedIds.ToArray());
                    usedIds.Add(id);
                });

                // requests on same connection
                using (var connection = server.CreateConnection())
                {
                    var buffer = new char[identifierLength];
                    for (var i = 0; i < iterations; i++)
                    {
                        await connection.SendEmptyGet();

                        await connection.Receive($"HTTP/1.1 200 OK",
                           $"Date: {server.Context.DateHeaderValue}",
                           $"Content-Length: {identifierLength}",
                           "",
                           "");

                        var offset = 0;

                        while (offset < identifierLength)
                        {
                            var read = await connection.Reader.ReadAsync(buffer, offset, identifierLength - offset);
                            offset += read;

                            Assert.NotEqual(0, read);
                        }

                        Assert.Equal(identifierLength, offset);
                        var id = new string(buffer, 0, offset);
                        Assert.DoesNotContain(id, usedIds.ToArray());
                        usedIds.Add(id);
                    }
                }
            }
        }

        [Fact]
        public async Task Http11KeptAliveByDefault()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "GET / HTTP/1.1",
                        "Host:",
                        "Connection: close",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                }
            }
        }


        [Fact]
        public async Task Http10NotKeptAliveByDefault()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EchoApp, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.0",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }

                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "Hello World");
                }
            }
        }

        [Fact]
        public async Task Http10KeepAlive()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.0",
                        "Connection: keep-alive",
                        "",
                        "POST / HTTP/1.0",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "Connection: keep-alive",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "\r\n");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                }
            }
        }

        [Fact]
        public async Task Http10KeepAliveNotHonoredIfResponseContentLengthNotSet()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EchoApp, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.0",
                        "Connection: keep-alive",
                        "",
                        "");

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "Connection: keep-alive",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "\r\n");

                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Connection: keep-alive",
                        "Content-Length: 7",
                        "",
                        "Goodbye");

                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "Goodbye");
                }
            }
        }

        [Fact]
        public async Task Http10KeepAliveHonoredIfResponseContentLengthSet()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Content-Length: 11",
                        "Connection: keep-alive",
                        "",
                        "Hello World");

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "Connection: keep-alive",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 11",
                        "",
                        "Hello World");

                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Connection: keep-alive",
                        "Content-Length: 11",
                        "",
                        "Hello Again");

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "Connection: keep-alive",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 11",
                        "",
                        "Hello Again");

                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Content-Length: 7",
                        "",
                        "Goodbye");

                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                }
            }
        }

        [Fact]
        public async Task Expect100ContinueHonored()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Expect: 100-continue",
                        "Connection: close",
                        "Content-Length: 11",
                        "\r\n");
                    await connection.Receive(
                        "HTTP/1.1 100 Continue",
                        "",
                        "");
                    await connection.Send("Hello World");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                }
            }
        }

        [Fact]
        public async Task ZeroContentLengthAssumedOnNonKeepAliveRequestsWithoutContentLengthOrTransferEncodingHeader()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                // This will hang if 0 content length is not assumed by the server
                Assert.Equal(0, await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1).DefaultTimeout());
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    // Use Send instead of SendEnd to ensure the connection will remain open while
                    // the app runs and reads 0 bytes from the body nonetheless. This checks that
                    // https://github.com/aspnet/KestrelHttpServer/issues/1104 is not regressing.
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "Connection: close",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }

                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.0",
                        "Host:",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ConnectionClosesWhenFinReceivedBeforeRequestCompletes()
        {
            var testContext = new TestServiceContext(LoggerFactory);
            // FIN callbacks are scheduled so run inline to make this test more reliable
            testContext.Scheduler = PipeScheduler.Inline;

            using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1");
                    connection.ShutdownSend();
                    await connection.ReceiveEnd();
                }

                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 7");
                    connection.ShutdownSend();
                    await connection.ReceiveEnd();
                }
            }
        }

        [Fact]
        public async Task RequestHeadersAreResetOnEachRequest()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            IHeaderDictionary originalRequestHeaders = null;
            var firstRequest = true;

            using (var server = new TestServer(httpContext =>
            {
                var requestFeature = httpContext.Features.Get<IHttpRequestFeature>();

                if (firstRequest)
                {
                    originalRequestHeaders = requestFeature.Headers;
                    requestFeature.Headers = new HttpRequestHeaders();
                    firstRequest = false;
                }
                else
                {
                    Assert.Same(originalRequestHeaders, requestFeature.Headers);
                }

                return Task.CompletedTask;
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task UpgradeRequestIsNotKeptAliveOrChunked()
        {
            const string message = "Hello World";

            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async context =>
            {
                var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();
                var duplexStream = await upgradeFeature.UpgradeAsync();

                var buffer = new byte[message.Length];

                await duplexStream.ReadUntilLengthAsync(buffer, message.Length).DefaultTimeout();

                await duplexStream.WriteAsync(buffer, 0, buffer.Length);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "Connection: Upgrade",
                        "",
                        message);
                    await connection.ReceiveEnd(
                        "HTTP/1.1 101 Switching Protocols",
                        "Connection: Upgrade",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        message);
                }
            }
        }

        [Fact]
        public async Task HeadersAndStreamsAreReusedAcrossRequests()
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var streamCount = 0;
            var requestHeadersCount = 0;
            var responseHeadersCount = 0;
            var loopCount = 20;
            Stream lastStream = null;
            IHeaderDictionary lastRequestHeaders = null;
            IHeaderDictionary lastResponseHeaders = null;

            using (var server = new TestServer(async context =>
            {
                if (context.Request.Body != lastStream)
                {
                    lastStream = context.Request.Body;
                    streamCount++;
                }
                if (context.Request.Headers != lastRequestHeaders)
                {
                    lastRequestHeaders = context.Request.Headers;
                    requestHeadersCount++;
                }
                if (context.Response.Headers != lastResponseHeaders)
                {
                    lastResponseHeaders = context.Response.Headers;
                    responseHeadersCount++;
                }

                var ms = new MemoryStream();
                await context.Request.Body.CopyToAsync(ms);
                var request = ms.ToArray();

                context.Response.ContentLength = request.Length;

                await context.Response.Body.WriteAsync(request, 0, request.Length);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    var requestData =
                        Enumerable.Repeat("GET / HTTP/1.1\r\nHost:\r\n", loopCount)
                            .Concat(new[] { "GET / HTTP/1.1\r\nHost:\r\nContent-Length: 7\r\nConnection: close\r\n\r\nGoodbye" });

                    var response = string.Join("\r\n", new string[] {
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        ""});

                    var lastResponse = string.Join("\r\n", new string[]
                    {
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 7",
                        "",
                        "Goodbye"
                    });

                    var responseData =
                        Enumerable.Repeat(response, loopCount)
                            .Concat(new[] { lastResponse });

                    await connection.Send(requestData.ToArray());

                    await connection.ReceiveEnd(responseData.ToArray());
                }

                Assert.Equal(1, streamCount);
                Assert.Equal(1, requestHeadersCount);
                Assert.Equal(1, responseHeadersCount);
            }
        }

        [Theory]
        [MemberData(nameof(HostHeaderData))]
        public async Task MatchesValidRequestTargetAndHostHeader(string request, string hostHeader)
        {
            using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send($"{request} HTTP/1.1",
                        $"Host: {hostHeader}",
                        "",
                        "");

                    await connection.Receive("HTTP/1.1 200 OK");
                }
            }
        }

        [Fact]
        public async Task ServerConsumesKeepAliveContentLengthRequest()
        {
            // The app doesn't read the request body, so it should be consumed by the server
            using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 5",
                        "",
                        "hello");

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");

                    // If the server consumed the previous request properly, the
                    // next request should be successful
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 5",
                        "",
                        "world");

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ServerConsumesKeepAliveChunkedRequest()
        {
            // The app doesn't read the request body, so it should be consumed by the server
            using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Transfer-Encoding: chunked",
                        "",
                        "5",
                        "hello",
                        "5",
                        "world",
                        "0",
                        "Trailer: value",
                        "",
                        "");

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");

                    // If the server consumed the previous request properly, the
                    // next request should be successful
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 5",
                        "",
                        "world");

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task NonKeepAliveRequestNotConsumedByAppCompletes()
        {
            // The app doesn't read the request body, so it should be consumed by the server
            using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAll(
                        "POST / HTTP/1.0",
                        "Host:",
                        "Content-Length: 5",
                        "",
                        "hello");

                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task UpgradedRequestNotConsumedByAppCompletes()
        {
            // The app doesn't read the request body, so it should be consumed by the server
            using (var server = new TestServer(async context =>
            {
                var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();
                var duplexStream = await upgradeFeature.UpgradeAsync();

                var response = Encoding.ASCII.GetBytes("goodbye");
                await duplexStream.WriteAsync(response, 0, response.Length);
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAll(
                        "GET / HTTP/1.1",
                        "Host:",
                        "Connection: upgrade",
                        "",
                        "hello");

                    await connection.ReceiveEnd(
                        "HTTP/1.1 101 Switching Protocols",
                        "Connection: Upgrade",
                        $"Date: {server.Context.DateHeaderValue}",
                        "",
                        "goodbye");
                }
            }
        }


        [Fact]
        public async Task DoesNotEnforceRequestBodyMinimumDataRateOnUpgradedRequest()
        {
            var appEvent = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var delayEvent = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var serviceContext = new TestServiceContext(LoggerFactory);
            var heartbeatManager = new HeartbeatManager(serviceContext.ConnectionManager);

            using (var server = new TestServer(async context =>
            {
                context.Features.Get<IHttpMinRequestBodyDataRateFeature>().MinDataRate =
                    new MinDataRate(bytesPerSecond: double.MaxValue, gracePeriod: Heartbeat.Interval + TimeSpan.FromTicks(1));

                using (var stream = await context.Features.Get<IHttpUpgradeFeature>().UpgradeAsync())
                {
                    appEvent.SetResult(null);

                    // Read once to go through one set of TryPauseTimingReads()/TryResumeTimingReads() calls
                    await stream.ReadAsync(new byte[1], 0, 1);

                    await delayEvent.Task.DefaultTimeout();

                    // Read again to check that the connection is still alive
                    await stream.ReadAsync(new byte[1], 0, 1);

                    // Send a response to distinguish from the timeout case where the 101 is still received, but without any content
                    var response = Encoding.ASCII.GetBytes("hello");
                    await stream.WriteAsync(response, 0, response.Length);
                }
            }, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "Connection: upgrade",
                        "",
                        "a");

                    await appEvent.Task.DefaultTimeout();

                    serviceContext.MockSystemClock.UtcNow += TimeSpan.FromSeconds(5);
                    heartbeatManager.OnHeartbeat(serviceContext.SystemClock.UtcNow);

                    delayEvent.SetResult(null);

                    await connection.Send("b");

                    await connection.ReceiveEnd(
                        "HTTP/1.1 101 Switching Protocols",
                        "Connection: Upgrade",
                        $"Date: {server.Context.DateHeaderValue}",
                        "",
                        "hello");
                }
            }
        }

        [Fact]
        public async Task SynchronousReadsAllowedByDefault()
        {
            var firstRequest = true;

            using (var server = new TestServer(async context =>
            {
                var bodyControlFeature = context.Features.Get<IHttpBodyControlFeature>();
                Assert.True(bodyControlFeature.AllowSynchronousIO);

                var buffer = new byte[6];
                var offset = 0;

                // The request body is 5 bytes long. The 6th byte (buffer[5]) is only used for writing the response body.
                buffer[5] = (byte)(firstRequest ? '1' : '2');

                if (firstRequest)
                {
                    while (offset < 5)
                    {
                        offset += context.Request.Body.Read(buffer, offset, 5 - offset);
                    }

                    firstRequest = false;
                }
                else
                {
                    bodyControlFeature.AllowSynchronousIO = false;

                    // Synchronous reads now throw.
                    var ioEx = Assert.Throws<InvalidOperationException>(() => context.Request.Body.Read(new byte[1], 0, 1));
                    Assert.Equal(CoreStrings.SynchronousReadsDisallowed, ioEx.Message);

                    var ioEx2 = Assert.Throws<InvalidOperationException>(() => context.Request.Body.CopyTo(Stream.Null));
                    Assert.Equal(CoreStrings.SynchronousReadsDisallowed, ioEx2.Message);

                    while (offset < 5)
                    {
                        offset += await context.Request.Body.ReadAsync(buffer, offset, 5 - offset);
                    }
                }

                Assert.Equal(0, await context.Request.Body.ReadAsync(new byte[1], 0, 1));
                Assert.Equal("Hello", Encoding.ASCII.GetString(buffer, 0, 5));

                context.Response.ContentLength = 6;
                await context.Response.Body.WriteAsync(buffer, 0, 6);
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 5",
                        "",
                        "HelloPOST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 5",
                        "",
                        "Hello");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 6",
                        "",
                        "Hello1HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 6",
                        "",
                        "Hello2");
                }
            }
        }

        [Fact]
        public async Task SynchronousReadsCanBeDisallowedGlobally()
        {
            var testContext = new TestServiceContext(LoggerFactory)
            {
                ServerOptions = { AllowSynchronousIO = false }
            };

            using (var server = new TestServer(async context =>
            {
                var bodyControlFeature = context.Features.Get<IHttpBodyControlFeature>();
                Assert.False(bodyControlFeature.AllowSynchronousIO);

                // Synchronous reads now throw.
                var ioEx = Assert.Throws<InvalidOperationException>(() => context.Request.Body.Read(new byte[1], 0, 1));
                Assert.Equal(CoreStrings.SynchronousReadsDisallowed, ioEx.Message);

                var ioEx2 = Assert.Throws<InvalidOperationException>(() => context.Request.Body.CopyTo(Stream.Null));
                Assert.Equal(CoreStrings.SynchronousReadsDisallowed, ioEx2.Message);

                var buffer = new byte[5];
                var read = await context.Request.Body.ReadUntilEndAsync(buffer).DefaultTimeout();

                Assert.Equal("Hello", Encoding.ASCII.GetString(buffer, 0, read));
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 5",
                        "",
                        "Hello");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        public static TheoryData<string, string> HostHeaderData => HttpParsingData.HostHeaderData;
    }
}
