// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class ConfigureServicesBuilder
    {
        public ConfigureServicesBuilder(MethodInfo configureServices)
        {
            MethodInfo = configureServices;
        }

        public MethodInfo MethodInfo { get; }

        public Func<Func<IServiceCollection, IServiceProvider>, Func<IServiceCollection, IServiceProvider>> StartupServiceFilters { get; set; } = f => f;

        public Func<IServiceCollection, IServiceProvider> Build(object instance) => services => Invoke(instance, services);

        private IServiceProvider Invoke(object instance, IServiceCollection services)
        {
            return StartupServiceFilters(Startup)(services);

            IServiceProvider Startup(IServiceCollection serviceCollection) => InvokeCore(instance, serviceCollection);
        }

        private IServiceProvider InvokeCore(object instance, IServiceCollection services)
        {
            if (MethodInfo == null)
            {
                return null;
            }

            // Only support IServiceCollection parameters
            var parameters = MethodInfo.GetParameters();
            if (parameters.Length > 1 ||
                parameters.Any(p => p.ParameterType != typeof(IServiceCollection)))
            {
                throw new InvalidOperationException("The ConfigureServices method must either be parameterless or take only one parameter of type IServiceCollection.");
            }

            // Create a delegate for the known types so that we can avoid target invocation exceptions
            if (MethodInfo.ReturnType == typeof(IServiceProvider))
            {
                if (parameters.Length == 0)
                {
                    return MethodInfo.CreateDelegate<Func<IServiceProvider>>(instance).Invoke();
                }

                return MethodInfo.CreateDelegate<Func<IServiceCollection, IServiceProvider>>(instance).Invoke(services);
            }
            else if (MethodInfo.ReturnType == typeof(void))
            {
                if (parameters.Length == 0)
                {
                    MethodInfo.CreateDelegate<Action>(instance).Invoke();
                    return null;
                }

                MethodInfo.CreateDelegate<Action<IServiceCollection>>(instance).Invoke(services);
                return null;
            }

            var arguments = new object[parameters.Length];

            if (parameters.Length > 0)
            {
                arguments[0] = services;
            }

            // Not sure what this return type would have been but, just delegate to a late bound invoke
            return MethodInfo.Invoke(instance, arguments) as IServiceProvider;
        }
    }
}
