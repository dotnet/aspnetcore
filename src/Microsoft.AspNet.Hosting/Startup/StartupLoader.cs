// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Hosting.Startup
{
    public class StartupLoader : IStartupLoader
    {
        private readonly IServiceProvider _services;
        private readonly IStartupLoader _next;

        public StartupLoader(
            IServiceProvider services,
            IStartupLoader next)
        {
            _services = services;
            _next = next;
        }

        private MethodInfo FindMethod(Type startupType, string methodName, string environmentName, Type returnType = null, bool required = true)
        {
            var methodNameWithEnv = methodName + environmentName;
            var methodInfo = startupType.GetTypeInfo().GetDeclaredMethod(methodNameWithEnv)
                ?? startupType.GetTypeInfo().GetDeclaredMethod(methodName);
            if (methodInfo == null)
            {
                if (required)
                {
                    throw new Exception(string.Format("TODO: {0} or {1} method not found",
                        methodNameWithEnv,
                        methodName));

                }
                return null;
            }

            if (returnType != null && methodInfo.ReturnType != returnType)
            {
                throw new Exception(string.Format("TODO: {0} method does not return " + returnType.Name,
                    methodInfo.Name));
            }

            return methodInfo;
        }

        private void Invoke(MethodInfo methodInfo, object instance, IApplicationBuilder builder, IServiceCollection services = null)
        {
            var parameterInfos = methodInfo.GetParameters();
            var parameters = new object[parameterInfos.Length];
            for (var index = 0; index != parameterInfos.Length; ++index)
            {
                var parameterInfo = parameterInfos[index];
                if (parameterInfo.ParameterType == typeof(IApplicationBuilder))
                {
                    parameters[index] = builder;
                }
                else if (services != null && parameterInfo.ParameterType == typeof(IServiceCollection))
                {
                    parameters[index] = services;
                }
                else
                {
                    try
                    {
                        parameters[index] = _services.GetService(parameterInfo.ParameterType);
                    }
                    catch (Exception)
                    {
                        throw new Exception(string.Format(
                            "TODO: Unable to resolve service for {0} method {1} {2}",
                            methodInfo.Name,
                            parameterInfo.Name,
                            parameterInfo.ParameterType.FullName));
                    }
                }
            }
            methodInfo.Invoke(instance, parameters);
        }

        public Action<IApplicationBuilder> LoadStartup(
            string applicationName,
            string environmentName,
            IList<string> diagnosticMessages)
        {
            if (String.IsNullOrEmpty(applicationName))
            {
                return _next.LoadStartup(applicationName, environmentName, diagnosticMessages);
            }

            var assembly = Assembly.Load(new AssemblyName(applicationName));
            if (assembly == null)
            {
                throw new Exception(String.Format("TODO: assembly {0} failed to load message", applicationName));
            }

            var startupName1 = "Startup" + environmentName;
            var startupName2 = "Startup";

            // Check the most likely places first
            var type =
                assembly.GetType(startupName1) ??
                assembly.GetType(applicationName + "." + startupName1) ??
                assembly.GetType(startupName2) ??
                assembly.GetType(applicationName + "." + startupName2);

            if (type == null)
            {
                // Full scan
                var definedTypes = assembly.DefinedTypes.ToList();

                var startupType1 = definedTypes.Where(info => info.Name.Equals(startupName1, StringComparison.Ordinal));
                var startupType2 = definedTypes.Where(info => info.Name.Equals(startupName2, StringComparison.Ordinal));

                var typeInfo = startupType1.Concat(startupType2).FirstOrDefault();
                if (typeInfo != null)
                {
                    type = typeInfo.AsType();
                }
            }

            if (type == null)
            {
                throw new Exception(String.Format("TODO: {0} or {1} class not found in assembly {2}",
                    startupName1,
                    startupName2,
                    applicationName));
            }

            var configureMethod = FindMethod(type, "Configure", environmentName, typeof(void), required: true);
            // TODO: accept IServiceProvider method as well?
            var servicesMethod = FindMethod(type, "ConfigureServices", environmentName, typeof(void), required: false);

            object instance = null;
            if (!configureMethod.IsStatic || (servicesMethod != null && !servicesMethod.IsStatic))
            {
                instance = ActivatorUtilities.GetServiceOrCreateInstance(_services, type);
            }
            return builder =>
            {
                if (servicesMethod != null)
                {
                    var services = new ServiceCollection();
                    services.Add(OptionsServices.GetDefaultServices());
                    Invoke(servicesMethod, instance, builder, services);
                    if (builder != null)
                    {
                        builder.ApplicationServices = services.BuildServiceProvider(builder.ApplicationServices);
                    }
                }
                Invoke(configureMethod, instance, builder);
            };
        }
    }
}
