// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// A component that can be used to intercept internal navigation events.
/// </summary>
public class InternalNavigationLock : IComponent, IHandleLocationChanging, IDisposable
{
    private bool _isInitialized;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    /// A callback to be invoked when an internal navigation event occurs.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public EventCallback<LocationChangingContext> OnLocationChanging { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (!OnLocationChanging.HasDelegate)
        {
            throw new InvalidOperationException($"{nameof(InternalNavigationLock)} requires a non-null value for the parameter {nameof(OnLocationChanging)}.");
        }

        if (!_isInitialized)
        {
            _isInitialized = true;
            NavigationManager.AddLocationChangingHandler(this);
        }

        return Task.CompletedTask;
    }

    async ValueTask IHandleLocationChanging.OnLocationChanging(LocationChangingContext context)
    {
        await OnLocationChanging.InvokeAsync(context);
    }

    void IDisposable.Dispose()
    {
        if (_isInitialized)
        {
            NavigationManager.RemoveLocationChangingHandler(this);
        }
    }
}
