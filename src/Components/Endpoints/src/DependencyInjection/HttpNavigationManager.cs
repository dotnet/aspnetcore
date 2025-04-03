// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class HttpNavigationManager : NavigationManager, IHostEnvironmentNavigationManager
{
    private const string EnableThrowNavigatioExceptionSwitchKey = "Microsoft.AspNetCore.Components.Endpoints.EnableThrowNavigatioException";
    private static readonly bool _throwNavigationException = AppContext.TryGetSwitch(EnableThrowNavigatioExceptionSwitchKey, out var switchValue) && switchValue;

    private EventHandler<NavigationEventArgs>? _onNavigateTo;
    public event EventHandler<NavigationEventArgs> OnNavigateTo
    {
        add => _onNavigateTo += value;
        remove => _onNavigateTo -= value;
    }

    void IHostEnvironmentNavigationManager.Initialize(string baseUri, string uri) => Initialize(baseUri, uri);

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        var absoluteUriString = ToAbsoluteUri(uri).AbsoluteUri;
        if (_throwNavigationException)
        {
            throw new NavigationException(absoluteUriString);
        }
        else
        {
            _onNavigateTo?.Invoke(this, new NavigationEventArgs(absoluteUriString));
        }
    }
}
