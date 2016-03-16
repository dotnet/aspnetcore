// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Microsoft.Extensions.DependencyModel
{
    internal class CompilationLibrary : Library
    {
        public CompilationLibrary(string type,
            string name,
            string version,
            string hash,
            IEnumerable<string> assemblies,
            IEnumerable<Dependency> dependencies,
            bool serviceable)
            : base(type, name, version, hash,  dependencies, serviceable)
        {
            Assemblies = assemblies.ToArray();
        }

        public IReadOnlyList<string> Assemblies { get; }

        internal static ICompilationAssemblyResolver DefaultResolver { get; } = new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
        {
            new PackageCacheCompilationAssemblyResolver(),
            new AppBaseCompilationAssemblyResolver(),
            new ReferenceAssemblyPathResolver(),
            new PackageCompilationAssemblyResolver()
        });

        public IEnumerable<string> ResolveReferencePaths()
        {
            var assemblies = new List<string>();
            if (!DefaultResolver.TryResolveAssemblyPaths(this, assemblies))
            {
                throw new InvalidOperationException($"Can not find compilation library location for package '{Name}'");
            }
            return assemblies;
        }
    }
}
