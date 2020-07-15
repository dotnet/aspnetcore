// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// Extension methods for registering head management services.
    /// </summary>
    public static class HeadManagementServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services for head management to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        public static void AddHeadManagement(this IServiceCollection services)
        {
            services.AddScoped<CircuitHandler, HeadManager>();
        }
    }
}
