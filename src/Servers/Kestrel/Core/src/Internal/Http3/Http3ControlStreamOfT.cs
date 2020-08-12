// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal sealed class Http3ControlStream<TContext> : Http3ControlStream, IHostContextContainer<TContext>
    {
        private readonly IHttpApplication<TContext> _application;

        public Http3ControlStream(IHttpApplication<TContext> application, Http3Connection connection, HttpConnectionContext context) : base(connection, context)
        {
            _application = application;
        }

        public override void Execute()
        {
            _ = ProcessRequestAsync(_application);
        }

        // Pooled Host context
        TContext IHostContextContainer<TContext>.HostContext { get; set; }
    }
}
