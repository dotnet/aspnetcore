// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal interface ICachedHttp3StreamFeature<TContext> where TContext : notnull
    {
        Http3Stream<TContext> CachedStream { get; }
    }

    internal class DefaultCachedHttp3StreamFeature<TContext> : ICachedHttp3StreamFeature<TContext> where TContext : notnull
    {
        public Http3Stream<TContext> CachedStream { get; }

        public DefaultCachedHttp3StreamFeature(Http3Stream<TContext> cachedStream)
        {
            CachedStream = cachedStream;
        }
    }
}
