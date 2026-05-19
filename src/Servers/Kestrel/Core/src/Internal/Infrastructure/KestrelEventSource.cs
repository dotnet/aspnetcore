// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

[EventSource(Name = "Microsoft-AspNetCore-Server-Kestrel")]
internal sealed class KestrelEventSource : EventSource
{
    public static readonly KestrelEventSource Log = new KestrelEventSource();

    private IncrementingPollingCounter? _connectionsPerSecondCounter;
    private IncrementingPollingCounter? _tlsHandshakesPerSecondCounter;
    private PollingCounter? _totalConnectionsCounter;
    private PollingCounter? _currentConnectionsCounter;
    private PollingCounter? _totalTlsHandshakesCounter;
    private PollingCounter? _currentTlsHandshakesCounter;
    private PollingCounter? _failedTlsHandshakesCounter;
    private PollingCounter? _connectionQueueLengthCounter;
    private PollingCounter? _httpRequestQueueLengthCounter;
    private PollingCounter? _currrentUpgradedHttpRequestsCounter;

    private long _totalConnections;
    private long _currentConnections;
    private long _connectionQueueLength;
    private long _totalTlsHandshakes;
    private long _currentTlsHandshakes;
    private long _failedTlsHandshakes;
    private long _httpRequestQueueLength;
    private long _currentUpgradedHttpRequests;

    private readonly List<WeakReference<KestrelServerOptions>> _options = new();

    private KestrelEventSource()
    {
    }

    // NOTE
    // - The 'Start' and 'Stop' suffixes on the following event names have special meaning in EventSource. They
    //   enable creating 'activities'.
    //   For more information, take a look at the following blog post:
    //   https://blogs.msdn.microsoft.com/vancem/2015/09/14/exploring-eventsource-activity-correlation-and-causation-features/
    // - A stop event's event id must be next one after its start event.
    // - Avoid renaming methods or parameters marked with EventAttribute. EventSource uses these to form the event object.

    [NonEvent]
    public void ConnectionStart(BaseConnectionContext connection)
    {
        Interlocked.Increment(ref _totalConnections);
        Interlocked.Increment(ref _currentConnections);

        // avoid allocating strings unless this event source is enabled
        if (IsEnabled(EventLevel.Informational, EventKeywords.None))
        {
            ConnectionStart(
                connection.ConnectionId,
                connection.LocalEndPoint?.ToString(),
                connection.RemoteEndPoint?.ToString());
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Event(1, Level = EventLevel.Informational)]
    private void ConnectionStart(string connectionId, string? localEndPoint, string? remoteEndPoint)
    {
        WriteEvent(1, connectionId, localEndPoint, remoteEndPoint);
    }

    [NonEvent]
    public void ConnectionStop(BaseConnectionContext connection)
    {
        Interlocked.Decrement(ref _currentConnections);

        if (IsEnabled(EventLevel.Informational, EventKeywords.None))
        {
            ConnectionStop(connection.ConnectionId);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Event(2, Level = EventLevel.Informational)]
    private void ConnectionStop(string connectionId)
    {
        WriteEvent(2, connectionId);
    }

    [NonEvent]
    public void RequestStart(HttpProtocol httpProtocol)
    {
        if (IsEnabled())
        {
            Core(httpProtocol);
        }

        [NonEvent]
        void Core(HttpProtocol httpProtocol)
        {
            // avoid allocating the trace identifier unless logging is enabled
            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
            {
                RequestStart(httpProtocol.ConnectionIdFeature, httpProtocol.TraceIdentifier, httpProtocol.HttpVersion, httpProtocol.Path!, httpProtocol.MethodText);
            }
        }
    }

    [Event(3, Level = EventLevel.Informational)]
    private void RequestStart(string connectionId, string requestId, string httpVersion, string path, string method)
    {
        WriteEvent(3, connectionId, requestId, httpVersion, path, method);
    }

    [NonEvent]
    public void RequestStop(HttpProtocol httpProtocol)
    {
        if (IsEnabled())
        {
            Core(httpProtocol);
        }

        [NonEvent]
        void Core(HttpProtocol httpProtocol)
        {
            // avoid allocating the trace identifier unless logging is enabled
            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
            {
                RequestStop(httpProtocol.ConnectionIdFeature, httpProtocol.TraceIdentifier, httpProtocol.HttpVersion, httpProtocol.Path!, httpProtocol.MethodText);
            }
        }
    }

    [Event(4, Level = EventLevel.Informational)]
    private void RequestStop(string connectionId, string requestId, string httpVersion, string path, string method)
    {
        WriteEvent(4, connectionId, requestId, httpVersion, path, method);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Event(5, Level = EventLevel.Informational)]
    public void ConnectionRejected(string connectionId)
    {
        if (IsEnabled(EventLevel.Informational, EventKeywords.None))
        {
            WriteEvent(5, connectionId);
        }
    }

    [NonEvent]
    public void ConnectionQueuedStart(BaseConnectionContext connection)
    {
        Interlocked.Increment(ref _connectionQueueLength);
    }

    [NonEvent]
    public void ConnectionQueuedStop(BaseConnectionContext connection)
    {
        Interlocked.Decrement(ref _connectionQueueLength);
    }

    [NonEvent]
    public void TlsHandshakeStart(BaseConnectionContext connectionContext, SslServerAuthenticationOptions sslOptions)
    {
        Interlocked.Increment(ref _currentTlsHandshakes);
        Interlocked.Increment(ref _totalTlsHandshakes);

        if (IsEnabled(EventLevel.Informational, EventKeywords.None))
        {
            TlsHandshakeStart(connectionContext.ConnectionId, sslOptions.EnabledSslProtocols.ToString());
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Event(8, Level = EventLevel.Informational)]
    private void TlsHandshakeStart(string connectionId, string sslProtocols)
    {
        WriteEvent(8, connectionId, sslProtocols);
    }

    [NonEvent]
    public void TlsHandshakeStop(BaseConnectionContext connectionContext, TlsConnectionFeature? feature)
    {
        Interlocked.Decrement(ref _currentTlsHandshakes);

        if (IsEnabled(EventLevel.Informational, EventKeywords.None))
        {
            // TODO: Write this without a string allocation using WriteEventData
            var applicationProtocol = feature == null ? string.Empty : Encoding.UTF8.GetString(feature.ApplicationProtocol.Span);
            var sslProtocols = feature?.Protocol.ToString() ?? string.Empty;
            var hostName = feature?.HostName ?? string.Empty;
            TlsHandshakeStop(connectionContext.ConnectionId, sslProtocols, applicationProtocol, hostName);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Event(9, Level = EventLevel.Informational)]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Parameters passed to WriteEvent are all primative values.")]
    private void TlsHandshakeStop(string connectionId, string sslProtocols, string applicationProtocol, string hostName)
    {
        WriteEvent(9, connectionId, sslProtocols, applicationProtocol, hostName);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Event(10, Level = EventLevel.Error)]
    public void TlsHandshakeFailed(string connectionId)
    {
        Interlocked.Increment(ref _failedTlsHandshakes);

        if (IsEnabled(EventLevel.Error, EventKeywords.None))
        {
            WriteEvent(10, connectionId);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Event(11, Level = EventLevel.LogAlways)]
    public void Configuration(int instanceId, string configuration)
    {
        // If the event source is already enabled, dump configuration
        WriteEvent(11, instanceId, configuration);
    }

    [NonEvent]
    public void Configuration(KestrelServerOptions options)
    {
        // If the event source is already enabled, dump configuration
        if (IsEnabled())
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            var writer = new Utf8JsonWriter(bufferWriter);

            writer.WriteStartObject();
            options.Serialize(writer);
            writer.WriteEndObject();
            writer.Flush();

            var serializedConfig = Encoding.UTF8.GetString(bufferWriter.WrittenSpan);

            Configuration(options.GetHashCode(), serializedConfig);
        }
    }

    [NonEvent]
    public void AddServerOptions(KestrelServerOptions options)
    {
        lock (_options)
        {
            _options.Add(new(options));
        }

        Configuration(options);
    }

    [NonEvent]
    public void RemoveServerOptions(KestrelServerOptions options)
    {
        lock (_options)
        {
            for (var i = _options.Count - 1; i >= 0; i--)
            {
                var weakReference = _options[i];
                if (!weakReference.TryGetTarget(out var target) || ReferenceEquals(target, options))
                {
                    _options.RemoveAt(i);
                }
            }
        }
    }

    [NonEvent]
    public void RequestQueuedStart(HttpProtocol httpProtocol, string httpVersion)
    {
        Interlocked.Increment(ref _httpRequestQueueLength);
    }

    [NonEvent]
    public void RequestQueuedStop(HttpProtocol httpProtocol, string httpVersion)
    {
        Interlocked.Decrement(ref _httpRequestQueueLength);
    }

    [NonEvent]
    public void RequestUpgradedStart(HttpProtocol httpProtocol)
    {
        Interlocked.Increment(ref _currentUpgradedHttpRequests);
    }

    [NonEvent]
    public void RequestUpgradedStop(HttpProtocol httpProtocol)
    {
        Interlocked.Decrement(ref _currentUpgradedHttpRequests);
    }

    protected override void OnEventCommand(EventCommandEventArgs command)
    {
        if (command.Command == EventCommand.Enable)
        {
            // This is the convention for initializing counters in the RuntimeEventSource (lazily on the first enable command).
            // They aren't disabled afterwards...

            _connectionsPerSecondCounter ??= new IncrementingPollingCounter("connections-per-second", this, () => Volatile.Read(ref _totalConnections))
            {
                DisplayName = "Connection Rate",
                DisplayRateTimeScale = TimeSpan.FromSeconds(1)
            };

            _totalConnectionsCounter ??= new PollingCounter("total-connections", this, () => Volatile.Read(ref _totalConnections))
            {
                DisplayName = "Total Connections",
            };

            _tlsHandshakesPerSecondCounter ??= new IncrementingPollingCounter("tls-handshakes-per-second", this, () => Volatile.Read(ref _totalTlsHandshakes))
            {
                DisplayName = "TLS Handshake Rate",
                DisplayRateTimeScale = TimeSpan.FromSeconds(1)
            };

            _totalTlsHandshakesCounter ??= new PollingCounter("total-tls-handshakes", this, () => Volatile.Read(ref _totalTlsHandshakes))
            {
                DisplayName = "Total TLS Handshakes",
            };

            _currentTlsHandshakesCounter ??= new PollingCounter("current-tls-handshakes", this, () => Volatile.Read(ref _currentTlsHandshakes))
            {
                DisplayName = "Current TLS Handshakes"
            };

            _failedTlsHandshakesCounter ??= new PollingCounter("failed-tls-handshakes", this, () => Volatile.Read(ref _failedTlsHandshakes))
            {
                DisplayName = "Failed TLS Handshakes"
            };

            _currentConnectionsCounter ??= new PollingCounter("current-connections", this, () => Volatile.Read(ref _currentConnections))
            {
                DisplayName = "Current Connections"
            };

            _connectionQueueLengthCounter ??= new PollingCounter("connection-queue-length", this, () => Volatile.Read(ref _connectionQueueLength))
            {
                DisplayName = "Connection Queue Length"
            };

            _httpRequestQueueLengthCounter ??= new PollingCounter("request-queue-length", this, () => Volatile.Read(ref _httpRequestQueueLength))
            {
                DisplayName = "Request Queue Length"
            };

            _currrentUpgradedHttpRequestsCounter ??= new PollingCounter("current-upgraded-requests", this, () => Volatile.Read(ref _currentUpgradedHttpRequests))
            {
                DisplayName = "Current Upgraded Requests (WebSockets)"
            };

            // Log the options here
            lock (_options)
            {
                for (var i = _options.Count - 1; i >= 0; i--)
                {
                    var weakReference = _options[i];
                    if (!weakReference.TryGetTarget(out var target))
                    {
                        // Remove any options that have been collected
                        _options.RemoveAt(i);
                    }
                    else
                    {
                        Configuration(target);
                    }
                }
            }
        }
    }

    [NonEvent]
    [SkipLocalsInit]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Parameters passed to WriteEvent are all primative values.")]
    private unsafe void WriteEvent(int eventId, string? arg1, string? arg2, string? arg3, string? arg4, string? arg5)
    {
        const int EventDataCount = 5;

        arg1 ??= string.Empty;
        arg2 ??= string.Empty;
        arg3 ??= string.Empty;
        arg4 ??= string.Empty;
        arg5 ??= string.Empty;

        fixed (char* arg1Ptr = arg1)
        fixed (char* arg2Ptr = arg2)
        fixed (char* arg3Ptr = arg3)
        fixed (char* arg4Ptr = arg4)
        fixed (char* arg5Ptr = arg5)
        {
            EventData* data = stackalloc EventData[EventDataCount];

            data[0] = new EventData
            {
                DataPointer = (IntPtr)arg1Ptr,
                Size = (arg1.Length + 1) * sizeof(char)
            };
            data[1] = new EventData
            {
                DataPointer = (IntPtr)arg2Ptr,
                Size = (arg2.Length + 1) * sizeof(char)
            };
            data[2] = new EventData
            {
                DataPointer = (IntPtr)arg3Ptr,
                Size = (arg3.Length + 1) * sizeof(char)
            };
            data[3] = new EventData
            {
                DataPointer = (IntPtr)arg4Ptr,
                Size = (arg4.Length + 1) * sizeof(char)
            };
            data[4] = new EventData
            {
                DataPointer = (IntPtr)arg5Ptr,
                Size = (arg5.Length + 1) * sizeof(char)
            };

            WriteEventCore(eventId, EventDataCount, data);
        }
    }
}
