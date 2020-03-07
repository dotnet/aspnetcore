// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    internal class NullParser<TRequestHandler> : IHttpParser<TRequestHandler> where TRequestHandler : struct, IHttpHeadersHandler, IHttpRequestLineHandler
    {
        private readonly byte[] _startLine = Encoding.ASCII.GetBytes("GET /plaintext HTTP/1.1\r\n");
        private readonly byte[] _target = Encoding.ASCII.GetBytes("/plaintext");
        private readonly byte[] _hostHeaderName = Encoding.ASCII.GetBytes("Host");
        private readonly byte[] _hostHeaderValue = Encoding.ASCII.GetBytes("www.example.com");
        private readonly byte[] _acceptHeaderName = Encoding.ASCII.GetBytes("Accept");
        private readonly byte[] _acceptHeaderValue = Encoding.ASCII.GetBytes("text/plain,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7\r\n\r\n");
        private readonly byte[] _connectionHeaderName = Encoding.ASCII.GetBytes("Connection");
        private readonly byte[] _connectionHeaderValue = Encoding.ASCII.GetBytes("keep-alive");

        public static readonly NullParser<Http1ParsingHandler> Instance = new NullParser<Http1ParsingHandler>();

        public bool ParseHeaders(TRequestHandler handler, ref SequenceReader<byte> reader)
        {
            handler.OnHeader(new Span<byte>(_hostHeaderName), new Span<byte>(_hostHeaderValue));
            handler.OnHeader(new Span<byte>(_acceptHeaderName), new Span<byte>(_acceptHeaderValue));
            handler.OnHeader(new Span<byte>(_connectionHeaderName), new Span<byte>(_connectionHeaderValue));
            handler.OnHeadersComplete(endStream: false);

            return true;
        }

        public bool ParseRequestLine(TRequestHandler handler, in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
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
