// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    internal class PackageCompilationAssemblyResolver: ICompilationAssemblyResolver
    {
        private readonly string _nugetPackageDirectory;

        internal PackageCompilationAssemblyResolver()
        {
            _nugetPackageDirectory = GetDefaultPackageDirectory(PlatformServices.Default.Runtime);
        }

        internal static string GetDefaultPackageDirectory(IRuntimeEnvironment runtimeEnvironment)
        {
            var packageDirectory = Environment.GetEnvironmentVariable("NUGET_PACKAGES");

            if (!string.IsNullOrEmpty(packageDirectory))
            {
                return packageDirectory;
            }

            string basePath;
            if (runtimeEnvironment.OperatingSystemPlatform == Platform.Windows)
            {
                basePath = Environment.GetEnvironmentVariable("USERPROFILE");
            }
            else
            {
                basePath = Environment.GetEnvironmentVariable("HOME");
            }
            if (string.IsNullOrEmpty(basePath))
            {
                return null;
            }
            return Path.Combine(basePath, ".nuget", "packages");
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            if (string.IsNullOrEmpty(_nugetPackageDirectory) ||
                !string.Equals(library.Type, "package", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string packagePath;

            if (ResolverUtils.TryResolvePackagePath(library, _nugetPackageDirectory, out packagePath))
            {
                assemblies.AddRange(ResolverUtils.ResolveFromPackagePath(library, packagePath));
                return true;
            }
            return false;
        }
    }
}