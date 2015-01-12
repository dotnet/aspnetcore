// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;

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
            var methodNameWithEnv = string.Format(CultureInfo.InvariantCulture, methodName, environmentName);
            var methodNameWithNoEnv = string.Format(CultureInfo.InvariantCulture, methodName, "");
            var methodInfo = startupType.GetTypeInfo().GetDeclaredMethod(methodNameWithEnv)
                ?? startupType.GetTypeInfo().GetDeclaredMethod(methodNameWithNoEnv);
            if (methodInfo == null)
            {
                if (required)
                {
                    throw new Exception(string.Format("TODO: {0} or {1} method not found",
                        methodNameWithEnv,
                        methodNameWithNoEnv));

                }
                return null;
            }
            if (returnType != null && methodInfo.ReturnType != returnType)
            {
                if (required)
                {
                    throw new Exception(string.Format("TODO: {0} method does not return " + returnType.Name,
                        methodInfo.Name));
                }
                return null;
            }
            return methodInfo;
        }

        private object Invoke(MethodInfo methodInfo, object instance, IApplicationBuilder builder, IServiceCollection services = null)
        {
            var serviceProvider = builder.ApplicationServices ?? _services;
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
                        parameters[index] = serviceProvider.GetRequiredService(parameterInfo.ParameterType);
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
            return methodInfo.Invoke(instance, parameters);
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

            var configureMethod = FindMethod(type, "Configure{0}", environmentName, typeof(void), required: true);
            var servicesMethod = FindMethod(type, "Configure{0}Services", environmentName, typeof(IServiceProvider), required: false)
                ?? FindMethod(type, "Configure{0}Services", environmentName, typeof(void), required: false);

            object instance = null;
            if (!configureMethod.IsStatic || (servicesMethod != null && !servicesMethod.IsStatic))
            {
                instance = ActivatorUtilities.GetServiceOrCreateInstance(_services, type);
            }
            return builder =>
            {
                if (servicesMethod != null)
                {
                    var services = HostingServices.Create(builder.ApplicationServices);
                    if (servicesMethod.ReturnType == typeof(IServiceProvider))
                    {
                        // IServiceProvider ConfigureServices(IServiceCollection)
                        builder.ApplicationServices = (Invoke(servicesMethod, instance, builder, services) as IServiceProvider)
                            ?? builder.ApplicationServices; 
                    }
                    else
                    {
                        // void ConfigureServices(IServiceCollection)
                        Invoke(servicesMethod, instance, builder, services);
                        if (builder != null)
                        {
                            builder.ApplicationServices = services.BuildServiceProvider();
                        }
                    }
                }
                Invoke(configureMethod, instance, builder);
            };
        }
    }
}
