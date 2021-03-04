// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.WebView.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ComponentsWebViewServiceCollectionExtensions
    {
        public static IServiceCollection AddBlazorWebView(this IServiceCollection services)
        {
            services.AddLogging();
            services.TryAddScoped<IJSRuntime, WebViewJSRuntime>();
            services.TryAddScoped<INavigationInterception, WebViewNavigationInterception>();
            services.TryAddScoped<NavigationManager, WebViewNavigationManager>();
            return services;
        }
    }
}
