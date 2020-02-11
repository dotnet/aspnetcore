// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
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

        public Http3ControlStream ControlStream { get; set; }
        public Http3ControlStream EncoderStream { get; set; }
        public Http3ControlStream DecoderStream { get; set; }

        private readonly ConcurrentDictionary<long, Http3Stream> _streams = new ConcurrentDictionary<long, Http3Stream>();

        private long _highestOpenedStreamId; // TODO lock to access
        private volatile bool _haveSentGoAway;
        private object _sync = new object();

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
            var controlTask = CreateControlStream(application);
            var encoderTask = CreateEncoderStream(application);
            var decoderTask = CreateDecoderStream(application);

            try
            {
                while (true)
                {
                    var connectionContext = await streamListenerFeature.AcceptAsync();
                    if (connectionContext == null || _haveSentGoAway)
                    {
                        break;
                    }

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

                    if (!streamFeature.CanWrite)
                    {
                        // Unidirectional stream
                        var stream = new Http3ControlStream<TContext>(application, this, httpConnectionContext);
                        ThreadPool.UnsafeQueueUserWorkItem(stream, preferLocal: false);
                    }
                    else
                    {
                        // Keep track of highest stream id seen for GOAWAY
                        var streamId = streamFeature.StreamId;

                        HighestStreamId = streamId;

                        var http3Stream = new Http3Stream<TContext>(application, this, httpConnectionContext);
                        var stream = http3Stream;
                        _streams[streamId] = http3Stream;
                        ThreadPool.UnsafeQueueUserWorkItem(stream, preferLocal: false);
                    }
                }
            }
            finally
            {
                // Abort all streams as connection has shutdown.
                foreach (var stream in _streams.Values)
                {
                    stream.Abort(new ConnectionAbortedException("Connection is shutting down."));
                }

                ControlStream.Abort(new ConnectionAbortedException("Connection is shutting down."));
                EncoderStream.Abort(new ConnectionAbortedException("Connection is shutting down."));
                DecoderStream.Abort(new ConnectionAbortedException("Connection is shutting down."));

                await controlTask;
                await encoderTask;
                await decoderTask;
            }
        }

        private async ValueTask CreateControlStream<TContext>(IHttpApplication<TContext> application)
        {
            var stream = await CreateNewUnidirectionalStreamAsync(application);
            ControlStream = stream;
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
                //ConnectionId = "", TODO getting stream ID from stream that isn't started throws an exception.
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
            lock (_sync)
            {
                if (ControlStream != null)
                {
                    // TODO need to await this somewhere or allow this to be called elsewhere?
                    ControlStream.SendGoAway(_highestOpenedStreamId).GetAwaiter().GetResult();
                }
            }

            _haveSentGoAway = true;

            // Abort currently active streams
            foreach (var stream in _streams.Values)
            {
                stream.Abort(new ConnectionAbortedException("The Http3Connection has been aborted"), Http3ErrorCode.UnexpectedFrame);
            }
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
