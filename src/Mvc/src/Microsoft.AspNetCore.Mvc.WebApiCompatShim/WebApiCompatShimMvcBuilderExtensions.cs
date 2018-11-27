// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http.Formatting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WebApiCompatShimMvcBuilderExtensions
    {
        public static IMvcBuilder AddWebApiConventions(this IMvcBuilder builder)
        {
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, WebApiCompatShimOptionsSetup>());
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<WebApiCompatShimOptions>, WebApiCompatShimOptionsSetup>());

            builder.Services.TryAddSingleton<IContentNegotiator, DefaultContentNegotiator>();

            return builder;
        }
    }
}
