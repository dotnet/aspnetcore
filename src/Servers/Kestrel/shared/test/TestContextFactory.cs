// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Testing;

internal static class TestContextFactory
{
    public static ServiceContext CreateServiceContext(
        KestrelServerOptions serverOptions,
        IHttpParser<Http1ParsingHandler> httpParser = null,
        PipeScheduler scheduler = null,
        ISystemClock systemClock = null,
        DateHeaderValueManager dateHeaderValueManager = null,
        ConnectionManager connectionManager = null,
        Heartbeat heartbeat = null)
    {
        var context = new ServiceContext
        {
            Log = new KestrelTrace(NullLoggerFactory.Instance),
            Scheduler = scheduler,
            HttpParser = httpParser,
            SystemClock = systemClock,
            DateHeaderValueManager = dateHeaderValueManager,
            ConnectionManager = connectionManager,
            Heartbeat = heartbeat,
            ServerOptions = serverOptions
        };

        return context;
    }

    public static HttpConnectionContext CreateHttpConnectionContext(
        ConnectionContext connectionContext,
        ServiceContext serviceContext,
        IDuplexPipe transport,
        IFeatureCollection connectionFeatures,
        MemoryPool<byte> memoryPool = null,
        IPEndPoint localEndPoint = null,
        IPEndPoint remoteEndPoint = null,
        ITimeoutControl timeoutControl = null)
    {
        var context = new HttpConnectionContext(
            "TestConnectionId",
            HttpProtocols.Http1,
            altSvcHeader: null,
            connectionContext,
            serviceContext,
            connectionFeatures,
            memoryPool ?? MemoryPool<byte>.Shared,
            localEndPoint,
            remoteEndPoint);
        context.TimeoutControl = timeoutControl;
        context.Transport = transport;

        return context;
    }

    public static HttpMultiplexedConnectionContext CreateHttp3ConnectionContext(
        MultiplexedConnectionContext connectionContext = null,
        ServiceContext serviceContext = null,
        IFeatureCollection connectionFeatures = null,
        MemoryPool<byte> memoryPool = null,
        IPEndPoint localEndPoint = null,
        IPEndPoint remoteEndPoint = null,
        ITimeoutControl timeoutControl = null)
    {
        var http3ConnectionContext = new HttpMultiplexedConnectionContext(
            "TEST",
            HttpProtocols.Http3,
            altSvcHeader: null,
            connectionContext ?? new TestMultiplexedConnectionContext { ConnectionId = "TEST" },
            serviceContext ?? CreateServiceContext(new KestrelServerOptions()),
            connectionFeatures ?? new FeatureCollection(),
            memoryPool ?? PinnedBlockMemoryPoolFactory.Create(),
            localEndPoint,
            remoteEndPoint)
        {
            TimeoutControl = timeoutControl
        };

        return http3ConnectionContext;
    }

    public static AddressBindContext CreateAddressBindContext(
        ServerAddressesFeature serverAddressesFeature,
        KestrelServerOptions serverOptions,
        ILogger logger,
        Func<ListenOptions, Task> createBinding)
    {
        var context = new AddressBindContext(
            serverAddressesFeature,
            serverOptions,
            logger,
            (listenOptions, cancellationToken) => createBinding(listenOptions));

        return context;
    }

    public static AddressBindContext CreateAddressBindContext(
        ServerAddressesFeature serverAddressesFeature,
        KestrelServerOptions serverOptions,
        ILogger logger,
        Func<ListenOptions, CancellationToken, Task> createBinding)
    {
        var context = new AddressBindContext(
            serverAddressesFeature,
            serverOptions,
            logger,
            createBinding);

        return context;
    }

    public static Http2StreamContext CreateHttp2StreamContext(
        string connectionId = null,
        ServiceContext serviceContext = null,
        IFeatureCollection connectionFeatures = null,
        MemoryPool<byte> memoryPool = null,
        IPEndPoint localEndPoint = null,
        IPEndPoint remoteEndPoint = null,
        int? streamId = null,
        IHttp2StreamLifetimeHandler streamLifetimeHandler = null,
        Http2PeerSettings clientPeerSettings = null,
        Http2PeerSettings serverPeerSettings = null,
        Http2FrameWriter frameWriter = null,
        InputFlowControl connectionInputFlowControl = null,
        ITimeoutControl timeoutControl = null)
    {
        var context = new Http2StreamContext
        (
            connectionId: connectionId ?? "TestConnectionId",
            protocols: HttpProtocols.Http2,
            altSvcHeader: null,
            serviceContext: serviceContext ?? CreateServiceContext(new KestrelServerOptions()),
            connectionFeatures: connectionFeatures ?? new FeatureCollection(),
            memoryPool: memoryPool ?? MemoryPool<byte>.Shared,
            localEndPoint: localEndPoint,
            remoteEndPoint: remoteEndPoint,
            streamId: streamId ?? 0,
            streamLifetimeHandler: streamLifetimeHandler ?? new TestHttp2StreamLifetimeHandler(),
            clientPeerSettings: clientPeerSettings ?? new Http2PeerSettings(),
            serverPeerSettings: serverPeerSettings ?? new Http2PeerSettings(),
            frameWriter: frameWriter,
            connectionInputFlowControl: connectionInputFlowControl
        );
        context.TimeoutControl = timeoutControl;

        return context;
    }

    public static Http3StreamContext CreateHttp3StreamContext(
        string connectionId = null,
        BaseConnectionContext connectionContext = null,
        ServiceContext serviceContext = null,
        IFeatureCollection connectionFeatures = null,
        MemoryPool<byte> memoryPool = null,
        IPEndPoint localEndPoint = null,
        IPEndPoint remoteEndPoint = null,
        IDuplexPipe transport = null,
        ITimeoutControl timeoutControl = null,
        IHttp3StreamLifetimeHandler streamLifetimeHandler = null)
    {
        var http3ConnectionContext = CreateHttp3ConnectionContext(
            null,
            serviceContext,
            connectionFeatures,
            memoryPool,
            localEndPoint,
            remoteEndPoint,
            timeoutControl);

        var http3Conection = new Http3Connection(http3ConnectionContext)
        {
            _streamLifetimeHandler = streamLifetimeHandler
        };

        return new Http3StreamContext
        (
            connectionId: connectionId ?? "TestConnectionId",
            protocols: HttpProtocols.Http3,
            altSvcHeader: null,
            connectionContext: connectionContext,
            serviceContext: serviceContext ?? CreateServiceContext(new KestrelServerOptions()),
            connectionFeatures: connectionFeatures ?? new FeatureCollection(),
            memoryPool: memoryPool ?? MemoryPool<byte>.Shared,
            localEndPoint: localEndPoint,
            remoteEndPoint: remoteEndPoint,
            streamContext: new DefaultConnectionContext(),
            connection: http3Conection
        )
        {
            TimeoutControl = timeoutControl,
            Transport = transport,
        };
    }

    private class TestHttp2StreamLifetimeHandler : IHttp2StreamLifetimeHandler
    {
        public void DecrementActiveClientStreamCount()
        {
        }

        public void OnStreamCompleted(Http2Stream stream)
        {
        }
    }

    private class TestMultiplexedConnectionContext : MultiplexedConnectionContext
    {
        public override string ConnectionId { get; set; }
        public override IFeatureCollection Features { get; }
        public override IDictionary<object, object> Items { get; set; }

        public override void Abort()
        {
        }

        public override void Abort(ConnectionAbortedException abortReason)
        {
        }

        public override ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
        {
            return default;
        }

        public override ValueTask<ConnectionContext> ConnectAsync(IFeatureCollection features = null, CancellationToken cancellationToken = default)
        {
            return default;
        }
    }
}
