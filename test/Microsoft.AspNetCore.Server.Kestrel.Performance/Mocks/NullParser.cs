// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class NullParser<TRequestHandler> : IHttpParser<TRequestHandler> where TRequestHandler : struct, IHttpHeadersHandler, IHttpRequestLineHandler
    {
        private readonly byte[] _startLine = Encoding.ASCII.GetBytes("GET /plaintext HTTP/1.1\r\n");
        private readonly byte[] _target = Encoding.ASCII.GetBytes("/plaintext");
        private readonly byte[] _hostHeaderName = Encoding.ASCII.GetBytes("Host");
        private readonly byte[] _hostHeaderValue = Encoding.ASCII.GetBytes("www.example.com");
        private readonly byte[] _acceptHeaderName = Encoding.ASCII.GetBytes("Accept");
        private readonly byte[] _acceptHeaderValue = Encoding.ASCII.GetBytes("text/plain,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7\r\n\r\n");
        private readonly byte[] _connectionHeaderName = Encoding.ASCII.GetBytes("Connection");
        private readonly byte[] _connectionHeaderValue = Encoding.ASCII.GetBytes("keep-alive");

        public static readonly NullParser<FrameAdapter> Instance = new NullParser<FrameAdapter>();

        public bool ParseHeaders(TRequestHandler handler, ReadableBuffer buffer, out ReadCursor consumed, out ReadCursor examined, out int consumedBytes)
        {
            handler.OnHeader(new Span<byte>(_hostHeaderName), new Span<byte>(_hostHeaderValue));
            handler.OnHeader(new Span<byte>(_acceptHeaderName), new Span<byte>(_acceptHeaderValue));
            handler.OnHeader(new Span<byte>(_connectionHeaderName), new Span<byte>(_connectionHeaderValue));

            consumedBytes = 0;
            consumed = buffer.Start;
            examined = buffer.End;

            return true;
        }

        public bool ParseRequestLine(TRequestHandler handler, ReadableBuffer buffer, out ReadCursor consumed, out ReadCursor examined)
        {
            handler.OnStartLine(HttpMethod.Get,
                HttpVersion.Http11,
                new Span<byte>(_target),
                new Span<byte>(_target),
                Span<byte>.Empty,
                Span<byte>.Empty,
                false);

            consumed = buffer.Start;
            examined = buffer.End;

            return true;
        }

        public void Reset()
        {
        }
    }
}
