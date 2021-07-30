// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Quic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Interop.FunctionalTests.Http3
{
    public class Http3RequestTests : LoggedTest
    {
        private class StreamingHttpContext : HttpContent
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
            using (var client = CreateClient())
            {
                await host.StartAsync().DefaultTimeout();

                var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request.Version = GetProtocol(protocol);
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                // Act
                var responseMessage = await client.SendAsync(request).DefaultTimeout();

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
        [InlineData(HttpProtocols.Http3, 1024, Skip = "HttpClient issue https://github.com/dotnet/runtime/issues/56115")]
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
            using (var client = CreateClient())
            {
                await host.StartAsync();

                var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request.Version = GetProtocol(protocol);
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                // Act
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal(GetProtocol(protocol), response.Version);

                var responseStream = await response.Content.ReadAsStreamAsync().DefaultTimeout();

                var buffer = new byte[clientBufferSize];
                await responseStream.ReadAtLeastLengthAsync(TestData.Length, buffer).DefaultTimeout();

                tcs.SetResult();
                await responseStream.ReadAtLeastLengthAsync(TestData.Length, buffer).DefaultTimeout();

                await host.StopAsync();
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

                var data = await body.ReadUntilLengthAsync(TestData.Length).DefaultTimeout();

                await context.Response.Body.WriteAsync(data);
            });

            using (var host = builder.Build())
            using (var client = CreateClient())
            {
                await host.StartAsync();

                var requestContent = new StreamingHttpContext();

                var request = new HttpRequestMessage(HttpMethod.Post, $"https://127.0.0.1:{host.GetPort()}/");
                request.Content = requestContent;
                request.Version = HttpVersion.Version30;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                // Act
                var responseTask = client.SendAsync(request);

                var requestStream = await requestContent.GetStreamAsync();

                // Send headers
                await requestStream.FlushAsync();
                // Write content
                await requestStream.WriteAsync(TestData);

                var response = await responseTask;

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal(HttpVersion.Version30, response.Version);
                var responseText = await response.Content.ReadAsStringAsync();
                Assert.Equal("Hello world", responseText);

                await host.StopAsync();
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
                context.RequestAborted.Register(() => cancelledTcs.SetResult());

                var body = context.Request.Body;

                // Read content
                await body.ReadUntilLengthAsync(TestData.Length).DefaultTimeout();

                // Sync with client
                await syncPoint.WaitToContinue();

                // Wait for task cancellation
                await cancelledTcs.Task;

                readAsyncTask.SetResult(body.ReadAsync(new byte[1024]).AsTask());
            }, protocol: protocol);

            using (var host = builder.Build())
            using (var client = CreateClient())
            {
                await host.StartAsync().DefaultTimeout();

                var cts = new CancellationTokenSource();
                var requestContent = new StreamingHttpContext();

                var request = new HttpRequestMessage(HttpMethod.Post, $"https://127.0.0.1:{host.GetPort()}/");
                request.Content = requestContent;
                request.Version = GetProtocol(protocol);
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                // Act
                var responseTask = client.SendAsync(request, cts.Token);

                var requestStream = await requestContent.GetStreamAsync().DefaultTimeout();

                // Send headers
                await requestStream.FlushAsync().DefaultTimeout();
                // Write content
                await requestStream.WriteAsync(TestData).DefaultTimeout();

                // Wait until content is read on server
                await syncPoint.WaitForSyncPoint().DefaultTimeout();

                // Cancel request
                cts.Cancel();

                // Continue on server
                syncPoint.Continue();

                // Assert
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => responseTask).DefaultTimeout();

                await cancelledTcs.Task.DefaultTimeout();

                var serverWriteTask = await readAsyncTask.Task.DefaultTimeout();

                await Assert.ThrowsAnyAsync<Exception>(() => serverWriteTask).DefaultTimeout();

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
            using (var client = CreateClient())
            {
                await host.StartAsync().DefaultTimeout();

                var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request.Version = GetProtocol(protocol);
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                // Act
                var ex = await Assert.ThrowsAnyAsync<HttpRequestException>(() => client.SendAsync(request)).DefaultTimeout();

                // Assert
                if (protocol == HttpProtocols.Http3)
                {
                    var innerEx = Assert.IsType<QuicStreamAbortedException>(ex.InnerException);
                    Assert.Equal(258, innerEx.ErrorCode);
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
            using (var client = CreateClient())
            {
                await host.StartAsync();

                // Act
                var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request1.Version = HttpVersion.Version30;
                request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                var response1 = await client.SendAsync(request1);
                response1.EnsureSuccessStatusCode();
                var firstRequestState = persistedState;

                // Delay to ensure the stream has enough time to return to pool
                await Task.Delay(100);

                var request2 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request2.Version = HttpVersion.Version30;
                request2.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                var response2 = await client.SendAsync(request2);
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
            using (var client = CreateClient())
            {
                await host.StartAsync();

                // Act
                var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request1.Version = HttpVersion.Version30;
                request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                var response1 = await client.SendAsync(request1);
                response1.EnsureSuccessStatusCode();

                var connectionId1 = connectionId;
                var traceId1 = traceId;

                var request2 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request2.Version = HttpVersion.Version30;
                request2.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                var response2 = await client.SendAsync(request2);
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
            using (var client = CreateClient())
            {
                await host.StartAsync();

                // Act
                var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request1.Headers.TryAddWithoutValidation("User-Agent", "TestUserAgent");
                request1.Version = HttpVersion.Version30;
                request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                var response1 = await client.SendAsync(request1);
                response1.EnsureSuccessStatusCode();

                // Delay to ensure the stream has enough time to return to pool
                await Task.Delay(100);

                var request2 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request2.Headers.TryAddWithoutValidation("User-Agent", "TestUserAgent");
                request2.Version = HttpVersion.Version30;
                request2.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                var response2 = await client.SendAsync(request2);
                response2.EnsureSuccessStatusCode();

                // Assert
                Assert.Equal("TestUserAgent", request1HeaderValue);
                Assert.Same(request1HeaderValue, request2HeaderValue);

                await host.StopAsync();
            }
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
                        listenOptions.UseHttps();
                        listenOptions.UseConnectionLogging();
                    });
                });

            using (var host = builder.Build())
            using (var client = CreateClient())
            {
                await host.StartAsync();

                var port = 5001;

                // Act
                var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{port}/");
                request1.Version = HttpVersion.Version30;
                request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                var response1 = await client.SendAsync(request1);
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

        private static HttpClient CreateClient()
        {
            var httpHandler = new HttpClientHandler();
            httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            return new HttpClient(httpHandler);
        }

        private IHostBuilder CreateHostBuilder(RequestDelegate requestDelegate, HttpProtocols? protocol = null, Action<KestrelServerOptions> configureKestrel = null)
        {
            return GetHostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseKestrel(o =>
                        {
                            if (configureKestrel == null)
                            {
                                o.Listen(IPAddress.Parse("127.0.0.1"), 0, listenOptions =>
                                {
                                    listenOptions.Protocols = protocol ?? HttpProtocols.Http3;
                                    listenOptions.UseHttps();
                                });
                            }
                            else
                            {
                                configureKestrel(o);
                            }
                        })
                        .Configure(app =>
                        {
                            app.Run(requestDelegate);
                        });
                })
                .ConfigureServices(AddTestLogging);
        }

        public static IHostBuilder GetHostBuilder(long? maxReadBufferSize = null)
        {
            return new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseQuic(options =>
                        {
                            options.MaxReadBufferSize = maxReadBufferSize;
                            options.Alpn = "h3-29";
                            options.IdleTimeout = TimeSpan.FromSeconds(20);
                        });
                });
        }
    }
}
