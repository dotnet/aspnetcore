// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Microsoft.Blazor.Internal.Common.FileProviders;
using System.Reflection;
using System;

namespace Microsoft.Blazor.Server
{
    internal class ReferencedAssemblyFileProvider : InMemoryFileProvider
    {
        public ReferencedAssemblyFileProvider(Assembly entrypointAssembly, IFileProvider clientBcl)
            : base(ComputeContents(entrypointAssembly, clientBcl))
        {
        }

        private static IEnumerable<(string, Stream)> ComputeContents(
            Assembly entrypointAssembly,
            IFileProvider clientBcl)
        {
            var foundAssemblies = new Dictionary<string, ReferencedAssembly>();
            AddWithReferencesRecursive(
                new ReferencedAssembly(AssemblyDefinition.ReadAssembly(entrypointAssembly.Location)),
                clientBcl,
                foundAssemblies);

            return foundAssemblies.Values.Select(assembly => (
                $"/bin/{assembly.Name}.dll",
                (Stream)new MemoryStream(assembly.Data)));
        }

        private static void AddWithReferencesRecursive(
            ReferencedAssembly root,
            IFileProvider clientBcl,
            IDictionary<string, ReferencedAssembly> results)
        {
            results.Add(root.Name, root);
            
            foreach (var module in root.Definition.Modules)
            {
                foreach (var referenceName in module.AssemblyReferences)
                {
                    if (!results.ContainsKey(referenceName.Name))
                    {
                        var resolvedReference = ResolveReference(clientBcl, module, referenceName);
                        if (resolvedReference != null)
                        {
                            AddWithReferencesRecursive(resolvedReference, clientBcl, results);
                        }
                    }
                }
            }
        }

        private static ReferencedAssembly ResolveReference(IFileProvider clientBcl, ModuleDefinition module, AssemblyNameReference referenceName)
        {
            if (SearchInFileProvider(clientBcl, string.Empty, $"{referenceName.Name}.dll", out var bclFile))
            {
                // Where possible, we resolve references to client BCL assemblies
                return new ReferencedAssembly(bclFile);
            }
            else
            {
                try
                {
                    // If it's not a client BCL assembly, maybe we can resolve it natively
                    // (e.g., if it's in the app's bin directory, or a NuGet package)
                    var nativelyResolved = module.AssemblyResolver.Resolve(referenceName);
                    return AllowServingAssembly(nativelyResolved)
                        ? new ReferencedAssembly(nativelyResolved)
                        : null;
                }
                catch (AssemblyResolutionException)
                {
                    // Some of the referenced assemblies aren't included in the Mono BCL, e.g.,
                    // Mono.Security.dll which is referenced from System.dll. These ones are not
                    // required at runtime, so just skip them.
                    return null;
                }
            }
        }

        private static bool AllowServingAssembly(AssemblyDefinition nativelyResolvedAssembly)
        {
            // When we use the native assembly resolver, it might return something from a NuGet
            // packages folder which we do want to serve, or it might return something from the
            // core .NET BCL which we *don't* want to serve (because the core BCL assemblies
            // should come from the Mono WASM distribution only). Currently there isn't a good
            // way to differentiate these cases, so as a temporary heuristic, assume anything
            // named System.* shouldn't be resolved natively.
            return !nativelyResolvedAssembly.MainModule.Name.StartsWith(
                "System.",
                StringComparison.Ordinal);
        }

        private static bool SearchInFileProvider(IFileProvider fileProvider, string searchRootDirNoTrailingSlash, string name, out IFileInfo file)
        {
            var possibleFullPath = $"{searchRootDirNoTrailingSlash}/{name}";
            var possibleResult = fileProvider.GetFileInfo(possibleFullPath);
            if (possibleResult.Exists)
            {
                file = possibleResult;
                return true;
            }

            var childDirs = fileProvider.GetDirectoryContents(searchRootDirNoTrailingSlash)
                .Where(item => item.IsDirectory);
            foreach (var childDir in childDirs)
            {
                if (SearchInFileProvider(fileProvider, childDir.PhysicalPath, name, out file))
                {
                    return true;
                }
            }

            file = null;
            return false;
        }

        private class ReferencedAssembly
        {
            public string Name { get; }
            public byte[] Data { get; }
            public AssemblyDefinition Definition { get; }

            public ReferencedAssembly(AssemblyDefinition definition)
            {
                Name = definition.Name.Name;
                Data = File.ReadAllBytes(definition.MainModule.FileName);
                Definition = definition;
            }

            public ReferencedAssembly(IFileInfo fileInfo)
            {
                using (var ms = new MemoryStream())
                using (var readStream = fileInfo.CreateReadStream())
                {
                    readStream.CopyTo(ms);
                    Data = ms.ToArray();
                }

                using (var readStream = new MemoryStream(Data))
                {
                    Definition = AssemblyDefinition.ReadAssembly(readStream);
                }

                Name = Definition.Name.Name;
            }
        }
    }
}
