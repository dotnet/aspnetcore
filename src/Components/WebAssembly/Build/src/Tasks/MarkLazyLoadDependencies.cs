// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ResourceHashesByNameDictionary = System.Collections.Generic.Dictionary<string, string>;

namespace Microsoft.AspNetCore.Components.WebAssembly.Build
{
    public class MarkLazyLoadDependencies : Task
    {
        [Required]
        public ITaskItem[] AssemblyReferences { get; set; }

        [Required]
        public ITaskItem[] PackageReferences { get; set; }

        [Required]
        public ITaskItem[] ExplicitlazyLoad { get; set; }

        [Output]
        public ITaskItem[] LazyLoadedAssemblies { get; set; }

        private List<string> _visitedAssemblies = new List<string>();
        private HashSet<ITaskItem> _lazyLoadAssemblies = new HashSet<ITaskItem>();

        private const string LAZY_LOAD_LABEL = "BlazorLazyLoad";
        private const string REFERENCE_SOURCE_KEY = "ReferenceSourceTarget";
        private const string PROJECT_REFERENCE = "ProjectReference";
        private const string ASSEMBLY_REFERENCE = "ResolveAssemblyReference";

        public override bool Execute()
        {
            // Add assemblies that have been explicitly set by the user in a
            // `BlazorLazyLoad` item group.
            //
            // <ItemGroup>
            //  <BlazorLazyLoad Include="SomeBigAssembly.dll" />
            // </ItemGroup>
            _lazyLoadAssemblies.UnionWith(ExplicitlazyLoad);

            foreach (var assembly in AssemblyReferences)
            {
                try
                {
                    ProcessAssembly(assembly);
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e);
                }
            }

            LazyLoadedAssemblies = _lazyLoadAssemblies.ToArray();

            return !Log.HasLoggedErrors;
        }

        private void ProcessAssembly(ITaskItem assembly)
        {
            var fileName = assembly.GetMetadata("FileName");

            var visited = _visitedAssemblies.Contains(fileName);
            if (visited)
            {
                return;
            }

            Log.LogMessage($"Checking if {fileName} should be marked for lazy-loading.");

            _visitedAssemblies.Add(fileName);

            // If the source is a project reference and the assembly is marked as
            // lazy then we are processing a lazily-loaded project.
            //
            // <ProjectReference Include="SomeProj.csproj" BlazorLazyLoad="true" />
            var sourceFromProjectReference = assembly.GetMetadata(REFERENCE_SOURCE_KEY) == PROJECT_REFERENCE;
            if (sourceFromProjectReference && assembly.GetMetadata(LAZY_LOAD_LABEL) == "true")
            {
                Log.LogMessage($"{fileName} is a lazy-loaded project reference.");
                _lazyLoadAssemblies.Add(new TaskItem(String.Concat(fileName, ".dll")));

                // If the project is lazy-loaded, get all its reference assemblies from the projects assembly and
                // mark them as lazy loaded.
                var referencAssemblyPath = assembly.GetMetadata("ReferenceAssembly");
                AddAssemblyReferencesToLazyLoad(referencAssemblyPath);
            }

            // If the assembly is resolved from a package reference, check to see
            // if the package reference was marked as lazy-load and label the assembly.
            //
            // <PackageReference Include="Newtonsoft.Json" BlazorLazyLoad="true" />
            var sourceFromPackageReference = assembly.GetMetadata(REFERENCE_SOURCE_KEY) == ASSEMBLY_REFERENCE;
            var packageReference = PackageReferences.SingleOrDefault(p => p.ItemSpec == assembly.GetMetadata("NuGetPackageId"));
            var foundPackageReference = sourceFromPackageReference && packageReference != null;
            if (foundPackageReference && packageReference.GetMetadata(LAZY_LOAD_LABEL) == "true")
            {
                Log.LogMessage($"{fileName} is a lazy-loaded package reference.");
                _lazyLoadAssemblies.Add(new TaskItem(String.Concat(fileName, ".dll")));

                // Lazily-loaded all packages referenced by this package as well.
                var referencAssemblyPath = assembly.GetMetadata("ReferenceAssembly");
                AddAssemblyReferencesToLazyLoad(referencAssemblyPath);
            }
        }

        private void AddAssemblyReferencesToLazyLoad(string referencAssemblyPath)
        {
            using var peReader = new PEReader(File.OpenRead(referencAssemblyPath));
            if (!peReader.HasMetadata)
            {
                return;
            }

            var metadataReader = peReader.GetMetadataReader();

            foreach (var handle in metadataReader.AssemblyReferences)
            {
                var reference = metadataReader.GetAssemblyReference(handle);
                var referenceName = metadataReader.GetString(reference.Name);

                _lazyLoadAssemblies.Add(new TaskItem(String.Concat(referenceName, ".dll")));
            }
        }
    }
}
