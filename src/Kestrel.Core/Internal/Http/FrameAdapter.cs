// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public struct FrameAdapter : IHttpRequestLineHandler, IHttpHeadersHandler
    {
        public Frame Frame;

        public FrameAdapter(Frame frame)
        {
            Frame = frame;
        }

        public void OnHeader(Span<byte> name, Span<byte> value)
            => Frame.OnHeader(name, value);

        public void OnStartLine(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded)
            => Frame.OnStartLine(method, version, target, path, query, customMethod, pathEncoded);
    }
}
