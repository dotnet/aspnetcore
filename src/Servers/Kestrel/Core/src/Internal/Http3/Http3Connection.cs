// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Net;
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

        private readonly ConcurrentDictionary<int, Http3Stream> _streams = new ConcurrentDictionary<int, Http3Stream>();

        public Http3Connection(HttpConnectionContext context)
        {
            Context = context;
            DynamicTable = new DynamicTable(0);
            // TODO qpack decoders need to be one per stream?

        }

        public async Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> application)
        {
            var streamFeature = Context.ConnectionFeatures.Get<IQuicStreamListenerFeature>();
            if (streamFeature == null)
            {
                throw new Http3ConnectionException();
            }

            try
            {
                var i = 0;
                while (true)
                {
                    var connectionContext = await streamFeature.AcceptAsync();
                    if (connectionContext == null)
                    {
                        break;
                    }

                    // Need to keep calling AcceptAsync here.
                    var httpConnectionContext = new HttpConnectionContext
                    {
                        ConnectionId = connectionContext.ConnectionId + i, // TODO this is just adding an int for the stream id, which isn't great.
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
                    if (httpConnectionContext.ConnectionFeatures.Get<IUnidirectionalStreamFeature>() != null)
                    {
                        stream = new Http3ControlStream(this, httpConnectionContext);
                    }
                    else
                    {
                        var http3Stream = new Http3Stream(this, httpConnectionContext);
                        stream = http3Stream;
                        _streams[i] = http3Stream;
                        // TODO Figure out how to make ids more unique
                    }

                    i++;
                    _ = Task.Run(() => stream.ProcessRequestAsync(application));
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
