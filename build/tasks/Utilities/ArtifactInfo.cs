// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using NuGet.Frameworks;
using Microsoft.Build.Framework;
using RepoTasks.ProjectModel;

namespace RepoTasks.Utilities
{
    internal abstract class ArtifactInfo
    {
        public static ArtifactInfo Parse(ITaskItem item)
        {
            ArtifactInfo info;
            switch (item.GetMetadata("ArtifactType").ToLowerInvariant())
            {
                case "nugetpackage":
                    info = new Package { PackageInfo = GetPackageInfo(item) };
                    break;
                case "nugetsymbolspackage":
                    info = new Package { PackageInfo = GetPackageInfo(item), IsSymbolsArtifact = true };
                    break;
                default:
                    throw new InvalidDataException($"Unrecognized artifact type: {item.GetMetadata("ArtifactType")} for artifact {item.ItemSpec}");
            }

            info.RepositoryRoot = item.GetMetadata("RepositoryRoot")?.TrimEnd(new [] { '\\', '/' });

            if (!string.IsNullOrEmpty(info.RepositoryRoot))
            {
                info.RepoName = Path.GetFileName(info.RepositoryRoot);
            }

            return info;
        }

        public string RepositoryRoot { get; private set; }
        public string RepoName { get; private set; }

        public class Package : ArtifactInfo
        {
            public PackageInfo PackageInfo { get; set; }
            public bool IsSymbolsArtifact { get; set; }
        }

        private static PackageInfo GetPackageInfo(ITaskItem item)
        {
            return new PackageInfo(
                item.GetMetadata("PackageId"),
                item.GetMetadata("Version"),
                string.IsNullOrEmpty(item.GetMetadata("TargetFramework"))
                    ? MSBuildListSplitter.SplitItemList(item.GetMetadata("TargetFramework")).Select(s => NuGetFramework.Parse(s)).ToArray()
                    : new [] { NuGetFramework.Parse(item.GetMetadata("TargetFramework")) },
                Path.GetDirectoryName(item.ItemSpec),
                item.GetMetadata("PackageType"));
        }
    }
}
