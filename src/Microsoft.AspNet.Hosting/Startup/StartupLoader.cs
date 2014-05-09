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
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;

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

        public Action<IBuilder> LoadStartup(
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

            var configureMethod1 = "Configure" + environmentName;
            var configureMethod2 = "Configure";
            var methodInfo = type.GetTypeInfo().GetDeclaredMethod(configureMethod1);
            if (methodInfo == null)
            {
                methodInfo = type.GetTypeInfo().GetDeclaredMethod(configureMethod2);
            }
            if (methodInfo == null)
            {
                throw new Exception(string.Format("TODO: {0} or {1} method not found",
                    configureMethod1,
                    configureMethod2));
            }

            if (methodInfo.ReturnType != typeof(void))
            {
                throw new Exception(string.Format("TODO: {0} method isn't void-returning.",
                    methodInfo.Name));
            }

            object instance = null;
            if (!methodInfo.IsStatic)
            {
                instance = ActivatorUtilities.GetServiceOrCreateInstance(_services, type);
            }

            return builder =>
            {
                var parameterInfos = methodInfo.GetParameters();
                var parameters = new object[parameterInfos.Length];
                for (var index = 0; index != parameterInfos.Length; ++index)
                {
                    var parameterInfo = parameterInfos[index];
                    if (parameterInfo.ParameterType == typeof(IBuilder))
                    {
                        parameters[index] = builder;
                    }
                    else
                    {
                        try
                        {
                            parameters[index] = _services.GetService(parameterInfo.ParameterType);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(string.Format(
                                "TODO: Unable to resolve service for startup method {0} {1}",
                                parameterInfo.Name,
                                parameterInfo.ParameterType.FullName));
                        }
                    }
                }
                methodInfo.Invoke(instance, parameters);
            };
        }
    }
}
