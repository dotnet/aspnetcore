using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;

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

            string typeName;
            string assemblyName;
            var parts = applicationName.Split(new[] { ',' }, 2);
            if (parts.Length == 1)
            {
                typeName = null;
                assemblyName = applicationName;
            }
            else if (parts.Length == 2)
            {
                typeName = parts[0];
                assemblyName = parts[1];
            }
            else
            {
                throw new Exception("TODO: Unrecognized format");
            }

            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            if (assembly == null)
            {
                throw new Exception(String.Format("TODO: assembly {0} failed to load message", assemblyName));
            }

            Type type = null;
            if (string.IsNullOrWhiteSpace(typeName))
            {
                var typeInfo = assembly.DefinedTypes.FirstOrDefault(aType => aType.Name.Equals("Startup"));
                if (typeInfo != null)
                {
                    type = typeInfo.AsType();
                }
            }
            else
            {
                type = assembly.GetType(typeName);
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

            object instance = null;
            if (!methodInfo.IsStatic)
            {
                instance = ActivatorUtilities.GetServiceOrCreateInstance(_services, type);
            }
            return builder => methodInfo.Invoke(instance, new object[] { builder });
        }
    }
}
