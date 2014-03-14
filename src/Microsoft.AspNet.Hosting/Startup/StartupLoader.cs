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
            type = assembly.GetType(typeName) ?? assembly.GetType(assembly + "." + typeName);

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

            object instance = null;
            if (!methodInfo.IsStatic)
            {
                instance = ActivatorUtilities.GetServiceOrCreateInstance(_services, type);
            }
            return builder => methodInfo.Invoke(instance, new object[] { builder });
        }
    }
}
