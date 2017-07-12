// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public interface IHttpParser<TRequestHandler> where TRequestHandler : IHttpHeadersHandler, IHttpRequestLineHandler
    {
        bool ParseRequestLine(TRequestHandler handler, ReadableBuffer buffer, out ReadCursor consumed, out ReadCursor examined);

        bool ParseHeaders(TRequestHandler handler, ReadableBuffer buffer, out ReadCursor consumed, out ReadCursor examined, out int consumedBytes);
    }
}