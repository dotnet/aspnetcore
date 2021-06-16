// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for configuring ApiExplorer using <see cref="Endpoint.Metadata"/>.
    /// </summary>
    public static class EndpointMetadataApiExplorerServiceCollectionExtensions
    {
        /// <summary>
        /// Configures ApiExplorer using <see cref="Endpoint.Metadata"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        public static void AddEndpointsApiExplorer(this IServiceCollection services)
        {
            // Try to add default services in case MVC services aren't added.
            services.TryAddSingleton<IActionDescriptorCollectionProvider, DefaultActionDescriptorCollectionProvider>();
            services.TryAddSingleton<IApiDescriptionGroupCollectionProvider, ApiDescriptionGroupCollectionProvider>();

            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IApiDescriptionProvider, EndpointMetadataApiDescriptionProvider>());
        }
    }
}
