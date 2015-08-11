// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    public static class MvcServiceCollectionExtensions
    {
        public static IServiceCollection AddMvc([NotNull] this IServiceCollection services)
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

            return services;
        }

        /// <summary>
        /// Configures a set of <see cref="MvcViewOptions"/> for the application.
        /// </summary>
        /// <param name="services">The services available in the application.</param>
        /// <param name="setupAction">The <see cref="MvcViewOptions"/> which need to be configured.</param>
        public static void ConfigureMvcViews(
            [NotNull] this IServiceCollection services,
            [NotNull] Action<MvcViewOptions> setupAction)
        {
            services.Configure(setupAction);
        }

        /// <summary>
        /// Configures a set of <see cref="MvcJsonOptions"/> for the application.
        /// </summary>
        /// <param name="services">The services available in the application.</param>
        /// <param name="setupAction">The <see cref="MvcJsonOptions"/> which need to be configured.</param>
        public static void ConfigureMvcJson(
            [NotNull] this IServiceCollection services,
            [NotNull] Action<MvcJsonOptions> setupAction)
        {
            services.Configure(setupAction);
        }

        /// <summary>
        /// Register the specified <paramref name="controllerTypes"/> as services and as a source for controller
        /// discovery.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="controllerTypes">A sequence of controller <see cref="Type"/>s to register in the
        /// <paramref name="services"/> and used for controller discovery.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection WithControllersAsServices(
           [NotNull] this IServiceCollection services,
           [NotNull] IEnumerable<Type> controllerTypes)
        {
            var controllerTypeProvider = new FixedSetControllerTypeProvider();
            foreach (var type in controllerTypes)
            {
                services.TryAddTransient(type, type);
                controllerTypeProvider.ControllerTypes.Add(type.GetTypeInfo());
            }

            services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());
            services.Replace(ServiceDescriptor.Instance<IControllerTypeProvider>(controllerTypeProvider));

            return services;
        }

        /// <summary>
        /// Registers controller types from the specified <paramref name="assemblies"/> as services and as a source
        /// for controller discovery.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="controllerAssemblies">Assemblies to scan.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection WithControllersAsServices(
            [NotNull] this IServiceCollection services,
            [NotNull] IEnumerable<Assembly> controllerAssemblies)
        {
            var assemblyProvider = new FixedSetAssemblyProvider();
            foreach (var assembly in controllerAssemblies)
            {
                assemblyProvider.CandidateAssemblies.Add(assembly);
            }

            var controllerTypeProvider = new DefaultControllerTypeProvider(assemblyProvider);
            var controllerTypes = controllerTypeProvider.ControllerTypes;

            return WithControllersAsServices(services, controllerTypes.Select(type => type.AsType()));
        }
    }
}
