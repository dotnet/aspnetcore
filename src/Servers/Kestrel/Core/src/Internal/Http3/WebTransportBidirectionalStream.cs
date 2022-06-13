// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
internal class WebTransportBidirectionalStream<TContext> : Http3BidirectionalStream<TContext> where TContext : notnull
{
    public WebTransportBidirectionalStream(IHttpApplication<TContext> application, Http3StreamContext context)
        : base(application, context) { }
}
