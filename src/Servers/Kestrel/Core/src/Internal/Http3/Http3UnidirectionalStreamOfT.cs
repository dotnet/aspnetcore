// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
internal class Http3UnidirectionalStream<TContext> : Http3UnidirectionalStream, IHostContextContainer<TContext> where TContext : notnull
{
    private readonly IHttpApplication<TContext> _application;

    public Http3UnidirectionalStream(IHttpApplication<TContext> application, Http3StreamContext context) : base(context)
    {
        _application = application;
    }

    public override void Execute()
    {
        _ = ProcessRequestAsync(_application);
    }

    // Pooled Host context
    TContext? IHostContextContainer<TContext>.HostContext { get; set; }
}
