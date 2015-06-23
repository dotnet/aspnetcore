// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http.Formatting;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.WebApiCompatShim;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class WebApiCompatShimServiceCollectionExtensions
    {
        public static IServiceCollection AddWebApiConventions(this IServiceCollection services)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, WebApiCompatShimOptionsSetup>());
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<WebApiCompatShimOptions>, WebApiCompatShimOptionsSetup>());

            // The constructors on DefaultContentNegotiator aren't DI friendly, so just
            // new it up.
            services.TryAdd(ServiceDescriptor.Instance<IContentNegotiator>(new DefaultContentNegotiator()));

            return services;
        }
    }
}
