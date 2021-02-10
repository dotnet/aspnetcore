// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal class ApplicationRequestContextFactory<TContext> : IRequestContextFactory where TContext : notnull
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
}
