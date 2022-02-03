// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

internal interface IHttpParser<TRequestHandler> where TRequestHandler : IHttpHeadersHandler, IHttpRequestLineHandler
{
    bool ParseRequestLine(TRequestHandler handler, ref SequenceReader<byte> reader);

    bool ParseHeaders(TRequestHandler handler, ref SequenceReader<byte> reader);
}
