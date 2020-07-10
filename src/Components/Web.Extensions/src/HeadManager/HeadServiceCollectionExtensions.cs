// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Web.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering head management services.
    /// </summary>
    public static class HeadServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services for head management to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        public static void AddHeadManager(this IServiceCollection services)
        {
            services.AddScoped<HeadManager>();
        }
    }
}
