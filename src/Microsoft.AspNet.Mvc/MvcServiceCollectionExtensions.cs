// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    public static class MvcServiceCollectionExtensions
    {
        public static IMvcBuilder AddMvc([NotNull] this IServiceCollection services)
        {
            return AddMvc(services, setupAction: null);
        }

        public static IMvcBuilder AddMvc([NotNull] this IServiceCollection services, Action<MvcOptions> setupAction)
        {
            var builder = services.AddMvcCore();

            builder.AddApiExplorer();
            builder.AddAuthorization();

            // Order added affects options setup order

            // Default framework order
            builder.AddFormatterMappings();
            builder.AddViews();
            builder.AddRazorViewEngine();

            // +1 order
            builder.AddDataAnnotations(); // +1 order

            // +10 order
            builder.AddJsonFormatters();

            builder.AddCors();

            if (setupAction != null)
            {
                builder.Services.Configure(setupAction);
            }
            return new MvcBuilder(services);
        }
    }
}
