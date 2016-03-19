// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up MVC services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class MvcServiceCollectionExtensions
    {
        /// <summary>
        /// Adds MVC services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
        public static IMvcBuilder AddMvc(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var builder = services.AddMvcCore();

            builder.AddApiExplorer();
            builder.AddAuthorization();

            // Order added affects options setup order

            // Default framework order
            builder.AddFormatterMappings();
            builder.AddViews();
            builder.AddRazorViewEngine();
            builder.AddCacheTagHelper();

            // +1 order
            builder.AddDataAnnotations(); // +1 order

            // +10 order
            builder.AddJsonFormatters();

            builder.AddCors();

            return new MvcBuilder(builder.Services, builder.PartManager);
        }

        /// <summary>
        /// Adds MVC services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="MvcOptions"/>.</param>
        /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
        public static IMvcBuilder AddMvc(this IServiceCollection services, Action<MvcOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            var builder = services.AddMvc();
            builder.Services.Configure(setupAction);

            return builder;
        }
    }
}
