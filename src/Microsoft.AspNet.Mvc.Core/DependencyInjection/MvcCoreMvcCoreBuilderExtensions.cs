// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MvcCoreMvcCoreBuilderExtensions
    {
        /// <summary>
        /// Registers an action to configure <see cref="MvcOptions"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <param name="setupAction">An <see cref="Action{MvcOptions}"/>.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder AddMvcOptions(
            this IMvcCoreBuilder builder,
            Action<MvcOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            builder.Services.Configure<MvcOptions>(setupAction);
            return builder;
        }

        public static IMvcCoreBuilder AddFormatterMappings(this IMvcCoreBuilder builder)
        {
            AddFormatterMappingsServices(builder.Services);
            return builder;
        }

        public static IMvcCoreBuilder AddFormatterMappings(
            this IMvcCoreBuilder builder,
            Action<FormatterMappings> setupAction)
        {
            AddFormatterMappingsServices(builder.Services);

            if (setupAction != null)
            {
                builder.Services.Configure<MvcOptions>((options) => setupAction(options.FormatterMappings));
            }

            return builder;
        }

        // Internal for testing.
        internal static void AddFormatterMappingsServices(IServiceCollection services)
        {
            services.TryAddSingleton<FormatFilter, FormatFilter>();
        }

        public static IMvcCoreBuilder AddAuthorization(this IMvcCoreBuilder builder)
        {
            AddAuthorizationServices(builder.Services);
            return builder;
        }

        public static IMvcCoreBuilder AddAuthorization(
            this IMvcCoreBuilder builder,
            Action<AuthorizationOptions> setupAction)
        {
            AddAuthorizationServices(builder.Services);

            if (setupAction != null)
            {
                builder.Services.Configure(setupAction);
            }

            return builder;
        }

        // Internal for testing.
        internal static void AddAuthorizationServices(IServiceCollection services)
        {
            services.AddAuthorization();

            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IApplicationModelProvider, AuthorizationApplicationModelProvider>());
        }

        /// <summary>
        /// Register the specified <paramref name="controllerTypes"/> as services and as a source for controller
        /// discovery.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <param name="controllerTypes">A sequence of controller <see cref="Type"/>s to register in the
        /// <paramref name="services"/> and used for controller discovery.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder AddControllersAsServices(
           this IMvcCoreBuilder builder,
           IEnumerable<Type> controllerTypes)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (controllerTypes == null)
            {
                throw new ArgumentNullException(nameof(controllerTypes));
            }

            ControllersAsServices.AddControllersAsServices(builder.Services, controllerTypes);
            return builder;
        }

        /// <summary>
        /// Registers controller types from the specified <paramref name="assemblies"/> as services and as a source
        /// for controller discovery.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <param name="controllerAssemblies">Assemblies to scan.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder AddControllersAsServices(
            this IMvcCoreBuilder builder,
            IEnumerable<Assembly> controllerAssemblies)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (controllerAssemblies == null)
            {
                throw new ArgumentNullException(nameof(controllerAssemblies));
            }

            ControllersAsServices.AddControllersAsServices(builder.Services, controllerAssemblies);
            return builder;
        }
    }
}
