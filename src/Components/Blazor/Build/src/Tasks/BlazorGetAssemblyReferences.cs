// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Blazor.Build
{
    public class BlazorGetAssemblyReferences : Task
    {
        [Required]
        public ITaskItem[] Assemblies { get; set; }

        [Output]
        public ITaskItem[] AssemblyReferences { get; set; }

        public override bool Execute()
        {
            AssemblyReferences = Assemblies
                .SelectMany(c => GetAssemblyReferenceNames(c.ItemSpec))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(c => c, StringComparer.Ordinal)
                .Select(c => new TaskItem(c))
                .ToArray();

            return true;
        }

        static IReadOnlyList<string> GetAssemblyReferenceNames(string assemblyPath)
        {
            try
            {
                using var peReader = new PEReader(File.OpenRead(assemblyPath));
                if (!peReader.HasMetadata)
                {
                    return Array.Empty<string>(); // not a managed assembly
                }

                var metadataReader = peReader.GetMetadataReader();

                var references = new List<string>();
                foreach (var handle in metadataReader.AssemblyReferences)
                {
                    var reference = metadataReader.GetAssemblyReference(handle);
                    var referenceName = metadataReader.GetString(reference.Name);

                    references.Add(referenceName);
                }

                return references;
            }
            catch (BadImageFormatException)
            {
                // not a PE file, or invalid metadata
            }

            return Array.Empty<string>(); // not a managed assembly
        }
    }
}
