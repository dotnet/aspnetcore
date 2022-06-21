// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// A builder for defining groups of endpoints with a common prefix that implements both the <see cref="IEndpointRouteBuilder"/>
/// and <see cref="IEndpointConventionBuilder"/> interfaces. This can be used to add endpoints with the prefix defined by
/// <see cref="EndpointRouteBuilderExtensions.MapGroup(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder, RoutePattern)"/>
/// and to customize those endpoints using conventions.
/// </summary>
public sealed class RouteGroupBuilder : IEndpointRouteBuilder, IEndpointConventionBuilder
{
    private readonly IEndpointRouteBuilder _outerEndpointRouteBuilder;
    private readonly RoutePattern _partialPrefix;

    private readonly List<EndpointDataSource> _dataSources = new();
    private readonly List<Action<EndpointBuilder>> _conventions = new();

    internal RouteGroupBuilder(IEndpointRouteBuilder outerEndpointRouteBuilder, RoutePattern partialPrefix)
    {
        _outerEndpointRouteBuilder = outerEndpointRouteBuilder;
        _partialPrefix = partialPrefix;
        _outerEndpointRouteBuilder.DataSources.Add(new GroupEndpointDataSource(this));
    }

    IServiceProvider IEndpointRouteBuilder.ServiceProvider => _outerEndpointRouteBuilder.ServiceProvider;
    IApplicationBuilder IEndpointRouteBuilder.CreateApplicationBuilder() => _outerEndpointRouteBuilder.CreateApplicationBuilder();
    ICollection<EndpointDataSource> IEndpointRouteBuilder.DataSources => _dataSources;
    void IEndpointConventionBuilder.Add(Action<EndpointBuilder> convention) => _conventions.Add(convention);

    private sealed class GroupEndpointDataSource : EndpointDataSource, IDisposable
    {
        private readonly RouteGroupBuilder _routeGroupBuilder;
        private CompositeEndpointDataSource? _compositeDataSource;

        public GroupEndpointDataSource(RouteGroupBuilder groupRouteBuilder)
        {
            _routeGroupBuilder = groupRouteBuilder;
        }

        public override IReadOnlyList<Endpoint> Endpoints =>
            GetGroupedEndpointsWithNullablePrefix(null, Array.Empty<Action<EndpointBuilder>>(), _routeGroupBuilder._outerEndpointRouteBuilder.ServiceProvider);

        public override IReadOnlyList<Endpoint> GetEndpointGroup(RouteGroupContext context) =>
            GetGroupedEndpointsWithNullablePrefix(context.Prefix, context.Conventions, context.ApplicationServices);

        public IReadOnlyList<Endpoint> GetGroupedEndpointsWithNullablePrefix(RoutePattern? prefix, IReadOnlyList<Action<EndpointBuilder>> conventions, IServiceProvider applicationServices)
        {
            return _routeGroupBuilder._dataSources.Count switch
            {
                0 => Array.Empty<Endpoint>(),
                1 => _routeGroupBuilder._dataSources[0].GetEndpointGroup(GetNextRouteGroupContext(prefix, conventions, applicationServices)),
                _ => SelectEndpointsFromAllDataSources(GetNextRouteGroupContext(prefix, conventions, applicationServices)),
            };
        }

        public override IChangeToken GetChangeToken() => _routeGroupBuilder._dataSources.Count switch
        {
            0 => NullChangeToken.Singleton,
            1 => _routeGroupBuilder._dataSources[0].GetChangeToken(),
            _ => GetCompositeChangeToken(),
        };

        public void Dispose()
        {
            _compositeDataSource?.Dispose();

            foreach (var dataSource in _routeGroupBuilder._dataSources)
            {
                (dataSource as IDisposable)?.Dispose();
            }
        }

        private RouteGroupContext GetNextRouteGroupContext(RoutePattern? prefix, IReadOnlyList<Action<EndpointBuilder>> conventions, IServiceProvider applicationServices)
        {
            var fullPrefix = RoutePatternFactory.Combine(prefix, _routeGroupBuilder._partialPrefix);
            // Apply conventions passed in from the outer group first so their metadata is added earlier in the list at a lower precedent.
            var combinedConventions = RoutePatternFactory.CombineLists(conventions, _routeGroupBuilder._conventions);
            return new RouteGroupContext(fullPrefix, combinedConventions, applicationServices);
        }

        private IReadOnlyList<Endpoint> SelectEndpointsFromAllDataSources(RouteGroupContext context)
        {
            var groupedEndpoints = new List<Endpoint>();

            foreach (var dataSource in _routeGroupBuilder._dataSources)
            {
                groupedEndpoints.AddRange(dataSource.GetEndpointGroup(context));
            }

            return groupedEndpoints;
        }

        private IChangeToken GetCompositeChangeToken()
        {
            // We are not guarding against concurrent RouteGroupBuilder._dataSources mutation.
            // This is only to avoid double initialization of _compositeDataSource if GetChangeToken() is called concurrently.
            lock (_routeGroupBuilder._dataSources)
            {
                _compositeDataSource ??= new CompositeEndpointDataSource(_routeGroupBuilder._dataSources);
            }

            return _compositeDataSource.GetChangeToken();
        }
    }
}
