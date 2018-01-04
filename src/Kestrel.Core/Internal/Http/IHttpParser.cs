// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Sequences;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public interface IHttpParser<TRequestHandler> where TRequestHandler : IHttpHeadersHandler, IHttpRequestLineHandler
    {
        bool ParseRequestLine(TRequestHandler handler, ReadOnlyBuffer buffer, out Position consumed, out Position examined);

        bool ParseHeaders(TRequestHandler handler, ReadOnlyBuffer buffer, out Position consumed, out Position examined, out int consumedBytes);
    }
}
