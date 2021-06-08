// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer.DependencyInjection
{
    /// <summary>
    /// 
    /// </summary>
    public static class EndpointMethodInfoApiExplorerServiceCollectionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        public static void AddMethodInfoApiExplorerServices(this IServiceCollection services)
        {
            // Try to add default services in case MVC services aren't added.
            services.TryAddSingleton<IActionDescriptorCollectionProvider, DefaultActionDescriptorCollectionProvider>();
            services.TryAddSingleton<IApiDescriptionGroupCollectionProvider, ApiDescriptionGroupCollectionProvider>();

            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IApiDescriptionProvider, EndpointMethodInfoApiDescriptionProvider>());
        }
    }
}
