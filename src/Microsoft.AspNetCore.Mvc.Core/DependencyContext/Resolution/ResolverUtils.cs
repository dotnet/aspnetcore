// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    internal static class ResolverUtils
    {
        internal static bool TryResolvePackagePath(CompilationLibrary library, string basePath, out string packagePath)
        {
            packagePath = Path.Combine(basePath, library.Name, library.Version);
            if (Directory.Exists(packagePath))
            {
                return true;
            }
            return false;
        }

        internal static IEnumerable<string> ResolveFromPackagePath(CompilationLibrary library, string basePath)
        {
            foreach (var assembly in library.Assemblies)
            {
                string fullName;
                if (!TryResolveAssemblyFile(basePath, assembly, out fullName))
                {
                    throw new InvalidOperationException($"Can not find assembly file for package {library.Name} at '{fullName}'");
                }
                yield return fullName;
            }
        }

        internal static bool TryResolveAssemblyFile(string basePath, string assemblyPath, out string fullName)
        {
            fullName = Path.Combine(basePath, assemblyPath);
            if (File.Exists(fullName))
            {
                return true;
            }
            return false;
        }
    }
}