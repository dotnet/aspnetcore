// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Startup
{
    public class ConfigureServicesBuilder
    {
        public ConfigureServicesBuilder(MethodInfo configureServices)
        {
            if (configureServices == null)
            {
                throw new ArgumentNullException(nameof(configureServices));
            }

            // Only support IServiceCollection parameters
            var parameters = configureServices.GetParameters();
            if (parameters.Length > 1 ||
                parameters.Any(p => p.ParameterType != typeof(IServiceCollection)))
            {
                throw new InvalidOperationException("ConfigureServices can take at most a single IServiceCollection parameter.");
            }

            MethodInfo = configureServices;
        }

        public MethodInfo MethodInfo { get; }

        public Func<IServiceCollection, IServiceProvider> Build(object instance) => services => Invoke(instance, services);

        private IServiceProvider Invoke(object instance, IServiceCollection exportServices)
        {
            if (exportServices == null)
            {
                throw new ArgumentNullException(nameof(exportServices));
            }

            var parameters = new object[MethodInfo.GetParameters().Length];

            // Ctor ensures we have at most one IServiceCollection parameter
            if (parameters.Length > 0)
            {
                parameters[0] = exportServices;
            }

            return MethodInfo.Invoke(instance, parameters) as IServiceProvider ?? exportServices.BuildServiceProvider();
        }
    }
}