// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Blazor.Build
{
    public class GenerateBlazorBootJson : Task
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

        [Required]
        public string OutputPath { get; set; }

        public override bool Execute()
        {
            // Build a two-level dictionary of the form:
            // - BootResourceType (e.g., "assembly")
            //   - UriPath (e.g., "System.Text.Json.dll")
            //     - ContentHash (e.g., "4548fa2e9cf52986")
            var resourcesByGroup = new Dictionary<string, Dictionary<string, string>>();
            foreach (var resource in Resources)
            {
                var resourceType = resource.GetMetadata("BootResourceType");
                if (string.IsNullOrEmpty(resourceType))
                {
                    continue;
                }

                if (!resourcesByGroup.TryGetValue(resourceType, out var group))
                {
                    group = new Dictionary<string, string>();
                    resourcesByGroup.Add(resourceType, group);
                }

                var uriPath = GetUriPath(resource);
                if (!group.ContainsKey(uriPath))
                {
                    // It's safe to truncate to a fairly short string, since the hash is not used for any
                    // security purpose - the developer produces these files themselves, and the hash is
                    // only used to check whether an earlier cached copy is up-to-date.
                    // This truncation halves the size of blazor.boot.json in typical cases.
                    group.Add(uriPath, resource.GetMetadata("FileHash").Substring(0, 16).ToLowerInvariant());
                }
            }

            var bootJsonData = new
            {
                EntryAssembly = AssemblyName.GetAssemblyName(AssemblyPath).Name,
                Resources = resourcesByGroup,
                DebugBuild,
                LinkerEnabled,
                CacheBootResources,
            };

            using (var fileStream = File.Create(OutputPath))
            using (var utf8Writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions { Indented = true }))
            {
                JsonSerializer.Serialize(utf8Writer, bootJsonData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                utf8Writer.Flush();
            }

            return true;
        }

        private static string GetUriPath(ITaskItem item)
        {
            var outputPath = item.GetMetadata("RelativeOutputPath");
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.GetFileName(item.ItemSpec);
            }

            return outputPath.Replace('\\', '/');
        }
    }
}
