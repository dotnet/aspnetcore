// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class EventSourceTests : LoggedTest
{
    private static readonly X509Certificate2 _x509Certificate2 = TestResources.GetTestCertificate();

    // To log all KestrelEventSource events, add `_listener = new TestEventListener(Logger);` to the start of the test method.
    // We could always construct TestEventListener with the test logger, but other concurrent tests could make this noisy.
    private readonly TestEventListener _listener = new TestEventListener();

    [Fact]
    public async Task Http1_EmitsStartAndStopEventsWithActivityIds()
    {
        int port;
        string connectionId = null;

        const int requestsToSend = 2;
        var requestIds = new string[requestsToSend];
        var requestsReceived = 0;

        await using (var server = new TestServer(async context =>
        {
            connectionId = context.Features.Get<IHttpConnectionFeature>().ConnectionId;
            requestIds[requestsReceived++] = context.TraceIdentifier;

            var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();

            if (upgradeFeature.IsUpgradableRequest)
            {
                await upgradeFeature.UpgradeAsync();
            }
        },
        new TestServiceContext(LoggerFactory)))
        {
            port = server.Port;

            using var connection = server.CreateConnection();

            await connection.SendEmptyGet();
            await connection.Receive(
                "HTTP/1.1 200 OK",
                "Content-Length: 0",
                $"Date: {server.Context.DateHeaderValue}",
                "",
                "");

            await connection.SendEmptyGetWithUpgrade();
            await connection.ReceiveEnd("HTTP/1.1 101 Switching Protocols",
                "Connection: Upgrade",
                $"Date: {server.Context.DateHeaderValue}",
                "",
                "");
        }

        Assert.NotNull(connectionId);
        Assert.Equal(2, requestsReceived);

        // Other tests executing in parallel may log events.
        var events = _listener.EventData.Where(e => e != null && GetProperty(e, "connectionId") == connectionId).ToList();
        var eventIndex = 0;

        var connectionStart = events[eventIndex++];
        Assert.Equal("ConnectionStart", connectionStart.EventName);
        Assert.Equal(1, connectionStart.EventId);
        Assert.All(new[] { "connectionId", "remoteEndPoint", "localEndPoint" }, p => Assert.Contains(p, connectionStart.PayloadNames));
        Assert.Equal($"127.0.0.1:{port}", GetProperty(connectionStart, "localEndPoint"));
        Assert.Same(KestrelEventSource.Log, connectionStart.EventSource);
        Assert.NotEqual(Guid.Empty, connectionStart.ActivityId);

        var firstRequestStart = events[eventIndex++];
        Assert.Equal("RequestStart", firstRequestStart.EventName);
        Assert.Equal(3, firstRequestStart.EventId);
        Assert.All(new[] { "connectionId", "requestId" }, p => Assert.Contains(p, firstRequestStart.PayloadNames));
        Assert.Equal(requestIds[0], GetProperty(firstRequestStart, "requestId"));
        Assert.Same(KestrelEventSource.Log, firstRequestStart.EventSource);
        Assert.NotEqual(Guid.Empty, firstRequestStart.ActivityId);
        Assert.Equal(connectionStart.ActivityId, firstRequestStart.RelatedActivityId);

        var firstRequestStop = events[eventIndex++];
        Assert.Equal("RequestStop", firstRequestStop.EventName);
        Assert.Equal(4, firstRequestStop.EventId);
        Assert.All(new[] { "connectionId", "requestId" }, p => Assert.Contains(p, firstRequestStop.PayloadNames));
        Assert.Same(KestrelEventSource.Log, firstRequestStop.EventSource);
        Assert.Equal(requestIds[0], GetProperty(firstRequestStop, "requestId"));
        Assert.Equal(firstRequestStart.ActivityId, firstRequestStop.ActivityId);
        Assert.Equal(Guid.Empty, firstRequestStop.RelatedActivityId);

        var secondRequestStart = events[eventIndex++];
        Assert.Equal("RequestStart", secondRequestStart.EventName);
        Assert.Equal(3, secondRequestStart.EventId);
        Assert.All(new[] { "connectionId", "requestId" }, p => Assert.Contains(p, secondRequestStart.PayloadNames));
        Assert.Equal(requestIds[1], GetProperty(secondRequestStart, "requestId"));
        Assert.Same(KestrelEventSource.Log, secondRequestStart.EventSource);
        Assert.NotEqual(Guid.Empty, secondRequestStart.ActivityId);
        Assert.Equal(connectionStart.ActivityId, secondRequestStart.RelatedActivityId);

        var secondRequestStop = events[eventIndex++];
        Assert.Equal("RequestStop", secondRequestStop.EventName);
        Assert.Equal(4, secondRequestStop.EventId);
        Assert.All(new[] { "connectionId", "requestId" }, p => Assert.Contains(p, secondRequestStop.PayloadNames));
        Assert.Same(KestrelEventSource.Log, secondRequestStop.EventSource);
        Assert.Equal(requestIds[1], GetProperty(secondRequestStop, "requestId"));
        Assert.Equal(secondRequestStart.ActivityId, secondRequestStop.ActivityId);
        Assert.Equal(Guid.Empty, secondRequestStop.RelatedActivityId);

        var connectionStop = events[eventIndex++];
        Assert.Equal("ConnectionStop", connectionStop.EventName);
        Assert.Equal(2, connectionStop.EventId);
        Assert.All(new[] { "connectionId" }, p => Assert.Contains(p, connectionStop.PayloadNames));
        Assert.Same(KestrelEventSource.Log, connectionStop.EventSource);
        Assert.Equal(connectionStart.ActivityId, connectionStop.ActivityId);
        Assert.Equal(Guid.Empty, connectionStop.RelatedActivityId);

        Assert.Equal(eventIndex, events.Count);
    }

    [ConditionalFact]
    [TlsAlpnSupported]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)]
    public async Task Http2_EmitsStartAndStopEventsWithActivityIds()
    {
        int port;
        string connectionId = null;

        const int requestsToSend = 2;
        var requestIds = new string[requestsToSend];
        var requestsReceived = 0;

        await using (var server = new TestServer(context =>
        {
            connectionId = context.Features.Get<IHttpConnectionFeature>().ConnectionId;
            requestIds[requestsReceived++] = context.TraceIdentifier;
            return Task.CompletedTask;
        },
        new TestServiceContext(LoggerFactory),
        listenOptions =>
        {
            listenOptions.UseHttps(_x509Certificate2);
            listenOptions.Protocols = HttpProtocols.Http2;
        }))
        {
            port = server.Port;

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

        // Other tests executing in parallel may log events.
        var events = _listener.EventData.Where(e => e != null && GetProperty(e, "connectionId") == connectionId).ToList();
        var eventIndex = 0;

        var connectionStart = events[eventIndex++];
        Assert.Equal("ConnectionStart", connectionStart.EventName);
        Assert.Equal(1, connectionStart.EventId);
        Assert.All(new[] { "connectionId", "remoteEndPoint", "localEndPoint" }, p => Assert.Contains(p, connectionStart.PayloadNames));
        Assert.Same(KestrelEventSource.Log, connectionStart.EventSource);
        Assert.Equal($"127.0.0.1:{port}", GetProperty(connectionStart, "localEndPoint"));
        Assert.NotEqual(Guid.Empty, connectionStart.ActivityId);

        var tlsHandshakeStart = events[eventIndex++];
        Assert.Equal("TlsHandshakeStart", tlsHandshakeStart.EventName);
        Assert.Equal(8, tlsHandshakeStart.EventId);
        Assert.All(new[] { "connectionId", "sslProtocols" }, p => Assert.Contains(p, tlsHandshakeStart.PayloadNames));
        Assert.Same(KestrelEventSource.Log, tlsHandshakeStart.EventSource);
        Assert.NotEqual(Guid.Empty, tlsHandshakeStart.ActivityId);
        Assert.Equal(connectionStart.ActivityId, tlsHandshakeStart.RelatedActivityId);

        var tlsHandshakeStop = events[eventIndex++];
        Assert.Equal("TlsHandshakeStop", tlsHandshakeStop.EventName);
        Assert.Equal(9, tlsHandshakeStop.EventId);
        Assert.All(new[] { "connectionId", "sslProtocols", "applicationProtocol", "hostName" }, p => Assert.Contains(p, tlsHandshakeStop.PayloadNames));
        Assert.Equal("h2", GetProperty(tlsHandshakeStop, "applicationProtocol"));
        Assert.Same(KestrelEventSource.Log, tlsHandshakeStop.EventSource);
        Assert.Equal(tlsHandshakeStart.ActivityId, tlsHandshakeStop.ActivityId);
        Assert.Equal(Guid.Empty, tlsHandshakeStop.RelatedActivityId);

        for (int i = 0; i < requestsToSend; i++)
        {
            var requestStart = events[eventIndex++];
            Assert.Equal("RequestStart", requestStart.EventName);
            Assert.Equal(3, requestStart.EventId);
            Assert.All(new[] { "connectionId", "requestId" }, p => Assert.Contains(p, requestStart.PayloadNames));
            Assert.Equal(requestIds[i], GetProperty(requestStart, "requestId"));
            Assert.Same(KestrelEventSource.Log, requestStart.EventSource);
            Assert.NotEqual(Guid.Empty, requestStart.ActivityId);
            Assert.Equal(connectionStart.ActivityId, requestStart.RelatedActivityId);

            var requestStop = events[eventIndex++];
            Assert.Equal("RequestStop", requestStop.EventName);
            Assert.Equal(4, requestStop.EventId);
            Assert.All(new[] { "connectionId", "requestId" }, p => Assert.Contains(p, requestStop.PayloadNames));
            Assert.Same(KestrelEventSource.Log, requestStop.EventSource);
            Assert.Equal(requestIds[i], GetProperty(requestStop, "requestId"));
            Assert.Equal(requestStart.ActivityId, requestStop.ActivityId);
            Assert.Equal(Guid.Empty, requestStop.RelatedActivityId);
        }

        var connectionStop = events[eventIndex++];
        Assert.Equal("ConnectionStop", connectionStop.EventName);
        Assert.Equal(2, connectionStop.EventId);
        Assert.All(new[] { "connectionId" }, p => Assert.Contains(p, connectionStop.PayloadNames));
        Assert.Same(KestrelEventSource.Log, connectionStop.EventSource);
        Assert.Equal(connectionStart.ActivityId, connectionStop.ActivityId);
        Assert.Equal(Guid.Empty, connectionStop.RelatedActivityId);

        Assert.Equal(eventIndex, events.Count);
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "SslStream.AuthenticateAsServerAsync() doesn't throw on Win 7 when the client tries SSL 2.0.")]
    public async Task TlsHandshakeFailure_EmitsStartAndStopEventsWithActivityIds()
    {
        int port;
        string connectionId = null;

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory),
        listenOptions =>
        {
            listenOptions.Use(next =>
            {
                return connectionContext =>
                {
                    connectionId = connectionContext.ConnectionId;
                    return next(connectionContext);
                };
            });

            listenOptions.UseHttps(_x509Certificate2);
        }))
        {
            port = server.Port;

            using var connection = server.CreateConnection();
            await using var sslStream = new SslStream(connection.Stream);

            var clientAuthOptions = new SslClientAuthenticationOptions
            {
                TargetHost = "localhost",

                // Only enabling SslProtocols.Ssl2 should cause a handshake failure on most platforms.
#pragma warning disable CS0618 // Type or member is obsolete
                EnabledSslProtocols = SslProtocols.Ssl2,
#pragma warning restore CS0618 // Type or member is obsolete
            };

            using var handshakeCts = new CancellationTokenSource(TestConstants.DefaultTimeout);
            await Assert.ThrowsAnyAsync<Exception>(() => sslStream.AuthenticateAsClientAsync(clientAuthOptions, handshakeCts.Token));
        }

        Assert.NotNull(connectionId);

        // Other tests executing in parallel may log events.
        var events = _listener.EventData.Where(e => e != null && GetProperty(e, "connectionId") == connectionId).ToList();
        var eventIndex = 0;

        var connectionStart = events[eventIndex++];
        Assert.Equal("ConnectionStart", connectionStart.EventName);
        Assert.Equal(1, connectionStart.EventId);
        Assert.All(new[] { "connectionId", "remoteEndPoint", "localEndPoint" }, p => Assert.Contains(p, connectionStart.PayloadNames));
        Assert.Equal($"127.0.0.1:{port}", GetProperty(connectionStart, "localEndPoint"));
        Assert.Same(KestrelEventSource.Log, connectionStart.EventSource);
        Assert.NotEqual(Guid.Empty, connectionStart.ActivityId);

        var tlsHandshakeStart = events[eventIndex++];
        Assert.Equal("TlsHandshakeStart", tlsHandshakeStart.EventName);
        Assert.Equal(8, tlsHandshakeStart.EventId);
        Assert.All(new[] { "connectionId", "sslProtocols" }, p => Assert.Contains(p, tlsHandshakeStart.PayloadNames));
        Assert.Same(KestrelEventSource.Log, tlsHandshakeStart.EventSource);
        Assert.NotEqual(Guid.Empty, tlsHandshakeStart.ActivityId);
        Assert.Equal(connectionStart.ActivityId, tlsHandshakeStart.RelatedActivityId);

        var tlsHandshakeFailed = events[eventIndex++];
        Assert.Equal("TlsHandshakeFailed", tlsHandshakeFailed.EventName);
        Assert.Equal(10, tlsHandshakeFailed.EventId);
        Assert.All(new[] { "connectionId" }, p => Assert.Contains(p, tlsHandshakeFailed.PayloadNames));
        Assert.Same(KestrelEventSource.Log, tlsHandshakeFailed.EventSource);
        Assert.Equal(tlsHandshakeStart.ActivityId, tlsHandshakeFailed.ActivityId);
        Assert.Equal(Guid.Empty, tlsHandshakeFailed.RelatedActivityId);

        var tlsHandshakeStop = events[eventIndex++];
        Assert.Equal("TlsHandshakeStop", tlsHandshakeStop.EventName);
        Assert.Equal(9, tlsHandshakeStop.EventId);
        Assert.All(new[] { "connectionId", "sslProtocols", "applicationProtocol", "hostName" }, p => Assert.Contains(p, tlsHandshakeStop.PayloadNames));
        Assert.Same(KestrelEventSource.Log, tlsHandshakeStop.EventSource);
        Assert.Equal(tlsHandshakeStart.ActivityId, tlsHandshakeStop.ActivityId);
        Assert.Equal(Guid.Empty, tlsHandshakeStop.RelatedActivityId);

        var connectionStop = events[eventIndex++];
        Assert.Equal("ConnectionStop", connectionStop.EventName);
        Assert.Equal(2, connectionStop.EventId);
        Assert.All(new[] { "connectionId" }, p => Assert.Contains(p, connectionStop.PayloadNames));
        Assert.Same(KestrelEventSource.Log, connectionStop.EventSource);
        Assert.Equal(connectionStart.ActivityId, connectionStop.ActivityId);
        Assert.Equal(Guid.Empty, connectionStop.RelatedActivityId);

        Assert.Equal(eventIndex, events.Count);
    }

    [Fact]
    public async Task ConnectionLimitExceeded_EmitsStartAndStopEventsWithActivityIds()
    {
        int port;
        string connectionId = null;

        var serviceContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(context => Task.CompletedTask, serviceContext,
        listenOptions =>
        {
            listenOptions.Use(next =>
            {
                return connectionContext =>
                {
                    connectionId = connectionContext.ConnectionId;
                    return next(connectionContext);
                };
            });

            listenOptions.Use(next =>
            {
                return new ConnectionLimitMiddleware<ConnectionContext>(c => next(c), connectionLimit: 0, serviceContext.Log, metrics: null).OnConnectionAsync;
            });
        }))
        {
            port = server.Port;

            using var connection = server.CreateConnection();
            await connection.ReceiveEnd();
        }

        Assert.NotNull(connectionId);

        // Other tests executing in parallel may log events.
        var events = _listener.EventData.Where(e => e != null && GetProperty(e, "connectionId") == connectionId).ToList();
        var eventIndex = 0;

        var connectionStart = events[eventIndex++];
        Assert.Equal("ConnectionStart", connectionStart.EventName);
        Assert.Equal(1, connectionStart.EventId);
        Assert.All(new[] { "connectionId", "remoteEndPoint", "localEndPoint" }, p => Assert.Contains(p, connectionStart.PayloadNames));
        Assert.Equal($"127.0.0.1:{port}", GetProperty(connectionStart, "localEndPoint"));
        Assert.Same(KestrelEventSource.Log, connectionStart.EventSource);
        Assert.NotEqual(Guid.Empty, connectionStart.ActivityId);

        var connectionRejected = events[eventIndex++];
        Assert.Equal("ConnectionRejected", connectionRejected.EventName);
        Assert.Equal(5, connectionRejected.EventId);
        Assert.All(new[] { "connectionId" }, p => Assert.Contains(p, connectionRejected.PayloadNames));
        Assert.Same(KestrelEventSource.Log, connectionRejected.EventSource);
        Assert.Equal(connectionStart.ActivityId, connectionRejected.ActivityId);
        Assert.Equal(Guid.Empty, connectionRejected.RelatedActivityId);

        var connectionStop = events[eventIndex++];
        Assert.Equal("ConnectionStop", connectionStop.EventName);
        Assert.Equal(2, connectionStop.EventId);
        Assert.All(new[] { "connectionId" }, p => Assert.Contains(p, connectionStop.PayloadNames));
        Assert.Same(KestrelEventSource.Log, connectionStop.EventSource);
        Assert.Equal(connectionStart.ActivityId, connectionStop.ActivityId);
        Assert.Equal(Guid.Empty, connectionStop.RelatedActivityId);

        Assert.Equal(eventIndex, events.Count);
    }

    private string GetProperty(EventSnapshot data, string propName) => data.Payload.TryGetValue(propName, out var value) ? value : null;

    private class TestEventListener : EventListener
    {
        private readonly ConcurrentQueue<EventSnapshot> _events = new ConcurrentQueue<EventSnapshot>();
        private readonly ILogger _logger;

        private readonly Lock _disposeLock = new();
        private bool _disposed;

        public TestEventListener()
        {
            EnableEvents(KestrelEventSource.Log, EventLevel.Verbose);
        }

        public TestEventListener(ILogger logger)
            : this()
        {
            _logger = logger;
        }

        public IEnumerable<EventSnapshot> EventData => _events;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "System.Threading.Tasks.TplEventSource")
            {
                // Enable TasksFlowActivityIds
                EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)0x80);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            lock (_disposeLock)
            {
                if (_disposed)
                {
                    return;
                }

                _logger?.LogInformation("{event}", JsonSerializer.Serialize(eventData, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

                // EventWrittenEventArgs.ActivityId sometimes falls back to EventSource.CurrentThreadActivityId,
                // so we need to take a snapshot to verify the ActivityId later on a different thread.
                // https://github.com/dotnet/runtime/blob/85162fbf9ccdeb4fa1df357f27308ae96579c066/src/libraries/System.Private.CoreLib/src/System/Diagnostics/Tracing/EventSource.cs#L4551
                _events.Enqueue(new EventSnapshot(eventData));
            }
        }

        public override void Dispose()
        {
            lock (_disposeLock)
            {
                _disposed = true;
            }

            base.Dispose();
        }
    }

    private class EventSnapshot
    {
        public EventSnapshot(EventWrittenEventArgs eventWrittenEventArgs)
        {
            EventName = eventWrittenEventArgs.EventName;
            EventId = eventWrittenEventArgs.EventId;
            EventSource = eventWrittenEventArgs.EventSource;
            ActivityId = eventWrittenEventArgs.ActivityId;
            RelatedActivityId = eventWrittenEventArgs.RelatedActivityId;
            Payload = new Dictionary<string, string>(eventWrittenEventArgs.PayloadNames.Count);

            for (int i = 0; i < eventWrittenEventArgs.PayloadNames.Count; i++)
            {
                Payload[eventWrittenEventArgs.PayloadNames[i]] = eventWrittenEventArgs.Payload[i] as string;
            }
        }

        public string EventName { get; }
        public int EventId { get; }
        public EventSource EventSource { get; }
        public Guid ActivityId { get; }
        public Guid RelatedActivityId { get; }
        public Dictionary<string, string> Payload { get; }

        public IEnumerable<string> PayloadNames => Payload.Keys;
    }

    public override void Dispose()
    {
        _listener.Dispose();
        base.Dispose();
    }
}
