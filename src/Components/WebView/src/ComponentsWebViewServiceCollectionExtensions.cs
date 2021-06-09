// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.WebView.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for adding component webview services to the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ComponentsWebViewServiceCollectionExtensions
    {
        /// <summary>
        /// Adds component webview services to the <paramref name="services"/> collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the component webview services to.</param>
        /// <returns></returns>
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
