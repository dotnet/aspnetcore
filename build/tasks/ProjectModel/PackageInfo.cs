// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Versioning;

namespace RepoTasks.ProjectModel
{
    internal class PackageInfo
    {
        public PackageInfo(string id,
            NuGetVersion version,
            IReadOnlyList<PackageDependencyGroup> dependencyGroups,
            string source,
            string packageType = "Dependency")
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }

            Id = id;
            Version = version ?? throw new ArgumentNullException(nameof(version));
            PackageType = packageType;
            Source = source;
            DependencyGroups = dependencyGroups ?? Array.Empty<PackageDependencyGroup>();
        }

        public string Id { get; }
        public NuGetVersion Version { get; }
        public string PackageType { get; }
        /// <summary>
        /// Can be a https feed or a file path. May be null.
        /// </summary>
        public string Source { get; }
        public IReadOnlyList<PackageDependencyGroup> DependencyGroups { get; }

        public override string ToString() => $"{Id}/{Version}";
    }
}
