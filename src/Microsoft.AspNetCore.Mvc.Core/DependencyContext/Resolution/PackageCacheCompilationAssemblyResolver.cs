// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    internal class PackageCacheCompilationAssemblyResolver: ICompilationAssemblyResolver
    {
        private readonly string _packageCacheDirectory;

        internal PackageCacheCompilationAssemblyResolver()
        {
            _packageCacheDirectory = Environment.GetEnvironmentVariable("DOTNET_PACKAGES_CACHE");
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            if (!string.Equals(library.Type, "package", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(_packageCacheDirectory))
            {
                var hashSplitterPos = library.Hash.IndexOf('-');
                if (hashSplitterPos <= 0 || hashSplitterPos == library.Hash.Length - 1)
                {
                    throw new InvalidOperationException($"Invalid hash entry '{library.Hash}' for package '{library.Name}'");
                }

                string packagePath;
                if (ResolverUtils.TryResolvePackagePath(library, _packageCacheDirectory, out packagePath))
                {
                    var hashAlgorithm = library.Hash.Substring(0, hashSplitterPos);
                    var cacheHashPath = Path.Combine(packagePath, $"{library.Name}.{library.Version}.nupkg.{hashAlgorithm}");

                    if (File.Exists(cacheHashPath) &&
                        File.ReadAllText(cacheHashPath) == library.Hash.Substring(hashSplitterPos + 1))
                    {
                        assemblies.AddRange(ResolverUtils.ResolveFromPackagePath(library, packagePath));
                        return true;
                    }
                }
            }
            return false;
        }
    }
}