// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
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
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class KestrelMetricsTests : TestApplicationErrorLoggerLoggedTest
{
    private static readonly X509Certificate2 _x509Certificate2 = TestResources.GetTestCertificate();

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
        using var connectionDuration = new InstrumentRecorder<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "connection-duration");
        using var currentConnections = new InstrumentRecorder<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "current-connections");
        using var queuedConnections = new InstrumentRecorder<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "queued-connections");

        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

        var sendString = "POST / HTTP/1.0\r\nContent-Length: 12\r\n\r\nHello World?";

        await using var server = new TestServer(EchoApp, serviceContext, listenOptions);

        using (var connection = server.CreateConnection())
        {
            await connection.Send(sendString);

            // Wait for connection to start on the server.
            await sync.WaitForSyncPoint();

            Assert.Empty(connectionDuration.GetMeasurements());
            Assert.Collection(currentConnections.GetMeasurements(), m => AssertCount(m, 1, "127.0.0.1:0"));

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

        Assert.Collection(connectionDuration.GetMeasurements(), m =>
        {
            AssertDuration(m, "127.0.0.1:0");
            Assert.Equal("value!", (string)m.Tags.ToArray().Single(t => t.Key == "custom").Value);
        });
        Assert.Collection(currentConnections.GetMeasurements(), m => AssertCount(m, 1, "127.0.0.1:0"), m => AssertCount(m, -1, "127.0.0.1:0"));
        Assert.Collection(queuedConnections.GetMeasurements(), m => AssertCount(m, 1, "127.0.0.1:0"), m => AssertCount(m, -1, "127.0.0.1:0"));
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

            using var connectionDuration = new InstrumentRecorder<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "connection-duration");
            using var currentConnections = new InstrumentRecorder<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "current-connections");
            using var queuedConnections = new InstrumentRecorder<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "queued-connections");

            // Signal that connection can continue.
            sync.Continue();

            await connection.ReceiveEnd(
                "HTTP/1.1 200 OK",
                "Connection: close",
                $"Date: {serviceContext.DateHeaderValue}",
                "",
                "Hello World?");

            await connection.WaitForConnectionClose();

            Assert.Empty(connectionDuration.GetMeasurements());
            Assert.Empty(currentConnections.GetMeasurements());
            Assert.Empty(queuedConnections.GetMeasurements());

            Assert.False(hasConnectionMetricsTagsFeature);
        }
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
        using var connectionDuration = new InstrumentRecorder<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "connection-duration");
        using var currentConnections = new InstrumentRecorder<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "current-connections");
        using var queuedConnections = new InstrumentRecorder<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "queued-connections");

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

            Assert.Empty(connectionDuration.GetMeasurements());
            Assert.Collection(currentConnections.GetMeasurements(), m => AssertCount(m, 1, "127.0.0.1:0"));

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

        Assert.Collection(connectionDuration.GetMeasurements(), m =>
        {
            AssertDuration(m, "127.0.0.1:0");
            Assert.Equal("value!", (string)m.Tags.ToArray().Single(t => t.Key == "custom").Value);
            Assert.Empty(m.Tags.ToArray().Where(t => t.Key == "test"));
        });
        Assert.Collection(currentConnections.GetMeasurements(), m => AssertCount(m, 1, "127.0.0.1:0"), m => AssertCount(m, -1, "127.0.0.1:0"));
        Assert.Collection(queuedConnections.GetMeasurements(), m => AssertCount(m, 1, "127.0.0.1:0"), m => AssertCount(m, -1, "127.0.0.1:0"));
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
        using var connectionDuration = new InstrumentRecorder<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "connection-duration");
        using var currentConnections = new InstrumentRecorder<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "current-connections");
        using var queuedConnections = new InstrumentRecorder<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "queued-connections");

        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

        var sendString = "POST / HTTP/1.0\r\nContent-Length: 12\r\n\r\nHello World?";

        await using var server = new TestServer(EchoApp, serviceContext, listenOptions);

        using (var connection = server.CreateConnection())
        {
            await connection.Send(sendString);

            // Wait for connection to start on the server.
            await sync.WaitForSyncPoint();

            Assert.Empty(connectionDuration.GetMeasurements());
            Assert.Collection(currentConnections.GetMeasurements(), m => AssertCount(m, 1, "127.0.0.1:0"));

            // Signal that connection can continue.
            sync.Continue();

            await connection.ReceiveEnd("");

            await connection.WaitForConnectionClose();
        }

        Assert.Collection(connectionDuration.GetMeasurements(), m =>
        {
            AssertDuration(m, "127.0.0.1:0");
            Assert.Equal("System.InvalidOperationException", (string)m.Tags.ToArray().Single(t => t.Key == "exception-name").Value);
        });
        Assert.Collection(currentConnections.GetMeasurements(), m => AssertCount(m, 1, "127.0.0.1:0"), m => AssertCount(m, -1, "127.0.0.1:0"));
        Assert.Collection(queuedConnections.GetMeasurements(), m => AssertCount(m, 1, "127.0.0.1:0"), m => AssertCount(m, -1, "127.0.0.1:0"));
    }

    [Fact]
    public async Task Http1Connection_Upgrade()
    {
        var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));

        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new InstrumentRecorder<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "connection-duration");
        using var currentConnections = new InstrumentRecorder<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "current-connections");
        using var currentUpgradedRequests = new InstrumentRecorder<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "current-upgraded-connections");

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

        Assert.Collection(connectionDuration.GetMeasurements(), m => AssertDuration(m, "127.0.0.1:0"));
        Assert.Collection(currentConnections.GetMeasurements(), m => AssertCount(m, 1, "127.0.0.1:0"), m => AssertCount(m, -1, "127.0.0.1:0"));
        Assert.Collection(currentUpgradedRequests.GetMeasurements(), m => Assert.Equal(1, m.Value), m => Assert.Equal(-1, m.Value));

        static async Task UpgradeApp(HttpContext context)
        {
            var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();

            if (upgradeFeature.IsUpgradableRequest)
            {
                await upgradeFeature.UpgradeAsync();
            }
        }
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
        using var connectionDuration = new InstrumentRecorder<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "connection-duration");
        using var currentConnections = new InstrumentRecorder<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "current-connections");
        using var queuedConnections = new InstrumentRecorder<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "queued-connections");
        using var queuedRequests = new InstrumentRecorder<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "queued-requests");
        using var tlsHandshakeDuration = new InstrumentRecorder<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "tls-handshake-duration");
        using var currentTlsHandshakes = new InstrumentRecorder<long>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "current-tls-handshakes");

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

        Assert.Collection(connectionDuration.GetMeasurements(), m => AssertDuration(m, "127.0.0.1:0"));
        Assert.Collection(currentConnections.GetMeasurements(), m => AssertCount(m, 1, "127.0.0.1:0"), m => AssertCount(m, -1, "127.0.0.1:0"));
        Assert.Collection(queuedConnections.GetMeasurements(), m => AssertCount(m, 1, "127.0.0.1:0"), m => AssertCount(m, -1, "127.0.0.1:0"));

        Assert.Collection(queuedRequests.GetMeasurements(),
            m => AssertRequestCount(m, 1, "HTTP/2"),
            m => AssertRequestCount(m, -1, "HTTP/2"),
            m => AssertRequestCount(m, 1, "HTTP/2"),
            m => AssertRequestCount(m, -1, "HTTP/2"));

        Assert.Collection(tlsHandshakeDuration.GetMeasurements(), m =>
        {
            Assert.True(m.Value > 0);
            Assert.Equal("Tls12", (string)m.Tags.ToArray().Single(t => t.Key == "protocol").Value);
        });
        Assert.Collection(currentTlsHandshakes.GetMeasurements(), m => Assert.Equal(1, m.Value), m => Assert.Equal(-1, m.Value));

        static void AssertRequestCount(Measurement<long> measurement, long expectedValue, string httpVersion)
        {
            Assert.Equal(expectedValue, measurement.Value);
            Assert.Equal(httpVersion, (string)measurement.Tags.ToArray().Single(t => t.Key == "version").Value);
        }
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

    private static void AssertDuration(Measurement<double> measurement, string localEndpoint)
    {
        Assert.True(measurement.Value > 0);
        Assert.Equal(localEndpoint, (string)measurement.Tags.ToArray().Single(t => t.Key == "endpoint").Value);
    }

    private static void AssertCount(Measurement<long> measurement, long expectedValue, string localEndpoint)
    {
        Assert.Equal(expectedValue, measurement.Value);
        Assert.Equal(localEndpoint, (string)measurement.Tags.ToArray().Single(t => t.Key == "endpoint").Value);
    }
}
