// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet;

namespace PinVersion
{
    public class Program
    {
        public void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage <repository-path> <package source>");
            }

            var repositoryPath = args[0];
            var packageSource = args[1];

            var packageRepository = PackageRepositoryFactory.Default.CreateRepository(packageSource);

            foreach (var file in Directory.EnumerateFiles(repositoryPath, "project.json", SearchOption.AllDirectories))
            {
                var projectJson = JObject.Parse(File.ReadAllText(file));
                var directoryName = Path.GetFileName(Path.GetDirectoryName(file));
                var latestPackage = packageRepository.FindPackage(directoryName);
                if (latestPackage != null)
                {
                    ((JValue)projectJson["version"]).Value = latestPackage.Version.ToNormalizedString();
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
                    latestPackage = packageRepository.FindPackage(dependency.Name);
                    if (latestPackage != null)
                    {
                        if (dependency.Value.Type == JTokenType.Object)
                        {
                            // "key": { "version": "1.0.0-*", "type": "build" }
                            var value = (JObject)dependency.Value;
                            value["version"] = latestPackage.Version.ToNormalizedString();
                        }
                        else
                        {
                            // "key": "version"
                            dependency.Value = latestPackage.Version.ToNormalizedString();
                        }
                    }
                }

                using (var fileWriter = new JsonTextWriter(new StreamWriter(file)))
                {
                    fileWriter.Formatting = Formatting.Indented;
                    fileWriter.Indentation = 4;
                    projectJson.WriteTo(fileWriter);
                }
            }
        }
    }
}
