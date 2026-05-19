// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

internal readonly struct Http1ParsingHandler : IHttpRequestLineHandler, IHttpHeadersHandler
{
    public readonly Http1Connection Connection;
    public readonly bool Trailers;

    public Http1ParsingHandler(Http1Connection connection)
    {
        Connection = connection;
        Trailers = false;
    }

    public Http1ParsingHandler(Http1Connection connection, bool trailers)
    {
        Connection = connection;
        Trailers = trailers;
    }

    public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        if (Trailers)
        {
            Connection.OnTrailer(name, value);
        }
        else
        {
            Connection.OnHeader(name, value, checkForNewlineChars: false);
        }
    }

    public void OnHeadersComplete(bool endStream)
    {
        if (Trailers)
        {
            Connection.OnTrailersComplete();
        }
        else
        {
            Connection.OnHeadersComplete();
        }
    }

    public void OnStartLine(HttpVersionAndMethod versionAndMethod, TargetOffsetPathLength targetPath, Span<byte> startLine)
        => Connection.OnStartLine(versionAndMethod, targetPath, startLine);

    public void OnStaticIndexedHeader(int index)
    {
        throw new NotImplementedException();
    }

    public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
    {
        throw new NotImplementedException();
    }
}
