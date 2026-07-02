// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class HttpNavigationManager : NavigationManager, IHostEnvironmentNavigationManager
{
    private const string _disableThrowNavigationException = "Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException";

    private static readonly bool s_throwNavigationException =
        !AppContext.TryGetSwitch(_disableThrowNavigationException, out var switchValue) || !switchValue;
    private static int s_throwNavigationExceptionOverride = -1;

    [FeatureSwitchDefinition(_disableThrowNavigationException)]
    private static bool _throwNavigationException => System.Threading.Volatile.Read(ref s_throwNavigationExceptionOverride) switch
    {
        0 => false,
        1 => true,
        _ => s_throwNavigationException,
    };

    private Func<string, Task>? _onNavigateTo;

    internal static IDisposable SetThrowNavigationExceptionOverrideForTest(bool value)
    {
        var previousValue = System.Threading.Interlocked.Exchange(ref s_throwNavigationExceptionOverride, value ? 1 : 0);
        return new ThrowNavigationExceptionOverrideScope(previousValue);
    }

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

    private sealed class ThrowNavigationExceptionOverrideScope(int previousValue) : IDisposable
    {
        public void Dispose()
        {
            _ = System.Threading.Interlocked.Exchange(ref s_throwNavigationExceptionOverride, previousValue);
        }
    }
}
