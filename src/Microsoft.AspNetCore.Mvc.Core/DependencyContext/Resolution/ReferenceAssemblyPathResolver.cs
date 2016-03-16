// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    internal class ReferenceAssemblyPathResolver: ICompilationAssemblyResolver
    {
        private readonly string _defaultReferenceAssembliesPath;
        private readonly string[] _fallbackSearchPaths;

        internal ReferenceAssemblyPathResolver()
        {
            _defaultReferenceAssembliesPath = GetDefaultReferenceAssembliesPath(PlatformServices.Default.Runtime);
            _fallbackSearchPaths = GetFallbackSearchPaths(PlatformServices.Default.Runtime);
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            if (!string.Equals(library.Type, "referenceassembly", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            foreach (var assembly in library.Assemblies)
            {
                string fullName;
                if (!TryResolveReferenceAssembly(assembly, out fullName))
                {
                    throw new InvalidOperationException($"Can not find reference assembly '{assembly}' file for package {library.Name}");
                }
                assemblies.Add(fullName);
            }
            return true;
        }

        private bool TryResolveReferenceAssembly(string path, out string fullPath)
        {
            fullPath = null;

            if (_defaultReferenceAssembliesPath != null)
            {
                var relativeToReferenceAssemblies = Path.Combine(_defaultReferenceAssembliesPath, path);
                if (File.Exists(relativeToReferenceAssemblies))
                {
                    fullPath = relativeToReferenceAssemblies;
                    return true;
                }
            }

            var name = Path.GetFileName(path);
            foreach (var fallbackPath in _fallbackSearchPaths)
            {
                var fallbackFile = Path.Combine(fallbackPath, name);
                if (File.Exists(fallbackFile))
                {
                    fullPath = fallbackFile;
                    return true;
                }
            }

            return false;
        }

        internal static string[] GetFallbackSearchPaths(IRuntimeEnvironment runtimeEnvironment)
        {
            if (runtimeEnvironment.OperatingSystemPlatform != Platform.Windows)
            {
                return new string[0];
            }

            var net20Dir = Path.Combine(Environment.GetEnvironmentVariable("WINDIR"), "Microsoft.NET", "Framework", "v2.0.50727");

            if (!Directory.Exists(net20Dir))
            {
                return new string[0];
            }
            return new[] { net20Dir };
        }

        internal static string GetDefaultReferenceAssembliesPath(IRuntimeEnvironment runtimeEnvironment)
        {
            // Allow setting the reference assemblies path via an environment variable
            var referenceAssembliesPath = DotNetReferenceAssembliesPathResolver.Resolve(runtimeEnvironment); 
            if (!string.IsNullOrEmpty(referenceAssembliesPath))
            {
                return referenceAssembliesPath;
            }

            if (runtimeEnvironment.OperatingSystemPlatform != Platform.Windows)
            {
                // There is no reference assemblies path outside of windows
                // The environment variable can be used to specify one
                return null;
            }

            // References assemblies are in %ProgramFiles(x86)% on
            // 64 bit machines
            var programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");

            if (string.IsNullOrEmpty(programFiles))
            {
                // On 32 bit machines they are in %ProgramFiles%
                programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            }

            if (string.IsNullOrEmpty(programFiles))
            {
                // Reference assemblies aren't installed
                return null;
            }

            return Path.Combine(
                programFiles,
                "Reference Assemblies", "Microsoft", "Framework");
        }
    }
}