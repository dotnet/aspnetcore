// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    class HttpComponentFactory : IHttpComponentFactory
    {
        private const int _maxPooledComponents = 128;
        private ConcurrentQueue<Streams> _streamPool = new ConcurrentQueue<Streams>();
        private ConcurrentQueue<Headers> _headerPool = new ConcurrentQueue<Headers>();

        public Streams CreateStreams(FrameContext owner)
        {
            Streams streams;

            if (!_streamPool.TryDequeue(out streams))
            {
                streams = new Streams();
            }

            streams.Initialize(owner);

            return streams;
        }

        public void DisposeStreams(Streams streams, bool poolingPermitted)
        {
            if (poolingPermitted && _streamPool.Count < _maxPooledComponents)
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
                headers = new Headers();
            }

            headers.Initialize(dateValueManager);

            return headers;
        }

        public void DisposeHeaders(Headers headers, bool poolingPermitted)
        {
            if (poolingPermitted && _headerPool.Count < _maxPooledComponents)
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

        public Headers()
        {
            RequestHeaders = new FrameRequestHeaders();
            ResponseHeaders = new FrameResponseHeaders();
        }

        public void Initialize(DateHeaderValueManager dateValueManager)
        {
            ResponseHeaders.SetRawDate(
                dateValueManager.GetDateHeaderValue(),
                dateValueManager.GetDateHeaderValueBytes());
            ResponseHeaders.SetRawServer("Kestrel", BytesServer);
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

        public void Initialize(FrameContext renter)
        {
            ResponseBody.Initialize(renter);
        }

        public void Uninitialize()
        {
            ResponseBody.Uninitialize();
        }
    }
}
