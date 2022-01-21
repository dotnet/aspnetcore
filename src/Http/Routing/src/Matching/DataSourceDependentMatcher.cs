// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

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
    internal Matcher CurrentMatcher => _cache.Value!;

    public override Task MatchAsync(HttpContext httpContext)
    {
        return CurrentMatcher.MatchAsync(httpContext);
    }

    private Matcher CreateMatcher(IReadOnlyList<Endpoint> endpoints)
    {
        var builder = _matcherBuilderFactory();
        var seenEndpointNames = new Dictionary<string, string?>();
        for (var i = 0; i < endpoints.Count; i++)
        {
            // By design we only look at RouteEndpoint here. It's possible to
            // register other endpoint types, which are non-routable, and it's
            // ok that we won't route to them.
            if (endpoints[i] is RouteEndpoint endpoint)
            {
                // Validate that endpoint names are unique.
                var endpointName = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName;
                if (endpointName is not null)
                {
                    if (seenEndpointNames.TryGetValue(endpointName, out var existingEndpoint))
                    {
                        throw new InvalidOperationException($"Duplicate endpoint name '{endpointName}' found on '{endpoint.DisplayName}' and '{existingEndpoint}'. Endpoint names must be globally unique.");
                    }

                    seenEndpointNames.Add(endpointName, endpoint.DisplayName ?? endpoint.RoutePattern.RawText);
                }

                // We check for duplicate endpoint names on all endpoints regardless
                // of whether they suppress matching because endpoint names can be
                // used in OpenAPI specifications as well.
                if (endpoint.Metadata.GetMetadata<ISuppressMatchingMetadata>()?.SuppressMatching != true)
                {
                    builder.AddEndpoint(endpoint);
                }
            }
        }

        return builder.Build();
    }

    // Used to tie the lifetime of a DataSourceDependentCache to the service provider
    public sealed class Lifetime : IDisposable
    {
        private readonly object _lock = new object();
        private DataSourceDependentCache<Matcher>? _cache;
        private bool _disposed;

        public DataSourceDependentCache<Matcher>? Cache
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
