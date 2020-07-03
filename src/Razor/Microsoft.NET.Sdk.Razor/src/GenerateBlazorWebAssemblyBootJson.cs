// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ResourceHashesByNameDictionary = System.Collections.Generic.Dictionary<string, string>;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class GenerateBlazorWebAssemblyBootJson : Task
    {
        [Required]
        public string AssemblyPath { get; set; }

        [Required]
        public ITaskItem[] Resources { get; set; }

        [Required]
        public bool DebugBuild { get; set; }

        [Required]
        public bool LinkerEnabled { get; set; }

        [Required]
        public bool CacheBootResources { get; set; }

        public ITaskItem[] ConfigurationFiles { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public ITaskItem[] LazyLoadedAssemblies { get; set; }

        public override bool Execute()
        {
            using var fileStream = File.Create(OutputPath);
            var entryAssemblyName = AssemblyName.GetAssemblyName(AssemblyPath).Name;

            try
            {
                WriteBootJson(fileStream, entryAssemblyName);
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
            }

            return !Log.HasLoggedErrors;
        }

        // Internal for tests
        public void WriteBootJson(Stream output, string entryAssemblyName)
        {
            var result = new BootJsonData
            {
                entryAssembly = entryAssemblyName,
                cacheBootResources = CacheBootResources,
                debugBuild = DebugBuild,
                linkerEnabled = LinkerEnabled,
                resources = new ResourcesData(),
                config = new List<string>(),
            };

            // Build a two-level dictionary of the form:
            // - assembly:
            //   - UriPath (e.g., "System.Text.Json.dll")
            //     - ContentHash (e.g., "4548fa2e9cf52986")
            // - runtime:
            //   - UriPath (e.g., "dotnet.js")
            //     - ContentHash (e.g., "3448f339acf512448")
            if (Resources != null)
            {
                var resourceData = result.resources;
                foreach (var resource in Resources)
                {
                    ResourceHashesByNameDictionary resourceList;

                    var fileName = resource.GetMetadata("FileName");
                    var extension = resource.GetMetadata("Extension");
                    var resourceCulture = resource.GetMetadata("Culture");
                    var assetType = resource.GetMetadata("AssetType");
                    var resourceName = $"{fileName}{extension}";

                    if (IsLazyLoadedAssembly(fileName))
                    {
                        resourceData.dynamicAssembly ??= new ResourceHashesByNameDictionary();
                        resourceList = resourceData.dynamicAssembly;
                    }
                    else if (!string.IsNullOrEmpty(resourceCulture))
                    {
                        resourceData.satelliteResources ??= new Dictionary<string, ResourceHashesByNameDictionary>(StringComparer.OrdinalIgnoreCase);
                        resourceName = resourceCulture + "/" + resourceName;

                        if (!resourceData.satelliteResources.TryGetValue(resourceCulture, out resourceList))
                        {
                            resourceList = new ResourceHashesByNameDictionary();
                            resourceData.satelliteResources.Add(resourceCulture, resourceList);
                        }
                    }
                    else if (string.Equals(extension, ".pdb", StringComparison.OrdinalIgnoreCase))
                    {
                        resourceData.pdb ??= new ResourceHashesByNameDictionary();
                        resourceList = resourceData.pdb;
                    }
                    else if (string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        resourceList = resourceData.assembly;
                    }
                    else if (string.Equals(assetType, "native", StringComparison.OrdinalIgnoreCase))
                    {
                        resourceList = resourceData.runtime;
                    }
                    else
                    {
                        // This should include items such as XML doc files, which do not need to be recorded in the manifest.
                        continue;
                    }

                    if (!resourceList.ContainsKey(resourceName))
                    {
                        resourceList.Add(resourceName, $"sha256-{resource.GetMetadata("FileHash")}");
                    }
                }
            }

            if (ConfigurationFiles != null)
            {
                foreach (var configFile in ConfigurationFiles)
                {
                    result.config.Add(Path.GetFileName(configFile.ItemSpec));
                }
            }

            var serializer = new DataContractJsonSerializer(typeof(BootJsonData), new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true
            });

            using var writer = JsonReaderWriterFactory.CreateJsonWriter(output, Encoding.UTF8, ownsStream: false, indent: true);
            serializer.WriteObject(writer, result);
        }

        private bool IsLazyLoadedAssembly(string fileName)
        {
            return LazyLoadedAssemblies != null && LazyLoadedAssemblies.Any(a => a.ItemSpec == fileName);
        }
    }
}
