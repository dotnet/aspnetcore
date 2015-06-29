// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Hosting.Server
{
    public class ServerLoader : IServerLoader
    {
        private readonly IServiceProvider _services;

        public ServerLoader(IServiceProvider services)
        {
            _services = services;
        }

        public IServerFactory LoadServerFactory([NotNull] string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName))
            {
                throw new ArgumentException(string.Empty, nameof(assemblyName));
            }

            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            if (assembly == null)
            {
                throw new Exception(string.Format("The assembly {0} failed to load.", assemblyName));
            }

            var serverTypeInfo = assembly.DefinedTypes.Where(
                t => t.ImplementedInterfaces.FirstOrDefault(interf => interf.Equals(typeof(IServerFactory))) != null)
                .FirstOrDefault();

            if (serverTypeInfo == null)
            {
                throw new InvalidOperationException($"No server type found that implements IServerFactory in assembly: {assemblyName}.");
            }

            return (IServerFactory)ActivatorUtilities.GetServiceOrCreateInstance(_services, serverTypeInfo.AsType());
        }
    }
}