// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace RepoTasks.ProjectModel
{
    internal class PackageInfo
    {
        public PackageInfo(string id,
            string version,
            IReadOnlyList<NuGetFramework> frameworks,
            string source,
            string packageType = "Dependency")
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }

            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentException(nameof(version));
            }

            Id = id;
            Version = NuGetVersion.Parse(version);
            Frameworks = frameworks;
            PackageType = packageType;
            Source = source;
        }

        public string Id { get; }
        public NuGetVersion Version { get; }
        public string PackageType { get; }
        /// <summary>
        /// Can be a https feed or a file path. May be null.
        /// </summary>
        public string Source { get; }
        public IReadOnlyList<NuGetFramework> Frameworks { get; }
    }
}
