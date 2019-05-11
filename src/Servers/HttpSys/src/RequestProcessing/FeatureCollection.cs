// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.HttpSys.Internal
{
    internal sealed class FeatureCollection<TContext> : FeatureCollection, IDefaultHttpContextContainer, IContextContainer<TContext>
    {
        private const int _maxPooledContexts = 512;
        private readonly static ConcurrentQueueSegment<DefaultHttpContext> _httpContexts = new ConcurrentQueueSegment<DefaultHttpContext>(_maxPooledContexts);
        private readonly static ConcurrentQueueSegment<TContext> _hostContexts = new ConcurrentQueueSegment<TContext>(_maxPooledContexts);

        public FeatureCollection(IFeatureCollection defaults) : base(defaults) { }

        bool IDefaultHttpContextContainer.TryGetContext(out DefaultHttpContext context)
            => _httpContexts.TryDequeue(out context);

        void IDefaultHttpContextContainer.ReleaseContext(DefaultHttpContext context)
            => _httpContexts.TryEnqueue(context);

        bool IContextContainer<TContext>.TryGetContext(out TContext context)
            =>_hostContexts.TryDequeue(out context);

        void IContextContainer<TContext>.ReleaseContext(TContext context)
            => _hostContexts.TryEnqueue(context);
    }
}
