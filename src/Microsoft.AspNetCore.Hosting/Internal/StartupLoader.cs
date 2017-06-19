// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class StartupLoader
    {
        public static StartupMethods LoadMethods(IServiceProvider hostingServiceProvider, Type startupType, string environmentName)
        {
            var configureMethod = FindConfigureDelegate(startupType, environmentName);
            var servicesMethod = FindConfigureServicesDelegate(startupType, environmentName);
            var configureContainerMethod = FindConfigureContainerDelegate(startupType, environmentName);

            object instance = null;
            if (!configureMethod.MethodInfo.IsStatic || (servicesMethod != null && !servicesMethod.MethodInfo.IsStatic))
            {
                instance = ActivatorUtilities.GetServiceOrCreateInstance(hostingServiceProvider, startupType);
            }

            var configureServicesCallback = servicesMethod.Build(instance);
            var configureContainerCallback = configureContainerMethod.Build(instance);

            Func<IServiceCollection, IServiceProvider> configureServices = services =>
            {
                // Call ConfigureServices, if that returned an IServiceProvider, we're done
                IServiceProvider applicationServiceProvider = configureServicesCallback.Invoke(services);

                if (applicationServiceProvider != null)
                {
                    return applicationServiceProvider;
                }

                // If there's a ConfigureContainer method
                if (configureContainerMethod.MethodInfo != null)
                {
                    // We have a ConfigureContainer method, get the IServiceProviderFactory<TContainerBuilder>
                    var serviceProviderFactoryType = typeof(IServiceProviderFactory<>).MakeGenericType(configureContainerMethod.GetContainerType());
                    var serviceProviderFactory = hostingServiceProvider.GetRequiredService(serviceProviderFactoryType);
                    // var builder = serviceProviderFactory.CreateBuilder(services);
                    var builder = serviceProviderFactoryType.GetMethod(nameof(DefaultServiceProviderFactory.CreateBuilder)).Invoke(serviceProviderFactory, new object[] { services });
                    configureContainerCallback.Invoke(builder);
                    // applicationServiceProvider = serviceProviderFactory.CreateServiceProvider(builder);
                    applicationServiceProvider = (IServiceProvider)serviceProviderFactoryType.GetMethod(nameof(DefaultServiceProviderFactory.CreateServiceProvider)).Invoke(serviceProviderFactory, new object[] { builder });
                }
                else
                {
                    // Get the default factory
                    var serviceProviderFactory = hostingServiceProvider.GetRequiredService<IServiceProviderFactory<IServiceCollection>>();

                    // Don't bother calling CreateBuilder since it just returns the default service collection
                    applicationServiceProvider = serviceProviderFactory.CreateServiceProvider(services);
                }

                return applicationServiceProvider ?? services.BuildServiceProvider();
            };

            return new StartupMethods(instance, configureMethod.Build(instance), configureServices);
        }

        public static Type FindStartupType(string startupAssemblyName, string environmentName)
        {
            if (string.IsNullOrEmpty(startupAssemblyName))
            {
                throw new ArgumentException(
                    string.Format("A startup method, startup type or startup assembly is required. If specifying an assembly, '{0}' cannot be null or empty.",
                    nameof(startupAssemblyName)),
                    nameof(startupAssemblyName));
            }

            var assembly = Assembly.Load(new AssemblyName(startupAssemblyName));
            if (assembly == null)
            {
                throw new InvalidOperationException(String.Format("The assembly '{0}' failed to load.", startupAssemblyName));
            }

            var startupNameWithEnv = "Startup" + environmentName;
            var startupNameWithoutEnv = "Startup";

            // Check the most likely places first
            var type =
                assembly.GetType(startupNameWithEnv) ??
                assembly.GetType(startupAssemblyName + "." + startupNameWithEnv) ??
                assembly.GetType(startupNameWithoutEnv) ??
                assembly.GetType(startupAssemblyName + "." + startupNameWithoutEnv);

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
                throw new InvalidOperationException(String.Format("A type named '{0}' or '{1}' could not be found in assembly '{2}'.",
                    startupNameWithEnv,
                    startupNameWithoutEnv,
                    startupAssemblyName));
            }

            return type;
        }

        private static ConfigureBuilder FindConfigureDelegate(Type startupType, string environmentName)
        {
            var configureMethod = FindMethod(startupType, "Configure{0}", environmentName, typeof(void), required: true);
            return new ConfigureBuilder(configureMethod);
        }

        private static ConfigureContainerBuilder FindConfigureContainerDelegate(Type startupType, string environmentName)
        {
            var configureMethod = FindMethod(startupType, "Configure{0}Container", environmentName, typeof(void), required: false);
            return new ConfigureContainerBuilder(configureMethod);
        }

        private static ConfigureServicesBuilder FindConfigureServicesDelegate(Type startupType, string environmentName)
        {
            var servicesMethod = FindMethod(startupType, "Configure{0}Services", environmentName, typeof(IServiceProvider), required: false)
                ?? FindMethod(startupType, "Configure{0}Services", environmentName, typeof(void), required: false);
            return new ConfigureServicesBuilder(servicesMethod);
        }

        private static MethodInfo FindMethod(Type startupType, string methodName, string environmentName, Type returnType = null, bool required = true)
        {
            var methodNameWithEnv = string.Format(CultureInfo.InvariantCulture, methodName, environmentName);
            var methodNameWithNoEnv = string.Format(CultureInfo.InvariantCulture, methodName, "");

            var methods = startupType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            var selectedMethods = methods.Where(method => method.Name.Equals(methodNameWithEnv)).ToList();
            if (selectedMethods.Count > 1)
            {
                throw new InvalidOperationException(string.Format("Having multiple overloads of method '{0}' is not supported.", methodNameWithEnv));
            }
            if (selectedMethods.Count == 0)
            {
                selectedMethods = methods.Where(method => method.Name.Equals(methodNameWithNoEnv)).ToList();
                if (selectedMethods.Count > 1)
                {
                    throw new InvalidOperationException(string.Format("Having multiple overloads of method '{0}' is not supported.", methodNameWithNoEnv));
                }
            }

            var methodInfo = selectedMethods.FirstOrDefault();
            if (methodInfo == null)
            {
                if (required)
                {
                    throw new InvalidOperationException(string.Format("A public method named '{0}' or '{1}' could not be found in the '{2}' type.",
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
                    throw new InvalidOperationException(string.Format("The '{0}' method in the type '{1}' must have a return type of '{2}'.",
                        methodInfo.Name,
                        startupType.FullName,
                        returnType.Name));
                }
                return null;
            }
            return methodInfo;
        }
    }
}