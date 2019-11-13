// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions.Features;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal class Http3Connection : IRequestProcessor
    {
        public HttpConnectionContext Context { get; private set; }

        public DynamicTable DynamicTable { get; set; }

        public Http3ControlStream SettingsStream { get; set; }
        public Http3ControlStream EncoderStream { get; set; }
        public Http3ControlStream DecoderStream { get; set; }

        private readonly ConcurrentDictionary<long, Http3Stream> _streams = new ConcurrentDictionary<long, Http3Stream>();

        // To be used by GO_AWAY
        private long _highestOpenedStreamId; // TODO lock to access
        //private volatile bool _haveSentGoAway;

        public Http3Connection(HttpConnectionContext context)
        {
            Context = context;
            DynamicTable = new DynamicTable(0);
        }

        internal long HighestStreamId
        {
            get
            {
                return _highestOpenedStreamId;
            }
            set
            {
                if (_highestOpenedStreamId < value)
                {
                    _highestOpenedStreamId = value;
                }
            }
        }

        public async Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> application)
        {
            var streamListenerFeature = Context.ConnectionFeatures.Get<IQuicStreamListenerFeature>();

            // Start other three unidirectional streams here.
            var settingsStream = CreateSettingsStream(application);
            var encoderStream = CreateEncoderStream(application);
            var decoderStream = CreateDecoderStream(application);

            try
            {
                while (true)
                {
                    var connectionContext = await streamListenerFeature.AcceptAsync();
                    if (connectionContext == null)
                    {
                        break;
                    }

                    //if (_haveSentGoAway)
                    //{
                    //    // error here.
                    //}

                    var httpConnectionContext = new HttpConnectionContext
                    {
                        ConnectionId = connectionContext.ConnectionId,
                        ConnectionContext = connectionContext,
                        Protocols = Context.Protocols,
                        ServiceContext = Context.ServiceContext,
                        ConnectionFeatures = connectionContext.Features,
                        MemoryPool = Context.MemoryPool,
                        Transport = connectionContext.Transport,
                        TimeoutControl = Context.TimeoutControl,
                        LocalEndPoint = connectionContext.LocalEndPoint as IPEndPoint,
                        RemoteEndPoint = connectionContext.RemoteEndPoint as IPEndPoint
                    };

                    var streamFeature = httpConnectionContext.ConnectionFeatures.Get<IQuicStreamFeature>();
                    var streamId = streamFeature.StreamId;
                    HighestStreamId = streamId;

                    if (streamFeature.IsUnidirectional)
                    {
                        var stream = new Http3ControlStream<TContext>(application, this, httpConnectionContext);
                        ThreadPool.UnsafeQueueUserWorkItem(stream, preferLocal: false);
                    }
                    else
                    {
                        var http3Stream = new Http3Stream<TContext>(application, this, httpConnectionContext);
                        var stream = http3Stream;
                        _streams[streamId] = http3Stream;
                        ThreadPool.UnsafeQueueUserWorkItem(stream, preferLocal: false);
                    }
                }
            }
            finally
            {
                await settingsStream;
                await encoderStream;
                await decoderStream;
                foreach (var stream in _streams.Values)
                {
                    stream.Abort(new ConnectionAbortedException(""));
                }
            }
        }

        private async ValueTask CreateSettingsStream<TContext>(IHttpApplication<TContext> application)
        {
            var stream = await CreateNewUnidirectionalStreamAsync(application);
            SettingsStream = stream;
            await stream.SendStreamIdAsync(id: 0);
            await stream.SendSettingsFrameAsync();
        }

        private async ValueTask CreateEncoderStream<TContext>(IHttpApplication<TContext> application)
        {
            var stream = await CreateNewUnidirectionalStreamAsync(application);
            EncoderStream = stream;
            await stream.SendStreamIdAsync(id: 2);
        }

        private async ValueTask CreateDecoderStream<TContext>(IHttpApplication<TContext> application)
        {
            var stream = await CreateNewUnidirectionalStreamAsync(application);
            DecoderStream = stream;
            await stream.SendStreamIdAsync(id: 3);
        }

        private async ValueTask<Http3ControlStream> CreateNewUnidirectionalStreamAsync<TContext>(IHttpApplication<TContext> application)
        {
            var connectionContext = await Context.ConnectionFeatures.Get<IQuicCreateStreamFeature>().StartUnidirectionalStreamAsync();
            var httpConnectionContext = new HttpConnectionContext
            {
                ConnectionId = connectionContext.ConnectionId,
                ConnectionContext = connectionContext,
                Protocols = Context.Protocols,
                ServiceContext = Context.ServiceContext,
                ConnectionFeatures = connectionContext.Features,
                MemoryPool = Context.MemoryPool,
                Transport = connectionContext.Transport,
                TimeoutControl = Context.TimeoutControl,
                LocalEndPoint = connectionContext.LocalEndPoint as IPEndPoint,
                RemoteEndPoint = connectionContext.RemoteEndPoint as IPEndPoint
            };

            return new Http3ControlStream<TContext>(application, this, httpConnectionContext);
        }

        public void StopProcessingNextRequest()
        {
        }

        public void HandleRequestHeadersTimeout()
        {
        }

        public void HandleReadDataRateTimeout()
        {
        }

        public void OnInputOrOutputCompleted()
        {
        }

        public void Tick(DateTimeOffset now)
        {
        }

        public void Abort(ConnectionAbortedException ex)
        {
            // Send goaway
            // Abort currently active streams
            // TODO need to figure out if there is server initiated connection close rather than stream close?
        }

        public void ApplyMaxHeaderListSize(long value)
        {
            // TODO something here to call OnHeader?
        }

        internal void ApplyBlockedStream(long value)
        {
        }

        internal void ApplyMaxTableCapacity(long value)
        {
            // TODO make sure this works
            //_maxDynamicTableSize = value;
        }
    }
}
