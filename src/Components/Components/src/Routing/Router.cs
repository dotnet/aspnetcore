// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable warnings

using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// A component that supplies route data corresponding to the current navigation state.
/// </summary>
public partial class Router : IComponent, IHandleAfterRender, IDisposable
{
    // Dictionary is intentionally used instead of ReadOnlyDictionary to reduce Blazor size
    static readonly IReadOnlyDictionary<string, object> _emptyParametersDictionary
        = new Dictionary<string, object>();

    RenderHandle _renderHandle;
    string _baseUri;
    string _locationAbsolute;
    bool _navigationInterceptionEnabled;
    ILogger<Router> _logger;

    private string _updateScrollPositionForHashLastLocation;
    private bool _updateScrollPositionForHash;

    private CancellationTokenSource _onNavigateCts;

    private Task _previousOnNavigateTask = Task.CompletedTask;

    private RouteKey _routeTableLastBuiltForRouteKey;

    private bool _onNavigateCalled;

    [Inject] private NavigationManager NavigationManager { get; set; }

    [Inject] private INavigationInterception NavigationInterception { get; set; }

    [Inject] private IScrollToLocationHash ScrollToLocationHash { get; set; }

    [Inject] private ILoggerFactory LoggerFactory { get; set; }

    [Inject] IServiceProvider ServiceProvider { get; set; }

    private IRoutingStateProvider? RoutingStateProvider { get; set; }

    /// <summary>
    /// Gets or sets the assembly that should be searched for components matching the URI.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public Assembly AppAssembly { get; set; }

    /// <summary>
    /// Gets or sets a collection of additional assemblies that should be searched for components
    /// that can match URIs.
    /// </summary>
    [Parameter] public IEnumerable<Assembly> AdditionalAssemblies { get; set; }

    /// <summary>
    /// Gets or sets the content to display when no match is found for the requested route.
    /// </summary>
    [Parameter]
    public RenderFragment NotFound { get; set; }

    /// <summary>
    /// Gets or sets the content to display when a match is found for the requested route.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public RenderFragment<RouteData> Found { get; set; }

    /// <summary>
    /// Get or sets the content to display when asynchronous navigation is in progress.
    /// </summary>
    [Parameter] public RenderFragment? Navigating { get; set; }

    /// <summary>
    /// Gets or sets a handler that should be called before navigating to a new page.
    /// </summary>
    [Parameter] public EventCallback<NavigationContext> OnNavigateAsync { get; set; }

    /// <summary>
    /// Gets or sets a flag to indicate whether route matching should prefer exact matches
    /// over wildcards.
    /// <para>This property is obsolete and configuring it does nothing.</para>
    /// </summary>
    [Obsolete("This property is obsolete and configuring it has not effect.")]
    [Parameter] public bool PreferExactMatches { get; set; }

    private RouteTable Routes { get; set; }

    /// <inheritdoc />
    public void Attach(RenderHandle renderHandle)
    {
        _logger = LoggerFactory.CreateLogger<Router>();
        _renderHandle = renderHandle;
        _baseUri = NavigationManager.BaseUri;
        _locationAbsolute = NavigationManager.Uri;
        NavigationManager.LocationChanged += OnLocationChanged;
        RoutingStateProvider = ServiceProvider.GetService<IRoutingStateProvider>();

        if (HotReloadManager.Default.MetadataUpdateSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += ClearRouteCaches;
        }
    }

    /// <inheritdoc />
    public async Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (AppAssembly == null)
        {
            throw new InvalidOperationException($"The {nameof(Router)} component requires a value for the parameter {nameof(AppAssembly)}.");
        }

        // Found content is mandatory, because even though we could use something like <RouteView ...> as a
        // reasonable default, if it's not declared explicitly in the template then people will have no way
        // to discover how to customize this (e.g., to add authorization).
        if (Found == null)
        {
            throw new InvalidOperationException($"The {nameof(Router)} component requires a value for the parameter {nameof(Found)}.");
        }

        if (!_onNavigateCalled)
        {
            _onNavigateCalled = true;
            await RunOnNavigateAsync(NavigationManager.ToBaseRelativePath(_locationAbsolute), isNavigationIntercepted: false);
        }
        else
        {
            Refresh(isNavigationIntercepted: false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        if (HotReloadManager.Default.MetadataUpdateSupported)
        {
            HotReloadManager.Default.OnDeltaApplied -= ClearRouteCaches;
        }
    }

    private static ReadOnlySpan<char> TrimQueryOrHash(ReadOnlySpan<char> str)
    {
        var firstIndex = str.IndexOfAny('?', '#');
        return firstIndex < 0
            ? str
            : str[0..firstIndex];
    }

    private void RefreshRouteTable()
    {
        var routeKey = new RouteKey(AppAssembly, AdditionalAssemblies);

        if (!routeKey.Equals(_routeTableLastBuiltForRouteKey))
        {
            Routes = RouteTableFactory.Instance.Create(routeKey, ServiceProvider);
            _routeTableLastBuiltForRouteKey = routeKey;
        }
    }

    private void ClearRouteCaches()
    {
        RouteTableFactory.Instance.ClearCaches();
        _routeTableLastBuiltForRouteKey = default;
    }

    internal virtual void Refresh(bool isNavigationIntercepted)
    {
        // If an `OnNavigateAsync` task is currently in progress, then wait
        // for it to complete before rendering. Note: because _previousOnNavigateTask
        // is initialized to a CompletedTask on initialization, this will still
        // allow first-render to complete successfully.
        if (_previousOnNavigateTask.Status != TaskStatus.RanToCompletion)
        {
            if (Navigating != null)
            {
                _renderHandle.Render(Navigating);
            }
            return;
        }

        var relativePath = NavigationManager.ToBaseRelativePath(_locationAbsolute.AsSpan());
        var locationPathSpan = TrimQueryOrHash(relativePath);
        var locationPath = $"/{locationPathSpan}";

        // In order to avoid routing twice we check for RouteData
        if (RoutingStateProvider?.RouteData is { } endpointRouteData)
        {
            // Other routers shouldn't provide RouteData, this is specific to our router component
            // and must abide by our syntax and behaviors.
            // Other routers must create their own abstractions to flow data from their SSR routing
            // scheme to their interactive router.
            Log.NavigatingToComponent(_logger, endpointRouteData.PageType, locationPath, _baseUri);
            // Post process the entry to add Blazor specific behaviors:
            // - Add 'null' for unused route parameters.
            // - Convert constrained parameters with (int, double, etc) to the target type.
            endpointRouteData = RouteTable.ProcessParameters(endpointRouteData);
            _renderHandle.Render(Found(endpointRouteData));
            return;
        }

        RefreshRouteTable();

        var context = new RouteContext(locationPath);
        Routes.Route(context);

        if (context.Handler != null)
        {
            if (!typeof(IComponent).IsAssignableFrom(context.Handler))
            {
                throw new InvalidOperationException($"The type {context.Handler.FullName} " +
                    $"does not implement {typeof(IComponent).FullName}.");
            }

            Log.NavigatingToComponent(_logger, context.Handler, locationPath, _baseUri);

            var routeData = new RouteData(
                context.Handler,
                context.Parameters ?? _emptyParametersDictionary);

            _renderHandle.Render(Found(routeData));

            // If you navigate to a different path, then after the next render we'll update the scroll position
            if (relativePath != _updateScrollPositionForHashLastLocation)
            {
                _updateScrollPositionForHashLastLocation = relativePath.ToString();
                _updateScrollPositionForHash = true;
            }
        }
        else
        {
            if (!isNavigationIntercepted)
            {
                Log.DisplayingNotFound(_logger, locationPath, _baseUri);

                // We did not find a Component that matches the route.
                // Only show the NotFound content if the application developer programatically got us here i.e we did not
                // intercept the navigation. In all other cases, force a browser navigation since this could be non-Blazor content.
                _renderHandle.Render(NotFound ?? DefaultNotFoundContent);
            }
            else
            {
                Log.NavigatingToExternalUri(_logger, _locationAbsolute, locationPath, _baseUri);
                NavigationManager.NavigateTo(_locationAbsolute, forceLoad: true);
            }
        }
    }

    private static void DefaultNotFoundContent(RenderTreeBuilder builder)
    {
        // This output can't use any layout (none is specified), and it can't use any web-specific concepts
        // such as <p role="alert">, and we can't localize the output. However none of that matters because
        // in all cases we expect either:
        // 1. The app to be hosted with MapRazorPages, and then it will never use any NotFound content
        // 2. Or, the app to supply its own NotFound content
        // ... so this is just a fallback for badly-set-up apps.
        builder.AddContent(0, "Not found");
    }

    internal async ValueTask RunOnNavigateAsync(string path, bool isNavigationIntercepted)
    {
        // Cancel the CTS instead of disposing it, since disposing does not
        // actually cancel and can cause unintended Object Disposed Exceptions.
        // This effectively cancels the previously running task and completes it.
        _onNavigateCts?.Cancel();
        // Then make sure that the task has been completely cancelled or completed
        // before starting the next one. This avoid race conditions where the cancellation
        // for the previous task was set but not fully completed by the time we get to this
        // invocation.
        await _previousOnNavigateTask;

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _previousOnNavigateTask = tcs.Task;

        if (!OnNavigateAsync.HasDelegate)
        {
            Refresh(isNavigationIntercepted);
        }

        _onNavigateCts = new CancellationTokenSource();
        var navigateContext = new NavigationContext(path, _onNavigateCts.Token);

        var cancellationTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        navigateContext.CancellationToken.Register(state =>
            ((TaskCompletionSource)state).SetResult(), cancellationTcs);

        try
        {
            // Task.WhenAny returns a Task<Task> so we need to await twice to unwrap the exception
            var task = await Task.WhenAny(OnNavigateAsync.InvokeAsync(navigateContext), cancellationTcs.Task);
            await task;
            tcs.SetResult();
            Refresh(isNavigationIntercepted);
        }
        catch (Exception e)
        {
            _renderHandle.Render(builder => ExceptionDispatchInfo.Throw(e));
        }
    }

    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
        _locationAbsolute = args.Location;
        if (_renderHandle.IsInitialized && Routes != null)
        {
            _ = RunOnNavigateAsync(NavigationManager.ToBaseRelativePath(_locationAbsolute), args.IsNavigationIntercepted).Preserve();
        }
    }

    async Task IHandleAfterRender.OnAfterRenderAsync()
    {
        if (!_navigationInterceptionEnabled)
        {
            _navigationInterceptionEnabled = true;
            await NavigationInterception.EnableNavigationInterceptionAsync();
        }

        if (_updateScrollPositionForHash)
        {
            _updateScrollPositionForHash = false;
            await ScrollToLocationHash.RefreshScrollPositionForHash(_locationAbsolute);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, $"Displaying {nameof(NotFound)} because path '{{Path}}' with base URI '{{BaseUri}}' does not match any component route", EventName = "DisplayingNotFound")]
        internal static partial void DisplayingNotFound(ILogger logger, string path, string baseUri);

        [LoggerMessage(2, LogLevel.Debug, "Navigating to component {ComponentType} in response to path '{Path}' with base URI '{BaseUri}'", EventName = "NavigatingToComponent")]
        internal static partial void NavigatingToComponent(ILogger logger, Type componentType, string path, string baseUri);

        [LoggerMessage(3, LogLevel.Debug, "Navigating to non-component URI '{ExternalUri}' in response to path '{Path}' with base URI '{BaseUri}'", EventName = "NavigatingToExternalUri")]
        internal static partial void NavigatingToExternalUri(ILogger logger, string externalUri, string path, string baseUri);
    }
}
