// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal sealed class DataSourceDependentMatcher : Matcher
    {
        private readonly Func<MatcherBuilder> _matcherBuilderFactory;
        private readonly DataSourceDependentCache<Matcher> _cache;

        public DataSourceDependentMatcher(
            EndpointDataSource dataSource,
            Lifetime lifetime,
            Func<MatcherBuilder> matcherBuilderFactory)
        {
            _matcherBuilderFactory = matcherBuilderFactory;

            _cache = new DataSourceDependentCache<Matcher>(dataSource, CreateMatcher);
            _cache.EnsureInitialized();

            // This will Dispose the cache when the lifetime is disposed, this allows
            // the service provider to manage the lifetime of the cache.
            lifetime.Cache = _cache;
        }

        // Used in tests
        internal Matcher CurrentMatcher => _cache.Value;

        public override Task MatchAsync(HttpContext httpContext)
        {
            return CurrentMatcher.MatchAsync(httpContext);
        }

        private Matcher CreateMatcher(IReadOnlyList<Endpoint> endpoints)
        {
            var builder = _matcherBuilderFactory();
            for (var i = 0; i < endpoints.Count; i++)
            {
                // By design we only look at RouteEndpoint here. It's possible to
                // register other endpoint types, which are non-routable, and it's
                // ok that we won't route to them.
                if (endpoints[i] is RouteEndpoint endpoint && endpoint.Metadata.GetMetadata<ISuppressMatchingMetadata>()?.SuppressMatching != true)
                {
                    builder.AddEndpoint(endpoint);
                }
            }

            return builder.Build();
        }

        // Used to tie the lifetime of a DataSourceDependentCache to the service provider
        public sealed class Lifetime : IDisposable
        {
            private readonly object _lock = new object();
            private DataSourceDependentCache<Matcher> _cache;
            private bool _disposed;

            public DataSourceDependentCache<Matcher> Cache
            {
                get => _cache;
                set
                {
                    lock (_lock)
                    {
                        if (_disposed)
                        {
                            value?.Dispose();
                        }

                        _cache = value;
                    }
                }
            }

            public void Dispose()
            {
                lock (_lock)
                {
                    _cache?.Dispose();
                    _cache = null;

                    _disposed = true;
                }
            }
        }
    }
}
