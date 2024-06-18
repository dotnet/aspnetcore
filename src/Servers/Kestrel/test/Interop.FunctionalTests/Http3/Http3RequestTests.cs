// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Primitives;

namespace Interop.FunctionalTests.Http3;

[Collection(nameof(NoParallelCollection))]
public class Http3RequestTests : LoggedTest
{
    private class StreamingHttpContent : HttpContent
    {
        private readonly TaskCompletionSource _completeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<Stream> _getStreamTcs = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            throw new NotSupportedException();
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken cancellationToken)
        {
            _getStreamTcs.TrySetResult(stream);

            var cancellationTcs = new TaskCompletionSource();
            cancellationToken.Register(() => cancellationTcs.TrySetCanceled());

            await Task.WhenAny(_completeTcs.Task, cancellationTcs.Task);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        public Task<Stream> GetStreamAsync()
        {
            return _getStreamTcs.Task;
        }

        public void CompleteStream()
        {
            _completeTcs.TrySetResult();
        }
    }

    private static readonly byte[] TestData = Encoding.UTF8.GetBytes("Hello world");

    [ConditionalFact]
    [MsQuicSupported]
    public async Task GET_Metrics_HttpProtocolAndTlsSet()
    {
        // Arrange
        var builder = CreateHostBuilder(context => Task.CompletedTask);

        using (var host = builder.Build())
        {
            var meterFactory = host.Services.GetRequiredService<IMeterFactory>();

            using var connectionDuration = new MetricCollector<double>(meterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

            await host.StartAsync();
            var client = HttpHelpers.CreateClient();

            // Act
            var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request1.Version = HttpVersion.Version30;
            request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response1 = await client.SendAsync(request1, CancellationToken.None);
            response1.EnsureSuccessStatusCode();

            // Dispose the client to end the connection.
            client.Dispose();
            // Wait for measurement to be available.
            await connectionDuration.WaitForMeasurementsAsync(minCount: 1).DefaultTimeout();

            // Assert
            Assert.Collection(connectionDuration.GetMeasurementSnapshot(),
                m =>
                {
                    Assert.True(m.Value > 0);
                    Assert.Equal("ipv4", (string)m.Tags["network.type"]);
                    Assert.Equal("http", (string)m.Tags["network.protocol.name"]);
                    Assert.Equal("3", (string)m.Tags["network.protocol.version"]);
                    Assert.Equal("udp", (string)m.Tags["network.transport"]);
                    Assert.Equal("127.0.0.1", (string)m.Tags["server.address"]);
                    Assert.Equal(host.GetPort(), (int)m.Tags["server.port"]);
                    Assert.Equal("1.3", (string)m.Tags["tls.protocol.version"]);
                });

            await host.StopAsync();
        }
    }

    // Verify HTTP/2 and HTTP/3 match behavior
    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(HttpProtocols.Http3)]
    [InlineData(HttpProtocols.Http2)]
    public async Task GET_MiddlewareIsRunWithConnectionLoggingScopeForHttpRequests(HttpProtocols protocol)
    {
        // Arrange
        var expectedLogMessage = "Log from connection scope!";
        string connectionIdFromFeature = null;

        var mockScopeLoggerProvider = new MockScopeLoggerProvider(expectedLogMessage);
        LoggerFactory.AddProvider(mockScopeLoggerProvider);

        var builder = CreateHostBuilder(async context =>
        {
            connectionIdFromFeature = context.Features.Get<IConnectionIdFeature>().ConnectionId;

            var logger = context.RequestServices.GetRequiredService<ILogger<Http3RequestTests>>();
            logger.LogInformation(expectedLogMessage);

            await context.Response.WriteAsync("hello, world");
        }, protocol: protocol);

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync().DefaultTimeout();

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request.Version = GetProtocol(protocol);
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // Act
            var responseMessage = await client.SendAsync(request, CancellationToken.None).DefaultTimeout();

            // Assert
            Assert.Equal("hello, world", await responseMessage.Content.ReadAsStringAsync());

            Assert.NotNull(connectionIdFromFeature);
            Assert.NotNull(mockScopeLoggerProvider.LogScope);
            Assert.Equal(connectionIdFromFeature, mockScopeLoggerProvider.LogScope[0].Value);
        }
    }

    private class MockScopeLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly string _expectedLogMessage;
        private IExternalScopeProvider _scopeProvider;

        public MockScopeLoggerProvider(string expectedLogMessage)
        {
            _expectedLogMessage = expectedLogMessage;
        }

        public IReadOnlyList<KeyValuePair<string, object>> LogScope { get; private set; }

        public ILogger CreateLogger(string categoryName)
        {
            return new MockScopeLogger(this);
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public void Dispose()
        {
        }

        private class MockScopeLogger : ILogger
        {
            private readonly MockScopeLoggerProvider _loggerProvider;

            public MockScopeLogger(MockScopeLoggerProvider parent)
            {
                _loggerProvider = parent;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return _loggerProvider._scopeProvider?.Push(state);
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (formatter(state, exception) != _loggerProvider._expectedLogMessage)
                {
                    return;
                }

                _loggerProvider._scopeProvider?.ForEachScope(
                    (scopeObject, loggerPovider) =>
                    {
                        loggerPovider.LogScope ??= scopeObject as IReadOnlyList<KeyValuePair<string, object>>;
                    },
                    _loggerProvider);
            }
        }
    }

    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(HttpProtocols.Http3, 11)]
    [InlineData(HttpProtocols.Http3, 1024)]
    [InlineData(HttpProtocols.Http2, 11)]
    [InlineData(HttpProtocols.Http2, 1024)]
    public async Task GET_ServerStreaming_ClientReadsPartialResponse(HttpProtocols protocol, int clientBufferSize)
    {
        // Arrange
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var builder = CreateHostBuilder(async context =>
        {
            await context.Response.Body.WriteAsync(TestData);

            await tcs.Task;

            await context.Response.Body.WriteAsync(TestData);
        }, protocol: protocol);

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync();

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request.Version = GetProtocol(protocol);
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // Act
            var response = await client.SendAsync(request, CancellationToken.None);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(GetProtocol(protocol), response.Version);

            var responseStream = await response.Content.ReadAsStreamAsync().DefaultTimeout();

            await responseStream.ReadAtLeastLengthAsync(TestData.Length, clientBufferSize).DefaultTimeout();

            tcs.SetResult();
            await responseStream.ReadAtLeastLengthAsync(TestData.Length, clientBufferSize).DefaultTimeout();

            await host.StopAsync();
        }
    }

    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(HttpProtocols.Http3)]
    [InlineData(HttpProtocols.Http2)]
    public async Task POST_ClientSendsOnlyHeaders_RequestReceivedOnServer(HttpProtocols protocol)
    {
        // Arrange
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);

        var builder = CreateHostBuilder(context =>
        {
            return Task.CompletedTask;
        }, protocol: protocol);

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync();

            var requestContent = new StreamingHttpContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://127.0.0.1:{host.GetPort()}/");
            request.Content = requestContent;
            request.Version = GetProtocol(protocol);
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // Act
            var responseTask = client.SendAsync(request, CancellationToken.None).DefaultTimeout();

            var requestStream = await requestContent.GetStreamAsync().DefaultTimeout();

            // Send headers
            await requestStream.FlushAsync().DefaultTimeout();

            var response = await responseTask.DefaultTimeout();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(GetProtocol(protocol), response.Version);

            await host.StopAsync();
        }
    }

    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/52573")]
    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(HttpProtocols.Http3)]
    [InlineData(HttpProtocols.Http2)]
    public async Task POST_MultipleRequests_PooledStreamAndHeaders(HttpProtocols protocol)
    {
        // Arrange
        string contentType = null;
        string authority = null;
        var builder = CreateHostBuilder(async context =>
        {
            contentType = context.Request.ContentType;
            authority = context.Request.Host.Value;

            var data = await context.Request.Body.ReadUntilEndAsync();
            await context.Response.Body.WriteAsync(data);
        }, protocol: protocol);

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync();

            // Act
            var response1 = await SendRequestAsync(protocol, host, client);
            var contentType1 = contentType;
            var authority1 = authority;

            if (protocol == HttpProtocols.Http3)
            {
                await WaitForLogAsync(logs =>
                {
                    return logs.Any(w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Transport.Quic" &&
                                         w.EventId.Name == "StreamPooled");
                }, "Wait for server to finish pooling stream.");
            }

            var response2 = await SendRequestAsync(protocol, host, client);
            var contentType2 = contentType;
            var authority2 = authority;

            // Assert
            Assert.NotNull(contentType1);
            Assert.NotNull(authority1);

            // We're testing `Same`, specifically, since we're trying to detect cache misses
            Assert.Same(contentType1, contentType2);
            Assert.Same(authority1, authority2);

            await host.StopAsync();
        }

        static async Task<HttpResponseMessage> SendRequestAsync(HttpProtocols protocol, IHost host, HttpMessageInvoker client)
        {
            var requestContent = new StreamingHttpContent();
            requestContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://127.0.0.1:{host.GetPort()}/");
            request.Content = requestContent;
            request.Version = GetProtocol(protocol);
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var responseTask = client.SendAsync(request, CancellationToken.None).DefaultTimeout();

            var requestStream = await requestContent.GetStreamAsync().DefaultTimeout();

            await requestStream.WriteAsync(new byte[] { 1, 2, 3 }).DefaultTimeout();

            requestContent.CompleteStream();

            var response = await responseTask.DefaultTimeout();
            response.EnsureSuccessStatusCode();

            await response.Content.ReadAsByteArrayAsync();

            return response;
        }
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task POST_ServerCompletesWithoutReadingRequestBody_ClientGetsResponse()
    {
        // Arrange
        var builder = CreateHostBuilder(async context =>
        {
            var body = context.Request.Body;

            var data = await body.ReadAtLeastLengthAsync(TestData.Length).DefaultTimeout();

            await context.Response.Body.WriteAsync(data);
        });

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync().DefaultTimeout();

            var requestContent = new StreamingHttpContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://127.0.0.1:{host.GetPort()}/");
            request.Content = requestContent;
            request.Version = HttpVersion.Version30;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // Act
            var responseTask = client.SendAsync(request, CancellationToken.None);

            var requestStream = await requestContent.GetStreamAsync().DefaultTimeout();

            // Send headers
            await requestStream.FlushAsync().DefaultTimeout();
            // Write content
            await requestStream.WriteAsync(TestData).DefaultTimeout();

            var response = await responseTask.DefaultTimeout();

            requestContent.CompleteStream();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version30, response.Version);
            var responseText = await response.Content.ReadAsStringAsync().DefaultTimeout();
            Assert.Equal("Hello world", responseText);

            await host.StopAsync().DefaultTimeout();
        }
    }

    // Verify HTTP/2 and HTTP/3 match behavior
    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(HttpProtocols.Http3)]
    [InlineData(HttpProtocols.Http2)]
    public async Task POST_ClientCancellationUpload_RequestAbortRaised(HttpProtocols protocol)
    {
        // Arrange
        var syncPoint = new SyncPoint();
        var cancelledTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readAsyncTask = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);

        var builder = CreateHostBuilder(async context =>
        {
            context.RequestAborted.Register(() =>
            {
                Logger.LogInformation("Server received cancellation");
                cancelledTcs.SetResult();
            });

            var body = context.Request.Body;

            Logger.LogInformation("Server reading content");
            await body.ReadAtLeastLengthAsync(TestData.Length).DefaultTimeout();

            // Sync with client
            await syncPoint.WaitToContinue();

            Logger.LogInformation("Server waiting for cancellation");
            await cancelledTcs.Task;

            readAsyncTask.SetResult(body.ReadAsync(new byte[1024]).AsTask());
        }, protocol: protocol);

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync().DefaultTimeout();

            var cts = new CancellationTokenSource();
            var requestContent = new StreamingHttpContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://127.0.0.1:{host.GetPort()}/");
            request.Content = requestContent;
            request.Version = GetProtocol(protocol);
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // Act
            var responseTask = client.SendAsync(request, cts.Token);

            var requestStream = await requestContent.GetStreamAsync().DefaultTimeout();

            Logger.LogInformation("Client sending request headers");
            await requestStream.FlushAsync().DefaultTimeout();

            Logger.LogInformation("Client sending request content");
            await requestStream.WriteAsync(TestData).DefaultTimeout();
            await requestStream.FlushAsync().DefaultTimeout();

            Logger.LogInformation("Client waiting until content is read on server");
            await syncPoint.WaitForSyncPoint().DefaultTimeout();

            Logger.LogInformation("Client cancelling");
            cts.Cancel();

            // Continue on server
            syncPoint.Continue();

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => responseTask).DefaultTimeout();

            await cancelledTcs.Task.DefaultTimeout();

            var serverReadTask = await readAsyncTask.Task.DefaultTimeout();

            var serverEx = await Assert.ThrowsAsync<IOException>(() => serverReadTask).DefaultTimeout();
            Assert.Equal("The client reset the request stream.", serverEx.Message);

            await host.StopAsync().DefaultTimeout();
        }
    }

    // Verify HTTP/2 and HTTP/3 match behavior
    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(HttpProtocols.Http3)]
    [InlineData(HttpProtocols.Http2)]
    public async Task POST_ServerAbort_ClientReceivesAbort(HttpProtocols protocol)
    {
        // Arrange
        var syncPoint = new SyncPoint();
        var cancelledTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readAsyncTask = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);

        var builder = CreateHostBuilder(async context =>
        {
            context.RequestAborted.Register(() => cancelledTcs.SetResult());

            context.Abort();

            // Sync with client
            await syncPoint.WaitToContinue();

            readAsyncTask.SetResult(context.Request.Body.ReadAsync(new byte[1024]).AsTask());
        }, protocol: protocol);

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync().DefaultTimeout();

            var requestContent = new StreamingHttpContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://127.0.0.1:{host.GetPort()}/");
            request.Content = requestContent;
            request.Version = GetProtocol(protocol);
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // Act
            var sendTask = client.SendAsync(request, CancellationToken.None);

            var requestStream = await requestContent.GetStreamAsync().DefaultTimeout();
            Logger.LogInformation("Client sending request headers");
            await requestStream.FlushAsync().DefaultTimeout();

            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => sendTask).DefaultTimeout();

            // Assert
            if (protocol == HttpProtocols.Http3)
            {
                var innerEx = Assert.IsType<HttpProtocolException>(ex.InnerException);
                Assert.Equal(Http3ErrorCode.InternalError, (Http3ErrorCode)innerEx.ErrorCode);
            }

            await cancelledTcs.Task.DefaultTimeout();

            // Sync with server to ensure RequestDelegate is still running
            await syncPoint.WaitForSyncPoint().DefaultTimeout();
            syncPoint.Continue();

            var serverReadTask = await readAsyncTask.Task.DefaultTimeout();
            var serverEx = await Assert.ThrowsAsync<ConnectionAbortedException>(() => serverReadTask).DefaultTimeout();
            Assert.Equal("The connection was aborted by the application.", serverEx.Message);

            await host.StopAsync().DefaultTimeout();
        }
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task POST_ServerAbortAfterWrite_ClientReceivesAbort()
    {
        // Arrange
        var builder = CreateHostBuilder(async context =>
        {
            Logger.LogInformation("Server writing content.");
            await context.Response.Body.WriteAsync(new byte[16]);

            // Note that there is a race here on what is sent before the abort is processed.
            // Abort may happen before or after response headers have been sent.
            Logger.LogInformation("Server aborting.");
            context.Abort();
        }, protocol: HttpProtocols.Http3);

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync().DefaultTimeout();

            for (var i = 0; i < 100; i++)
            {
                Logger.LogInformation($"Client sending request {i}");

                var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request.Version = GetProtocol(HttpProtocols.Http3);
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                // Act
                var sendTask = client.SendAsync(request, CancellationToken.None);

                // Assert
                var ex = await Assert.ThrowsAsync<HttpRequestException>(async () =>
                {
                    // Note that there is a race here on what is sent before the abort is processed.
                    // Abort may happen before or after response headers have been sent.
                    Logger.LogInformation($"Client awaiting response {i}");
                    var response = await sendTask;

                    Logger.LogInformation($"Client awaiting content {i}");
                    await response.Content.ReadAsByteArrayAsync();
                }).DefaultTimeout();

                var protocolException = ex.GetProtocolException();
                Assert.Equal((long)Http3ErrorCode.InternalError, protocolException.ErrorCode);
            }

            await host.StopAsync().DefaultTimeout();
        }
    }

    // Verify HTTP/2 and HTTP/3 match behavior
    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(HttpProtocols.Http3)]
    [InlineData(HttpProtocols.Http2)]
    public async Task GET_ServerAbort_ClientReceivesAbort(HttpProtocols protocol)
    {
        // Arrange
        var syncPoint = new SyncPoint();
        var cancelledTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writeAsyncTask = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);

        var builder = CreateHostBuilder(async context =>
        {
            context.RequestAborted.Register(() => cancelledTcs.SetResult());

            context.Abort();

            // Sync with client
            await syncPoint.WaitToContinue();

            writeAsyncTask.SetResult(context.Response.Body.WriteAsync(TestData).AsTask());
        }, protocol: protocol);

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync().DefaultTimeout();

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request.Version = GetProtocol(protocol);
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // Act
            var ex = await Assert.ThrowsAnyAsync<HttpRequestException>(() => client.SendAsync(request, CancellationToken.None)).DefaultTimeout();

            // Assert
            if (protocol == HttpProtocols.Http3)
            {
                var innerEx = Assert.IsType<HttpProtocolException>(ex.InnerException);
                Assert.Equal(Http3ErrorCode.InternalError, (Http3ErrorCode)innerEx.ErrorCode);
            }

            await cancelledTcs.Task.DefaultTimeout();

            // Sync with server to ensure RequestDelegate is still running
            await syncPoint.WaitForSyncPoint().DefaultTimeout();
            syncPoint.Continue();

            var serverWriteTask = await writeAsyncTask.Task.DefaultTimeout();
            await serverWriteTask.DefaultTimeout();

            await host.StopAsync().DefaultTimeout();
        }
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task POST_Expect100Continue_Get100Continue()
    {
        // Arrange
        var builder = CreateHostBuilder(async context =>
        {
            var body = context.Request.Body;

            var data = await body.ReadAtLeastLengthAsync(TestData.Length).DefaultTimeout();

            await context.Response.Body.WriteAsync(data);
        });

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient(expect100ContinueTimeout: TimeSpan.FromMinutes(20)))
        {
            await host.StartAsync().DefaultTimeout();

            var requestContent = new StringContent("Hello world");

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://127.0.0.1:{host.GetPort()}/");
            request.Content = requestContent;
            request.Version = HttpVersion.Version30;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
            request.Headers.ExpectContinue = true;

            // Act
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            var responseTask = client.SendAsync(request, cts.Token);

            var response = await responseTask.DefaultTimeout();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version30, response.Version);
            var responseText = await response.Content.ReadAsStringAsync().DefaultTimeout();
            Assert.Equal("Hello world", responseText);

            await host.StopAsync().DefaultTimeout();
        }
    }

    private static Version GetProtocol(HttpProtocols protocol)
    {
        switch (protocol)
        {
            case HttpProtocols.Http2:
                return HttpVersion.Version20;
            case HttpProtocols.Http3:
                return HttpVersion.Version30;
            default:
                throw new InvalidOperationException();
        }
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task GET_ConnectionsMakingMultipleRequests_AllSuccess()
    {
        // Arrange
        var requestCount = 0;

        var builder = CreateHostBuilder(context =>
        {
            Interlocked.Increment(ref requestCount);
            return Task.CompletedTask;
        });

        using (var host = builder.Build())
        {
            await host.StartAsync();

            var address = $"https://127.0.0.1:{host.GetPort()}/";

            // Act
            var connectionRequestTasks = new List<Task<int>>();

            for (var i = 0; i < 10; i++)
            {
                connectionRequestTasks.Add(RunConnection(address));
            }

            var calls = (await Task.WhenAll(connectionRequestTasks)).Sum();

            // Assert
            Assert.Equal(1000, calls);
            Assert.Equal(1000, requestCount);

            await host.StopAsync();
        }

        static async Task<int> RunConnection(string address)
        {
            using (var client = HttpHelpers.CreateClient())
            {
                var requestTasks = new List<Task>();

                for (var j = 0; j < 100; j++)
                {
                    requestTasks.Add(MakeRequest(client, address, j));
                }

                await Task.WhenAll(requestTasks);

                return requestTasks.Count;
            }
        }

        static async Task MakeRequest(HttpMessageInvoker client, string address, int count)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, address);
            request.Version = HttpVersion.Version30;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response = await client.SendAsync(request, CancellationToken.None);
            response.EnsureSuccessStatusCode();
        }
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task GET_MultipleRequestsInSequence_ReusedState()
    {
        // Arrange
        object persistedState = null;
        var requestCount = 0;

        var builder = CreateHostBuilder(context =>
        {
            requestCount++;
            var persistentStateCollection = context.Features.Get<IPersistentStateFeature>().State;
            if (persistentStateCollection.TryGetValue("Counter", out var value))
            {
                persistedState = value;
            }
            persistentStateCollection["Counter"] = requestCount;

            return Task.CompletedTask;
        });

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync();

            // Act
            var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request1.Version = HttpVersion.Version30;
            request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response1 = await client.SendAsync(request1, CancellationToken.None);
            response1.EnsureSuccessStatusCode();
            var firstRequestState = persistedState;

            // Delay to ensure the stream has enough time to return to pool
            await Task.Delay(100);

            var request2 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request2.Version = HttpVersion.Version30;
            request2.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response2 = await client.SendAsync(request2, CancellationToken.None);
            response2.EnsureSuccessStatusCode();
            var secondRequestState = persistedState;

            // Assert
            // First request has no persisted state
            Assert.Null(firstRequestState);

            // State persisted on first request was available on the second request
            Assert.Equal(1, secondRequestState);

            await host.StopAsync();
        }
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task GET_MultipleRequests_RequestVersionOrHigher_UpgradeToHttp3()
    {
        // Arrange
        await ServerRetryHelper.BindPortsWithRetry(async port =>
        {
            var requestHeaders = new List<Dictionary<string, StringValues>>();

            var builder = CreateHostBuilder(
                context =>
                {
                    requestHeaders.Add(context.Request.Headers.ToDictionary(k => k.Key, k => k.Value, StringComparer.OrdinalIgnoreCase));
                    return Task.CompletedTask;
                },
                configureKestrel: o =>
                {
                    o.Listen(IPAddress.Parse("127.0.0.1"), port, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                        listenOptions.UseHttps(TestResources.GetTestCertificate());
                    });
                });

            using (var host = builder.Build())
            using (var client = HttpHelpers.CreateClient())
            {
                await host.StartAsync();

                // Act 1
                var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request1.Headers.Add("id", "1");
                request1.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

                var response1 = await client.SendAsync(request1, CancellationToken.None);
                response1.EnsureSuccessStatusCode();
                var request1Headers = requestHeaders.Single(i => i["id"] == "1");

                // Assert 1
                Assert.Equal(HttpVersion.Version20, response1.Version);
                Assert.False(request1Headers.ContainsKey("alt-used"));

                // Act 2
                var request2 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request2.Headers.Add("id", "2");
                request2.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

                var response2 = await client.SendAsync(request2, CancellationToken.None);
                response2.EnsureSuccessStatusCode();
                var request2Headers = requestHeaders.Single(i => i["id"] == "2");

                // Assert 2
                Assert.Equal(HttpVersion.Version30, response2.Version);
                Assert.True(request2Headers.ContainsKey("alt-used"));

                // Delay to ensure the stream has enough time to return to pool
                await Task.Delay(100);

                // Act 3
                var request3 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request3.Headers.Add("id", "3");
                request3.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

                var response3 = await client.SendAsync(request3, CancellationToken.None);
                response3.EnsureSuccessStatusCode();
                var request3Headers = requestHeaders.Single(i => i["id"] == "3");

                // Assert 3
                Assert.Equal(HttpVersion.Version30, response3.Version);
                Assert.True(request3Headers.ContainsKey("alt-used"));

                Assert.Same((string)request2Headers["alt-used"], (string)request3Headers["alt-used"]);

                await host.StopAsync();
            }
        }, Logger);
    }

    // Verify HTTP/2 and HTTP/3 match behavior
    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(HttpProtocols.Http3)]
    [InlineData(HttpProtocols.Http2)]
    public async Task POST_ClientCancellationBidirectional_RequestAbortRaised(HttpProtocols protocol)
    {
        // Arrange
        var cancelledTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readAsyncTask = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientHasCancelledSyncPoint = new SyncPoint();

        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);

        var builder = CreateHostBuilder(async context =>
        {
            context.RequestAborted.Register(() =>
            {
                Logger.LogInformation("Server received request aborted.");
                cancelledTcs.SetResult();
            });

            var requestBody = context.Request.Body;
            var responseBody = context.Response.Body;

            // Read content
            Logger.LogInformation("Server reading request body.");
            var data = await requestBody.ReadAtLeastLengthAsync(TestData.Length);

            Logger.LogInformation("Server writing response body.");
            await responseBody.WriteAsync(data);
            await responseBody.FlushAsync();

            await clientHasCancelledSyncPoint.WaitForSyncPoint().DefaultTimeout();
            clientHasCancelledSyncPoint.Continue();

            Logger.LogInformation("Server waiting for cancellation.");
            await cancelledTcs.Task;

            readAsyncTask.SetResult(requestBody.ReadAsync(data).AsTask());
        }, protocol: protocol);

        var httpClientHandler = new HttpClientHandler();
        httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using (var host = builder.Build())
        using (var client = new HttpClient(httpClientHandler))
        {
            await host.StartAsync().DefaultTimeout();

            var cts = new CancellationTokenSource();
            var requestContent = new StreamingHttpContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://127.0.0.1:{host.GetPort()}/");
            request.Content = requestContent;
            request.Version = GetProtocol(protocol);
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // Act
            var responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            var requestStream = await requestContent.GetStreamAsync().DefaultTimeout();

            // Send headers
            await requestStream.FlushAsync().DefaultTimeout();
            // Write content
            await requestStream.WriteAsync(TestData).DefaultTimeout();
            await requestStream.FlushAsync().DefaultTimeout();

            var response = await responseTask.DefaultTimeout();

            var responseStream = await response.Content.ReadAsStreamAsync().DefaultTimeout();

            Logger.LogInformation("Client reading response.");
            var data = await responseStream.ReadAtLeastLengthAsync(TestData.Length).DefaultTimeout();

            Logger.LogInformation("Client canceled request.");
            response.Dispose();

            await clientHasCancelledSyncPoint.WaitToContinue().DefaultTimeout();

            // Assert
            await cancelledTcs.Task.DefaultTimeout();

            var serverReadTask = await readAsyncTask.Task.DefaultTimeout();

            var serverEx = await Assert.ThrowsAsync<IOException>(() => serverReadTask).DefaultTimeout();
            Assert.Equal("The client reset the request stream.", serverEx.Message);

            await host.StopAsync().DefaultTimeout();
        }

        // Ensure this log wasn't written:
        // Critical: Http3OutputProducer.ProcessDataWrites observed an unexpected exception.
        var badLogWrite = TestSink.Writes.FirstOrDefault(w => w.LogLevel == LogLevel.Critical);
        if (badLogWrite != null)
        {
            Assert.True(false, "Bad log write: " + badLogWrite + Environment.NewLine + badLogWrite.Exception);
        }
    }

    // Verify HTTP/2 and HTTP/3 match behavior
    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(HttpProtocols.Http3)]
    [InlineData(HttpProtocols.Http2)]
    public async Task POST_Bidirectional_LargeData_Cancellation_Error(HttpProtocols protocol)
    {
        // Arrange
        var data = new byte[1024 * 64 * 10];

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var builder = CreateHostBuilder(async context =>
        {
            var requestBody = context.Request.Body;

            await context.Response.BodyWriter.FlushAsync();

            while (true)
            {
                Logger.LogInformation("Server reading request body.");
                var currentData = await requestBody.ReadAtLeastLengthAsync(data.Length);
                if (currentData == null)
                {
                    break;
                }

                tcs.TrySetResult();

                Logger.LogInformation("Server writing response body.");

                context.Response.BodyWriter.GetSpan(data.Length);
                context.Response.BodyWriter.Advance(data.Length);

                await context.Response.BodyWriter.FlushAsync();
            }
        }, protocol: protocol);

        var httpClientHandler = new HttpClientHandler();
        httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using (var host = builder.Build())
        using (var client = new HttpClient(httpClientHandler))
        {
            await host.StartAsync().DefaultTimeout();

            var cts = new CancellationTokenSource();
            var requestContent = new StreamingHttpContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://127.0.0.1:{host.GetPort()}/");
            request.Content = requestContent;
            request.Version = GetProtocol(protocol);
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // Act
            var responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            var requestStream = await requestContent.GetStreamAsync().DefaultTimeout();

            Logger.LogInformation("Client sending headers.");
            await requestStream.FlushAsync().DefaultTimeout();

            Logger.LogInformation("Client waiting for headers.");
            var response = await responseTask.DefaultTimeout();
            var responseStream = await response.Content.ReadAsStreamAsync().DefaultTimeout();

            Logger.LogInformation("Client writing request.");
            await requestStream.WriteAsync(data).DefaultTimeout();
            await requestStream.FlushAsync().DefaultTimeout();

            await tcs.Task;

            Logger.LogInformation("Client canceled request.");
            response.Dispose();

            await Task.Delay(50);

            // Ensure this log wasn't written:
            // Critical: Http3OutputProducer.ProcessDataWrites observed an unexpected exception.
            var badLogWrite = TestSink.Writes.FirstOrDefault(w => w.LogLevel >= LogLevel.Critical);
            if (badLogWrite != null)
            {
                Assert.True(false, "Bad log write: " + badLogWrite + Environment.NewLine + badLogWrite.Exception);
            }

            // Assert
            await host.StopAsync().DefaultTimeout();
        }
    }

    // Verify HTTP/2 and HTTP/3 match behavior
    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(HttpProtocols.Http3)]
    [InlineData(HttpProtocols.Http2)]
    public async Task GET_ClientCancellationAfterResponseHeaders_RequestAbortRaised(HttpProtocols protocol)
    {
        // Arrange
        var cancelledTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var builder = CreateHostBuilder(async context =>
        {
            context.RequestAborted.Register(() =>
            {
                Logger.LogInformation("Server received request aborted.");
                cancelledTcs.SetResult();
            });

            var responseBody = context.Response.Body;
            await responseBody.WriteAsync(TestData);
            await responseBody.FlushAsync();

            // Wait for task cancellation
            await cancelledTcs.Task;
        }, protocol: protocol);

        var httpClientHandler = new HttpClientHandler();
        httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using (var host = builder.Build())
        using (var client = new HttpClient(httpClientHandler))
        {
            await host.StartAsync().DefaultTimeout();

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request.Version = GetProtocol(protocol);
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // Act
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            var responseStream = await response.Content.ReadAsStreamAsync().DefaultTimeout();

            var data = await responseStream.ReadAtLeastLengthAsync(TestData.Length).DefaultTimeout();

            Logger.LogInformation("Client canceled request.");
            response.Dispose();

            // Assert
            await cancelledTcs.Task.DefaultTimeout();

            await host.StopAsync().DefaultTimeout();
        }
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task StreamResponseContent_DelayAndTrailers_ClientSuccess()
    {
        // Arrange
        var builder = CreateHostBuilder(async context =>
        {
            var feature = context.Features.Get<IHttpResponseTrailersFeature>();

            for (var i = 1; i < 200; i++)
            {
                feature.Trailers.Append($"trailer-{i}", new string('!', i));
            }

            Logger.LogInformation($"Server trailer count: {feature.Trailers.Count}");

            await context.Request.BodyReader.ReadAtLeastAsync(TestData.Length);

            for (var i = 0; i < 3; i++)
            {
                await context.Response.BodyWriter.WriteAsync(TestData);

                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }
        });

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://127.0.0.1:{host.GetPort()}/");
            request.Content = new ByteArrayContent(TestData);
            request.Version = HttpVersion.Version30;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response = await client.SendAsync(request, CancellationToken.None);
            response.EnsureSuccessStatusCode();

            var responseStream = await response.Content.ReadAsStreamAsync();

            await responseStream.ReadUntilEndAsync();

            Logger.LogInformation($"Client trailer count: {response.TrailingHeaders.Count()}");

            for (var i = 1; i < 200; i++)
            {
                try
                {
                    var value = response.TrailingHeaders.GetValues($"trailer-{i}").Single();
                    Assert.Equal(new string('!', i), value);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error checking trailer {i}", ex);
                }
            }

            await host.StopAsync();
        }
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task GET_MultipleRequests_ConnectionAndTraceIdsUpdated()
    {
        // Arrange
        string connectionId = null;
        string traceId = null;

        var builder = CreateHostBuilder(context =>
        {
            connectionId = context.Connection.Id;
            traceId = context.TraceIdentifier;

            return Task.CompletedTask;
        });

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync();

            // Act
            var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request1.Version = HttpVersion.Version30;
            request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response1 = await client.SendAsync(request1, CancellationToken.None);
            response1.EnsureSuccessStatusCode();

            var connectionId1 = connectionId;
            var traceId1 = traceId;

            var request2 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request2.Version = HttpVersion.Version30;
            request2.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response2 = await client.SendAsync(request2, CancellationToken.None);
            response2.EnsureSuccessStatusCode();

            var connectionId2 = connectionId;
            var traceId2 = traceId;

            // Assert
            Assert.True(!string.IsNullOrEmpty(connectionId1), "ConnectionId should have a value.");
            Assert.Equal(connectionId1, connectionId2); // ConnectionId unchanged

            Assert.Equal($"{connectionId1}:00000000", traceId1);
            Assert.Equal($"{connectionId2}:00000004", traceId2);

            await host.StopAsync();
        }
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task GET_MultipleRequestsInSequence_ReusedRequestHeaderStrings()
    {
        // Arrange
        string request1HeaderValue = null;
        string request2HeaderValue = null;
        var requestCount = 0;

        var builder = CreateHostBuilder(context =>
        {
            requestCount++;

            if (requestCount == 1)
            {
                request1HeaderValue = context.Request.Headers.UserAgent;
            }
            else if (requestCount == 2)
            {
                request2HeaderValue = context.Request.Headers.UserAgent;
            }
            else
            {
                throw new InvalidOperationException();
            }

            return Task.CompletedTask;
        });

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync();

            // Act
            var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request1.Headers.TryAddWithoutValidation("User-Agent", "TestUserAgent");
            request1.Version = HttpVersion.Version30;
            request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response1 = await client.SendAsync(request1, CancellationToken.None);
            response1.EnsureSuccessStatusCode();

            // Delay to ensure the stream has enough time to return to pool
            await Task.Delay(100);

            var request2 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request2.Headers.TryAddWithoutValidation("User-Agent", "TestUserAgent");
            request2.Version = HttpVersion.Version30;
            request2.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response2 = await client.SendAsync(request2, CancellationToken.None);
            response2.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal("TestUserAgent", request1HeaderValue);
            Assert.Same(request1HeaderValue, request2HeaderValue);

            await host.StopAsync();
        }
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task Get_CompleteAsyncAndReset_StreamNotPooled()
    {
        // Arrange
        var requestCount = 0;
        var contexts = new List<HttpContext>();
        var builder = CreateHostBuilder(async context =>
        {
            contexts.Add(context);
            requestCount++;
            Logger.LogInformation($"Server received request {requestCount}");
            if (requestCount == 1)
            {
                await context.Response.CompleteAsync();

                context.Features.Get<IHttpResetFeature>().Reset(256);
            }
        });

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync();

            // Act
            var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request1.Version = HttpVersion.Version30;
            request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // TODO: There is a race between CompleteAsync and Reset.
            // https://github.com/dotnet/aspnetcore/issues/34915
            try
            {
                Logger.LogInformation("Client sending request 1");
                await client.SendAsync(request1, CancellationToken.None);
            }
            catch (HttpRequestException)
            {
            }

            // Delay to ensure the stream has enough time to return to pool
            await Task.Delay(100);

            var request2 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request2.Version = HttpVersion.Version30;
            request2.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            Logger.LogInformation("Client sending request 2");
            var response2 = await client.SendAsync(request2, CancellationToken.None);

            // Assert
            response2.EnsureSuccessStatusCode();

            await host.StopAsync();
        }

        Assert.NotSame(contexts[0], contexts[1]);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task GET_ConnectionLoggingConfigured_OutputToLogs()
    {
        // Arrange
        var builder = CreateHostBuilder(
            context =>
            {
                return Task.CompletedTask;
            },
            configureKestrel: kestrel =>
            {
                kestrel.ListenLocalhost(5001, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http3;
                    listenOptions.UseHttps(TestResources.GetTestCertificate());
                    listenOptions.UseConnectionLogging();
                });
            });

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync();

            var port = 5001;

            // Act
            var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{port}/");
            request1.Version = HttpVersion.Version30;
            request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response1 = await client.SendAsync(request1, CancellationToken.None);
            response1.EnsureSuccessStatusCode();

            // Assert
            var hasWriteLog = TestSink.Writes.Any(
                w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Core.Internal.LoggingConnectionMiddleware" &&
                w.Message.StartsWith("WriteAsync", StringComparison.Ordinal));
            Assert.True(hasWriteLog);

            var hasReadLog = TestSink.Writes.Any(
                w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Core.Internal.LoggingConnectionMiddleware" &&
                w.Message.StartsWith("ReadAsync", StringComparison.Ordinal));
            Assert.True(hasReadLog);

            await host.StopAsync();
        }
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task GET_UseHttpsCallback_ConnectionContextAvailable()
    {
        // Arrange
        BaseConnectionContext connectionContext = null;
        var builder = CreateHostBuilder(
            context =>
            {
                return Task.CompletedTask;
            },
            configureKestrel: kestrel =>
            {
                kestrel.ListenLocalhost(5001, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http3;
                    listenOptions.UseHttps(new TlsHandshakeCallbackOptions
                    {
                        OnConnection = context =>
                        {
                            connectionContext = context.Connection;
                            return ValueTask.FromResult(new SslServerAuthenticationOptions
                            {
                                ServerCertificate = TestResources.GetTestCertificate()
                            });
                        }
                    });
                });
            });

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync();

            var port = 5001;

            // Act
            var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{port}/");
            request1.Version = HttpVersion.Version30;
            request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response1 = await client.SendAsync(request1, CancellationToken.None);
            response1.EnsureSuccessStatusCode();

            // Assert
            Assert.NotNull(connectionContext);

            await host.StopAsync();
        }
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task GET_ClientDisconnected_ConnectionAbortRaised()
    {
        // Arrange
        var connectionClosedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var connectionStartedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var builder = CreateHostBuilder(
            context =>
            {
                return Task.CompletedTask;
            },
            configureKestrel: kestrel =>
            {
                kestrel.Listen(IPAddress.Parse("127.0.0.1"), 0, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http3;
                    listenOptions.UseHttps(TestResources.GetTestCertificate());

                    IMultiplexedConnectionBuilder multiplexedConnectionBuilder = listenOptions;
                    multiplexedConnectionBuilder.Use(next =>
                    {
                        return context =>
                        {
                            connectionStartedTcs.SetResult();
                            context.ConnectionClosed.Register(() => connectionClosedTcs.SetResult());
                            return next(context);
                        };
                    });
                });
            });

        using (var host = builder.Build())
        {
            await host.StartAsync();

            var client = HttpHelpers.CreateClient();
            try
            {
                var port = host.GetPort();

                // Act
                var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{port}/");
                request1.Version = HttpVersion.Version30;
                request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                var response1 = await client.SendAsync(request1, CancellationToken.None);
                response1.EnsureSuccessStatusCode();

                await connectionStartedTcs.Task.DefaultTimeout();
            }
            finally
            {
                Logger.LogInformation("Disposing client.");
                client.Dispose();
            }

            Logger.LogInformation("Waiting for server to receive connection close.");
            await connectionClosedTcs.Task.DefaultTimeout();

            await host.StopAsync();
        }
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task ConnectionLifetimeNotificationFeature_RequestClose_ConnectionEnds()
    {
        // Arrange
        var syncPoint1 = new SyncPoint();
        var connectionStartedTcs1 = new TaskCompletionSource<IConnectionLifetimeNotificationFeature>(TaskCreationOptions.RunContinuationsAsynchronously);

        var connectionStartedTcs2 = new TaskCompletionSource<IConnectionLifetimeNotificationFeature>(TaskCreationOptions.RunContinuationsAsynchronously);

        var connectionStartedTcs3 = new TaskCompletionSource<IConnectionLifetimeNotificationFeature>(TaskCreationOptions.RunContinuationsAsynchronously);

        var builder = CreateHostBuilder(
            context =>
            {
                switch (context.Request.Path.ToString())
                {
                    case "/1":
                        connectionStartedTcs1.SetResult(context.Features.Get<IConnectionLifetimeNotificationFeature>());
                        return syncPoint1.WaitToContinue();
                    case "/2":
                        connectionStartedTcs2.SetResult(context.Features.Get<IConnectionLifetimeNotificationFeature>());
                        return Task.CompletedTask;
                    case "/3":
                        connectionStartedTcs3.SetResult(context.Features.Get<IConnectionLifetimeNotificationFeature>());
                        return Task.CompletedTask;
                    default:
                        throw new InvalidOperationException();
                }
            });

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync();

            var port = host.GetPort();

            // Act
            var responseTask1 = client.SendAsync(CreateHttp3Request(HttpMethod.Get, $"https://127.0.0.1:{port}/1"), CancellationToken.None);

            // Connection started.
            var connection = await connectionStartedTcs1.Task.DefaultTimeout();

            // Request in progress.
            await syncPoint1.WaitForSyncPoint();

            connection.RequestClose();

            // Assert

            // Server should send a GOAWAY to the client to indicate connection is closing.
            await WaitForLogAsync(logs =>
            {
                return logs.Any(w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Http3" &&
                                     w.Message.Contains("GOAWAY stream ID 4611686018427387903."));
            }, "Check for initial GOAWAY frame sent on server initiated shutdown.");

            // TODO https://github.com/dotnet/runtime/issues/56944
            //Logger.LogInformation("Sending request after GOAWAY.");
            //var response2 = await client.SendAsync(CreateHttp3Request(HttpMethod.Get, $"https://127.0.0.1:{port}/2"), CancellationToken.None);
            //response2.EnsureSuccessStatusCode();

            // Allow request to finish so connection shutdown can happen.
            syncPoint1.Continue();

            // Request completes successfully on client.
            var response1 = await responseTask1.DefaultTimeout();
            response1.EnsureSuccessStatusCode();

            // Server has aborted connection.
            await WaitForLogAsync(logs =>
            {
                const int applicationAbortedConnectionId = 6;
                var connectionAbortLog = logs.FirstOrDefault(
                    w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Transport.Quic" &&
                        w.EventId == applicationAbortedConnectionId);
                if (connectionAbortLog == null)
                {
                    return false;
                }

                // This message says the client closed the connection because the server
                // sends a GOAWAY and the client then closes the connection once all requests are finished.
                Assert.Contains("The client closed the connection.", connectionAbortLog.Message);
                return true;
            }, "Wait for connection abort.");

            Logger.LogInformation("Sending request after connection abort.");
            var response3 = await client.SendAsync(CreateHttp3Request(HttpMethod.Get, $"https://127.0.0.1:{port}/3"), CancellationToken.None);
            response3.EnsureSuccessStatusCode();

            await host.StopAsync();
        }
    }

    private HttpRequestMessage CreateHttp3Request(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Version = HttpVersion.Version30;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

        return request;
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task GET_ServerAbortTransport_ConnectionAbortRaised()
    {
        // Arrange
        var syncPoint = new SyncPoint();
        var connectionStartedTcs = new TaskCompletionSource<MultiplexedConnectionContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var builder = CreateHostBuilder(
            context =>
            {
                return syncPoint.WaitToContinue();
            },
            configureKestrel: kestrel =>
            {
                kestrel.Listen(IPAddress.Parse("127.0.0.1"), 0, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http3;
                    listenOptions.UseHttps(TestResources.GetTestCertificate());

                    IMultiplexedConnectionBuilder multiplexedConnectionBuilder = listenOptions;
                    multiplexedConnectionBuilder.Use(next =>
                    {
                        return context =>
                        {
                            connectionStartedTcs.SetResult(context);
                            return next(context);
                        };
                    });
                });
            });

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync().DefaultTimeout();

            var port = host.GetPort();

            // Act
            var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{port}/");
            request1.Version = HttpVersion.Version30;
            request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var responseTask = client.SendAsync(request1, CancellationToken.None);

            // Connection started.
            var connection = await connectionStartedTcs.Task.DefaultTimeout();

            // Request in progress.
            await syncPoint.WaitForSyncPoint().DefaultTimeout();

            // Server connection middleware triggers close.
            // Note that this aborts the transport, not the HTTP/3 connection.
            connection.Abort();

            await Assert.ThrowsAsync<HttpRequestException>(() => responseTask).DefaultTimeout();

            // Assert
            const int applicationAbortedConnectionId = 6;
            Assert.Single(TestSink.Writes.Where(w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Transport.Quic" &&
                                                     w.EventId == applicationAbortedConnectionId));

            syncPoint.Continue();

            await host.StopAsync().DefaultTimeout();
        }
    }

    private async Task WaitForLogAsync(Func<IEnumerable<WriteContext>, bool> testLogs, string message)
    {
        Logger.LogInformation($"Started waiting for logs: {message}");

        var retryCount = !Debugger.IsAttached ? 5 : int.MaxValue;
        for (var i = 0; i < retryCount; i++)
        {
            if (testLogs(TestSink.Writes))
            {
                Logger.LogInformation($"Successfully received logs: {message}");
                return;
            }

            await Task.Delay(100 * (i + 1));
        }

        throw new Exception($"Wait for logs failure: {message}");
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task GET_ConnectionInfo_PropertiesSet()
    {
        string connectionId = null;
        IPAddress remoteAddress = null;
        int? remotePort = null;
        IPAddress localAddress = null;
        int? localPort = null;

        // Arrange
        var builder = CreateHostBuilder(context =>
        {
            connectionId = context.Connection.Id;
            remoteAddress = context.Connection.RemoteIpAddress;
            remotePort = context.Connection.RemotePort;
            localAddress = context.Connection.LocalIpAddress;
            localPort = context.Connection.LocalPort;
            return Task.CompletedTask;
        });

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync();

            var port = host.GetPort();

            // Act
            var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{port}/");
            request1.Version = HttpVersion.Version30;
            request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response1 = await client.SendAsync(request1, CancellationToken.None);
            response1.EnsureSuccessStatusCode();

            // Assert
            Assert.NotNull(connectionId);

            Assert.NotNull(remoteAddress);
            Assert.NotNull(remotePort);

            Assert.NotNull(localAddress);
            Assert.Equal(port, localPort);

            await host.StopAsync();
        }
    }

    // Verify HTTP/2 and HTTP/3 match behavior
    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(HttpProtocols.Http3)]
    [InlineData(HttpProtocols.Http2)]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/39985")]
    public async Task GET_GracefulServerShutdown_AbortRequestsAfterHostTimeout(HttpProtocols protocol)
    {
        // Arrange
        var requestStartedTcs = new TaskCompletionSource<HttpContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var readAsyncTask = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestAbortedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var builder = CreateHostBuilder(async context =>
        {
            context.RequestAborted.Register(() => requestAbortedTcs.SetResult());

            requestStartedTcs.SetResult(context);

            Logger.LogInformation("Server sending response headers");
            await context.Response.Body.FlushAsync();

            Logger.LogInformation("Server reading");
            var readTask = context.Request.Body.ReadUntilEndAsync();

            readAsyncTask.SetResult(readTask);

            await readTask;
        },
        protocol: protocol,
        configureKestrel: kestrel =>
        {
            // Disable the min rate limit to ensure a shutdown timeout aborts an ongoing read and not the rate limit.
            // This could also be fixed by sending more data from the client.
            kestrel.Limits.MinRequestBodyDataRate = null;

            // This would normally be done automatically for us if the "configureKestrel" callback we're in was left null.
            kestrel.Listen(IPAddress.Loopback, 0, listenOptions =>
            {
                listenOptions.Protocols = protocol;
                listenOptions.UseHttps(TestResources.GetTestCertificate());
            });
        });

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync().DefaultTimeout();

            var requestContent = new StreamingHttpContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://127.0.0.1:{host.GetPort()}/");
            request.Content = requestContent;
            request.Version = GetProtocol(protocol);
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // Act
            var responseTask = client.SendAsync(request, CancellationToken.None);

            var requestStream = await requestContent.GetStreamAsync().DefaultTimeout();

            // Send headers
            await requestStream.FlushAsync();
            // Write content
            await requestStream.WriteAsync(TestData);

            var response = await responseTask.DefaultTimeout();

            var httpContext = await requestStartedTcs.Task.DefaultTimeout();

            Logger.LogInformation("Stopping host");
            var stopTask = host.StopAsync();

            if (protocol == HttpProtocols.Http3)
            {
                await WaitForLogAsync(logs =>
                {
                    return logs.Any(w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Http3" &&
                                         w.Message.Contains("GOAWAY stream ID 4611686018427387903."));
                }, "Check for initial GOAWAY frame sent on server initiated shutdown.");
            }

            var readTask = await readAsyncTask.Task.DefaultTimeout();

            // Assert
            var ex = await Assert.ThrowsAnyAsync<Exception>(() => readTask).DefaultTimeout();
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            Assert.IsType<ConnectionAbortedException>(ex);
            Assert.Equal("The connection was aborted because the server is shutting down and request processing didn't complete within the time specified by HostOptions.ShutdownTimeout.", ex.Message);

            await requestAbortedTcs.Task.DefaultTimeout();

            await stopTask.DefaultTimeout();

            if (protocol == HttpProtocols.Http3)
            {
                // Server has aborted connection.
                await WaitForLogAsync(logs =>
                {
                    return logs.Any(w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Http3" &&
                                         w.Message.Contains("GOAWAY stream ID 4."));
                }, "Check for exact GOAWAY frame sent on server initiated shutdown.");
            }

            Assert.Contains(TestSink.Writes, m => m.Message.Contains("Some connections failed to close gracefully during server shutdown."));
        }
    }

    // Verify HTTP/2 and HTTP/3 match behavior
    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(HttpProtocols.Http3)]
    [InlineData(HttpProtocols.Http2)]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/35070")]
    public async Task GET_GracefulServerShutdown_RequestCompleteSuccessfullyInsideHostTimeout(HttpProtocols protocol)
    {
        // Arrange
        var requestStartedTcs = new TaskCompletionSource<HttpContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestAbortedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var builder = CreateHostBuilder(async context =>
        {
            context.RequestAborted.Register(() => requestAbortedTcs.SetResult());

            requestStartedTcs.SetResult(context);

            Logger.LogInformation("Server sending response headers");
            await context.Response.Body.FlushAsync();

            Logger.LogInformation("Server reading");
            var data = await context.Request.Body.ReadUntilEndAsync();

            Logger.LogInformation("Server writing");
            await context.Response.Body.WriteAsync(data);
        }, protocol: protocol);

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync().DefaultTimeout();

            var requestContent = new StreamingHttpContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://127.0.0.1:{host.GetPort()}/");
            request.Content = requestContent;
            request.Version = GetProtocol(protocol);
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // Act
            var responseTask = client.SendAsync(request, CancellationToken.None);

            var requestStream = await requestContent.GetStreamAsync().DefaultTimeout();

            // Send headers
            await requestStream.FlushAsync();
            // Write content
            await requestStream.WriteAsync(TestData);

            var response = await responseTask.DefaultTimeout();

            var httpContext = await requestStartedTcs.Task.DefaultTimeout();

            Logger.LogInformation("Stopping host");
            var stopTask = host.StopAsync();

            // Assert
            Assert.False(stopTask.IsCompleted, "Waiting for host which is wating for request.");

            Logger.LogInformation("Client completing request stream");
            requestContent.CompleteStream();

            var data = await response.Content.ReadAsByteArrayAsync();
            Assert.Equal(TestData, data);

            await stopTask.DefaultTimeout();

            Assert.DoesNotContain(TestSink.Writes, m => m.Message.Contains("Some connections failed to close gracefully during server shutdown."));
        }
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task ServerReset_InvalidErrorCode()
    {
        var ranHandler = false;
        var hostBuilder = CreateHostBuilder(context =>
        {
            ranHandler = true;
            // Can't test a too-large value since it's bigger than int
            //Assert.Throws<ArgumentOutOfRangeException>(() => context.Features.Get<IHttpResetFeature>().Reset(-1)); // Invalid negative value
            context.Features.Get<IHttpResetFeature>().Reset(-1);
            return Task.CompletedTask;
        });

        using var host = await hostBuilder.StartAsync().DefaultTimeout();
        using var client = HttpHelpers.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
        request.Version = GetProtocol(HttpProtocols.Http3);
        request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

        var response = await client.SendAsync(request, CancellationToken.None).DefaultTimeout();
        await host.StopAsync().DefaultTimeout();

        Assert.True(ranHandler);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private IHostBuilder CreateHostBuilder(RequestDelegate requestDelegate, HttpProtocols? protocol = null, Action<KestrelServerOptions> configureKestrel = null)
    {
        return HttpHelpers.CreateHostBuilder(AddTestLogging, requestDelegate, protocol, configureKestrel);
    }
}
