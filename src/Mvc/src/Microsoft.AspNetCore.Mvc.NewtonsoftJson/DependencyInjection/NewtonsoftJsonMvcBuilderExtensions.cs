// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions methods for configuring MVC via an <see cref="IMvcBuilder"/>.
    /// </summary>
    public static class NewtonsoftJsonMvcBuilderExtensions
    {
        /// <summary>
        /// Configures Newtonsoft.Json specific features such as input and output formatters.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddNewtonsoftJson(this IMvcBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            NewtonsoftJsonMvcCoreBuilderExtensions.AddServicesCore(builder.Services);
            return builder;
        }

        /// <summary>
        /// Configures Newtonsoft.Json specific features such as input and output formatters.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="setupAction">Callback to configure <see cref="MvcNewtonsoftJsonOptions"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddNewtonsoftJson(
            this IMvcBuilder builder,
            Action<MvcNewtonsoftJsonOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            NewtonsoftJsonMvcCoreBuilderExtensions.AddServicesCore(builder.Services);
            builder.Services.Configure(setupAction);

            return builder;
        }
    }
}
