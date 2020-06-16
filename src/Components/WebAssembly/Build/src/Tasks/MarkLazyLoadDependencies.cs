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
        public ITaskItem[] ResolvedAssemblies { get; set; }

        [Output]
        public ITaskItem[] EagerlyLoadedAssemblies { get; set; }

        private HashSet<string> _visitedAssemblies = new HashSet<string>();
        private HashSet<ITaskItem> _eagerlyLoadedAssemblies = new HashSet<ITaskItem>();

        private const string LAZY_LOAD_LABEL = "BlazorLazyLoad";
        private const string REFERENCE_SOURCE_KEY = "ReferenceSourceTarget";
        private const string PROJECT_REFERENCE = "ProjectReference";

        public override bool Execute()
        {
            foreach (var reference in ResolvedAssemblies)
            {
                try
                {
                    ProcessAssembly(reference);
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e);
                }
            }

            EagerlyLoadedAssemblies = _eagerlyLoadedAssemblies.ToArray();

            return !Log.HasLoggedErrors;
        }

        private void ProcessAssembly(ITaskItem reference)
        {
            var fileName = reference.GetMetadata("FileName");
            var assembly = AssemblyReferences.SingleOrDefault(p => p.GetMetadata("FileName") == fileName);

            if (!_visitedAssemblies.Add(fileName)) {
                return;
            }

            // if we can't find the assembly in the resolved assemblies then
            // assume it should be eagerly loaded since we can't make a decision on it
            if (assembly == null) {
                _eagerlyLoadedAssemblies.Add(reference);
                return;
            }

            // Project references that aren't labelled for lazy loading
            var sourceFromProjectReference = assembly.GetMetadata(REFERENCE_SOURCE_KEY) == PROJECT_REFERENCE;
            if (sourceFromProjectReference && assembly.GetMetadata(LAZY_LOAD_LABEL) != "true")
            {
                _eagerlyLoadedAssemblies.Add(reference);
            }

            // Package references that aren't labelled for lazy-loading
            var packageReference = PackageReferences.SingleOrDefault(p => p.ItemSpec == assembly.GetMetadata("NuGetPackageId"));
            if (!sourceFromProjectReference && (packageReference == null || packageReference.GetMetadata(LAZY_LOAD_LABEL) != "true"))
            {
                _eagerlyLoadedAssemblies.Add(reference);
            }
        }
    }
}
