// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;

namespace Microsoft.AspNetCore.Components.Web;

// Shared interop constants
internal static partial class BrowserNavigationManagerInterop
{
    public const string NavigationManagerPrefix = "Blazor._internal.navigationManager.";

    public const string EnableNavigationInterceptionName = NavigationManagerPrefix + "enableNavigationInterception";

    public const string NavigateTo = NavigationManagerPrefix + "navigateTo";
}
