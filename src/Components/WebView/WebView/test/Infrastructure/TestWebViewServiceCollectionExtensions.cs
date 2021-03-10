// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.WebView
{
    public static class TestWebViewServiceCollectionExtensions
    {
        public static IServiceCollection AddTestBlazorWebView(this IServiceCollection services)
        {
            services.AddBlazorWebView();
            return services;
        }
    }
}
