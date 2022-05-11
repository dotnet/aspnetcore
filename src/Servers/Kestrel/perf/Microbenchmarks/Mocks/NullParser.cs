// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

internal sealed class NullParser<TRequestHandler> : IHttpParser<TRequestHandler> where TRequestHandler : struct, IHttpHeadersHandler, IHttpRequestLineHandler
{
    private readonly byte[] _startLine = Encoding.ASCII.GetBytes("GET /plaintext HTTP/1.1\r\n");
    private readonly byte[] _hostHeaderName = Encoding.ASCII.GetBytes("Host");
    private readonly byte[] _hostHeaderValue = Encoding.ASCII.GetBytes("www.example.com");
    private readonly byte[] _acceptHeaderName = Encoding.ASCII.GetBytes("Accept");
    private readonly byte[] _acceptHeaderValue = Encoding.ASCII.GetBytes("text/plain,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7");
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

    public bool ParseRequestLine(TRequestHandler handler, ref SequenceReader<byte> reader)
    {
        Span<byte> startLine = _startLine;

        handler.OnStartLine(
            new HttpVersionAndMethod(HttpMethod.Get, 3) { Version = HttpVersion.Http11 },
            new TargetOffsetPathLength(4, startLine.Length - 4, false),
            startLine);

        return true;
    }

    public void Reset()
    {
    }
}
