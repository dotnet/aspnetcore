// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Web
{
    // Shared interop constants
    internal static class BrowserNavigationManagerInterop
    {
        private const string Prefix = "Blazor._internal.navigationManager.";

        public const string EnableNavigationInterception = Prefix + "enableNavigationInterception";

        public const string GetLocationHref = Prefix + "getUnmarshalledLocationHref";

        public const string GetBaseUri = Prefix + "getUnmarshalledBaseURI";

        public const string NavigateTo = Prefix + "navigateTo";
    }
}
