// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.JsonParser.Sources;

namespace Microsoft.Dnx.Runtime
{
    internal static class NamedResourceReader
    {
        public static IDictionary<string, string> ReadNamedResources(JsonObject rawProject, string projectFilePath)
        {
            if (!rawProject.Keys.Contains("namedResource"))
            {
                return new Dictionary<string, string>();
            }

            var namedResourceToken = rawProject.ValueAsJsonObject("namedResource");
            if (namedResourceToken == null)
            {
                throw FileFormatException.Create("Value must be object.", rawProject.Value("namedResource"), projectFilePath);
            }

            var namedResources = new Dictionary<string, string>();

            foreach (var namedResourceKey in namedResourceToken.Keys)
            {
                var resourcePath = namedResourceToken.ValueAsString(namedResourceKey);
                if (resourcePath == null)
                {
                    throw FileFormatException.Create("Value must be string.", namedResourceToken.Value(namedResourceKey), projectFilePath);
                }

                if (resourcePath.Value.Contains("*"))
                {
                    throw FileFormatException.Create("Value cannot contain wildcards.", resourcePath, projectFilePath);
                }

                var resourceFileFullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectFilePath), resourcePath));

                if (namedResources.ContainsKey(namedResourceKey))
                {
                    throw FileFormatException.Create(
                        string.Format("The named resource {0} already exists.", namedResourceKey),
                        resourcePath,
                        projectFilePath);
                }

                namedResources.Add(
                    namedResourceKey,
                    resourceFileFullPath);
            }

            return namedResources;
        }

        public static void ApplyNamedResources(IDictionary<string, string> namedResources, IDictionary<string, string> resources)
        {
            foreach (var namedResource in namedResources)
            {
                // The named resources dictionary is like the project file
                // key = name, value = path to resource
                if (resources.ContainsKey(namedResource.Value))
                {
                    resources[namedResource.Value] = namedResource.Key;
                }
                else
                {
                    resources.Add(namedResource.Value, namedResource.Key);
                }
            }
        }
    }
}