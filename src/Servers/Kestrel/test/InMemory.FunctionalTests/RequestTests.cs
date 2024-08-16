// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Text;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class RequestTests : TestApplicationErrorLoggerLoggedTest
{
    [Fact]
    public async Task StreamsAreNotPersistedAcrossRequests()
    {
        var requestBodyPersisted = false;
        var responseBodyPersisted = false;

        await using (var server = new TestServer(async context =>
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
    public async Task PipesAreNotPersistedAcrossRequests()
    {
        var responseBodyPersisted = false;
        PipeWriter bodyPipe = null;
        await using (var server = new TestServer(async context =>
        {
            if (context.Response.BodyWriter == bodyPipe)
            {
                responseBodyPersisted = true;
            }
            bodyPipe = context.Response.BodyWriter;

            await context.Response.WriteAsync("hello, world");
        }, new TestServiceContext(LoggerFactory)))
        {
            Assert.Equal("hello, world", await server.HttpClientSlim.GetStringAsync($"http://localhost:{server.Port}/"));
            Assert.Equal("hello, world", await server.HttpClientSlim.GetStringAsync($"http://localhost:{server.Port}/"));

            Assert.False(responseBodyPersisted);
        }
    }

    [Fact]
    public async Task RequestBodyPipeReaderDoesZeroByteReads()
    {
        await using (var server = new TestServer(async context =>
        {
            var bufferLengths = new List<int>();

            var mockStream = new Mock<Stream>();

            mockStream.Setup(s => s.CanRead).Returns(true);
            mockStream.Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>())).Returns<Memory<byte>, CancellationToken>((buffer, token) =>
            {
                bufferLengths.Add(buffer.Length);
                return ValueTask.FromResult(0);
            });

            context.Request.Body = mockStream.Object;
            var data = await context.Request.BodyReader.ReadAsync();

            Assert.Equal(2, bufferLengths.Count);
            Assert.Equal(0, bufferLengths[0]);
            Assert.Equal(4096, bufferLengths[1]);

            await context.Response.WriteAsync("hello, world");
        }, new TestServiceContext(LoggerFactory)))
        {
            Assert.Equal("hello, world", await server.HttpClientSlim.GetStringAsync($"http://localhost:{server.Port}/"));
        }
    }

    [Fact]
    public async Task RequestBodyReadAsyncCanBeCancelled()
    {
        var helloTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cts = new CancellationTokenSource();

        await using (var server = new TestServer(async context =>
        {
            var data = new byte[6];
            try
            {
                await context.Request.Body.FillEntireBufferAsync(data, cts.Token).DefaultTimeout();

                Assert.Equal("Hello ", Encoding.ASCII.GetString(data));

                helloTcs.TrySetResult();
            }
            catch (Exception ex)
            {
                // This shouldn't fail
                helloTcs.TrySetException(ex);
            }

            try
            {
                var task = context.Request.Body.ReadAsync(data, 0, data.Length, cts.Token);
                readTcs.TrySetResult();
                await task;

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
                await readTcs.Task;

                // Cancel the body after hello is read
                cts.Cancel();

                await connection.Receive($"HTTP/1.1 200 OK",
                       "Content-Length: 14",
                       $"Date: {server.Context.DateHeaderValue}",
                       "",
                       "Read cancelled");
            }
        }
    }

    [Fact]
    public async Task CanUpgradeRequestWithConnectionKeepAliveUpgradeHeader()
    {
        var dataRead = false;

        await using (var server = new TestServer(async context =>
        {
            var stream = await context.Features.Get<IHttpUpgradeFeature>().UpgradeAsync();

            var data = new byte[3];
            await stream.FillEntireBufferAsync(data).DefaultTimeout();

            dataRead = Encoding.ASCII.GetString(data) == "abc";
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
                    $"Date: {server.Context.DateHeaderValue}",
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

        await using (var server = new TestServer(async context =>
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
    public async Task CanHandleTwoAbsoluteFormRequestsInARow()
    {
        // Regression test for https://github.com/dotnet/aspnetcore/issues/18438
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "GET http://localhost/ HTTP/1.1",
                    "Host: localhost",
                    "",
                    "GET http://localhost/ HTTP/1.1",
                    "Host: localhost",
                    "",
                    "");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task ExecutionContextMutationsOfValueTypeDoNotLeakAcrossRequestsOnSameConnection()
    {
        var local = new AsyncLocal<int>();

        // It's important this method isn't async as that will revert the ExecutionContext
        Task ExecuteApplication(HttpContext context)
        {
            var value = local.Value;
            Assert.Equal(0, value);

            context.Response.OnStarting(() =>
            {
                local.Value++;
                return Task.CompletedTask;
            });

            context.Response.OnCompleted(() =>
            {
                local.Value++;
                return Task.CompletedTask;
            });

            local.Value++;
            context.Response.ContentLength = 1;
            return context.Response.WriteAsync($"{value}");
        }

        var testContext = new TestServiceContext(LoggerFactory);

        await using var server = new TestServer(ExecuteApplication, testContext);
        await TestAsyncLocalValues(testContext, server);
    }

    [Fact]
    public async Task ExecutionContextMutationsOfReferenceTypeDoNotLeakAcrossRequestsOnSameConnection()
    {
        var local = new AsyncLocal<IntAsClass>();

        // It's important this method isn't async as that will revert the ExecutionContext
        Task ExecuteApplication(HttpContext context)
        {
            Assert.Null(local.Value);
            local.Value = new IntAsClass();

            var value = local.Value.Value;
            Assert.Equal(0, value);

            context.Response.OnStarting(() =>
            {
                local.Value.Value++;
                return Task.CompletedTask;
            });

            context.Response.OnCompleted(() =>
            {
                local.Value.Value++;
                return Task.CompletedTask;
            });

            local.Value.Value++;
            context.Response.ContentLength = 1;
            return context.Response.WriteAsync($"{value}");
        }

        var testContext = new TestServiceContext(LoggerFactory);

        await using var server = new TestServer(ExecuteApplication, testContext);
        await TestAsyncLocalValues(testContext, server);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    [Fact]
    public async Task ExecutionContextMutationsDoNotLeakAcrossAwaits()
    {
        var local = new AsyncLocal<int>();

        // It's important this method isn't async as that will revert the ExecutionContext
        Task ExecuteApplication(HttpContext context)
        {
            var value = local.Value;
            Assert.Equal(0, value);

            context.Response.OnStarting(async () =>
            {
                local.Value++;
                Assert.Equal(1, local.Value);
            });

            context.Response.OnCompleted(async () =>
            {
                local.Value++;
                Assert.Equal(1, local.Value);
            });

            context.Response.ContentLength = 1;
            return context.Response.WriteAsync($"{value}");
        }

        var testContext = new TestServiceContext(LoggerFactory);

        await using var server = new TestServer(ExecuteApplication, testContext);
        await TestAsyncLocalValues(testContext, server);
    }

    [Fact]
    public async Task ExecutionContextMutationsOfValueTypeFlowIntoButNotOutOfAsyncEvents()
    {
        var local = new AsyncLocal<int>();

        async Task ExecuteApplication(HttpContext context)
        {
            var value = local.Value;
            Assert.Equal(0, value);

            context.Response.OnStarting(async () =>
            {
                local.Value++;
                Assert.Equal(2, local.Value);
            });

            context.Response.OnCompleted(async () =>
            {
                local.Value++;
                Assert.Equal(2, local.Value);
            });

            local.Value++;
            Assert.Equal(1, local.Value);

            context.Response.ContentLength = 1;
            await context.Response.WriteAsync($"{value}");

            local.Value++;
            Assert.Equal(2, local.Value);
        }

        var testContext = new TestServiceContext(LoggerFactory);

        await using var server = new TestServer(ExecuteApplication, testContext);
        await TestAsyncLocalValues(testContext, server);
    }

    [Fact]
    public async Task ExecutionContextMutationsOfReferenceTypeFlowThroughAsyncEvents()
    {
        var local = new AsyncLocal<IntAsClass>();

        async Task ExecuteApplication(HttpContext context)
        {
            Assert.Null(local.Value);
            local.Value = new IntAsClass();

            var value = local.Value.Value;
            Assert.Equal(0, value); // Start

            context.Response.OnStarting(async () =>
            {
                local.Value.Value++;
                Assert.Equal(2, local.Value.Value); // Second
            });

            context.Response.OnCompleted(async () =>
            {
                local.Value.Value++;
                Assert.Equal(4, local.Value.Value); // Fourth
            });

            local.Value.Value++;
            Assert.Equal(1, local.Value.Value); // First

            context.Response.ContentLength = 1;
            await context.Response.WriteAsync($"{value}");

            local.Value.Value++;
            Assert.Equal(3, local.Value.Value); // Third
        }

        var testContext = new TestServiceContext(LoggerFactory);

        await using var server = new TestServer(ExecuteApplication, testContext);
        await TestAsyncLocalValues(testContext, server);
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    [Fact]
    public async Task ExecutionContextMutationsOfValueTypeFlowIntoButNotOutOfNonAsyncEvents()
    {
        var local = new AsyncLocal<int>();

        async Task ExecuteApplication(HttpContext context)
        {
            var value = local.Value;
            Assert.Equal(0, value);

            context.Response.OnStarting(() =>
            {
                local.Value++;
                Assert.Equal(2, local.Value);

                return Task.CompletedTask;
            });

            context.Response.OnCompleted(() =>
            {
                local.Value++;
                Assert.Equal(2, local.Value);

                return Task.CompletedTask;
            });

            local.Value++;
            Assert.Equal(1, local.Value);

            context.Response.ContentLength = 1;
            await context.Response.WriteAsync($"{value}");

            local.Value++;
            Assert.Equal(2, local.Value);
        }

        var testContext = new TestServiceContext(LoggerFactory);

        await using var server = new TestServer(ExecuteApplication, testContext);
        await TestAsyncLocalValues(testContext, server);
    }

    private static async Task TestAsyncLocalValues(TestServiceContext testContext, TestServer server)
    {
        using var connection = server.CreateConnection();

        await connection.Send(
            "GET / HTTP/1.1",
            "Host:",
            "",
            "");

        await connection.Receive(
            "HTTP/1.1 200 OK",
            "Content-Length: 1",
            $"Date: {testContext.DateHeaderValue}",
            "",
            "0");

        await connection.Send(
            "GET / HTTP/1.1",
            "Host:",
            "",
            "");

        await connection.Receive(
            "HTTP/1.1 200 OK",
            "Content-Length: 1",
            $"Date: {testContext.DateHeaderValue}",
            "",
            "0");
    }

    [Fact]
    public async Task AppCanSetTraceIdentifier()
    {
        const string knownId = "xyz123";
        await using (var server = new TestServer(async context =>
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

        await using (var server = new TestServer(async context =>
        {
            Assert.Equal(identifierLength, Encoding.ASCII.GetByteCount(context.TraceIdentifier));
            context.Response.ContentLength = identifierLength;
            await context.Response.WriteAsync(context.TraceIdentifier);
        }, new TestServiceContext(LoggerFactory)))
        {
            var usedIds = new ConcurrentBag<string>();

            // requests on separate connections in parallel
            var tasks = new List<Task>(iterations);
            for (var i = 0; i < iterations; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var id = await server.HttpClientSlim.GetStringAsync($"http://localhost:{server.Port}/");
                    Assert.DoesNotContain(id, usedIds.ToArray());
                    usedIds.Add(id);
                }));
            }
            await Task.WhenAll(tasks);

            // requests on same connection
            using (var connection = server.CreateConnection())
            {
                var buffer = new char[identifierLength];
                for (var i = 0; i < iterations; i++)
                {
                    await connection.SendEmptyGet();

                    await connection.Receive($"HTTP/1.1 200 OK",
                       $"Content-Length: {identifierLength}",
                       $"Date: {server.Context.DateHeaderValue}",
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

        await using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
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
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "HTTP/1.1 200 OK",
                    "Content-Length: 7",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Goodbye");
            }
        }
    }

    [Fact]
    public async Task Http10NotKeptAliveByDefault()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(TestApp.EchoApp, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "GET / HTTP/1.0",
                    "",
                    "");
                await connection.ReceiveEnd(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
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

        await using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
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
                    "Content-Length: 0",
                    "Connection: keep-alive",
                    $"Date: {testContext.DateHeaderValue}",
                    "\r\n");
                await connection.ReceiveEnd(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 7",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Goodbye");
            }
        }
    }

    [Fact]
    public async Task Http10KeepAliveNotHonoredIfResponseContentLengthNotSet()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(TestApp.EchoApp, testContext))
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
                    "Content-Length: 0",
                    "Connection: keep-alive",
                    $"Date: {testContext.DateHeaderValue}",
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

        await using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
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
                    "Content-Length: 11",
                    "Connection: keep-alive",
                    $"Date: {testContext.DateHeaderValue}",
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
                    "Content-Length: 11",
                    "Connection: keep-alive",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello Again");

                await connection.Send(
                    "POST / HTTP/1.0",
                    "Content-Length: 7",
                    "",
                    "Goodbye");

                await connection.ReceiveEnd(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 7",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Goodbye");
            }
        }
    }

    [Fact]
    public async Task Expect100ContinueHonored()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
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
                    "Content-Length: 11",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World");
            }
        }
    }

    [Fact]
    public async Task Expect100ContinueHonoredWhenMinRequestBodyDataRateIsDisabled()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        // This may seem unrelated, but this is a regression test for
        // https://github.com/dotnet/aspnetcore/issues/30449
        testContext.ServerOptions.Limits.MinRequestBodyDataRate = null;

        await using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
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
                    "Content-Length: 11",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World");
            }
        }
    }

    [Fact]
    public async Task ZeroContentLengthAssumedOnNonKeepAliveRequestsWithoutContentLengthOrTransferEncodingHeader()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async httpContext =>
        {
            // This will hang if 0 content length is not assumed by the server
            Assert.Equal(0, await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1).DefaultTimeout());
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "Connection: close",
                    "",
                    "");
                await connection.ReceiveEnd(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
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
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task ZeroContentLengthAssumedOnNonKeepAliveRequestsWithoutContentLengthOrTransferEncodingHeaderPipeReader()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async httpContext =>
        {
            var readResult = await httpContext.Request.BodyReader.ReadAsync().AsTask().DefaultTimeout();
            // This will hang if 0 content length is not assumed by the server
            Assert.True(readResult.IsCompleted);
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "Connection: close",
                    "",
                    "");
                await connection.ReceiveEnd(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
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
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task ContentLengthReadAsyncPipeReader()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async httpContext =>
        {
            var readResult = await httpContext.Request.BodyReader.ReadAsync();
            // This will hang if 0 content length is not assumed by the server
            Assert.Equal(5, readResult.Buffer.Length);
            httpContext.Request.BodyReader.AdvanceTo(readResult.Buffer.End);
        }, testContext))
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
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task ContentLengthReadAsyncPipeReaderBufferRequestBody()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async httpContext =>
        {
            var readResult = await httpContext.Request.BodyReader.ReadAsync();
            // This will hang if 0 content length is not assumed by the server
            Assert.Equal(5, readResult.Buffer.Length);
            httpContext.Request.BodyReader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
            readResult = await httpContext.Request.BodyReader.ReadAsync();
            Assert.Equal(5, readResult.Buffer.Length);
            httpContext.Request.BodyReader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
        }, testContext))
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
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task ContentLengthReadAsyncPipeReaderBufferRequestBodyMultipleTimes()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async httpContext =>
        {
            var readResult = await httpContext.Request.BodyReader.ReadAsync();
            // This will hang if 0 content length is not assumed by the server
            Assert.Equal(5, readResult.Buffer.Length);
            httpContext.Request.BodyReader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);

            for (var i = 0; i < 2; i++)
            {
                readResult = await httpContext.Request.BodyReader.ReadAsync();
                Assert.Equal(5, readResult.Buffer.Length);
                httpContext.Request.BodyReader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
            }
        }, testContext))
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
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task ContentLengthReadAsyncPipeReaderReadsCompletedBody()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async httpContext =>
        {
            using var ms1 = new MemoryStream();
            using var ms2 = new MemoryStream();

            // Read the body completely, and ensure the second read doesn't fail
            await httpContext.Request.BodyReader.CopyToAsync(ms1);
            await httpContext.Request.BodyReader.CopyToAsync(ms2);

            Assert.Equal(22, ms1.ToArray().Length);
            Assert.Empty(ms2.ToArray());
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.0",
                    "Host:",
                    "Content-Length: 22",
                    "",
                    "MyVariableOne=ValueOne");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task ContentLengthReadAsyncSingleBytesAtATime()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var testContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tcs2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        static async Task<ReadResult> ReadAtLeastAsync(PipeReader reader, int numBytes)
        {
            var result = await reader.ReadAsync();

            while (!result.IsCompleted && result.Buffer.Length < numBytes)
            {
                reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                result = await reader.ReadAsync();
            }

            if (result.Buffer.Length < numBytes)
            {
                throw new IOException("Unexpected end of content.");
            }

            return result;
        }

        await using (var server = new TestServer(async httpContext =>
        {
            // Buffer 3 bytes.
            var readResult = await ReadAtLeastAsync(httpContext.Request.BodyReader, numBytes: 3);
            Assert.Equal(3, readResult.Buffer.Length);
            tcs.SetResult();

            httpContext.Request.BodyReader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);

            // Buffer 1 more byte.
            readResult = await httpContext.Request.BodyReader.ReadAsync();
            httpContext.Request.BodyReader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
            tcs2.SetResult();

            // Buffer 1 last byte.
            readResult = await httpContext.Request.BodyReader.ReadAsync();
            Assert.Equal(5, readResult.Buffer.Length);

            // Do one more read to ensure completion is always observed.
            httpContext.Request.BodyReader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
            readResult = await httpContext.Request.BodyReader.ReadAsync();
            Assert.True(readResult.IsCompleted);
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.0",
                    "Host:",
                    "Content-Length: 5",
                    "",
                    "fun");
                await tcs.Task.DefaultTimeout();
                await connection.Send(
                    "n");
                await tcs2.Task.DefaultTimeout();
                await connection.Send(
                    "y");
                await connection.ReceiveEnd(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.UnexpectedEndOfRequestContent, m.Tags));
    }

    [Fact]
    public async Task ContentLengthDoesNotConsumeEntireBufferDoesNotThrow()
    {
        var testContext = new TestServiceContext(LoggerFactory);
        await using (var server = new TestServer(async httpContext =>
        {
            var readResult = await httpContext.Request.BodyReader.ReadAsync();

            httpContext.Request.BodyReader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);

            readResult = await httpContext.Request.BodyReader.ReadAsync();
            httpContext.Request.BodyReader.AdvanceTo(readResult.Buffer.Slice(1).Start, readResult.Buffer.End);
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(
                    "POST / HTTP/1.0",
                    "Host:",
                    "Content-Length: 5",
                    "",
                    "funny");

                await connection.ReceiveEnd(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact(Skip = "This test is racy and requires a product change.")]
    public async Task ConnectionClosesWhenFinReceivedBeforeRequestCompletes()
    {
        var testContext = new TestServiceContext(LoggerFactory)
        {
            Scheduler = PipeScheduler.Inline
        };

        await using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1");
                connection.ShutdownSend();
                await connection.TransportConnection.WaitForCloseTask;
                await connection.ReceiveEnd();
            }

            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 7");
                connection.ShutdownSend();
                await connection.TransportConnection.WaitForCloseTask;
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

        await using (var server = new TestServer(httpContext =>
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
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
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

        await using (var server = new TestServer(async context =>
        {
            var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();
            var duplexStream = await upgradeFeature.UpgradeAsync();

            var data = new byte[message.Length];
            await duplexStream.FillEntireBufferAsync(data).DefaultTimeout();

            await duplexStream.WriteAsync(data, 0, data.Length);
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

        await using (var server = new TestServer(async context =>
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
                        "Content-Length: 0",
                        $"Date: {testContext.DateHeaderValue}",
                        ""});

                var lastResponse = string.Join("\r\n", new string[]
                {
                        "HTTP/1.1 200 OK",
                        "Content-Length: 7",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
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
        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
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
        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task ServerConsumesKeepAliveChunkedRequest()
    {
        // The app doesn't read the request body, so it should be consumed by the server
        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task NonKeepAliveRequestNotConsumedByAppCompletes()
    {
        // The app doesn't read the request body, so it should be consumed by the server
        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
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
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task UpgradedRequestNotConsumedByAppCompletes()
    {
        // The app doesn't read the request body, so it should be consumed by the server
        await using (var server = new TestServer(async context =>
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
        var appEvent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var delayEvent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var serviceContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async context =>
        {
            context.Features.Get<IHttpMinRequestBodyDataRateFeature>().MinDataRate =
                new MinDataRate(bytesPerSecond: double.MaxValue, gracePeriod: Heartbeat.Interval + TimeSpan.FromTicks(1));

            using (var stream = await context.Features.Get<IHttpUpgradeFeature>().UpgradeAsync())
            {
                appEvent.SetResult();

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

                serviceContext.FakeTimeProvider.Advance(TimeSpan.FromSeconds(5));
                serviceContext.ConnectionManager.OnHeartbeat();

                delayEvent.SetResult();

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
    public async Task SynchronousReadsDisallowedByDefault()
    {
        await using (var server = new TestServer(async context =>
        {
            var bodyControlFeature = context.Features.Get<IHttpBodyControlFeature>();
            Assert.False(bodyControlFeature.AllowSynchronousIO);

            var buffer = new byte[6];
            var offset = 0;

            // The request body is 5 bytes long. The 6th byte (buffer[5]) is only used for writing the response body.
            buffer[5] = (byte)'1';

            // Synchronous reads throw.
            var ioEx = Assert.Throws<InvalidOperationException>(() => context.Request.Body.Read(new byte[1], 0, 1));
            Assert.Equal(CoreStrings.SynchronousReadsDisallowed, ioEx.Message);

            var ioEx2 = Assert.Throws<InvalidOperationException>(() => context.Request.Body.CopyTo(Stream.Null));
            Assert.Equal(CoreStrings.SynchronousReadsDisallowed, ioEx2.Message);

            while (offset < 5)
            {
                offset += await context.Request.Body.ReadAsync(buffer, offset, 5 - offset);
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
                    "Hello");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 6",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "Hello1");
            }
        }
    }

    [Fact]
    public async Task SynchronousReadsAllowedByOptIn()
    {
        await using (var server = new TestServer(async context =>
        {
            var bodyControlFeature = context.Features.Get<IHttpBodyControlFeature>();
            Assert.False(bodyControlFeature.AllowSynchronousIO);

            var buffer = new byte[5];
            var offset = 0;

            bodyControlFeature.AllowSynchronousIO = true;

            while (offset < 5)
            {
                offset += context.Request.Body.Read(buffer, offset, 5 - offset);
            }

            Assert.Equal(0, await context.Request.Body.ReadAsync(new byte[1], 0, 1));
            Assert.Equal("Hello", Encoding.ASCII.GetString(buffer, 0, 5));

            context.Response.ContentLength = 5;
            await context.Response.Body.WriteAsync(buffer, 0, 5);
        }, new TestServiceContext(LoggerFactory)))
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
                    "Content-Length: 5",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "Hello");
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

        await using (var server = new TestServer(async context =>
        {
            var bodyControlFeature = context.Features.Get<IHttpBodyControlFeature>();
            Assert.False(bodyControlFeature.AllowSynchronousIO);

            // Synchronous reads now throw.
            var ioEx = Assert.Throws<InvalidOperationException>(() => context.Request.Body.Read(new byte[1], 0, 1));
            Assert.Equal(CoreStrings.SynchronousReadsDisallowed, ioEx.Message);

            var ioEx2 = Assert.Throws<InvalidOperationException>(() => context.Request.Body.CopyTo(Stream.Null));
            Assert.Equal(CoreStrings.SynchronousReadsDisallowed, ioEx2.Message);

            var buffer = new byte[5];
            var length = await context.Request.Body.FillBufferUntilEndAsync(buffer).DefaultTimeout();

            Assert.Equal(5, length);
            Assert.Equal("Hello", Encoding.ASCII.GetString(buffer));
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task SynchronousReadsCanBeAllowedGlobally()
    {
        var testContext = new TestServiceContext(LoggerFactory)
        {
            ServerOptions = { AllowSynchronousIO = true }
        };

        await using (var server = new TestServer(async context =>
        {
            var bodyControlFeature = context.Features.Get<IHttpBodyControlFeature>();
            Assert.True(bodyControlFeature.AllowSynchronousIO);

            int offset = 0;
            var buffer = new byte[5];
            while (offset < 5)
            {
                offset += context.Request.Body.Read(buffer, offset, 5 - offset);
            }

            Assert.Equal(0, await context.Request.Body.ReadAsync(new byte[1], 0, 1));
            Assert.Equal("Hello", Encoding.ASCII.GetString(buffer, 0, 5));
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task ContentLengthSwallowedUnexpectedEndOfRequestContentDoesNotResultInWarnings()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async httpContext =>
        {
            try
            {
                await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1);
            }
            catch
            {
            }
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 5",
                    "",
                    "");
                connection.ShutdownSend();

                await connection.ReceiveEnd();
            }
        }

        Assert.Empty(LogMessages.Where(m => m.LogLevel >= LogLevel.Warning));
    }

    [Fact]
    public async Task ContentLengthRequestCallCancelPendingReadWorks()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async httpContext =>
        {
            var response = httpContext.Response;
            var request = httpContext.Request;

            Assert.Equal("POST", request.Method);

            var readResult = await request.BodyReader.ReadAsync();
            request.BodyReader.AdvanceTo(readResult.Buffer.End);

            var requestTask = httpContext.Request.BodyReader.ReadAsync();

            httpContext.Request.BodyReader.CancelPendingRead();

            Assert.True((await requestTask).IsCanceled);

            tcs.SetResult();

            response.Headers["Content-Length"] = new[] { "11" };

            await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello World"), 0, 11));

        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 5",
                    "",
                    "H");
                await tcs.Task;
                await connection.Send(
                    "ello");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 11",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World");
            }
        }
    }

    [Fact]
    public async Task ContentLengthRequestCallCompleteThrowsExceptionOnRead()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async httpContext =>
        {
            var response = httpContext.Response;
            var request = httpContext.Request;

            Assert.Equal("POST", request.Method);

            var readResult = await request.BodyReader.ReadAsync();
            request.BodyReader.AdvanceTo(readResult.Buffer.End);

            httpContext.Request.BodyReader.Complete();

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await request.BodyReader.ReadAsync());

            response.Headers["Content-Length"] = new[] { "11" };

            await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello World"), 0, 11));

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
                    "Content-Length: 11",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World");
            }
        }
    }

    [Fact]
    public async Task ContentLengthRequestCallCompleteDoesNotCauseException()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using (var server = new TestServer(async httpContext =>
        {
            var request = httpContext.Request;

            var readResult = await request.BodyReader.ReadAsync();
            request.BodyReader.AdvanceTo(readResult.Buffer.End);

            httpContext.Request.BodyReader.Complete();

            tcs.SetResult();

        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 5",
                    "",
                    "He");
                await tcs.Task;
                await connection.Send("llo");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.All(TestSink.Writes, w => Assert.InRange(w.LogLevel, LogLevel.Trace, LogLevel.Information));
    }

    [Fact]
    public async Task ContentLengthCallCompleteWithExceptionCauses500()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async httpContext =>
        {
            var response = httpContext.Response;
            var request = httpContext.Request;

            Assert.Equal("POST", request.Method);
            Assert.True(request.CanHaveBody());
            var readResult = await request.BodyReader.ReadAsync();
            request.BodyReader.AdvanceTo(readResult.Buffer.End);

            httpContext.Request.BodyReader.Complete(new Exception());

            response.Headers["Content-Length"] = new[] { "11" };

            await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello World"), 0, 11));

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
                    "HTTP/1.1 500 Internal Server Error",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task ReuseRequestHeaderStrings()
    {
        var testContext = new TestServiceContext(LoggerFactory);
        string customHeaderValue = null;
        string contentTypeHeaderValue = null;

        await using (var server = new TestServer(context =>
        {
            customHeaderValue = context.Request.Headers["X-CustomHeader"];
            contentTypeHeaderValue = context.Request.ContentType;
            return Task.CompletedTask;
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                // First request
                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "Content-Type: application/test",
                    "X-CustomHeader: customvalue",
                    "",
                    "");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");

                var initialCustomHeaderValue = customHeaderValue;
                var initialContentTypeValue = contentTypeHeaderValue;

                // Second request
                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "Content-Type: application/test",
                    "X-CustomHeader: customvalue",
                    "",
                    "");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");

                Assert.NotSame(initialCustomHeaderValue, customHeaderValue);
                Assert.Same(initialContentTypeValue, contentTypeHeaderValue);
            }
        }
    }

    [Fact]
    public async Task PersistentStateBetweenRequests()
    {
        var testContext = new TestServiceContext(LoggerFactory);
        object persistedState = null;
        var requestCount = 0;

        await using (var server = new TestServer(context =>
        {
            requestCount++;
            var persistentStateCollection = context.Features.Get<IPersistentStateFeature>().State;
            if (persistentStateCollection.TryGetValue("Counter", out var value))
            {
                persistedState = value;
            }
            persistentStateCollection["Counter"] = requestCount;
            return Task.CompletedTask;
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                // First request
                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "Content-Type: application/test",
                    "X-CustomHeader: customvalue",
                    "",
                    "");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
                var firstRequestState = persistedState;

                // Second request
                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "Content-Type: application/test",
                    "X-CustomHeader: customvalue",
                    "",
                    "");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
                var secondRequestState = persistedState;

                // First request has no persisted state
                Assert.Null(firstRequestState);

                // State persisted on first request was available on the second request
                Assert.Equal(1, secondRequestState);
            }
        }
    }

    [Fact]
    public async Task Latin1HeaderValueAcceptedWhenLatin1OptionIsConfigured()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        testContext.ServerOptions.RequestHeaderEncodingSelector = _ => Encoding.Latin1;

        await using (var server = new TestServer(context =>
        {
            Assert.Equal("£", context.Request.Headers["X-Test"]);
            return Task.CompletedTask;
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                // The StreamBackedTestConnection will encode £ using the "iso-8859-1" aka Latin1 encoding.
                // It will be encoded as 0xA3 which isn't valid UTF-8.
                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "X-Test: £",
                    "",
                    "");

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task Latin1HeaderValueRejectedWhenLatin1OptionIsNotConfigured()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(_ => Task.CompletedTask, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                // The StreamBackedTestConnection will encode £ using the "iso-8859-1" aka Latin1 encoding.
                // It will be encoded as 0xA3 which isn't valid UTF-8.
                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "X-Test: £",
                    "",
                    "");

                await connection.ReceiveEnd(
                    "HTTP/1.1 400 Bad Request",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task TlsOverHttp()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var testContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

        await using (var server = new TestServer(context =>
        {
            return Task.CompletedTask;
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Stream.WriteAsync(new byte[] { 0x16, 0x03, 0x01, 0x02, 0x00, 0x01, 0x00, 0xfc, 0x03, 0x03, 0x03, 0xca, 0xe0, 0xfd, 0x0a }).DefaultTimeout();

                await connection.ReceiveEnd(
                    "HTTP/1.1 400 Bad Request",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.TlsNotSupported, m.Tags));
    }

    [Fact]
    public async Task CustomRequestHeaderEncodingSelectorCanBeConfigured()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        testContext.ServerOptions.RequestHeaderEncodingSelector = _ => Encoding.UTF32;

        await using (var server = new TestServer(context =>
        {
            Assert.Equal("£", context.Request.Headers["X-Test"]);
            return Task.CompletedTask;
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "X-Test: ");

                await connection.Stream.WriteAsync(Encoding.UTF32.GetBytes("£")).DefaultTimeout();

                await connection.Send("",
                    "",
                    "");

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task SingleLineFeedIsSupportedAnywhere()
    {
        // Exercises all combinations of LF and CRLF as line separators.
        // Uses a bit mask for all the possible combinations.

        var lines = new[]
        {
                $"GET / HTTP/1.1",
                "Content-Length: 0",
                $"Host: localhost",
                "",
            };

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory, disableHttp1LineFeedTerminators: false)))
        {
            var mask = Math.Pow(2, lines.Length) - 1;

            for (var m = 0; m <= mask; m++)
            {
                using (var client = server.CreateConnection())
                {
                    var sb = new StringBuilder();

                    for (var pos = 0; pos < lines.Length; pos++)
                    {
                        sb.Append(lines[pos]);
                        var separator = (m & (1 << pos)) != 0 ? "\n" : "\r\n";
                        sb.Append(separator);
                    }

                    var text = sb.ToString();
                    var writer = new StreamWriter(client.Stream, Encoding.GetEncoding("iso-8859-1"));
                    await writer.WriteAsync(text).ConfigureAwait(false);
                    await writer.FlushAsync().ConfigureAwait(false);
                    await client.Stream.FlushAsync().ConfigureAwait(false);

                    await client.Receive("HTTP/1.1 200");
                }
            }
        }
    }

    public static TheoryData<string, string> HostHeaderData => HttpParsingData.HostHeaderData;

    private class IntAsClass
    {
        public int Value;
    }
}
