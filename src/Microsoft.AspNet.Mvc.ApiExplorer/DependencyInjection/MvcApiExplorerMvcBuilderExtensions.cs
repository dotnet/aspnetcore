// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ApiExplorer;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    public static class MvcApiExplorerMvcBuilderExtensions
    {
        public static IMvcBuilder AddApiExplorer([NotNull] this IMvcBuilder builder)
        {
            AddApiExplorerServices(builder.Services);
            return builder;
        }

        // Internal for testing.
        internal static void AddApiExplorerServices(IServiceCollection services)
        {
            services.TryAddSingleton<IApiDescriptionGroupCollectionProvider, ApiDescriptionGroupCollectionProvider>();
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IApiDescriptionProvider, DefaultApiDescriptionProvider>());
        }
    }
}
