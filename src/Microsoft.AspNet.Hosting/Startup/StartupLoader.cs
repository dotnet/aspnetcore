// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Startup
{
    public class StartupLoader : IStartupLoader
    {
        private readonly IServiceProvider _services;

        public StartupLoader(IServiceProvider services)
        {
            _services = services;
        }

        private MethodInfo FindMethod(Type startupType, string methodName, string environmentName, Type returnType = null, bool required = true)
        {
            var methodNameWithEnv = string.Format(CultureInfo.InvariantCulture, methodName, environmentName);
            var methodNameWithNoEnv = string.Format(CultureInfo.InvariantCulture, methodName, "");
            var methodInfo = startupType.GetMethod(methodNameWithEnv, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                ?? startupType.GetMethod(methodNameWithNoEnv, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            if (methodInfo == null)
            {
                if (required)
                {
                    throw new Exception(string.Format("A method named '{0}' or '{1}' in the type '{2}' could not be found.",
                        methodNameWithEnv,
                        methodNameWithNoEnv,
                        startupType.FullName));

                }
                return null;
            }
            if (returnType != null && methodInfo.ReturnType != returnType)
            {
                if (required)
                {
                    throw new Exception(string.Format("The '{0}' method in the type '{1}' must have a return type of '{2}'.",
                        methodInfo.Name,
                        startupType.FullName,
                        returnType.Name));
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
                            "Could not resolve a service of type '{0}' for the parameter '{1}' of method '{2}' on type '{3}'.",
                            parameterInfo.ParameterType.FullName,
                            parameterInfo.Name,
                            methodInfo.Name,
                            methodInfo.DeclaringType.FullName));
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
            if (string.IsNullOrEmpty(applicationName))
            {
                throw new ArgumentException("Value cannot be null or empty.", "applicationName");
            }

            var assembly = Assembly.Load(new AssemblyName(applicationName));
            if (assembly == null)
            {
                throw new Exception(String.Format("The assembly '{0}' failed to load.", applicationName));
            }

            var startupNameWithEnv = "Startup" + environmentName;
            var startupNameWithoutEnv = "Startup";

            // Check the most likely places first
            var type =
                assembly.GetType(startupNameWithEnv) ??
                assembly.GetType(applicationName + "." + startupNameWithEnv) ??
                assembly.GetType(startupNameWithoutEnv) ??
                assembly.GetType(applicationName + "." + startupNameWithoutEnv);

            if (type == null)
            {
                // Full scan
                var definedTypes = assembly.DefinedTypes.ToList();

                var startupType1 = definedTypes.Where(info => info.Name.Equals(startupNameWithEnv, StringComparison.Ordinal));
                var startupType2 = definedTypes.Where(info => info.Name.Equals(startupNameWithoutEnv, StringComparison.Ordinal));

                var typeInfo = startupType1.Concat(startupType2).FirstOrDefault();
                if (typeInfo != null)
                {
                    type = typeInfo.AsType();
                }
            }

            if (type == null)
            {
                throw new Exception(String.Format("A type named '{0}' or '{1}' could not be found in assembly '{2}'.",
                    startupNameWithEnv,
                    startupNameWithoutEnv,
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
