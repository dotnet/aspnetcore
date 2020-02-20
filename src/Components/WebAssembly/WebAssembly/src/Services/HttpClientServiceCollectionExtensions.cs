// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpClientServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a <see cref="HttpClient" /> instance to the <paramref name="serviceCollection" /> that is
        /// configured to use the application's base address (<seealso cref="NavigationManager.BaseUri" />).
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection" />.</param>
        /// <returns>The configured <see cref="IServiceCollection" />.</returns>
        public static IServiceCollection AddBaseAddressHttpClient(this IServiceCollection serviceCollection)
        {
             return serviceCollection.AddSingleton(s =>
            {
                // Creating the URI helper needs to wait until the JS Runtime is initialized, so defer it.
                var navigationManager = s.GetRequiredService<NavigationManager>();
                return new HttpClient
                {
                    BaseAddress = new Uri(navigationManager.BaseUri)
                };
            });
        }
    }
}
