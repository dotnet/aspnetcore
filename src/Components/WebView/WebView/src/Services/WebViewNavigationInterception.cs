// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.WebView.Services
{
    internal class WebViewNavigationInterception : INavigationInterception
    {
        // On this platform, it's sufficient for the JS-side code to enable it unconditionally,
        // so there's no need to send a notification.
        public Task EnableNavigationInterceptionAsync() => Task.CompletedTask;
    }
}
