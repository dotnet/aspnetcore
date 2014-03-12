using Microsoft.AspNet.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Hosting.Server
{
    public class ServerManager : IServerManager
    {
        private readonly IServiceProvider _services;

        public ServerManager(IServiceProvider services)
        {
            _services = services;
        }

        public IServerFactory GetServerFactory(string serverFactoryIdentifier)
        {
            if (string.IsNullOrWhiteSpace(serverFactoryIdentifier))
            {
                throw new ArgumentNullException("serverFactoryIdentifier");
            }

            string typeName;
            string assemblyName;
            var parts = serverFactoryIdentifier.Split(new[] { ',' }, 2);
            if (parts.Length == 1)
            {
                typeName = null;
                assemblyName = serverFactoryIdentifier;
            }
            else if (parts.Length == 2)
            {
                typeName = parts[0];
                assemblyName = parts[1];
            }
            else
            {
                throw new ArgumentException("TODO: Unrecognized format", "serverFactoryIdentifier");
            }

            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            if (assembly == null)
            {
                throw new Exception(String.Format("TODO: assembly {0} failed to load message", assemblyName));
            }

            Type type = null;
            Type interfaceInfo;
            if (string.IsNullOrWhiteSpace(typeName))
            {
                foreach (var typeInfo in assembly.DefinedTypes)
                {
                    interfaceInfo = typeInfo.ImplementedInterfaces.FirstOrDefault(interf =>
                        interf.FullName == typeof(IServerFactory).FullName);
                    if (interfaceInfo != null)
                    {
                        type = typeInfo.AsType();
                    }
                }

                if (type == null)
                {
                    throw new Exception(String.Format("TODO: type {0} failed to load message", typeName ?? "<null>"));
                }
            }
            else
            {
                type = assembly.GetType(typeName) ?? assembly.GetType(assemblyName + "." + typeName);

                if (type == null)
                {
                    throw new Exception(String.Format("TODO: type {0} failed to load message", typeName ?? "<null>"));
                }

                interfaceInfo = type.GetTypeInfo().ImplementedInterfaces.FirstOrDefault(interf =>
                    interf.FullName == typeof(IServerFactory).FullName);

                if (interfaceInfo == null)
                {
                    throw new Exception("TODO: IServerFactory interface not found");
                }
            }

            object instance = ActivatorUtilities.GetServiceOrCreateInstance(_services, type);
            return (IServerFactory)instance;
        }
    }
}