// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using System.Buffers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class KestrelMetricsTests : TestApplicationErrorLoggerLoggedTest
{
    private static readonly X509Certificate2 _x509Certificate2 = TestResources.GetTestCertificate();

    [Fact]
    public void ConnectionEndReasonMappings()
    {
        foreach (var reason in Enum.GetValues<ConnectionEndReason>())
        {
            var hasValue = KestrelMetrics.TryGetErrorType(reason, out var value);
            Assert.True(hasValue || value == null, $"ConnectionEndReason '{reason}' doesn't have a mapping.");
        }
    }

    [Fact]
    public async Task Http1Connection()
    {
        var sync = new SyncPoint();

        var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
        listenOptions.Use(next =>
        {
            return async connectionContext =>
            {
                connectionContext.Features.Get<IConnectionMetricsTagsFeature>().Tags.Add(new KeyValuePair<string, object>("custom", "value!"));

                // Wait for the test to verify the connection has started.
                await sync.WaitToContinue();

                await next(connectionContext);
            };
        });

        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");
        using var activeConnections = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.active_connections");
        using var queuedConnections = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.queued_connections");

        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

        var sendString = "POST / HTTP/1.0\r\nContent-Length: 12\r\n\r\nHello World?";

        await using var server = new TestServer(EchoApp, serviceContext, listenOptions);

        using (var connection = server.CreateConnection())
        {
            await connection.Send(sendString).DefaultTimeout();

            // Wait for connection to start on the server.
            await sync.WaitForSyncPoint().DefaultTimeout();

            Assert.Empty(connectionDuration.GetMeasurementSnapshot());
            Assert.Collection(activeConnections.GetMeasurementSnapshot(), m => AssertCount(m, 1, "127.0.0.1", localPort: 0, "tcp", "ipv4"));

            // Signal that connection can continue.
            sync.Continue();

            await connection.ReceiveEnd(
                "HTTP/1.1 200 OK",
                "Connection: close",
                $"Date: {serviceContext.DateHeaderValue}",
                "",
                "Hello World?").DefaultTimeout();

            await connection.WaitForConnectionClose().DefaultTimeout();
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m =>
        {
            AssertDuration(m, "127.0.0.1", localPort: 0, "tcp", "ipv4", KestrelMetrics.Http11);
            Assert.Equal("value!", (string)m.Tags["custom"]);
        });
        Assert.Collection(activeConnections.GetMeasurementSnapshot(), m => AssertCount(m, 1, "127.0.0.1", localPort: 0, "tcp", "ipv4"), m => AssertCount(m, -1, "127.0.0.1", localPort: 0, "tcp", "ipv4"));
        Assert.Collection(queuedConnections.GetMeasurementSnapshot(), m => AssertCount(m, 1, "127.0.0.1", localPort: 0, "tcp", "ipv4"), m => AssertCount(m, -1, "127.0.0.1", localPort: 0, "tcp", "ipv4"));
    }

    [Fact]
    public async Task Http1Connection_BeginListeningAfterConnectionStarted()
    {
        var sync = new SyncPoint();
        bool? hasConnectionMetricsTagsFeature = null;

        var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
        listenOptions.Use(next =>
        {
            return async connectionContext =>
            {
                hasConnectionMetricsTagsFeature = connectionContext.Features.Get<IConnectionMetricsTagsFeature>() != null;

                // Wait for the test to verify the connection has started.
                await sync.WaitToContinue();

                await next(connectionContext);
            };
        });

        var testMeterFactory = new TestMeterFactory();
        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

        var sendString = "POST / HTTP/1.0\r\nContent-Length: 12\r\n\r\nHello World?";

        await using var server = new TestServer(EchoApp, serviceContext, listenOptions);

        using (var connection = server.CreateConnection())
        {
            await connection.Send(sendString);

            // Wait for connection to start on the server.
            await sync.WaitForSyncPoint();

            using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");
            using var activeConnections = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.active_connections");
            using var queuedConnections = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.queued_connections");

            // Signal that connection can continue.
            sync.Continue();

            await connection.ReceiveEnd(
                "HTTP/1.1 200 OK",
                "Connection: close",
                $"Date: {serviceContext.DateHeaderValue}",
                "",
                "Hello World?");

            await connection.WaitForConnectionClose();

            Assert.Empty(connectionDuration.GetMeasurementSnapshot());
            Assert.Empty(activeConnections.GetMeasurementSnapshot());
            Assert.Empty(queuedConnections.GetMeasurementSnapshot());

            Assert.False(hasConnectionMetricsTagsFeature);
        }
    }

    [Fact]
    public async Task Http1Connection_RequestEndsWithIncompleteReadAsync()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

        var sendString = "POST / HTTP/1.0\r\nContent-Length: 12\r\n\r\nHello World?";

        await using var server = new TestServer(async context =>
        {
            var result = await context.Request.BodyReader.ReadAsync();

            // The request body might be incomplete, but there should be something in the first read.
            Assert.True(result.Buffer.Length > 0);
            Assert.Equal(result.Buffer.ToSpan(), "Hello World?"u8[..(int)result.Buffer.Length]);

            await context.Response.WriteAsync("Hello World?");
            // No BodyReader.Advance. Connection will fail when attempting to complete body.
        }, serviceContext);

        using (var connection = server.CreateConnection())
        {
            await connection.Send(sendString);

            await connection.ReceiveEnd(
                "HTTP/1.1 200 OK",
                "Connection: close",
                $"Date: {serviceContext.DateHeaderValue}",
                "",
                "Hello World?");

            await connection.WaitForConnectionClose();

            Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m =>
            {
                AssertDuration(m, "127.0.0.1", localPort: 0, "tcp", "ipv4", KestrelMetrics.Http11, error: KestrelMetrics.GetErrorType(ConnectionEndReason.InvalidBodyReaderState));
            });
        }
    }

    [Fact]
    public async Task Http1Connection_ServerShutdown_Graceful()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory))
        {
            ShutdownTimeout = TimeSpan.FromSeconds(60)
        };

        var sendString = "POST / HTTP/1.0\r\nContent-Length: 12\r\n\r\nHello World?";

        var getNotificationFeatureTcs = new TaskCompletionSource<IConnectionLifetimeNotificationFeature>(TaskCreationOptions.RunContinuationsAsynchronously);
        var server = new TestServer(async c =>
        {
            getNotificationFeatureTcs.TrySetResult(c.Features.Get<IConnectionLifetimeNotificationFeature>());
            await EchoApp(c);
        }, serviceContext);
        using var connection = server.CreateConnection();

        try
        {
            await connection.Send(sendString);
        }
        finally
        {
            Logger.LogInformation("Waiting for notification feature");
            var notificationFeature = await getNotificationFeatureTcs.Task.DefaultTimeout();

            // Dispose while the connection is in-progress.
            var shutdownTask = server.DisposeAsync();

            var waitForConnectionCloseRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            notificationFeature.ConnectionClosedRequested.Register(() =>
            {
                Logger.LogInformation("ConnectionClosedRequested");
                waitForConnectionCloseRequest.TrySetResult();
            });

            Logger.LogInformation("Waiting for connection close request.");
            await waitForConnectionCloseRequest.Task.DefaultTimeout();

            Logger.LogInformation("Receiving data and closing connection.");
            await connection.ReceiveEnd(
                "HTTP/1.1 200 OK",
                "Connection: close",
                $"Date: {serviceContext.DateHeaderValue}",
                "",
                "Hello World?");
            await connection.WaitForConnectionClose();
            connection.Dispose();

            Logger.LogInformation("Finishing shutting down.");
            await shutdownTask;
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m =>
        {
            AssertDuration(m, "127.0.0.1", localPort: 0, "tcp", "ipv4", KestrelMetrics.Http11);
        });
    }

    [Fact]
    public async Task Http1Connection_ServerShutdown_Abort()
    {
        ThrowOnUngracefulShutdown = false;

        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory))
        {
            MemoryPoolFactory = PinnedBlockMemoryPoolFactory.CreatePinnedBlockMemoryPool,
            ShutdownTimeout = TimeSpan.Zero
        };

        var sendString = "POST / HTTP/1.0\r\nContent-Length: 12\r\n\r\nHello World?";
        var connectionCloseTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestReceivedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var server = new TestServer(async c =>
        {
            requestReceivedTcs.TrySetResult();
            await c.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes("Hello world"));
            await c.Response.BodyWriter.FlushAsync();
            await connectionCloseTcs.Task;
            Logger.LogInformation("Server request delegate finishing.");
        }, serviceContext);

        using var connection = server.CreateConnection();
        connection.TransportConnection.ConnectionClosed.Register(() =>
        {
            Logger.LogInformation("Connection closed raised.");
            connectionCloseTcs.TrySetResult();
        });

        try
        {
            await connection.Send(sendString);
            await requestReceivedTcs.Task.DefaultTimeout();
        }
        finally
        {
            // Dispose while the connection is in-progress.
            Logger.LogInformation("Shutting down server.");
            await server.DisposeAsync();
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m =>
        {
            AssertDuration(m, "127.0.0.1", localPort: 0, "tcp", "ipv4", KestrelMetrics.Http11, error: KestrelMetrics.GetErrorType(ConnectionEndReason.AppShutdownTimeout));
        });
    }

    [Fact]
    public async Task Http1Connection_IHttpConnectionTagsFeatureIgnoreFeatureSetOnTransport()
    {
        var sync = new SyncPoint();
        ConnectionContext currentConnectionContext = null;

        var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
        listenOptions.Use(next =>
        {
            return async connectionContext =>
            {
                currentConnectionContext = connectionContext;

                connectionContext.Features.Get<IConnectionMetricsTagsFeature>().Tags.Add(new KeyValuePair<string, object>("custom", "value!"));

                // Wait for the test to verify the connection has started.
                await sync.WaitToContinue();

                await next(connectionContext);
            };
        });

        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");
        using var activeConnections = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.active_connections");
        using var queuedConnections = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.queued_connections");

        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

        var sendString = "POST / HTTP/1.0\r\nContent-Length: 12\r\n\r\nHello World?";

        await using var server = new TestServer(EchoApp, serviceContext, listenOptions);

        // This feature will be overidden by Kestrel. Kestrel is the owner of the feature and is resposible for setting it.
        var overridenFeature = new TestConnectionMetricsTagsFeature();
        overridenFeature.Tags.Add(new KeyValuePair<string, object>("test", "Value!"));

        using (var connection = server.CreateConnection(featuresAction: features =>
        {
            features.Set<IConnectionMetricsTagsFeature>(overridenFeature);
        }))
        {
            await connection.Send(sendString);

            // Wait for connection to start on the server.
            await sync.WaitForSyncPoint();

            Assert.NotEqual(overridenFeature, currentConnectionContext.Features.Get<IConnectionMetricsTagsFeature>());

            Assert.Empty(connectionDuration.GetMeasurementSnapshot());
            Assert.Collection(activeConnections.GetMeasurementSnapshot(), m => AssertCount(m, 1, "127.0.0.1", localPort: 0, "tcp", "ipv4"));

            // Signal that connection can continue.
            sync.Continue();

            await connection.ReceiveEnd(
                "HTTP/1.1 200 OK",
                "Connection: close",
                $"Date: {serviceContext.DateHeaderValue}",
                "",
                "Hello World?");

            await connection.WaitForConnectionClose();
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m =>
        {
            AssertDuration(m, "127.0.0.1", localPort: 0, "tcp", "ipv4", KestrelMetrics.Http11);
            Assert.Equal("value!", (string)m.Tags["custom"]);
            Assert.False(m.Tags.ContainsKey("test"));
        });
        Assert.Collection(activeConnections.GetMeasurementSnapshot(), m => AssertCount(m, 1, "127.0.0.1", localPort: 0, "tcp", "ipv4"), m => AssertCount(m, -1, "127.0.0.1", localPort: 0, "tcp", "ipv4"));
        Assert.Collection(queuedConnections.GetMeasurementSnapshot(), m => AssertCount(m, 1, "127.0.0.1", localPort: 0, "tcp", "ipv4"), m => AssertCount(m, -1, "127.0.0.1", localPort: 0, "tcp", "ipv4"));
    }

    [Fact]
    public async Task Http1Connection_ServerAbort_HasErrorType()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

        var sendString = "POST / HTTP/1.0\r\nContent-Length: 12\r\n\r\nHello World?";
        var finishedSendingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var server = new TestServer(async c =>
        {
            await c.Request.Body.ReadUntilEndAsync();

            // An extra check to ensure that client is done sending before the server aborts.
            // This might not be necessary since we're reading to the end of the request body, but it doesn't hurt.
            await finishedSendingTcs.Task;

            c.Abort();
        }, serviceContext);

        using (var connection = server.CreateConnection())
        {
            await connection.Send(sendString).DefaultTimeout();

            finishedSendingTcs.SetResult();

            await connection.ReceiveEnd().DefaultTimeout();

            await connection.WaitForConnectionClose().DefaultTimeout();
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m =>
        {
            AssertDuration(m, "127.0.0.1", localPort: 0, "tcp", "ipv4", KestrelMetrics.Http11, error: KestrelMetrics.GetErrorType(ConnectionEndReason.AbortedByApp));
        });
    }

    private sealed class TestConnectionMetricsTagsFeature : IConnectionMetricsTagsFeature
    {
        public ICollection<KeyValuePair<string, object>> Tags { get; } = new List<KeyValuePair<string, object>>();
    }

    [Fact]
    public async Task Http1Connection_Error()
    {
        var sync = new SyncPoint();

        var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
        listenOptions.Use(next =>
        {
            return async connectionContext =>
            {
                // Wait for the test to verify the connection has started.
                await sync.WaitToContinue();

                throw new InvalidOperationException("Test");
            };
        });

        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");
        using var activeConnections = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.active_connections");
        using var queuedConnections = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.queued_connections");

        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

        var sendString = "POST / HTTP/1.0\r\nContent-Length: 12\r\n\r\nHello World?";

        await using var server = new TestServer(EchoApp, serviceContext, listenOptions);

        using (var connection = server.CreateConnection())
        {
            await connection.Send(sendString);

            // Wait for connection to start on the server.
            await sync.WaitForSyncPoint();

            Assert.Empty(connectionDuration.GetMeasurementSnapshot());
            Assert.Collection(activeConnections.GetMeasurementSnapshot(), m => AssertCount(m, 1, "127.0.0.1", localPort: 0, "tcp", "ipv4"));

            // Signal that connection can continue.
            sync.Continue();

            await connection.ReceiveEnd("");

            await connection.WaitForConnectionClose();
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m =>
        {
            AssertDuration(m, "127.0.0.1", localPort: 0, "tcp", "ipv4", httpVersion: null, error: "System.InvalidOperationException");
        });
        Assert.Collection(activeConnections.GetMeasurementSnapshot(), m => AssertCount(m, 1, "127.0.0.1", localPort: 0, "tcp", "ipv4"), m => AssertCount(m, -1, "127.0.0.1", localPort: 0, "tcp", "ipv4"));
        Assert.Collection(queuedConnections.GetMeasurementSnapshot(), m => AssertCount(m, 1, "127.0.0.1", localPort: 0, "tcp", "ipv4"), m => AssertCount(m, -1, "127.0.0.1", localPort: 0, "tcp", "ipv4"));
    }

    [Fact]
    public async Task Http1Connection_Upgrade()
    {
        var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));

        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");
        using var activeConnections = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.active_connections");
        using var currentUpgradedRequests = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.upgraded_connections");

        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

        await using var server = new TestServer(UpgradeApp, serviceContext, listenOptions);

        using (var connection = server.CreateConnection())
        {
            await connection.SendEmptyGetWithUpgrade();
            await connection.ReceiveEnd("HTTP/1.1 101 Switching Protocols",
                "Connection: Upgrade",
                $"Date: {server.Context.DateHeaderValue}",
                "",
                "");
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => AssertDuration(m, "127.0.0.1", localPort: 0, "tcp", "ipv4", KestrelMetrics.Http11));
        Assert.Collection(activeConnections.GetMeasurementSnapshot(), m => AssertCount(m, 1, "127.0.0.1", localPort: 0, "tcp", "ipv4"), m => AssertCount(m, -1, "127.0.0.1", localPort: 0, "tcp", "ipv4"));
        Assert.Collection(currentUpgradedRequests.GetMeasurementSnapshot(), m => Assert.Equal(1, m.Value), m => Assert.Equal(-1, m.Value));

        static async Task UpgradeApp(HttpContext context)
        {
            var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();

            if (upgradeFeature.IsUpgradableRequest)
            {
                await upgradeFeature.UpgradeAsync();
            }
        }
    }

    [Fact]
    public async Task Http2Connection_ServerShutdown_Graceful()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var getNotificationFeatureTcs = new TaskCompletionSource<IConnectionLifetimeNotificationFeature>(TaskCreationOptions.RunContinuationsAsynchronously);
        var server = new TestServer(async context =>
        {
            getNotificationFeatureTcs.TrySetResult(context.Features.Get<IConnectionLifetimeNotificationFeature>());
            await context.Response.BodyWriter.FlushAsync();
            await tcs.Task;
        },
        new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory))
        {
            ShutdownTimeout = TimeSpan.FromSeconds(200)
        },
        listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
        });

        HttpResponseMessage responseMessage = null;
        Stream responseStream = null;
        using var connection = server.CreateConnection();
        using var socketsHandler = new SocketsHttpHandler()
        {
            ConnectCallback = (_, _) =>
            {
                return new ValueTask<Stream>(connection.Stream);
            }
        };
        using var httpClient = new HttpClient(socketsHandler);

        try
        {

            var httpRequestMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri("http://localhost/"),
                Version = new Version(2, 0),
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            };

            responseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
            responseMessage.EnsureSuccessStatusCode();
            responseStream = await responseMessage.Content.ReadAsStreamAsync();
        }
        finally
        {
            var notificationFeature = await getNotificationFeatureTcs.Task.DefaultTimeout();

            var shutdownTask = server.DisposeAsync();

            var waitForConnectionCloseRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            notificationFeature.ConnectionClosedRequested.Register(() =>
            {
                waitForConnectionCloseRequest.TrySetResult();
            });

            await waitForConnectionCloseRequest.Task.DefaultTimeout();
            tcs.TrySetResult();

            await responseStream.ReadUntilEndAsync().DefaultTimeout();
            responseMessage.Dispose();

            connection.Dispose();

            await shutdownTask;
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => AssertDuration(m, "127.0.0.1", localPort: 0, "tcp", "ipv4", KestrelMetrics.Http2));
    }

    [Fact]
    public async Task Http2Connection_ServerShutdown_Abort()
    {
        ThrowOnUngracefulShutdown = false;

        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory))
        {
            ShutdownTimeout = TimeSpan.Zero,
            MemoryPoolFactory = PinnedBlockMemoryPoolFactory.CreatePinnedBlockMemoryPool
        };

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var server = new TestServer(async context =>
        {
            await context.Response.BodyWriter.FlushAsync();
            await tcs.Task;
        },
        serviceContext,
        listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
        });

        HttpResponseMessage responseMessage = null;
        using var connection = server.CreateConnection();
        connection.TransportConnection.ConnectionClosed.Register(() => tcs.TrySetResult());

        using var socketsHandler = new SocketsHttpHandler()
        {
            ConnectCallback = (_, _) =>
            {
                return new ValueTask<Stream>(connection.Stream);
            }
        };

        using var httpClient = new HttpClient(socketsHandler);

        try
        {
            var httpRequestMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri("http://localhost/"),
                Version = new Version(2, 0),
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            };

            responseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
            responseMessage.EnsureSuccessStatusCode();
        }
        finally
        {
            var shutdownTask = server.DisposeAsync().DefaultTimeout();

            await shutdownTask;
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => AssertDuration(m, "127.0.0.1", localPort: 0, "tcp", "ipv4", KestrelMetrics.Http2, error: KestrelMetrics.GetErrorType(ConnectionEndReason.AppShutdownTimeout)));
    }

    [ConditionalFact]
    [TlsAlpnSupported]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)]
    public async Task Http2Connection()
    {
        string connectionId = null;

        const int requestsToSend = 2;
        var requestsReceived = 0;

        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");
        using var activeConnections = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.active_connections");
        using var queuedConnections = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.queued_connections");
        using var queuedRequests = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.queued_requests");
        using var tlsHandshakeDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.tls_handshake.duration");
        using var activeTlsHandshakes = new MetricCollector<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.active_tls_handshakes");

        await using (var server = new TestServer(context =>
        {
            connectionId = context.Features.Get<IHttpConnectionFeature>().ConnectionId;
            requestsReceived++;
            return Task.CompletedTask;
        },
        new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory)),
        listenOptions =>
        {
            listenOptions.UseHttps(_x509Certificate2, options =>
            {
                options.SslProtocols = SslProtocols.Tls12;
            });
            listenOptions.Protocols = HttpProtocols.Http2;
        }))
        {
            using var connection = server.CreateConnection();

            using var socketsHandler = new SocketsHttpHandler()
            {
                ConnectCallback = (_, _) =>
                {
                    // This test should only require a single connection.
                    if (connectionId != null)
                    {
                        throw new InvalidOperationException();
                    }

                    return new ValueTask<Stream>(connection.Stream);
                },
                SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = (_, _, _, _) => true
                }
            };

            using var httpClient = new HttpClient(socketsHandler);

            for (int i = 0; i < requestsToSend; i++)
            {
                using var httpRequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri("https://localhost/"),
                    Version = new Version(2, 0),
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                };

                using var responseMessage = await httpClient.SendAsync(httpRequestMessage);
                responseMessage.EnsureSuccessStatusCode();
            }
        }

        Assert.NotNull(connectionId);
        Assert.Equal(2, requestsReceived);

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => AssertDuration(m, "127.0.0.1", localPort: 0, "tcp", "ipv4", KestrelMetrics.Http2, "1.2"));
        Assert.Collection(activeConnections.GetMeasurementSnapshot(), m => AssertCount(m, 1, "127.0.0.1", localPort: 0, "tcp", "ipv4"), m => AssertCount(m, -1, "127.0.0.1", localPort: 0, "tcp", "ipv4"));
        Assert.Collection(queuedConnections.GetMeasurementSnapshot(), m => AssertCount(m, 1, "127.0.0.1", localPort: 0, "tcp", "ipv4"), m => AssertCount(m, -1, "127.0.0.1", localPort: 0, "tcp", "ipv4"));

        Assert.Collection(queuedRequests.GetMeasurementSnapshot(),
            m => AssertRequestCount(m, 1, KestrelMetrics.Http2),
            m => AssertRequestCount(m, -1, KestrelMetrics.Http2),
            m => AssertRequestCount(m, 1, KestrelMetrics.Http2),
            m => AssertRequestCount(m, -1, KestrelMetrics.Http2));

        Assert.Collection(tlsHandshakeDuration.GetMeasurementSnapshot(), m =>
        {
            Assert.True(m.Value > 0);
            Assert.Equal("1.2", (string)m.Tags["tls.protocol.version"]);
            Assert.DoesNotContain("error.type", m.Tags.Keys);
        });
        Assert.Collection(activeTlsHandshakes.GetMeasurementSnapshot(), m => Assert.Equal(1, m.Value), m => Assert.Equal(-1, m.Value));

        static void AssertRequestCount(CollectedMeasurement<long> measurement, long expectedValue, string httpVersion)
        {
            Assert.Equal(expectedValue, measurement.Value);
            Assert.Equal("http", (string)measurement.Tags["network.protocol.name"]);
            Assert.Equal(httpVersion, (string)measurement.Tags["network.protocol.version"]);
        }
    }

    [Fact]
    public async Task Http2Connection_ServerAbort_NoErrorType()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var server = new TestServer(
            context =>
            {
                context.Response.WriteAsync("Hello world");
                Logger.LogInformation("Server aborting request.");
                context.Abort();
                return Task.CompletedTask;
            },
            new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory)),
            listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });

        using var connection = server.CreateConnection();

        using var socketsHandler = new SocketsHttpHandler()
        {
            ConnectCallback = (_, _) =>
            {
                return new ValueTask<Stream>(connection.Stream);
            },
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            },
            KeepAlivePingDelay = Timeout.InfiniteTimeSpan
        };

        using var httpClient = new HttpClient(socketsHandler);
        Task shutdownTask = Task.CompletedTask;

        try
        {
            using var httpRequestMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri("http://localhost/"),
                Version = new Version(2, 0),
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            };

            Logger.LogInformation("Client sending request.");
            using var responseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();

            Logger.LogInformation("Client validating response status code.");
            responseMessage.EnsureSuccessStatusCode();

            Logger.LogInformation("Client reading response until end.");
            var stream = await responseMessage.Content.ReadAsStreamAsync().DefaultTimeout();
            await Assert.ThrowsAnyAsync<Exception>(() => stream.ReadUntilEndAsync()).DefaultTimeout();
        }
        finally
        {
            Logger.LogInformation("Start server shutdown. The connection should be closed because it has no active requests.");
            shutdownTask = server.DisposeAsync().AsTask();
        }

        Logger.LogInformation("Waiting for measurement.");
        await connectionDuration.WaitForMeasurementsAsync(minCount: 1).DefaultTimeout();

        Logger.LogInformation("Asserting metrics.");
        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => AssertDuration(m, "127.0.0.1", localPort: 0, "tcp", "ipv4", KestrelMetrics.Http2));

        connection.ShutdownSend();
        await shutdownTask.DefaultTimeout();
    }

    [ConditionalFact]
    [TlsAlpnSupported]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)]
    public async Task Http2Connection_TlsError()
    {
        string connectionId = null;

        //const int requestsToSend = 2;
        var requestsReceived = 0;

        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");
        using var tlsHandshakeDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.tls_handshake.duration");

        await using (var server = new TestServer(context =>
        {
            connectionId = context.Features.Get<IHttpConnectionFeature>().ConnectionId;
            requestsReceived++;
            return Task.CompletedTask;
        },
        new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory)),
        listenOptions =>
        {
            listenOptions.UseHttps(_x509Certificate2, options =>
            {
                options.SslProtocols = SslProtocols.Tls12;
                options.ClientCertificateMode = Https.ClientCertificateMode.RequireCertificate;
            });
            listenOptions.Protocols = HttpProtocols.Http2;
        }))
        {
            using var connection = server.CreateConnection();

            using var socketsHandler = new SocketsHttpHandler()
            {
                ConnectCallback = (_, _) =>
                {
                    // This test should only require a single connection.
                    if (connectionId != null)
                    {
                        throw new InvalidOperationException();
                    }

                    return new ValueTask<Stream>(connection.Stream);
                },
                SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = (_, _, _, _) => true
                }
            };

            using var httpClient = new HttpClient(socketsHandler);

            //for (int i = 0; i < requestsToSend; i++)
            {
                using var httpRequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri("https://localhost/"),
                    Version = new Version(2, 0),
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                };

                await Assert.ThrowsAsync<HttpRequestException>(() => httpClient.SendAsync(httpRequestMessage));
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m =>
        {
            AssertDuration(m, "127.0.0.1", localPort: 0, "tcp", "ipv4", httpVersion: null, tlsProtocolVersion: null, error: KestrelMetrics.GetErrorType(ConnectionEndReason.TlsHandshakeFailed));
        });

        Assert.Collection(tlsHandshakeDuration.GetMeasurementSnapshot(), m =>
        {
            Assert.True(m.Value > 0);
            Assert.Equal(typeof(AuthenticationException).FullName, (string)m.Tags["error.type"]);
            Assert.DoesNotContain("tls.protocol.version", m.Tags.Keys);
        });
    }

    private static async Task EchoApp(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var response = httpContext.Response;
        var buffer = new byte[httpContext.Request.ContentLength ?? 0];

        if (buffer.Length > 0)
        {
            await request.Body.FillBufferUntilEndAsync(buffer).DefaultTimeout();
            await response.Body.WriteAsync(buffer, 0, buffer.Length);
        }
    }

    private static void AssertDuration(CollectedMeasurement<double> measurement, string localAddress, int? localPort, string networkTransport, string networkType, string httpVersion, string tlsProtocolVersion = null, string error = null)
    {
        Assert.True(measurement.Value > 0);
        Assert.Equal(networkTransport, (string)measurement.Tags["network.transport"]);
        Assert.Equal(localAddress, (string)measurement.Tags["server.address"]);
        if (localPort is not null)
        {
            Assert.Equal(localPort, (int)measurement.Tags["server.port"]);
        }
        else
        {
            Assert.DoesNotContain("server.port", measurement.Tags.Keys);
        }
        if (networkType is not null)
        {
            Assert.Equal(networkType, (string)measurement.Tags["network.type"]);
        }
        else
        {
            Assert.DoesNotContain("network.type", measurement.Tags.Keys);
        }
        if (httpVersion is not null)
        {
            Assert.Equal("http", (string)measurement.Tags["network.protocol.name"]);
            Assert.Equal(httpVersion, (string)measurement.Tags["network.protocol.version"]);
        }
        else
        {
            Assert.DoesNotContain("network.protocol.name", measurement.Tags.Keys);
            Assert.DoesNotContain("network.protocol.version", measurement.Tags.Keys);
        }
        if (tlsProtocolVersion is not null)
        {
            Assert.Equal(tlsProtocolVersion, (string)measurement.Tags["tls.protocol.version"]);
        }
        else
        {
            Assert.DoesNotContain("tls.protocol.version", measurement.Tags.Keys);
        }
        if (error is not null)
        {
            Assert.Equal(error, (string)measurement.Tags["error.type"]);
        }
        else
        {
            try
            {
                Assert.DoesNotContain("error.type", measurement.Tags.Keys);
            }
            catch (Exception ex)
            {
                throw new Exception($"Connection has unexpected error.type value: {measurement.Tags["error.type"]}", ex);
            }
        }
    }

    private static void AssertCount(CollectedMeasurement<long> measurement, long expectedValue, string localAddress, int? localPort, string networkTransport, string networkType)
    {
        Assert.Equal(expectedValue, measurement.Value);
        Assert.Equal(networkTransport, (string)measurement.Tags["network.transport"]);
        Assert.Equal(localAddress, (string)measurement.Tags["server.address"]);
        if (localPort is not null)
        {
            Assert.Equal(localPort, (int)measurement.Tags["server.port"]);
        }
        else
        {
            Assert.DoesNotContain("server.port", measurement.Tags.Keys);
        }
        if (networkType is not null)
        {
            Assert.Equal(networkType, (string)measurement.Tags["network.type"]);
        }
        else
        {
            Assert.DoesNotContain("network.type", measurement.Tags.Keys);
        }
    }
}
