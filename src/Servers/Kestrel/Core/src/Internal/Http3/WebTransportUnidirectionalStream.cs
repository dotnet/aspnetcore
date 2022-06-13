// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
internal class WebTransportUnidirectionalStream<TContext> : Http3UnidirectionalStream<TContext> where TContext : notnull
{
    public WebTransportUnidirectionalStream(IHttpApplication<TContext> application, Http3StreamContext context)
        : base(application, context) { }
}
