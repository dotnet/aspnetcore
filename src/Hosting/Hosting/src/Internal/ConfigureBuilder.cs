// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class ConfigureBuilder
    {
        public ConfigureBuilder(MethodInfo configure)
        {
            MethodInfo = configure;
        }

        public MethodInfo MethodInfo { get; }

        public Action<IApplicationBuilder> Build(object instance) => builder => Invoke(instance, builder);

        private void Invoke(object instance, IApplicationBuilder builder)
        {
            // Create a scope for Configure, this allows creating scoped dependencies
            // without the hassle of manually creating a scope.
            using (var scope = builder.ApplicationServices.CreateScope())
            {
                var instanceArg = Expression.Parameter(typeof(object));
                var argsArray = Expression.Parameter(typeof(object[]));

                var serviceProvider = scope.ServiceProvider;
                var parameterInfos = MethodInfo.GetParameters();
                var parameters = new object[parameterInfos.Length];
                var arguments = new Expression[parameterInfos.Length];

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
                            throw new Exception(string.Format(
                                "Could not resolve a service of type '{0}' for the parameter '{1}' of method '{2}' on type '{3}'.",
                                parameterInfo.ParameterType.FullName,
                                parameterInfo.Name,
                                MethodInfo.Name,
                                MethodInfo.DeclaringType.FullName), ex);
                        }
                    }

                    // (ParameterType)args[index]
                    arguments[index] = Expression.Convert(Expression.ArrayIndex(argsArray, Expression.Constant(index)), parameterInfo.ParameterType);
                }

                // We're going to build a dynamic method to invoke the Configure method
                // void Configure(object instance, object[] args)
                // {
                //     ((Startup)instance).Configure((ParameterType)args[1..n])
                // }

                // ((Startup)instance)
                var instanceExpr = Expression.Convert(instanceArg, instance.GetType());

                // (Startup)instance).Configure(...)
                var bodyExpr = Expression.Call(instanceExpr, MethodInfo, arguments);
                var invoker = Expression.Lambda<Action<object, object[]>>(bodyExpr, instanceArg, argsArray).Compile();
                invoker.Invoke(instance, parameters);
            }
        }
    }
}
