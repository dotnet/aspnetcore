// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Formatters.Json.Internal;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.OptionsModel;
using Newtonsoft.Json;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MvcJsonMvcCoreBuilderExtensions
    {
        public static IMvcCoreBuilder AddJsonFormatters(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddJsonFormatterServices(builder.Services);
            return builder;
        }

        public static IMvcCoreBuilder AddJsonFormatters(
            this IMvcCoreBuilder builder,
            Action<JsonSerializerSettings> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            AddJsonFormatterServices(builder.Services);

            if (setupAction != null)
            {
                builder.Services.Configure<MvcJsonOptions>((options) => setupAction(options.SerializerSettings));
            }

            return builder;
        }

        // Internal for testing.
        internal static void AddJsonFormatterServices(IServiceCollection services)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, MvcJsonMvcOptionsSetup>());
            services.TryAddSingleton<JsonResultExecutor>();
        }
    }
}
