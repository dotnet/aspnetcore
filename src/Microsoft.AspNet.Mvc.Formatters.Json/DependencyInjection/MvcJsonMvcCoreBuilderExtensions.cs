// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;
using Newtonsoft.Json;

namespace Microsoft.Framework.DependencyInjection
{
    public static class MvcJsonMvcCoreBuilderExtensions
    {
        public static IMvcCoreBuilder AddJsonFormatters([NotNull] this IMvcCoreBuilder builder)
        {
            AddJsonFormatterServices(builder.Services);
            return builder;
        }

        public static IMvcCoreBuilder AddJsonFormatters(
            [NotNull] this IMvcCoreBuilder builder,
            [NotNull] Action<JsonSerializerSettings> setupAction)
        {
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
        }
    }
}
