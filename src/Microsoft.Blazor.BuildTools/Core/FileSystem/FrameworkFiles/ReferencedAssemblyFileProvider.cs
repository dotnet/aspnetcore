// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Microsoft.Blazor.Internal.Common.FileProviders;

namespace Microsoft.Blazor.BuildTools.Core.FileSystem
{
    internal class ReferencedAssemblyFileProvider : InMemoryFileProvider
    {
        public ReferencedAssemblyFileProvider(string rootAssemblyName, ReferencedAssemblyResolver resolver)
            : base(ComputeContents(rootAssemblyName, resolver))
        {
        }

        private static IEnumerable<(string, Stream)> ComputeContents(
            string rootAssemblyName,
            ReferencedAssemblyResolver resolver)
        {
            var foundAssemblies = new Dictionary<string, ReferencedAssemblyInfo>();
            AddWithReferencesRecursive(rootAssemblyName, resolver, foundAssemblies);

            return foundAssemblies.Values.Select(assembly => (
                $"/{assembly.Definition.Name.Name}.dll",
                (Stream)new MemoryStream(assembly.Data)));
        }

        private static void AddWithReferencesRecursive(
            string name,
            ReferencedAssemblyResolver resolver,
            IDictionary<string, ReferencedAssemblyInfo> results)
        {
            if (resolver.TryResolve(name, out var assemblyBytes))
            {
                var assemblyInfo = new ReferencedAssemblyInfo(assemblyBytes);
                results.Add(assemblyInfo.Definition.Name.Name, assemblyInfo);

                var childReferencesToAdd = assemblyInfo.Definition.Modules
                    .SelectMany(module => module.AssemblyReferences)
                    .Select(childReference => childReference.Name)
                    .Where(childReferenceName => !results.ContainsKey(childReferenceName));
                foreach (var childReferenceName in childReferencesToAdd)
                {
                    AddWithReferencesRecursive(childReferenceName, resolver, results);
                }
            }
        }

        private class ReferencedAssemblyInfo
        {
            public byte[] Data { get; }
            public AssemblyDefinition Definition { get; }

            public ReferencedAssemblyInfo(byte[] rawData)
            {
                Data = rawData;

                using (var ms = new MemoryStream(rawData))
                {
                    Definition = AssemblyDefinition.ReadAssembly(ms);
                }
            }
        }
    }
}
