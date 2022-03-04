// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding Newtonsoft.Json to <see cref="MvcCoreBuilder"/>.
    /// </summary>
    public static class NewtonsoftJsonMvcCoreBuilderExtensions
    {
        /// <summary>
        /// Configures Newtonsoft.Json specific features such as input and output formatters.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder AddNewtonsoftJson(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddServicesCore(builder.Services);
            return builder;
        }

        /// <summary>
        /// Configures Newtonsoft.Json specific features such as input and output formatters.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <param name="setupAction">Callback to configure <see cref="MvcNewtonsoftJsonOptions"/>.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder AddNewtonsoftJson(
            this IMvcCoreBuilder builder,
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

            AddServicesCore(builder.Services);

            builder.Services.Configure(setupAction);

            return builder;
        }

        // Internal for testing.
        internal static void AddServicesCore(IServiceCollection services)
        {
            services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, NewtonsoftJsonMvcOptionsSetup>());
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IApiDescriptionProvider, JsonPatchOperationsArrayProvider>());


            var jsonResultExecutor = services.FirstOrDefault(f =>
               f.ServiceType == typeof(IActionResultExecutor<JsonResult>) &&
               f.ImplementationType?.Assembly == typeof(JsonResult).Assembly);

            if (jsonResultExecutor != null)
            {
                services.Remove(jsonResultExecutor);
            }
            services.TryAddSingleton<IActionResultExecutor<JsonResult>, NewtonsoftJsonResultExecutor>();

            var viewFeaturesAssembly = typeof(IHtmlHelper).Assembly;
            var tempDataSerializer = services.FirstOrDefault(f =>
                f.ServiceType == typeof(TempDataSerializer) &&
                f.ImplementationType?.Assembly == viewFeaturesAssembly);

            if (tempDataSerializer != null)
            {
                // Replace the default implementation of TempDataSerializer
                services.Remove(tempDataSerializer);
            }
            services.TryAddSingleton<TempDataSerializer, BsonTempDataSerializer>();

            //
            // JSON Helper
            //
            var jsonHelper = services.FirstOrDefault(
                f => f.ServiceType == typeof(IJsonHelper) &&
                f.ImplementationType?.Assembly == viewFeaturesAssembly);
            if (jsonHelper != null)
            {
                services.Remove(jsonHelper);
            }

            services.TryAddSingleton<IJsonHelper, NewtonsoftJsonHelper>();
        }
    }
}
