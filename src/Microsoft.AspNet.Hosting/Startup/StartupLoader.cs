// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
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

        public StartupMethods Load(
            Type startupType,
            string environmentName,
            IList<string> diagnosticMessages)
        {
            var configureMethod = FindConfigureDelegate(startupType, environmentName);
            var servicesMethod = FindConfigureServicesDelegate(startupType, environmentName);

            object instance = null;
            if (!configureMethod.MethodInfo.IsStatic || (servicesMethod != null && !servicesMethod.MethodInfo.IsStatic))
            {
                instance = ActivatorUtilities.GetServiceOrCreateInstance(_services, startupType);
            }

            return new StartupMethods(configureMethod.Build(instance), servicesMethod?.Build(instance));
        }

        public StartupMethods Load(
            string startupAssemblyName,
            string environmentName,
            IList<string> diagnosticMessages)
        {
            if (string.IsNullOrEmpty(startupAssemblyName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(startupAssemblyName));
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

            return Load(type, environmentName, diagnosticMessages);
        }

        private static ConfigureBuilder FindConfigureDelegate(Type startupType, string environmentName)
        {
            var configureMethod = FindMethod(startupType, "Configure{0}", environmentName, typeof(void), required: true);
            return new ConfigureBuilder(configureMethod);
        }

        private static ConfigureServicesBuilder FindConfigureServicesDelegate(Type startupType, string environmentName)
        {
            var servicesMethod = FindMethod(startupType, "Configure{0}Services", environmentName, typeof(IServiceProvider), required: false)
                ?? FindMethod(startupType, "Configure{0}Services", environmentName, typeof(void), required: false);
            return servicesMethod == null ? null : new ConfigureServicesBuilder(servicesMethod);
        }

        private static MethodInfo FindMethod(Type startupType, string methodName, string environmentName, Type returnType = null, bool required = true)
        {
            var methodNameWithEnv = string.Format(CultureInfo.InvariantCulture, methodName, environmentName);
            var methodNameWithNoEnv = string.Format(CultureInfo.InvariantCulture, methodName, "");
            var methodInfo = startupType.GetMethod(methodNameWithEnv, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                ?? startupType.GetMethod(methodNameWithNoEnv, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            if (methodInfo == null)
            {
                if (required)
                {
                    throw new InvalidOperationException(string.Format("A method named '{0}' or '{1}' in the type '{2}' could not be found.",
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