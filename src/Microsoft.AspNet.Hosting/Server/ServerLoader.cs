// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Server
{
    public class ServerLoader : IServerLoader
    {
        private readonly IServiceProvider _services;

        public ServerLoader(IServiceProvider services)
        {
            _services = services;
        }

        public IServerFactory LoadServerFactory(string serverFactoryIdentifier)
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
                throw new Exception(string.Format("The assembly {0} failed to load.", assemblyName));
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
                    throw new Exception(string.Format("The type {0} failed to load.", typeName ?? "<null>"));
                }
            }
            else
            {
                type = assembly.GetType(typeName);

                if (type == null)
                {
                    throw new Exception(String.Format("The type {0} failed to load.", typeName ?? "<null>"));
                }

                interfaceInfo = type.GetTypeInfo().ImplementedInterfaces.FirstOrDefault(interf => interf.Equals(typeof(IServerFactory)));

                if (interfaceInfo == null)
                {
                    throw new Exception(string.Format("The type '{0}' does not implement the '{1}' interface.", type, typeof(IServerFactory).FullName));
                }
            }

            return (IServerFactory)ActivatorUtilities.GetServiceOrCreateInstance(_services, type);
        }
    }
}