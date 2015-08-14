// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http.Formatting;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.WebApiCompatShim;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class WebApiCompatShimMvcBuilderExtensions
    {
        public static IMvcBuilder AddWebApiConventions(this IMvcBuilder builder)
        {
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, WebApiCompatShimOptionsSetup>());
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<WebApiCompatShimOptions>, WebApiCompatShimOptionsSetup>());

            // The constructors on DefaultContentNegotiator aren't DI friendly, so just
            // new it up.
            builder.Services.TryAdd(ServiceDescriptor.Instance<IContentNegotiator>(new DefaultContentNegotiator()));

            return builder;
        }
    }
}
