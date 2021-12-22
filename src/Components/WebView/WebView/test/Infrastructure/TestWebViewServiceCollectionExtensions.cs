// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.WebView;

public static class TestWebViewServiceCollectionExtensions
{
    public static IServiceCollection AddTestBlazorWebView(this IServiceCollection services)
    {
        services.AddBlazorWebView();
        return services;
    }
}
