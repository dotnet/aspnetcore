// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

// Shared interop constants
internal static class BrowserNavigationManagerInterop
{
    private const string Prefix = "Blazor._internal.navigationManager.";

    public const string EnableNavigationInterception = Prefix + "enableNavigationInterception";

    public const string GetLocationHref = Prefix + "getLocationHref";

    public const string GetBaseUri = Prefix + "getBaseURI";

    public const string NavigateTo = Prefix + "navigateTo";

    public const string Refresh = Prefix + "refresh";

    public const string SetHasLocationChangingListeners = Prefix + "setHasLocationChangingListeners";

    public const string ScrollToElement = Prefix + "scrollToElement";
}
