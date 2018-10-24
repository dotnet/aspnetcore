// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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

            builder.Services.Configure(setupAction);
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
            services.AddAuthenticationCore();
            services.AddAuthorization();
            services.AddAuthorizationPolicyEvaluator();

            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IApplicationModelProvider, AuthorizationApplicationModelProvider>());
        }

        /// <summary>
        /// Registers discovered controllers as services in the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder AddControllersAsServices(this IMvcCoreBuilder builder)
        {
            var feature = new ControllerFeature();
            builder.PartManager.PopulateFeature(feature);

            foreach (var controller in feature.Controllers.Select(c => c.AsType()))
            {
                builder.Services.TryAddTransient(controller, controller);
            }

            builder.Services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());

            return builder;
        }

        /// <summary>
        /// Adds an <see cref="ApplicationPart"/> to the list of <see cref="ApplicationPartManager.ApplicationParts"/> on the
        /// <see cref="IMvcCoreBuilder.PartManager"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <param name="assembly">The <see cref="Assembly"/> of the <see cref="ApplicationPart"/>.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder AddApplicationPart(this IMvcCoreBuilder builder, Assembly assembly)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            builder.ConfigureApplicationPartManager(manager => manager.ApplicationParts.Add(new AssemblyPart(assembly)));

            return builder;
        }

        /// <summary>
        /// Configures the <see cref="ApplicationPartManager"/> of the <see cref="IMvcCoreBuilder.PartManager"/> using
        /// the given <see cref="Action{ApplicationPartManager}"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <param name="setupAction">The <see cref="Action{ApplicationPartManager}"/></param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder ConfigureApplicationPartManager(
            this IMvcCoreBuilder builder,
            Action<ApplicationPartManager> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            setupAction(builder.PartManager);

            return builder;
        }

        /// <summary>
        /// Configures <see cref="ApiBehaviorOptions"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <param name="setupAction">The configure action.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder ConfigureApiBehaviorOptions(
            this IMvcCoreBuilder builder,
            Action<ApiBehaviorOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            builder.Services.Configure(setupAction);

            return builder;
        }

        /// <summary>
        /// Sets the <see cref="CompatibilityVersion"/> for ASP.NET Core MVC for the application.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <param name="version">The <see cref="CompatibilityVersion"/> value to configure.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder SetCompatibilityVersion(this IMvcCoreBuilder builder, CompatibilityVersion version)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.Configure<MvcCompatibilityOptions>(o => o.CompatibilityVersion = version);
            return builder;
        }
    }
}
