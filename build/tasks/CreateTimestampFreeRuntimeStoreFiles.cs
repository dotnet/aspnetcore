// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Versioning;

namespace RepoTasks
{
    public class CreateTimestampFreeRuntimeStoreFiles : Task
    {
        [Required]
        public string VersionNumber { get; set; }

        [Required]
        public string RuntimeStoresDirectory { get; set; }

        [Output]
        public string OutputDirectory { get; set; }

        public override bool Execute()
        {
            var version = new NuGetVersion(VersionNumber);

            var currentReleaseLabel = $"-{version.Release}";
            var updatedReleaseLabel = Utilities.GetNoTimestampReleaseLabel(version.Release);

            if (!string.IsNullOrEmpty(updatedReleaseLabel))
            {
                updatedReleaseLabel = $"-{updatedReleaseLabel}";
            }

            foreach (var file in Directory.EnumerateFiles(RuntimeStoresDirectory, "*", SearchOption.AllDirectories))
            {
                var destinationFile = file.Replace(RuntimeStoresDirectory, OutputDirectory).Replace(currentReleaseLabel, updatedReleaseLabel);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                File.Copy(file, destinationFile);
            }

            foreach (var depsFile in Directory.EnumerateFiles(OutputDirectory, "*.deps.json", SearchOption.AllDirectories))
            {
                File.WriteAllText(depsFile, File.ReadAllText(depsFile).Replace(currentReleaseLabel, updatedReleaseLabel));
            }

            return true;
        }
    }
}
