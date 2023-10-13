// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;

namespace Microsoft.Extensions.DependencyInjection;

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
        services.TryAddScoped<IScrollToLocationHash, WebViewScrollToLocationHash>();
        services.TryAddScoped<NavigationManager, WebViewNavigationManager>();
        services.TryAddScoped<IErrorBoundaryLogger, WebViewErrorBoundaryLogger>();
        services.AddSupplyValueFromQueryProvider();

        return services;
    }
}
