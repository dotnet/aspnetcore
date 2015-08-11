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
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage <repository-path> <package source> <Korebuild Version>");
            }

            var repositoryPath = args[0];
            var packageSource = args[1];
            var korebuildVersion = args[2];

            var packageRepository = PackageRepositoryFactory.Default.CreateRepository(packageSource);

            // Pin project.json files
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

            // Pin the build scripts
            //TODO: handle build.sh files too
            var dnxPackageName = "dnx-clr-win-x86";
            var dnxPackage = packageRepository.FindPackage(dnxPackageName);
            if (dnxPackage == null)
            {
                throw new InvalidOperationException(
                    $"Could not find the DNX package with name '{dnxPackageName}' to " +
                    "pin the build script");
            }

            var buildCmdFiles = Directory.GetFiles(repositoryPath, "build.cmd", SearchOption.TopDirectoryOnly);
            if (buildCmdFiles == null || buildCmdFiles.Length == 0)
            {
                throw new InvalidOperationException($"No build.cmd files found at {repositoryPath}");
            }

            var buildCmdFile = buildCmdFiles[0];
            var buildCmdFileContent = File.ReadAllText(buildCmdFile);
            buildCmdFileContent = buildCmdFileContent.Replace(
                "SET BUILDCMD_KOREBUILD_VERSION=\"\"",
                $"SET BUILDCMD_KOREBUILD_VERSION={korebuildVersion}");
            buildCmdFileContent = buildCmdFileContent.Replace(
                "SET BUILDCMD_DNX_VERSION=\"\"",
                $"SET BUILDCMD_DNX_VERSION={dnxPackage.Version.ToNormalizedString()}");

            // Replace all content of the file
            File.WriteAllText(buildCmdFile, buildCmdFileContent);
        }
    }
}
