// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// A builder for defining groups of endpoints with a common prefix that implements both the <see cref="IEndpointRouteBuilder"/>
/// and <see cref="IEndpointConventionBuilder"/> interfaces. This can be used to add endpoints with the given <see cref="GroupPrefix"/>,
/// and to customize those endpoints using conventions.
/// </summary>
public sealed class GroupRouteBuilder : IEndpointRouteBuilder, IEndpointConventionBuilder
{
    private readonly IEndpointRouteBuilder _outerEndpointRouteBuilder;
    private readonly RoutePattern _pattern;

    private readonly List<EndpointDataSource> _dataSources = new();
    private readonly List<Action<EndpointBuilder>> _conventions = new();

    internal GroupRouteBuilder(IEndpointRouteBuilder outerEndpointRouteBuilder, RoutePattern pattern)
    {
        _outerEndpointRouteBuilder = outerEndpointRouteBuilder;
        _pattern = pattern;

        if (outerEndpointRouteBuilder is GroupRouteBuilder outerGroup)
        {
            GroupPrefix = RoutePatternFactory.Combine(outerGroup.GroupPrefix, pattern);
        }
        else
        {
            GroupPrefix = pattern;
        }

        _outerEndpointRouteBuilder.DataSources.Add(new GroupDataSource(this));
    }

    /// <summary>
    /// The <see cref="RoutePattern"/> prefixing all endpoints defined using this <see cref="GroupRouteBuilder"/>.
    /// This accounts for nested groups and gives the full group prefix, not just the prefix supplied to the last call to
    /// <see cref="EndpointRouteBuilderExtensions.MapGroup(IEndpointRouteBuilder, RoutePattern)"/>.
    /// </summary>
    public RoutePattern GroupPrefix { get; }

    IServiceProvider IEndpointRouteBuilder.ServiceProvider => _outerEndpointRouteBuilder.ServiceProvider;
    IApplicationBuilder IEndpointRouteBuilder.CreateApplicationBuilder() => _outerEndpointRouteBuilder.CreateApplicationBuilder();
    ICollection<EndpointDataSource> IEndpointRouteBuilder.DataSources => _dataSources;
    void IEndpointConventionBuilder.Add(Action<EndpointBuilder> convention) => _conventions.Add(convention);

    private bool IsRoot => ReferenceEquals(GroupPrefix, _pattern);

    private sealed class GroupDataSource : EndpointDataSource
    {
        private readonly GroupRouteBuilder _groupRouteBuilder;

        public GroupDataSource(GroupRouteBuilder groupRouteBuilder)
        {
            _groupRouteBuilder = groupRouteBuilder;
        }

        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                var list = new List<Endpoint>();

                foreach (var dataSource in _groupRouteBuilder._dataSources)
                {
                    foreach (var endpoint in dataSource.Endpoints)
                    {
                        // Endpoint does not provide a RoutePattern but RouteEndpoint does. So it's impossible to apply a prefix for custom Endpoints.
                        // Supporting arbitrary Endpoints just to add group metadata would require changing the Endpoint type breaking any real scenario.
                        if (endpoint is not RouteEndpoint routeEndpoint)
                        {
                            throw new NotSupportedException(Resources.FormatMapGroup_CustomEndpointUnsupported(endpoint.GetType()));
                        }

                        // Make the full route pattern visible to IEndpointConventionBuilder extension methods called on the group.
                        // This includes patterns from any parent groups.
                        var fullRoutePattern = RoutePatternFactory.Combine(_groupRouteBuilder.GroupPrefix, routeEndpoint.RoutePattern);

                        // RequestDelegate can never be null on a RouteEndpoint. The nullability carries over from Endpoint.
                        var routeEndpointBuilder = new RouteEndpointBuilder(routeEndpoint.RequestDelegate!, fullRoutePattern, routeEndpoint.Order)
                        {
                            DisplayName = routeEndpoint.DisplayName,
                        };

                        // Apply group conventions to each endpoint in the group at a lower precedent than Metadata already.
                        foreach (var convention in _groupRouteBuilder._conventions)
                        {
                            convention(routeEndpointBuilder);
                        }

                        // If we supported mutating the route pattern via a group convention, RouteEndpointBuilder.RoutePattern would have
                        // to be the partialRoutePattern (below) instead of the fullRoutePattern (above) since that's all we can control. We cannot
                        // change a parent prefix. In order to allow to conventions to read the fullRoutePattern, we do not support mutation.
                        if (!ReferenceEquals(fullRoutePattern, routeEndpointBuilder.RoutePattern))
                        {
                            throw new NotSupportedException(Resources.FormatMapGroup_ChangingRoutePatternUnsupported(
                                fullRoutePattern.RawText, routeEndpointBuilder.RoutePattern.RawText));
                        }

                        // Any metadata already on the RouteEndpoint must have been applied directly to the endpoint or to a nested group.
                        // This makes the metadata more specific than what's being applied to this group. So add it after this group's conventions.
                        //
                        // REVIEW: This means group conventions don't get visibility into endpoint-specific metadata nor the ability to override it.
                        // We should consider allowing group-aware conventions the ability to read and mutate this metadata in future releases.
                        foreach (var metadata in routeEndpoint.Metadata)
                        {
                            routeEndpointBuilder.Metadata.Add(metadata);
                        }

                        // Use _pattern instead of GroupPrefix when we're calculating an intermediate RouteEndpoint.
                        var partialRoutePattern = _groupRouteBuilder.IsRoot
                            ? fullRoutePattern : RoutePatternFactory.Combine(_groupRouteBuilder._pattern, routeEndpoint.RoutePattern);

                        // The RequestDelegate, Order and DisplayName can all be overridden by non-group-aware conventions. Unlike with metadata,
                        // if a convention is applied to a group that changes any of these, I would expect these to be overridden as there's no
                        // reasonable way to merge these properties.
                        list.Add(new RouteEndpoint(
                            // Again, RequestDelegate can never be null given a RouteEndpoint.
                            routeEndpointBuilder.RequestDelegate!,
                            partialRoutePattern,
                            routeEndpointBuilder.Order,
                            new(routeEndpointBuilder.Metadata),
                            routeEndpointBuilder.DisplayName));
                    }
                }

                return list;
            }
        }

        public override IChangeToken GetChangeToken() => new CompositeEndpointDataSource(_groupRouteBuilder._dataSources).GetChangeToken();
    }
}
