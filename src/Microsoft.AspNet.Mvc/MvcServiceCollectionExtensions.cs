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
            builder.AddCors();
            builder.AddDataAnnotations();
            builder.AddFormatterMappings();
            builder.AddJsonFormatters();
            builder.AddViews();
            builder.AddRazorViewEngine();

            if (setupAction != null)
            {
                builder.Services.Configure(setupAction);
            }

            return new MvcBuilder(services);
        }
    }
}
