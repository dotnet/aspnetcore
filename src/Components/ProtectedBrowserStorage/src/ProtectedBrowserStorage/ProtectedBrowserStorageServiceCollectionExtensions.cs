// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.ProtectedBrowserStorage;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering Protected Browser Storage services.
    /// </summary>
    public static class ProtectedBrowserStorageServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services for protected browser storage to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        public static void AddProtectedBrowserStorage(this IServiceCollection services)
        {
            services.AddDataProtection();
            services.AddScoped<ProtectedLocalStorage>();
            services.AddScoped<ProtectedSessionStorage>();
        }
    }
}
