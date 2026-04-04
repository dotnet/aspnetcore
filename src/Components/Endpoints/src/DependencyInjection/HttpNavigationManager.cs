// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class HttpNavigationManager : NavigationManager, IHostEnvironmentNavigationManager
{
    private const string _disableThrowNavigationException = "Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException";

    [FeatureSwitchDefinition(_disableThrowNavigationException)]
    private static bool _throwNavigationException =>
        !AppContext.TryGetSwitch(_disableThrowNavigationException, out var switchValue) || !switchValue;

    private Func<string, Task>? _onNavigateTo;

    void IHostEnvironmentNavigationManager.Initialize(string baseUri, string uri) => Initialize(baseUri, uri);

    void IHostEnvironmentNavigationManager.Initialize(string baseUri, string uri, Func<string, Task> onNavigateTo)
    {
        _onNavigateTo = onNavigateTo;
        Initialize(baseUri, uri);
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        var absoluteUriString = ToAbsoluteUri(uri).AbsoluteUri;
        if (_throwNavigationException)
        {
            throw new NavigationException(absoluteUriString);
        }
        else
        {
            _ = PerformNavigationAsync();
        }

        async Task PerformNavigationAsync()
        {
            if (_onNavigateTo == null)
            {
                throw new InvalidOperationException($"'{GetType().Name}' method for endpoint-based navigation has not been initialized.");
            }
            await _onNavigateTo(absoluteUriString);
        }
    }
}
