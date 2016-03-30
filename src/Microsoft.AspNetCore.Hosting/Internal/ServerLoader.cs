using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    internal static class ServerLoader
    {
        internal static Type ResolveServerFactoryType(string assemblyName)
        {
            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            if (assembly == null)
            {
                throw new InvalidOperationException(string.Format("The assembly {0} failed to load.", assemblyName));
            }

            var serverTypeInfo = assembly.DefinedTypes.Where(
                t => t.ImplementedInterfaces.FirstOrDefault(interf => interf.Equals(typeof(IServerFactory))) != null)
                .FirstOrDefault();

            if (serverTypeInfo == null)
            {
                throw new InvalidOperationException($"No server type found that implements IServerFactory in assembly: {assemblyName}.");
            }

            return serverTypeInfo.AsType();
        }

    }
}
