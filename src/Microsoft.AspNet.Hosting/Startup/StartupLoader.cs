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
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.AspNet.Builder;

namespace Microsoft.AspNet.Hosting.Startup
{
    public class StartupLoader : IStartupLoader
    {
        private readonly IServiceProvider _services;
        private readonly IStartupLoader _next;

        public StartupLoader(IServiceProvider services, IStartupLoader next)
        {
            _services = services;
            _next = next;
        }

        public Action<IBuilder> LoadStartup(string applicationName, IList<string> diagnosticMessages)
        {
            if (String.IsNullOrEmpty(applicationName))
            {
                return _next.LoadStartup(applicationName, diagnosticMessages);
            }

            var nameParts = Utilities.SplitTypeName(applicationName);
            string typeName = nameParts.Item1;
            string assemblyName = nameParts.Item2;

            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            if (assembly == null)
            {
                throw new Exception(String.Format("TODO: assembly {0} failed to load message", assemblyName));
            }

            Type type = null;
            if (string.IsNullOrEmpty(typeName))
            {
                typeName = "Startup";
            }

            // Check the most likely places first
            type = assembly.GetType(typeName) ?? assembly.GetType(assembly.GetName().Name + "." + typeName);

            if (type == null)
            {
                // Full scan
                var typeInfo = assembly.DefinedTypes.FirstOrDefault(aType => aType.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
                if (typeInfo != null)
                {
                    type = typeInfo.AsType();
                }
            }

            if (type == null)
            {
                throw new Exception(String.Format("TODO: type {0} failed to load message", typeName));
            }

            var methodInfo = type.GetTypeInfo().GetDeclaredMethod("Configuration");
            if (methodInfo == null)
            {
                throw new Exception("TODO: Configuration method not found");
            }

            if (methodInfo.ReturnType != typeof(void))
            {
                throw new Exception("TODO: Configuration method isn't void-returning.");
            }

            object instance = null;
            if (!methodInfo.IsStatic)
            {
                instance = ActivatorUtilities.GetServiceOrCreateInstance(_services, type);
            }
            return builder => methodInfo.Invoke(instance, new object[] { builder });
        }
    }
}
