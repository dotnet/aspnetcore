// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class DefaultExtensionDependencyChecker : ExtensionDependencyChecker
    {
        // These are treated as prefixes. So `Microsoft.CodeAnalysis.Razor` would be assumed to work.
        private static readonly string[] DefaultIgnoredAssemblies = new string[]
        {
            "mscorlib",
            "netstandard",
            "System",
            "Microsoft.CodeAnalysis",
            "Microsoft.AspNetCore.Razor.Language",
        };

        private readonly ExtensionAssemblyLoader _loader;
        private readonly TextWriter _output;
        private readonly string[] _ignoredAssemblies;

        public DefaultExtensionDependencyChecker(
            ExtensionAssemblyLoader loader,
            TextWriter output,
            string[] ignoredAssemblies = null)
        {
            _loader = loader;
            _output = output;
            _ignoredAssemblies = ignoredAssemblies ?? DefaultIgnoredAssemblies;
        }

        public override bool Check(IEnumerable<string> assmblyFilePaths)
        {
            try
            {
                return CheckCore(assmblyFilePaths);
            }
            catch (Exception ex)
            {
                _output.WriteLine("Exception performing Extension dependency check:");
                _output.WriteLine(ex.ToString());
                return false;
            }
        }

        private bool CheckCore(IEnumerable<string> assemblyFilePaths)
        {
            var items = assemblyFilePaths.Select(a => ExtensionVerificationItem.Create(a)).ToArray();
            var assemblies = new HashSet<AssemblyIdentity>(items.Select(i => i.Identity));

            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                _output.WriteLine($"Verifying assembly at {item.FilePath}");

                if (!Path.IsPathRooted(item.FilePath))
                {
                    _output.WriteLine($"The file path '{item.FilePath}' is not a rooted path. File paths must be absolute and fully-qualified.");
                    return false;
                }

                foreach (var reference in item.References)
                {
                    if (_ignoredAssemblies.Any(n => reference.Name.StartsWith(n)))
                    {
                        // This is on the allow list, keep going.
                        continue;
                    }

                    if (assemblies.Contains(reference))
                    {
                        // This was also provided as a dependency, keep going.
                        continue;
                    }

                    // If we get here we can't resolve this assembly. This is an error.
                    _output.WriteLine($"Extension assembly '{item.Identity.Name}' depends on '{reference.ToString()} which is missing.");
                    return false;
                }
            }

            // Assuming we get this far, the set of assemblies we have is at least a coherent set (barring
            // version conflicts). Register all of the paths with the loader so they can find each other by
            // name.
            for (var i = 0; i < items.Length; i++)
            {
                _loader.AddAssemblyLocation(items[i].FilePath);
            }

            // Now try to load everything. This has the side effect of resolving all of these items
            // in the loader's caches.
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                item.Assembly = _loader.LoadFromPath(item.FilePath);
            }

            // Third, check that the MVIDs of the files on disk match the MVIDs of the loaded assemblies.
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.Mvid != item.Assembly.ManifestModule.ModuleVersionId)
                {
                    _output.WriteLine($"Extension assembly '{item.Identity.Name}' at '{item.FilePath}' has a different ModuleVersionId than loaded assembly '{item.Assembly.FullName}'");
                    return false;
                }
            }

            return true;
        }

        private class ExtensionVerificationItem
        {
            public static ExtensionVerificationItem Create(string filePath)
            {
                using (var peReader = new PEReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    var metadataReader = peReader.GetMetadataReader();
                    var identity = metadataReader.GetAssemblyIdentity();
                    var mvid = metadataReader.GetGuid(metadataReader.GetModuleDefinition().Mvid);
                    var references = metadataReader.GetReferencedAssembliesOrThrow();

                    return new ExtensionVerificationItem(filePath, identity, mvid, references.ToArray());
                }
            }

            private ExtensionVerificationItem(string filePath, AssemblyIdentity identity, Guid mvid, AssemblyIdentity[] references)
            {
                FilePath = filePath;
                Identity = identity;
                Mvid = mvid;
                References = references;
            }

            public string FilePath { get; }

            public Assembly Assembly { get; set; }

            public AssemblyIdentity Identity { get; }

            public Guid Mvid { get; }

            public IReadOnlyList<AssemblyIdentity> References { get; }
        }
    }
}