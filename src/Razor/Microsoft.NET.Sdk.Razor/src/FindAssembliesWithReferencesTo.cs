// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class FindAssembliesWithReferencesTo : Task
    {
        [Required]
        public ITaskItem[] TargetAssemblyNames { get; set; }

        [Required]
        public ITaskItem[] Assemblies { get; set; }

        [Output]
        public string[] ResolvedAssemblies { get; set; }

        public override bool Execute()
        {
            var referenceItems = new List<AssemblyItem>(Assemblies.Length);
            foreach (var item in Assemblies)
            {
                const string FusionNameKey = "FusionName";
                var fusionName = item.GetMetadata(FusionNameKey);
                if (string.IsNullOrEmpty(fusionName))
                {
                    Log.LogError($"Missing required metadata '{FusionNameKey}' for '{item.ItemSpec}.");
                    return false;
                }

                var assemblyName = new AssemblyName(fusionName).Name;
                referenceItems.Add(new AssemblyItem
                {
                    AssemblyName = assemblyName,
                    IsFrameworkReference = item.GetMetadata("IsFrameworkReference") == "true",
                    Path = item.ItemSpec,
                });
            }

            var targetAssemblyNames = TargetAssemblyNames.Select(s => s.ItemSpec).ToList();

            var provider = new ReferenceResolver(targetAssemblyNames, referenceItems);
            try
            {
                var assemblyNames = provider.ResolveAssemblies();
                ResolvedAssemblies = assemblyNames.ToArray();
            }
            catch (ReferenceAssemblyNotFoundException ex)
            {
                // Print a warning and return. We cannot produce a correct document at this point.
                var warning = "Reference assembly {0} could not be found. This is typically caused by build errors in referenced projects.";
                Log.LogWarning(null, "RAZORSDK1007", null, null, 0, 0, 0, 0, warning, ex.FileName);
                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
            }

            return !Log.HasLoggedErrors;
        }
    }
}
