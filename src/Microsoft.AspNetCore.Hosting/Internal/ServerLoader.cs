// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    internal static class ServerLoader
    {
        internal static Type ResolveServerType(string assemblyName)
        {
            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            if (assembly == null)
            {
                throw new InvalidOperationException(string.Format("The assembly {0} failed to load.", assemblyName));
            }

            var serverTypeInfo = assembly.DefinedTypes.Where(
                t => t.ImplementedInterfaces.FirstOrDefault(interf => interf.Equals(typeof(IServer))) != null)
                .FirstOrDefault();

            if (serverTypeInfo == null)
            {
                throw new InvalidOperationException($"No server type found that implements IServer in assembly: {assemblyName}.");
            }

            return serverTypeInfo.AsType();
        }

    }
}
