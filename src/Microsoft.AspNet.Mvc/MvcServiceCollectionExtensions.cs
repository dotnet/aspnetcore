// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Internal;

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
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IMvcBuilder AddMvc(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return AddMvc(services, setupAction: null);
        }

        /// <summary>
        /// Adds MVC services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">An action delegate to configure the provided <see cref="MvcOptions"/>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IMvcBuilder AddMvc(this IServiceCollection services, Action<MvcOptions> setupAction)
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
