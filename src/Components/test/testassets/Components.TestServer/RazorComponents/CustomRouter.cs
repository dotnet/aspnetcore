// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Components.TestServer.RazorComponents;

/// <summary>
/// A custom router implementation for testing NavigationManager.NotFound() with custom routers.
/// This implementation matches the built-in Router component's behavior but with a different implementation
/// to ensure custom routers can properly handle NotFound events.
/// </summary>
public class CustomRouter : IComponent, IHandleAfterRender, IDisposable
{
    private RenderHandle _renderHandle;
    private bool _navigationInterceptionEnabled;
    private ILogger<CustomRouter> _logger = default!;
    private NavigationManager _navigationManager = default!;
    private INavigationInterception _navigationInterception = default!;

    [Inject] private IRoutingStateProvider? RoutingStateProvider { get; set; }

    [Inject]
    private NavigationManager NavigationManager
    {
        get => _navigationManager;
        set
        {
            _navigationManager = value;
            _navigationManager.LocationChanged += OnLocationChanged;
        }
    }

    [Inject] private INavigationInterception NavigationInterception
    {
        get => _navigationInterception;
        set => _navigationInterception = value;
    }

    [Inject] private ILoggerFactory LoggerFactory
    {
        get => throw new InvalidOperationException($"{nameof(LoggerFactory)} should only be set via injection.");
        set => _logger = value.CreateLogger<CustomRouter>();
    }

    [Parameter]
    [EditorRequired]
    public Assembly AppAssembly { get; set; } = default!;

    [Parameter] public IEnumerable<Assembly> AdditionalAssemblies { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public RenderFragment<Microsoft.AspNetCore.Components.RouteData> Found { get; set; } = default!;

    [Parameter] public bool PreferExactMatches { get; set; }

    public void Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (AppAssembly is null)
        {
            throw new InvalidOperationException($"The {nameof(CustomRouter)} component requires a value for the parameter {nameof(AppAssembly)}.");
        }

        if (Found is null)
        {
            throw new InvalidOperationException($"The {nameof(CustomRouter)} component requires a value for the parameter {nameof(Found)}.");
        }

        RefreshRouteTable();
        RefreshComponent();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }

    private RouteTable? _routeTable;

    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    private void RefreshRouteTable()
    {
        var routeKey = new RouteKey(AppAssembly, AdditionalAssemblies);
        _routeTable = RouteTableFactory.Instance.Create(routeKey, ServiceProvider);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        RefreshComponent();
    }

    private void RefreshComponent()
    {
        if (_renderHandle.IsInitialized)
        {
            var uri = NavigationManager.Uri;
            var locationPath = NavigationManager.ToBaseRelativePath(uri);
            locationPath = StringUntilAny(locationPath, '?', '#');
            var locationPathWithLeadingSlash = $"/{locationPath}";

            // First check if the routing state provider has route data for us
            if (RoutingStateProvider?.RouteData is { } routeData)
            {
                _logger.LogInformation($"CustomRouter: Using route from RoutingStateProvider for component {routeData.PageType}");

                // Process parameters to ensure proper handling of route constraints and optional parameters
                var processedRouteData = RouteTable.ProcessParameters(routeData);

                _renderHandle.Render(Found(processedRouteData));
                return;
            }

            // Otherwise, use our route table to find a matching route
            var context = new Microsoft.AspNetCore.Components.Routing.RouteContext(locationPathWithLeadingSlash);
            _routeTable?.Route(context);

            if (context.Handler != null)
            {
                _logger.LogInformation($"CustomRouter: Navigating to component {context.Handler}");
                var matchedRouteData = new Microsoft.AspNetCore.Components.RouteData(
                    context.Handler,
                    context.Parameters ?? new Dictionary<string, object?>());

                // Set the template to maintain route information
                if (context.Entry != null)
                {
                    matchedRouteData.Template = context.Entry.RoutePattern.RawText;
                }

                _renderHandle.Render(Found(matchedRouteData));
            }
        }
    }

    private static string StringUntilAny(string str, params char[] chars)
    {
        var firstIndex = str.IndexOfAny(chars);
        return firstIndex < 0
            ? str
            : str.Substring(0, firstIndex);
    }

    Task IHandleAfterRender.OnAfterRenderAsync()
    {
        if (!_navigationInterceptionEnabled)
        {
            _navigationInterceptionEnabled = true;
            return NavigationInterception.EnableNavigationInterceptionAsync();
        }

        return Task.CompletedTask;
    }
}
