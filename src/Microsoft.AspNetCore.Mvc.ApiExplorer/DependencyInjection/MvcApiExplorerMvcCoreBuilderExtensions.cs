// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MvcApiExplorerMvcCoreBuilderExtensions
    {
        public static IMvcCoreBuilder AddApiExplorer(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddApiExplorerServices(builder.Services);
            return builder;
        }

        // Internal for testing.
        internal static void AddApiExplorerServices(IServiceCollection services)
        {
            services.TryAddSingleton<IApiDescriptionGroupCollectionProvider, ApiDescriptionGroupCollectionProvider>();
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IApiDescriptionProvider, DefaultApiDescriptionProvider>());
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IApiDescriptionProvider, ApiBehaviorApiDescriptionProvider>());
        }
    }
}
