// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Common;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Core.v3;
using NuGet.Versioning;

namespace PinVersion
{
    public class Program
    {
        private static ConcurrentDictionary<string, Task<NuGetVersion>> _packageVersionLookup =
            new ConcurrentDictionary<string, Task<NuGetVersion>>(StringComparer.OrdinalIgnoreCase);

        public static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage <package source> <Korebuild Tag> <repository-names-file>");
            }

            var packageSource = args[0];
            var korebuildTag = args[1];

            var repositoryNames = File.ReadAllLines(args[2]);

            Task.WaitAll(repositoryNames
                .Select(repositoryPath => ExecuteAsync(repositoryPath, packageSource, korebuildTag))
                .ToArray());
        }

        public static async Task ExecuteAsync(string repositoryPath, string packageSource, string korebuildTag)
        {
            var packageRepository = Repository.Factory.GetCoreV3(packageSource);
            var metadataResource = await packageRepository.GetResourceAsync<MetadataResource>();

            // Pin project.json files
            foreach (var file in Directory.EnumerateFiles(repositoryPath, "project.json", SearchOption.AllDirectories))
            {
                var projectJson = JObject.Parse(File.ReadAllText(file));
                var projectName = Path.GetFileName(Path.GetDirectoryName(file));
                var latestPackageVersion = await GetOrAddVersion(metadataResource, projectName);
                if (latestPackageVersion != null)
                {
                    ((JValue)projectJson["version"]).Value = latestPackageVersion.ToNormalizedString();
                }

                var frameworkDependencies = projectJson["frameworks"]
                    ?.Cast<JProperty>()
                    ?.Select(f => ((JObject)f.Value)["dependencies"])
                    ?? Enumerable.Empty<JToken>();
                var dependencies = Enumerable.Concat(new[] { projectJson["dependencies"] }, frameworkDependencies)
                    .Where(d => d != null)
                    .SelectMany(d => d)
                    .Cast<JProperty>();

                foreach (var dependency in dependencies)
                {
                    latestPackageVersion = await GetOrAddVersion(metadataResource, dependency.Name);
                    if (latestPackageVersion != null)
                    {
                        if (dependency.Value.Type == JTokenType.Object)
                        {
                            // "key": { "version": "1.0.0-*", "type": "build" }
                            var value = (JObject)dependency.Value;
                            value["version"] = latestPackageVersion.ToNormalizedString();
                        }
                        else
                        {
                            // "key": "version"
                            dependency.Value = latestPackageVersion.ToNormalizedString();
                        }
                    }
                }

                using (var fileWriter = new JsonTextWriter(new StreamWriter(file)))
                {
                    fileWriter.Formatting = Formatting.Indented;
                    fileWriter.Indentation = 2;
                    projectJson.WriteTo(fileWriter);
                }

                // Update KoreBuild path

                var buildFiles = new[] { "build.ps1", "build.sh" };
                foreach (var buildFile in buildFiles)
                {
                    var buildFilePath = Path.Combine(repositoryPath, buildFile);
                    if (File.Exists(buildFilePath))
                    {
                        var content = File.ReadAllText(buildFilePath);
                        var replaced = content.Replace("KoreBuild/archive/release.zip", $"KoreBuild/archive/{korebuildTag}.zip");

                        if (content != replaced)
                        {
                            File.WriteAllText(buildFilePath, replaced);
                        }
                    }
                }
            }
        }

        private static Task<NuGetVersion> GetOrAddVersion(MetadataResource resource, string packageId)
        {
            return _packageVersionLookup.GetOrAdd(packageId, id =>
            {
                return resource.GetLatestVersion(
                    packageId,
                    includePrerelease: true,
                    includeUnlisted: false,
                    log: NullLogger.Instance,
                    token: default(CancellationToken));
            });
        }
    }
}
