// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for configuring MVC using an <see cref="IMvcBuilder"/>.
    /// </summary>
    public static class MvcCoreMvcBuilderExtensions
    {
        /// <summary>
        /// Registers an action to configure <see cref="MvcOptions"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="setupAction">An <see cref="Action{MvcOptions}"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddMvcOptions(
            this IMvcBuilder builder,
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

        public static IMvcBuilder AddFormatterMappings(
            this IMvcBuilder builder,
            Action<FormatterMappings> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            builder.Services.Configure<MvcOptions>((options) => setupAction(options.FormatterMappings));
            return builder;
        }

        /// <summary>
        /// Adds an <see cref="ApplicationPart"/> to the list of <see cref="ApplicationPartManager.ApplicationParts"/> on the
        /// <see cref="IMvcBuilder.PartManager"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="assembly">The <see cref="Assembly"/> of the <see cref="ApplicationPart"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddApplicationPart(this IMvcBuilder builder, Assembly assembly)
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
        /// Configures the <see cref="ApplicationPartManager"/> of the <see cref="IMvcBuilder.PartManager"/> using
        /// the given <see cref="Action{ApplicationPartManager}"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="setupAction">The <see cref="Action{ApplicationPartManager}"/></param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder ConfigureApplicationPartManager(
            this IMvcBuilder builder,
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
        /// Register the specified <paramref name="controllerTypes"/> as services and as a source for controller
        /// discovery.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="controllerTypes">A sequence of controller <see cref="Type"/>s to register.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddControllersAsServices(
           this IMvcBuilder builder,
           params Type[] controllerTypes)
        {
            return builder.AddControllersAsServices(controllerTypes.AsEnumerable());
        }

        /// <summary>
        /// Register the specified <paramref name="controllerTypes"/> as services and as a source for controller
        /// discovery.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="controllerTypes">A sequence of controller <see cref="Type"/>s to register.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddControllersAsServices(
           this IMvcBuilder builder,
           IEnumerable<Type> controllerTypes)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            ControllersAsServices.AddControllersAsServices(builder.Services, controllerTypes);
            return builder;
        }

        /// <summary>
        /// Registers controller types from the specified <paramref name="controllerAssemblies"/> as services and as a source
        /// for controller discovery.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="controllerAssemblies">Assemblies to scan.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddControllersAsServices(
            this IMvcBuilder builder,
            params Assembly[] controllerAssemblies)
        {
            return builder.AddControllersAsServices(controllerAssemblies.AsEnumerable());
        }

        /// <summary>
        /// Registers controller types from the specified <paramref name="controllerAssemblies"/> as services and as a source
        /// for controller discovery.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="controllerAssemblies">Assemblies to scan.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddControllersAsServices(
            this IMvcBuilder builder,
            IEnumerable<Assembly> controllerAssemblies)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            ControllersAsServices.AddControllersAsServices(builder.Services, controllerAssemblies);
            return builder;
        }
    }
}
