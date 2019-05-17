// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal sealed class Http2Stream<TContext> : Http2Stream, IHostContextContainer<TContext>
    {
        private readonly IHttpApplication<TContext> _application;
        private TContext _context;

        public Http2Stream(IHttpApplication<TContext> application, Http2StreamContext context) : base(context)
        {
            _application = application;
        }

        public override void Execute()
        {
            // REVIEW: Should we store this in a field for easy debugging?
            _ = ProcessRequestsAsync(_application);
        }

        ref TContext IHostContextContainer<TContext>.HostContext => ref _context;
    }
}
