// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    internal class CompositeCompilationAssemblyResolver: ICompilationAssemblyResolver
    {
        private readonly ICompilationAssemblyResolver[] _resolvers;

        public CompositeCompilationAssemblyResolver(ICompilationAssemblyResolver[] resolvers)
        {
            _resolvers = resolvers;
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            foreach (var resolver in _resolvers)
            {
                if (resolver.TryResolveAssemblyPaths(library, assemblies))
                {
                    return true;
                }
            }
            return false;;
        }
    }
}