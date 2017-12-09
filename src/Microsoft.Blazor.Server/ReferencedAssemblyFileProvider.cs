// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Mono.Cecil;
using Microsoft.Blazor.Mono;

namespace Microsoft.Blazor.Server
{
    internal class ReferencedAssemblyFileProvider<TApp> : IFileProvider
    {
        private static readonly Dictionary<string, IFileInfo> _frameworkDlls = ReadFrameworkDlls();
        private readonly Contents _referencedAssemblyContents = new Contents(FindReferencedAssemblies());
        private readonly Contents _emptyContents = new Contents(null);

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return subpath == string.Empty
                ? _referencedAssemblyContents
                : _emptyContents;
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            throw new System.NotImplementedException();
        }

        public IChangeToken Watch(string filter)
        {
            throw new System.NotImplementedException();
        }

        private static IEnumerable<AssemblyDefinition> FindReferencedAssemblies()
        {
            var foundAssemblies = new Dictionary<string, AssemblyDefinition>();
            FindReferencedAssembliesRecursive(
                AssemblyDefinition.ReadAssembly(typeof(TApp).Assembly.Location),
                foundAssemblies);
            return foundAssemblies.Values;
        }

        private static void FindReferencedAssembliesRecursive(AssemblyDefinition root, IDictionary<string, AssemblyDefinition> results)
        {
            results.Add(root.Name.Name, root);

            foreach (var module in root.Modules)
            {
                foreach (var referenceName in module.AssemblyReferences)
                {
                    if (!results.ContainsKey(referenceName.Name))
                    {
                        var resolvedReference = FindAssembly(referenceName, module.AssemblyResolver);

                        // Some of the referenced assemblies aren't included in the Mono BCL, e.g.,
                        // Mono.Security.dll which is referenced from System.dll. These ones are not
                        // required at runtime, so just skip them.
                        if (resolvedReference != null)
                        {
                            FindReferencedAssembliesRecursive(resolvedReference, results);
                        }
                    }
                }
            }
        }

        private static AssemblyDefinition FindAssembly(AssemblyNameReference name, IAssemblyResolver nativeResolver)
        {
            try
            {
                return _frameworkDlls.TryGetValue($"{name.Name}.dll", out var fileInfo)
                    ? AssemblyDefinition.ReadAssembly(fileInfo.CreateReadStream())
                    : AllowNativeDllResolution(name) ? nativeResolver.Resolve(name) : null;
            }
            catch (AssemblyResolutionException)
            {
                return null;
            }
        }

        private static bool AllowNativeDllResolution(AssemblyNameReference name)
        {
            // System.* assemblies must only be resolved from the browser-reachable FrameworkFiles.
            // It's no use resolving them using the native resolver, because those files wouldn't
            // be accessible at runtime anyway.
            return !name.Name.StartsWith("System.", StringComparison.Ordinal);
        }

        private static Dictionary<string, IFileInfo> ReadFrameworkDlls()
        {
            // TODO: Stop leaking knowledge of the Microsoft.Blazor.Mono file provider internal
            // structure into this unrelated class. Currently it's needed because that file provider
            // doesn't support proper directory hierarchies and therefore keeps all files in the
            // top-level directory, putting '$' into filenames in place of directories.
            // To fix this, make MonoStaticFileProvider expose a regular directory structure, and
            // then change this method to walk it recursively.

            return MonoStaticFileProvider.Instance
                .GetDirectoryContents(string.Empty)
                .Where(file => file.Name.EndsWith(".dll", StringComparison.Ordinal))
                .ToDictionary(MonoEmbeddedResourceToFilename, file => file);

            string MonoEmbeddedResourceToFilename(IFileInfo fileInfo)
            {
                var name = fileInfo.Name;
                var lastDirSeparatorPos = name.LastIndexOf('$');
                return name.Substring(lastDirSeparatorPos + 1);
            }
        }

        private class Contents : IDirectoryContents
        {
            private readonly bool _exists;
            private readonly IReadOnlyDictionary<string, IFileInfo> _items;

            public Contents(IEnumerable<AssemblyDefinition> assemblies)
            {
                _exists = assemblies != null;
                _items = (assemblies ?? Enumerable.Empty<AssemblyDefinition>())
                    .Select(assembly => new AssemblyFileInfo(assembly))
                    .ToDictionary(item => item.Name, item => (IFileInfo)item);
            }

            public bool Exists => _exists;

            public IEnumerator<IFileInfo> GetEnumerator() => _items.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _items.Values.GetEnumerator();
        }

        private class AssemblyFileInfo : IFileInfo
        {
            private readonly byte[] _data;

            public bool Exists => true;

            public long Length => _data.Length;

            public string PhysicalPath => Name;

            public string Name { get; }

            public DateTimeOffset LastModified => default(DateTimeOffset);

            public bool IsDirectory => false;

            public Stream CreateReadStream()
            {
                return new MemoryStream(_data);
            }

            public AssemblyFileInfo(AssemblyDefinition assembly)
            {
                Name = $"{assembly.Name.Name}.dll";

                using (var ms = new MemoryStream())
                {
                    assembly.Write(ms);
                    _data = ms.GetBuffer();
                }
            }
        }
    }
}
