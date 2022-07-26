// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// A component that can be used to intercept navigation events. 
/// </summary>
public class NavigationLock : IComponent, IAsyncDisposable
{
    private readonly string _id = Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);

    private bool HasOnBeforeInternalNavigationCallback => OnBeforeInternalNavigation.HasDelegate;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    /// Gets or sets a callback to be invoked when an internal navigation event occurs.
    /// </summary>
    [Parameter]
    public EventCallback<LocationChangingContext> OnBeforeInternalNavigation { get; set; }

    /// <summary>
    /// Gets or sets whether a browser dialog should prompt the user to either confirm or cancel
    /// external navigations.
    /// </summary>
    [Parameter]
    public bool ConfirmExternalNavigation { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
    }

    async Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        var lastHasOnBeforeInternalNavigationCallback = HasOnBeforeInternalNavigationCallback;
        var lastConfirmExternalNavigation = ConfirmExternalNavigation;

        parameters.SetParameterProperties(this);

        var hasOnBeforeInternalNavigationCallback = HasOnBeforeInternalNavigationCallback;
        if (hasOnBeforeInternalNavigationCallback != lastHasOnBeforeInternalNavigationCallback)
        {
            if (hasOnBeforeInternalNavigationCallback)
            {
                NavigationManager.AddLocationChangingHandler(OnLocationChanging);
            }
            else
            {
                NavigationManager.RemoveLocationChangingHandler(OnLocationChanging);
            }
        }

        var confirmExternalNavigation = ConfirmExternalNavigation;
        if (confirmExternalNavigation != lastConfirmExternalNavigation)
        {
            if (confirmExternalNavigation)
            {
                await JSRuntime.InvokeVoidAsync(NavigationLockInterop.EnableNavigationPrompt, _id);
            }
            else
            {
                await JSRuntime.InvokeVoidAsync(NavigationLockInterop.DisableNavigationPrompt, _id);
            }
        }
    }

    async ValueTask OnLocationChanging(LocationChangingContext context)
    {
        await OnBeforeInternalNavigation.InvokeAsync(context);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        NavigationManager.RemoveLocationChangingHandler(OnLocationChanging);
        await JSRuntime.InvokeVoidAsync(NavigationLockInterop.DisableNavigationPrompt, _id);
    }
}
