// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Abstractions;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal sealed class Http2Stream<TContext> : Http2Stream, IHostContextContainer<TContext> where TContext : notnull
    {
        private readonly IHttpApplication<TContext> _application;

        public Http2Stream(IHttpApplication<TContext> application, Http2StreamContext context) 
        {
            Initialize(context);
            _application = application;
        }

        public override void Execute()
        {
            KestrelEventSource.Log.RequestQueuedStop(this, AspNetCore.Http.HttpProtocol.Http2);
            // REVIEW: Should we store this in a field for easy debugging?
            _ = ProcessRequestsAsync(_application);
        }

        // Pooled Host context
        TContext? IHostContextContainer<TContext>.HostContext { get; set; }
    }
}
