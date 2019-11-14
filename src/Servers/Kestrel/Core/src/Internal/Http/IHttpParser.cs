// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Net.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal interface IHttpParser<TRequestHandler> where TRequestHandler : IHttpHeadersHandler, IHttpRequestLineHandler
    {
        bool ParseRequestLine(TRequestHandler handler, in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined);

        bool ParseHeaders(TRequestHandler handler, ref SequenceReader<byte> reader);
    }
}
