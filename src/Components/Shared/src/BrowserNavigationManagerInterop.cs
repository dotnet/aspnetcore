// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;

namespace Microsoft.AspNetCore.Components.Web;

// Shared interop constants
internal static partial class BrowserNavigationManagerInterop
{
    private const string Prefix = "Blazor._internal.navigationManager.";

    public const string EnableNavigationInterceptionName = Prefix + "enableNavigationInterception";

    public const string NavigateTo = Prefix + "navigateTo";

    [JSImport(EnableNavigationInterceptionName)]
    public static partial void EnableNavigationInterception();

    [JSImport(Prefix + "getLocationHref")]
    public static partial string GetLocationHref();

    [JSImport(Prefix + "getBaseUri")]
    public static partial string GetBaseUri();
}
