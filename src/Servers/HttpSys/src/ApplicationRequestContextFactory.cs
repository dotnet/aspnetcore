// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal sealed class ApplicationRequestContextFactory<TContext> : IRequestContextFactory where TContext : notnull
{
    private readonly IHttpApplication<TContext> _application;
    private readonly MessagePump _messagePump;

    public ApplicationRequestContextFactory(IHttpApplication<TContext> application, MessagePump messagePump)
    {
        _application = application;
        _messagePump = messagePump;
    }

    public RequestContext CreateRequestContext(uint? bufferSize, ulong requestId)
    {
        return new RequestContext<TContext>(_application, _messagePump, _messagePump.Listener, bufferSize, requestId);
    }
}
