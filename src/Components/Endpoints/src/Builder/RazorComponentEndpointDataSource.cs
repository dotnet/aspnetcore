// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Discovery;
using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class RazorComponentEndpointDataSource<[DynamicallyAccessedMembers(Component)] TRootComponent> : EndpointDataSource
{
    private readonly object _lock = new();
    private readonly List<Action<EndpointBuilder>> _conventions = new();
    private readonly List<Action<EndpointBuilder>> _finallyConventions = new();
    private readonly RazorComponentDataSourceOptions _options = new();
    private readonly ComponentApplicationBuilder _builder;
    private readonly IApplicationBuilder _applicationBuilder;
    private readonly RenderModeEndpointProvider[] _renderModeEndpointProviders;
    private readonly RazorComponentEndpointFactory _factory;
    private readonly HotReloadService? _hotReloadService;
    private List<Endpoint>? _endpoints;
    private CancellationTokenSource _cancellationTokenSource;
    private IChangeToken _changeToken;

    // Internal for testing.
    internal ComponentApplicationBuilder Builder => _builder;
    internal List<Action<EndpointBuilder>> Conventions => _conventions;

    public RazorComponentEndpointDataSource(
        ComponentApplicationBuilder builder,
        IEnumerable<RenderModeEndpointProvider> renderModeEndpointProviders,
        IApplicationBuilder applicationBuilder,
        RazorComponentEndpointFactory factory,
        HotReloadService? hotReloadService = null)
    {
        _builder = builder;
        _applicationBuilder = applicationBuilder;
        _renderModeEndpointProviders = renderModeEndpointProviders.ToArray();
        _factory = factory;
        _hotReloadService = hotReloadService;
        DefaultBuilder = new RazorComponentsEndpointConventionBuilder(
            _lock,
            builder,
            _options,
            _conventions,
            _finallyConventions);

        _cancellationTokenSource = new CancellationTokenSource();
        _changeToken = new CancellationChangeToken(_cancellationTokenSource.Token);
    }

    internal RazorComponentsEndpointConventionBuilder DefaultBuilder { get; }

    public override IReadOnlyList<Endpoint> Endpoints
    {
        get
        {
            // Note it is important that this is lazy, since we only want to create the endpoints after the user had a chance to populate
            // the list of conventions.
            // The order is as follows:
            // * MapRazorComponents gets called and the data source gets created.
            // * The RazorComponentEndpointConventionBuilder is returned and the user gets a chance to call on it to add conventions.
            // * The first request arrives and the DfaMatcherBuilder accesses the data sources to get the endpoints.
            // * The endpoints get created and the conventions get applied.
            Initialize();
            Debug.Assert(_changeToken != null);
            Debug.Assert(_endpoints != null);
            return _endpoints;
        }
    }

    internal RazorComponentDataSourceOptions Options => _options;

    private void Initialize()
    {
        if (_endpoints == null)
        {
            lock (_lock)
            {
                if (_endpoints == null)
                {
                    UpdateEndpoints();
                }
            }
        }
    }

    private void UpdateEndpoints()
    {
        lock (_lock)
        {
            var endpoints = new List<Endpoint>();
            var context = _builder.Build();
            var configuredRenderModesMetadata = new ConfiguredRenderModesMetadata(
                Options.ConfiguredRenderModes.ToArray());

            foreach (var definition in context.Pages)
            {
                _factory.AddEndpoints(endpoints, typeof(TRootComponent), definition, _conventions, _finallyConventions, configuredRenderModesMetadata);
            }

            ICollection<IComponentRenderMode> renderModes = Options.ConfiguredRenderModes;

            foreach (var renderMode in renderModes)
            {
                var found = false;
                foreach (var provider in _renderModeEndpointProviders)
                {
                    if (provider.Supports(renderMode))
                    {
                        found = true;
                        RenderModeEndpointProvider.AddEndpoints(
                            endpoints,
                            typeof(TRootComponent),
                            provider.GetEndpointBuilders(renderMode, _applicationBuilder.New()),
                            renderMode,
                            _conventions,
                            _finallyConventions);
                    }
                }

                if (!found)
                {
                    throw new InvalidOperationException($"Unable to find a provider for the render mode: {renderMode.GetType().FullName}. This generally " +
                        "means that a call to 'AddInteractiveWebAssemblyComponents' or 'AddInteractiveServerComponents' is missing. " +
                        "For example, change builder.Services.AddRazorComponents() to builder.Services.AddRazorComponents().AddInteractiveServerComponents().");
                }
            }

            var oldCancellationTokenSource = _cancellationTokenSource;
            _endpoints = endpoints;
            _cancellationTokenSource = new CancellationTokenSource();
            _changeToken = new CancellationChangeToken(_cancellationTokenSource.Token);
            oldCancellationTokenSource?.Cancel();
            if (_hotReloadService is { MetadataUpdateSupported : true })
            {
                ChangeToken.OnChange(_hotReloadService.GetChangeToken, UpdateEndpoints);
            }
        }
    }

    public override IChangeToken GetChangeToken()
    {
        Initialize();
        Debug.Assert(_changeToken != null);
        Debug.Assert(_endpoints != null);
        return _changeToken;
    }
}
