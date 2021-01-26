// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class HttpNavigationManager : NavigationManager, IHostEnvironmentNavigationManager
    {
        void IHostEnvironmentNavigationManager.Initialize(string baseUri, string uri) => Initialize(baseUri, uri);

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            throw new NavigationException(uri);
        }
    }
}
