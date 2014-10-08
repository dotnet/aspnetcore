// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.WebApiCompatShim;

namespace Microsoft.Framework.DependencyInjection
{
    public static class WebApiCompatShimServiceCollectionExtensions
    {
        public static IServiceCollection AddWebApiConventions(this IServiceCollection services)
        {
            services.AddOptionsAction<WebApiCompatShimOptionsSetup>();
            return services;
        }
    }
}
