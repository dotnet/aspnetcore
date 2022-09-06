// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// A component that can be used to intercept navigation events. 
/// </summary>
public sealed class NavigationLock : ComponentBase, IAsyncDisposable
{
    private readonly string _id = Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);

    private IDisposable? _locationChangingRegistration;
    private bool _lastConfirmExternalNavigation;

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

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var lastHasLocationChangingHandler = _locationChangingRegistration is not null;
        var hasLocationChangingHandler = OnBeforeInternalNavigation.HasDelegate;
        if (lastHasLocationChangingHandler != hasLocationChangingHandler)
        {
            _locationChangingRegistration?.Dispose();
            _locationChangingRegistration = hasLocationChangingHandler
                ? NavigationManager.RegisterLocationChangingHandler(OnLocationChanging)
                : null;
        }

        var confirmExternalNavigation = ConfirmExternalNavigation;
        if (_lastConfirmExternalNavigation != confirmExternalNavigation)
        {
            if (confirmExternalNavigation)
            {
                await JSRuntime.InvokeVoidAsync(NavigationLockInterop.EnableNavigationPrompt, _id);
            }
            else
            {
                await JSRuntime.InvokeVoidAsync(NavigationLockInterop.DisableNavigationPrompt, _id);
            }

            _lastConfirmExternalNavigation = confirmExternalNavigation;
        }
    }

    private async ValueTask OnLocationChanging(LocationChangingContext context)
    {
        await OnBeforeInternalNavigation.InvokeAsync(context);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        _locationChangingRegistration?.Dispose();

        if (_lastConfirmExternalNavigation)
        {
            await JSRuntime.InvokeVoidAsync(NavigationLockInterop.DisableNavigationPrompt, _id);
        }
    }
}
