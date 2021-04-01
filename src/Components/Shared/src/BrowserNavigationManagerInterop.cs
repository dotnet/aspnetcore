// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web
{
    // Shared interop constants
    internal static class BrowserNavigationManagerInterop
    {
        private static readonly string Prefix = "Blazor._internal.navigationManager.";

        public static readonly string EnableNavigationInterception = Prefix + "enableNavigationInterception";

        public static readonly string GetLocationHref = Prefix + "getUnmarshalledLocationHref";

        public static readonly string GetBaseUri = Prefix + "getUnmarshalledBaseURI";

        public static readonly string NavigateTo = Prefix + "navigateTo";
    }
}
