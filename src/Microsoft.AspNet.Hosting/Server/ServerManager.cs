// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;

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
            if (string.IsNullOrEmpty(serverFactoryIdentifier))
            {
                throw new ArgumentException(string.Empty, "serverFactoryIdentifier");
            }

            var nameParts = HostingUtilities.SplitTypeName(serverFactoryIdentifier);
            string typeName = nameParts.Item1;
            string assemblyName = nameParts.Item2;

            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            if (assembly == null)
            {
                throw new Exception(String.Format("TODO: assembly {0} failed to load message", assemblyName));
            }

            Type type = null;
            Type interfaceInfo;
            if (string.IsNullOrEmpty(typeName))
            {
                foreach (var typeInfo in assembly.DefinedTypes)
                {
                    interfaceInfo = typeInfo.ImplementedInterfaces.FirstOrDefault(interf => interf.Equals(typeof(IServerFactory)));
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
                type = assembly.GetType(typeName);

                if (type == null)
                {
                    throw new Exception(String.Format("TODO: type {0} failed to load message", typeName ?? "<null>"));
                }

                interfaceInfo = type.GetTypeInfo().ImplementedInterfaces.FirstOrDefault(interf => interf.Equals(typeof(IServerFactory)));

                if (interfaceInfo == null)
                {
                    throw new Exception("TODO: IServerFactory interface not found");
                }
            }

            return (IServerFactory)ActivatorUtilities.GetServiceOrCreateInstance(_services, type);
        }
    }
}