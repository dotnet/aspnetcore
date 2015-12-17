// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.Controllers;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNet.Mvc.Internal
{
    public static class ControllersAsServices
    {
        public static void AddControllersAsServices(IServiceCollection services, IEnumerable<Type> types)
        {
            var controllerTypeProvider = new StaticControllerTypeProvider();
            foreach (var type in types)
            {
                services.TryAddTransient(type, type);
                controllerTypeProvider.ControllerTypes.Add(type.GetTypeInfo());
            }

            services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());
            services.Replace(ServiceDescriptor.Singleton<IControllerTypeProvider>(controllerTypeProvider));
        }

        public static void AddControllersAsServices(IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            var assemblyProvider = new StaticAssemblyProvider();
            foreach (var assembly in assemblies)
            {
                assemblyProvider.CandidateAssemblies.Add(assembly);
            }

            var controllerTypeProvider = new DefaultControllerTypeProvider(assemblyProvider);
            var controllerTypes = controllerTypeProvider.ControllerTypes;

            AddControllersAsServices(services, controllerTypes.Select(type => type.AsType()));
        }
    }
}
