// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

// Shared interop constants
internal static class BrowserNavigationManagerInterop
{
    private const string Prefix = "Blazor._internal.navigationManager.";

    public const string EnableNavigationInterception = Prefix + "enableNavigationInterception";

    public const string GetLocationHref = Prefix + "getUnmarshalledLocationHref";

    public const string GetBaseUri = Prefix + "getUnmarshalledBaseURI";

    public const string NavigateTo = Prefix + "navigateTo";

    public const string SetHasLocationChangingListeners = Prefix + "setHasLocationChangingListeners";
}
