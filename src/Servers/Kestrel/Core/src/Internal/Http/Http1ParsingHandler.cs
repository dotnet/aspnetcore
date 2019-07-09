// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
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

        public void OnHeader(Span<byte> name, Span<byte> value)
        {
            if (Trailers)
            {
                Connection.OnTrailer(name, value);
            }
            else
            {
                Connection.OnHeader(name, value);
            }
        }

        public void OnHeadersComplete()
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

        public void OnStartLine(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded)
            => Connection.OnStartLine(method, version, target, path, query, customMethod, pathEncoded);
    }
}
