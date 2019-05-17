// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting.Server.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal sealed class Http1Connection<TContext> : Http1Connection, IHostContextContainer<TContext>
    {
        private TContext _context;

        public Http1Connection(HttpConnectionContext context) : base(context) { }

        ref TContext IHostContextContainer<TContext>.HostContext => ref _context;
    }
}
