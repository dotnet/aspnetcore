// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
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
        private long _highestOpenedStreamId;

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

            try
            {
                while (true)
                {
                    var connectionContext = await streamListenerFeature.AcceptAsync();
                    if (connectionContext == null)
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

                    IHttp3Stream stream;
                    var streamFeature = httpConnectionContext.ConnectionFeatures.Get<IQuicStreamFeature>();
                    var streamId = streamFeature.StreamId;
                    HighestStreamId = streamId;

                    if (streamFeature.IsUnidirectional)
                    {
                        stream = new Http3ControlStream<TContext>(application, this, httpConnectionContext);
                    }
                    else
                    {
                        var http3Stream = new Http3Stream<TContext>(application, this, httpConnectionContext);
                        stream = http3Stream;
                        _streams[streamId] = http3Stream;
                    }

                    ThreadPool.UnsafeQueueUserWorkItem(stream, preferLocal: false);
                }
            }
            finally
            {
                foreach (var stream in _streams.Values)
                {
                    stream.Abort(new ConnectionAbortedException(""));
                }
            }
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
