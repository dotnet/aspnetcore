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

            var parts = applicationName.Split(new[] { ',' }, 2);
            if (parts.Length == 2)
            {
                var typeName = parts[0];
                var assemblyName = parts[1];

                var assembly = Assembly.Load(new AssemblyName(assemblyName));
                if (assembly == null)
                {
                    throw new Exception(String.Format("TODO: assembly {0} failed to load message", assemblyName));
                }

                var type = assembly.GetType(typeName);
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
            throw new Exception("TODO: Unrecognized format");
        }
    }
}
