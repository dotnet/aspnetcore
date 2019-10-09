// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// Calls into user provided 'Configure' methods for configuring a middleware pipeline. The semantics of finding
    /// the 'Configure' methods is similar to the application Startup class.
    /// </summary>
    internal class MiddlewareFilterConfigurationProvider
    {
        public Action<IApplicationBuilder> CreateConfigureDelegate(Type configurationType)
        {
            if (configurationType == null)
            {
                throw new ArgumentNullException(nameof(configurationType));
            }

            if (!HasParameterlessConstructor(configurationType.GetTypeInfo()))
            {
                throw new InvalidOperationException(
                    Resources.FormatMiddlewareFilterConfigurationProvider_CreateConfigureDelegate_CannotCreateType(configurationType, nameof(configurationType)));
            }

            var instance = Activator.CreateInstance(configurationType);
            var configureDelegateBuilder = GetConfigureDelegateBuilder(configurationType);
            return configureDelegateBuilder.Build(instance);
        }

        private static ConfigureBuilder GetConfigureDelegateBuilder(Type startupType)
        {
            var configureMethod = FindMethod(startupType, typeof(void));
            return new ConfigureBuilder(configureMethod);
        }

        private static MethodInfo FindMethod(Type startupType, Type returnType = null)
        {
            var methodName = "Configure";

            var methods = startupType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            var selectedMethods = methods.Where(method => method.Name.Equals(methodName)).ToList();
            if (selectedMethods.Count > 1)
            {
                throw new InvalidOperationException(
                    Resources.FormatMiddewareFilter_ConfigureMethodOverload(methodName));
            }

            var methodInfo = selectedMethods.FirstOrDefault();
            if (methodInfo == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatMiddewareFilter_NoConfigureMethod(
                        methodName,
                        startupType.FullName));
            }

            if (returnType != null && methodInfo.ReturnType != returnType)
            {
                throw new InvalidOperationException(
                    Resources.FormatMiddlewareFilter_InvalidConfigureReturnType(
                        methodInfo.Name,
                        startupType.FullName,
                        returnType.Name));
            }
            return methodInfo;
        }

        private static bool HasParameterlessConstructor(TypeInfo modelTypeInfo)
        {
            return !modelTypeInfo.IsAbstract && modelTypeInfo.GetConstructor(Type.EmptyTypes) != null;
        }

        private class ConfigureBuilder
        {
            public ConfigureBuilder(MethodInfo configure)
            {
                MethodInfo = configure;
            }

            public MethodInfo MethodInfo { get; }

            public Action<IApplicationBuilder> Build(object instance)
            {
                return (applicationBuilder) => Invoke(instance, applicationBuilder);
            }

            private void Invoke(object instance, IApplicationBuilder builder)
            {
                var serviceProvider = builder.ApplicationServices;
                var parameterInfos = MethodInfo.GetParameters();
                var parameters = new object[parameterInfos.Length];
                for (var index = 0; index < parameterInfos.Length; index++)
                {
                    var parameterInfo = parameterInfos[index];
                    if (parameterInfo.ParameterType == typeof(IApplicationBuilder))
                    {
                        parameters[index] = builder;
                    }
                    else
                    {
                        try
                        {
                            parameters[index] = serviceProvider.GetRequiredService(parameterInfo.ParameterType);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(
                                Resources.FormatMiddlewareFilter_ServiceResolutionFail(
                                    parameterInfo.ParameterType.FullName,
                                    parameterInfo.Name,
                                    MethodInfo.Name,
                                    MethodInfo.DeclaringType.FullName),
                                ex);
                        }
                    }
                }
                MethodInfo.Invoke(instance, parameters);
            }
        }
    }
}
