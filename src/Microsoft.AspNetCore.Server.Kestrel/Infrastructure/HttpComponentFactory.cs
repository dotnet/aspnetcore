// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    class HttpComponentFactory : IHttpComponentFactory
    {
        
        private ConcurrentQueue<Streams> _streamPool = new ConcurrentQueue<Streams>();
        private ConcurrentQueue<Headers> _headerPool = new ConcurrentQueue<Headers>();

        public KestrelServerOptions ServerOptions { get; set; }

        public HttpComponentFactory(KestrelServerOptions serverOptions)
        {
            ServerOptions = serverOptions;
        }

        public Streams CreateStreams(IFrameControl frameControl)
        {
            Streams streams;

            if (!_streamPool.TryDequeue(out streams))
            {
                streams = new Streams();
            }

            streams.Initialize(frameControl);

            return streams;
        }

        public void DisposeStreams(Streams streams)
        {
            if (_streamPool.Count < ServerOptions.MaxPooledStreams)
            {
                streams.Uninitialize();

                _streamPool.Enqueue(streams);
            }
        }

        public Headers CreateHeaders(DateHeaderValueManager dateValueManager)
        {
            Headers headers;

            if (!_headerPool.TryDequeue(out headers))
            {
                headers = new Headers(ServerOptions);
            }

            headers.Initialize(dateValueManager);

            return headers;
        }

        public void DisposeHeaders(Headers headers)
        {
            if (_headerPool.Count < ServerOptions.MaxPooledHeaders)
            {
                headers.Uninitialize();

                _headerPool.Enqueue(headers);
            }
        }
    }

    internal class Headers
    {
        public static readonly byte[] BytesServer = Encoding.ASCII.GetBytes("\r\nServer: Kestrel");

        public readonly FrameRequestHeaders RequestHeaders = new FrameRequestHeaders();
        public readonly FrameResponseHeaders ResponseHeaders = new FrameResponseHeaders();

        private readonly KestrelServerOptions _options;

        public Headers(KestrelServerOptions options)
        {
            _options = options;
        }

        public void Initialize(DateHeaderValueManager dateValueManager)
        {
            var dateHeaderValues = dateValueManager.GetDateHeaderValues();

            ResponseHeaders.SetRawDate(dateHeaderValues.String, dateHeaderValues.Bytes);

            if (_options.AddServerHeader)
            {
                ResponseHeaders.SetRawServer(Constants.ServerName, BytesServer);
            }
        }

        public void Uninitialize()
        {
            RequestHeaders.Reset();
            ResponseHeaders.Reset();
        }
    }

    internal class Streams
    {
        public readonly FrameRequestStream RequestBody;
        public readonly FrameResponseStream ResponseBody;
        public readonly FrameDuplexStream DuplexStream;

        public Streams()
        {
            RequestBody = new FrameRequestStream();
            ResponseBody = new FrameResponseStream();
            DuplexStream = new FrameDuplexStream(RequestBody, ResponseBody);
        }

        public void Initialize(IFrameControl frameControl)
        {
            ResponseBody.Initialize(frameControl);
        }

        public void Uninitialize()
        {
            ResponseBody.Uninitialize();
        }
    }
}
