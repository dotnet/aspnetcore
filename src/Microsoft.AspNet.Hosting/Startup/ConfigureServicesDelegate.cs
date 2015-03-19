// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Startup
{
    public delegate IServiceProvider ConfigureServicesDelegate(IServiceCollection services);

    public class ConfigureServicesBuilder
    {
        public ConfigureServicesBuilder(MethodInfo configureServices)
        {
            MethodInfo = configureServices;
        }

        public MethodInfo MethodInfo { get; }

        public ConfigureServicesDelegate Build(object instance)
        {
            return services => Invoke(instance, services);
        }

        private IServiceProvider Invoke(object instance, IServiceCollection exportServices)
        {
            var parameterInfos = MethodInfo.GetParameters();
            var parameters = new object[parameterInfos.Length];
            for (var index = 0; index != parameterInfos.Length; ++index)
            {
                var parameterInfo = parameterInfos[index];
                if (exportServices != null && parameterInfo.ParameterType == typeof(IServiceCollection))
                {
                    parameters[index] = exportServices;
                }
            }

            // REVIEW: We null ref if exportServices is null, cuz it should not be null
            return MethodInfo.Invoke(instance, parameters) as IServiceProvider ?? exportServices.BuildServiceProvider();
        }
    }
}