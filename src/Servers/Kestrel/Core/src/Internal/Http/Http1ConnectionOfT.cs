// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal sealed class Http1Connection<TContext> : Http1Connection, IContextContainer<TContext>
    {
        private TContext _context;

        public Http1Connection(HttpConnectionContext context) : base(context) { }

        bool IContextContainer<TContext>.TryGetContext(out TContext context)
        {
            context = _context;
            if (context is object)
            {
                _context = default;
                return true;
            }

            return false;
        }

        void IContextContainer<TContext>.ReleaseContext(TContext context) => _context = context;
    }
}
